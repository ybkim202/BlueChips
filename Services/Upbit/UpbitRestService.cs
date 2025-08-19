// Services/Upbit/UpbitRestService.Mapping.cs (예시 스니펫)
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using BlueChips.Contracts;
using BlueChips.Models.Upbit;

namespace BlueChips.Services.Upbit {
    internal sealed partial class UpbitRestService : IUpbitRestService {


        /** Member Variables **/
        private readonly HttpClient _http;
        private static readonly JsonSerializerOptions _json = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        public UpbitRestService(HttpClient http) => _http = http;


        /** Member Methods **/

        /* Markets */
        public async Task<IReadOnlyList<Market>> GetMarketsAsync(bool includeDetails = false, CancellationToken ct = default) {
            var url = $"/v1/market/all?isDetails={(includeDetails ? "true" : "false")}";
            using var res = await _http.GetAsync(url, ct);
            res.EnsureSuccessStatusCode();

            var dto = await JsonSerializer.DeserializeAsync<List<_MarketDto>>(await res.Content.ReadAsStreamAsync(ct), _json, ct)
                      ?? new();
            return dto.Select(d => new Market
            {
                Symbol = d.market ?? "",
                KoreanName = d.korean_name ?? "",
                EnglishName = d.english_name ?? ""
            }).ToList();
        }


        /* Tickers */
        public async Task<IReadOnlyList<Ticker>> GetTickersAsync(IEnumerable<string> markets, CancellationToken ct = default) {
            var m = string.Join(",", markets);
            using var res = await _http.GetAsync($"/v1/ticker?markets={m}", ct);
            res.EnsureSuccessStatusCode();

            var dto = await JsonSerializer.DeserializeAsync<List<_TickerDto>>(await res.Content.ReadAsStreamAsync(ct), _json, ct)
                      ?? new();
            return dto.Select(d => new Ticker
            {
                Market = d.market ?? "",
                TradePrice = d.trade_price,
                AccVolume24h = d.acc_trade_volume_24h,
                AccTradePrice24h = d.acc_trade_price_24h,
                TimestampUtc = DateTimeOffset.FromUnixTimeMilliseconds(d.timestamp)
            }).ToList();
        }


        /* Orderbook */
        public async Task<Orderbook> GetOrderbookAsync(string market, CancellationToken ct = default) {
            using var res = await _http.GetAsync($"/v1/orderbook?markets={market}", ct);
            res.EnsureSuccessStatusCode();

            var arr = await JsonSerializer.DeserializeAsync<List<_OrderbookDto>>(await res.Content.ReadAsStreamAsync(ct), _json, ct)
                      ?? new();
            var d = arr.First();

            var ob = new Orderbook
            {
                Market = d.market ?? "",
                TimestampUtc = DateTimeOffset.FromUnixTimeMilliseconds(d.timestamp),
            };
            foreach (var u in d.orderbook_units ?? Enumerable.Empty<_OrderbookUnit>()) {
                ob.Bids.Add(new OrderbookLevel { Price = u.bid_price, Size = u.bid_size });
                ob.Asks.Add(new OrderbookLevel { Price = u.ask_price, Size = u.ask_size });
            }
            return ob;
        }


        /* Trades */
        public async Task<IReadOnlyList<TradeTick>> GetRecentTradesAsync(string market, int count = 200, DateTimeOffset? to = null, CancellationToken ct = default) {
            var url = $"/v1/trades/ticks?market={market}&count={count}";
            if (to.HasValue) url += $"&to={to.Value.UtcDateTime:yyyy-MM-dd HH:mm:ss}";
            using var res = await _http.GetAsync(url, ct);
            res.EnsureSuccessStatusCode();

            var dto = await JsonSerializer.DeserializeAsync<List<_TradeDto>>(await res.Content.ReadAsStreamAsync(ct), _json, ct)
                      ?? new();
            return dto.Select(d => new TradeTick
            {
                Market = d.market ?? market,
                TimestampUtc = DateTimeOffset.FromUnixTimeMilliseconds(d.trade_timestamp),
                Price = d.trade_price,
                Volume = d.trade_volume,
                AskBid = d.ask_bid ?? ""
            }).ToList();
        }


        /* Candles */
        public async Task<IReadOnlyList<Candle>> GetMinuteCandlesAsync(string market, int unit, int count = 200, DateTimeOffset? to = null, CancellationToken ct = default) {
            var url = $"/v1/candles/minutes/{unit}?market={market}&count={count}";
            if (to.HasValue) url += $"&to={to.Value.UtcDateTime:yyyy-MM-dd HH:mm:ss}";
            return await FetchCandlesAsync(url, ct);
        }

        public async Task<IReadOnlyList<Candle>> GetDayCandlesAsync(string market, int count = 200, DateTimeOffset? to = null, CancellationToken ct = default) {
            var url = $"/v1/candles/days?market={market}&count={count}";
            if (to.HasValue) url += $"&to={to.Value.UtcDateTime:yyyy-MM-dd}";
            return await FetchCandlesAsync(url, ct);
        }

        private async Task<IReadOnlyList<Candle>> FetchCandlesAsync(string url, CancellationToken ct) {
            using var res = await _http.GetAsync(url, ct);
            res.EnsureSuccessStatusCode();

            var dto = await JsonSerializer.DeserializeAsync<List<_CandleDto>>(await res.Content.ReadAsStreamAsync(ct), _json, ct)
                      ?? new();

            // Upbit는 최신→과거 순으로 반환 → 우리가 사용하는 모델은 과거→최신 정렬
            var mapped = dto.Select(d => new Candle
            {
                Market = d.market ?? "",
                CandleTimeUtc = DateTimeOffset.Parse(d.candle_date_time_utc + "Z"),
                OpenPrice = d.opening_price,
                HighPrice = d.high_price,
                LowPrice = d.low_price,
                ClosePrice = d.trade_price,
                Volume = d.candle_acc_trade_volume
            })
            .OrderBy(c => c.CandleTimeUtc)
            .ToList();

            return mapped;
        }


        private sealed class _MarketDto { public string? market { get; set; } public string? korean_name { get; set; } public string? english_name { get; set; } }
        private sealed class _TickerDto { public string? market { get; set; } public decimal trade_price { get; set; } public decimal acc_trade_volume_24h { get; set; } public decimal acc_trade_price_24h { get; set; } public long timestamp { get; set; } }
        private sealed class _OrderbookDto { public string? market { get; set; } public long timestamp { get; set; } public List<_OrderbookUnit>? orderbook_units { get; set; } }
        private sealed class _OrderbookUnit { public decimal ask_price { get; set; } public decimal bid_price { get; set; } public decimal ask_size { get; set; } public decimal bid_size { get; set; } }
        private sealed class _TradeDto { public string? market { get; set; } public long trade_timestamp { get; set; } public decimal trade_price { get; set; } public decimal trade_volume { get; set; } public string? ask_bid { get; set; } }
        private sealed class _CandleDto { public string? market { get; set; } public string candle_date_time_utc { get; set; } = ""; public decimal opening_price { get; set; } public decimal high_price { get; set; } public decimal low_price { get; set; } public decimal trade_price { get; set; } public decimal candle_acc_trade_volume { get; set; } }

    }
}
