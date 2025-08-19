using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using BlueChips.Contracts;
using BlueChips.Models.AutoTrade;
using BlueChips.Models.Upbit;
using BlueChips.Models.Trading;

namespace BlueChips.Services.AutoTrade {
    /// <summary>
    /// 전략 백테스트 서비스 기본 구현.
    /// - Upbit REST를 통해 캔들을 페이지로 수집
    /// - Strategy 정의에 따라 주문/체결을 가정(로컬 시뮬)하여 성과를 산출
    /// - 수수료/슬리피지는 IFeePolicy/ISlippageModel로 주입
    /// </summary>
    public sealed class BacktestService : IBacktestService {
        private readonly IUpbitRestService _rest;
        private readonly IFeePolicy _fee;
        private readonly ISlippageModel _slippage;

        public BacktestService(IUpbitRestService rest, IFeePolicy fee, ISlippageModel slippage) {
            _rest = rest;
            _fee = fee;
            _slippage = slippage;
        }

        public async Task<BacktestResult> RunAsync(
            Strategy strategy,
            string market,
            string timeframe,
            DateTimeOffset from,
            DateTimeOffset to,
            CancellationToken ct = default) {
            if (from >= to) throw new ArgumentException("'from' must be earlier than 'to'.");

            var candles = await FetchCandlesAsync(market, timeframe, from, to, ct);
            return await RunOnCandlesAsync(strategy, candles, ct);
        }

        public Task<BacktestResult> RunOnCandlesAsync(
            Strategy strategy,
            IReadOnlyList<Candle> candles,
            CancellationToken ct = default) {
            if (candles == null || candles.Count == 0)
                throw new ArgumentException("candles must not be empty.", nameof(candles));

            // ===== 1) 초기 상태 =====
            // 현금/보유/포지션/주문/체결 등 시뮬레이션 상태 준비
            decimal cash = GetInitialCash(strategy) /* 예: 파라미터 또는 기본값 */;   // TODO: 전략 파라미터에서 초기현금 읽기
            decimal positionQty = 0m;
            decimal avgPrice = 0m;
            var fills = new List<BlueChips.Models.Trading.Fill>();
            var equityCurve = new List<BlueChips.Models.Reporting.EquityPoint>();
            var tradeLog = new List<BlueChips.Models.Reporting.TradeLogEntry>();

            // ===== 2) 전략 신호 계산 =====
            // 여기서는 "골격"만 제공. 실제 신호(SMA, RSI 등)는 TODO 영역에 채워 넣으세요.
            // 예) 간단: 'BuyAndHold'면 첫 캔들 종가에 전량 매수, 마지막 캔들 종가에 전량 매도

            // TODO: 전략 파싱 (strategy.Name / strategy.Parameters)
            var mode = (strategy?.Name ?? "BuyAndHold").Trim();

            // ===== 3) 캔들 루프 =====
            foreach (var c in candles.OrderBy(c => c.CandleTimeUtc)) {
                ct.ThrowIfCancellationRequested();

                // 3-1) 전략 신호 산출
                // TODO: 예시) SMA 크로스/모멘텀 등 신호 계산

                // 3-2) 체결 시뮬레이션 (슬리피지/수수료 반영)
                // 여기선 Buy&Hold 예시(최소 동작 보장)
                if (mode.Equals("BuyAndHold", StringComparison.OrdinalIgnoreCase)) {
                    // 첫 바에서 전량 매수, 마지막 바에서 전량 매도
                    if (ReferenceEquals(c, candles.First())) {
                        var price = ApplySlippage("KRW-" + c.Market ?? "", OrderSide.Buy, c.ClosePrice, c);
                        var qty = cash > 0m && price > 0m ? cash / price : 0m;

                        if (qty > 0m) {
                            var fee = _fee.CalcFee("KRW-" + c.Market ?? "", OrderSide.Buy, price, qty);
                            cash -= price * qty + fee;
                            positionQty += qty;
                            avgPrice = price;

                            fills.Add(NewFill(Guid.Empty, "KRW-" + c.Market ?? "", OrderSide.Buy, c.CandleTimeUtc, price, qty, fee));
                            tradeLog.Add(NewTradeLog("BUY", c.CandleTimeUtc, price, qty, fee, "BuyAndHold Entry"));
                        }
                    }
                    else if (ReferenceEquals(c, candles.Last()) && positionQty > 0m) {
                        var price = ApplySlippage("KRW-" + c.Market ?? "", OrderSide.Sell, c.ClosePrice, c);
                        var qty = positionQty;

                        var fee = _fee.CalcFee("KRW-" + c.Market ?? "", OrderSide.Sell, price, qty);
                        cash += price * qty - fee;
                        positionQty = 0m;

                        fills.Add(NewFill(Guid.Empty, "KRW-" + c.Market ?? "", OrderSide.Sell, c.CandleTimeUtc, price, qty, fee));
                        tradeLog.Add(NewTradeLog("SELL", c.CandleTimeUtc, price, qty, fee, "BuyAndHold Exit"));
                    }
                }

                // 3-3) 에쿼티 곡선 기록 (미실현 포함)
                var markPrice = c.ClosePrice;
                var equity = cash + positionQty * markPrice;
                equityCurve.Add(NewEquityPoint(c.CandleTimeUtc, equity));
            }

            // ===== 4) 결과 집계 =====
            var result = new BacktestResult();
            // TODO: BacktestResult의 실제 속성 구조에 맞춰 아래를 설정하세요.
            // 예시:
            // result.Start = candles.First().CandleTimeUtc;
            // result.End   = candles.Last().CandleTimeUtc;
            // result.EquityCurve = equityCurve;
            // result.Trades = tradeLog;
            // result.Fills  = fills;
            // result.Metrics = ComputeMetrics(equityCurve, fills, initialCash: GetInitialCash(strategy));

            return Task.FromResult(result);
        }

        #region 캔들 수집

        private async Task<IReadOnlyList<Candle>> FetchCandlesAsync(
            string market,
            string timeframe,
            DateTimeOffset from,
            DateTimeOffset to,
            CancellationToken ct) {
            // Upbit REST 제약: 한 번에 최대 200개. -> to 기준 역방향 페이징 수집
            // timeframe 파싱: m1,m3,m5,m10,m15,m30,m60,m240,h1=60,d1
            var (kind, unit) = ParseTimeframe(timeframe);

            var all = new List<Candle>(1024);
            var cursor = to;

            while (cursor > from) {
                ct.ThrowIfCancellationRequested();

                IReadOnlyList<Candle> page = kind switch
                {
                    "m" => await _rest.GetMinuteCandlesAsync(market, unit, count: 200, to: cursor, ct),
                    "d" => await _rest.GetDayCandlesAsync(market, count: 200, to: cursor, ct),
                    _ => throw new NotSupportedException($"Unsupported timeframe: {timeframe}")
                };

                if (page.Count == 0) break;

                // 시간 역순(최신→과거)으로 온다면 정렬/필터
                foreach (var c in page.OrderBy(c => c.CandleTimeUtc)) {
                    if (c.CandleTimeUtc >= from && c.CandleTimeUtc <= to)
                        all.Add(c);
                }

                // 다음 페이지 기준시간 갱신 (가장 오래된 바의 시각보다 1틱 이전)
                var oldest = page.Min(c => c.CandleTimeUtc);
                if (oldest <= from) break;
                cursor = oldest.AddTicks(-1);
            }

            // 최종 정렬
            all.Sort((a, b) => a.CandleTimeUtc.CompareTo(b.CandleTimeUtc));
            return all;
        }

        private static (string kind, int unit) ParseTimeframe(string tf) {
            if (string.IsNullOrWhiteSpace(tf)) throw new ArgumentNullException(nameof(tf));
            tf = tf.Trim().ToLowerInvariant();

            if (tf.StartsWith("m")) {
                // m1, m3, m5, m10, m15, m30, m60, m240
                var num = int.Parse(tf.Substring(1));
                return ("m", num);
            }
            if (tf == "h1" || tf == "1h") {
                return ("m", 60);
            }
            if (tf == "d1" || tf == "1d" || tf == "d") {
                return ("d", 1);
            }

            throw new NotSupportedException($"Unknown timeframe: {tf}");
        }

        #endregion

        #region 유틸 (슬리피지/수수료/로그 객체 생성)

        private static decimal GetInitialCash(Strategy? strategy) {
            // TODO: Strategy.Parameters에서 "InitialCash" 키를 읽어오도록 구현
            // 미지정 시 기본 10,000,000 KRW
            return 10_000_000m;
        }

        private decimal ApplySlippage(string market, OrderSide side, decimal intendedPrice, Candle bar) {
            var last = bar.ClosePrice;
            var px = _slippage.AdjustPrice(market, side, intendedPrice, last, /*orderbook*/ null);
            return px;
        }

        private static BlueChips.Models.Trading.Fill NewFill(
            Guid orderId, string market, OrderSide side, DateTimeOffset ts, decimal price, decimal qty, decimal fee)
            => new()
            {
                OrderId = orderId,
                Market = market,
                Side = side,
                Ts = ts,
                Price = price,
                Quantity = qty,
                Fee = fee
            };

        private static BlueChips.Models.Reporting.TradeLogEntry NewTradeLog(
            string side, DateTimeOffset ts, decimal price, decimal qty, decimal fee, string note)
            => new()
            {
                // TODO: TradeLogEntry 필드명에 맞춰 설정
                // 예시:
                // Timestamp = ts,
                // Side = side,
                // Price = price,
                // Quantity = qty,
                // Fee = fee,
                // Note = note
            };

        private static BlueChips.Models.Reporting.EquityPoint NewEquityPoint(DateTimeOffset ts, decimal equity)
            => new()
            {
                // TODO: EquityPoint 필드명에 맞춰 설정
                // 예시:
                // Ts = ts,
                // Equity = equity
            };

        #endregion
    }
}
