using BlueChips.Models.Upbit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlueChips.Contracts
{
    public interface IUpbitRestService {
        /** Methods **/
        Task<IReadOnlyList<Market>> GetMarketsAsync(bool includeDetails = false, CancellationToken ct = default);
        Task<IReadOnlyList<Ticker>> GetTickersAsync(IEnumerable<string> markets, CancellationToken ct = default);
        Task<Orderbook> GetOrderbookAsync(string market, CancellationToken ct = default);
        Task<IReadOnlyList<TradeTick>> GetRecentTradesAsync(string market, int count = 200, DateTimeOffset? to = null, CancellationToken ct = default);
        Task<IReadOnlyList<Candle>> GetMinuteCandlesAsync(string market, int unit, int count = 200, DateTimeOffset? to = null, CancellationToken ct = default);
        Task<IReadOnlyList<Candle>> GetDayCandlesAsync(string market, int count = 200, DateTimeOffset? to = null, CancellationToken ct = default);
    }
}
