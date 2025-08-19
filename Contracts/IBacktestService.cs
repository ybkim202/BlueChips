using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using BlueChips.Models.AutoTrade;   // Strategy, BacktestResult
using BlueChips.Models.Upbit;       // Candle

namespace BlueChips.Contracts {
    /// <summary>
    /// 전략 백테스트 서비스의 계약.
    /// - RunAsync: 기간/마켓/타임프레임으로 캔들을 가져와 백테스트 수행
    /// - RunOnCandlesAsync: 외부에서 제공한 캔들로 백테스트 수행
    /// </summary>
    public interface IBacktestService {
        /// <summary>
        /// Upbit REST에서 캔들을 조달해 백테스트 수행.
        /// </summary>
        Task<BacktestResult> RunAsync(
            Strategy strategy,
            string market,
            string timeframe,                  // 예: "m1","m5","m15","h1","d1"
            DateTimeOffset from,
            DateTimeOffset to,
            CancellationToken ct = default);

        /// <summary>
        /// 외부에서 제공된 캔들 시퀀스로 백테스트 수행.
        /// </summary>
        Task<BacktestResult> RunOnCandlesAsync(
            Strategy strategy,
            IReadOnlyList<Candle> candles,
            CancellationToken ct = default);
    }
}
