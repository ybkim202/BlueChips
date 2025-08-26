using BlueChips.Models.Upbit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlueChips.Contracts
{
    public interface IPriceFeed {

        /** Methods **/
        Task StartAsync(CancellationToken ct = default);
        Task StopAsync();
        decimal? GetLastPrice(string market);
        Orderbook? GetOrderbookSnapshot(string market);
        IReadOnlyList<TradeTick> GetRecentTrades(string market, int take = 50);

        /** Events **/
        event Action<string, Ticker>? TickerUpdated;        // (market, ticker)
        event Action<string, Orderbook>? OrderbookUpdated;  // (market, orderbook)
        event Action<string, TradeTick>? TradeTicked;       // (market, trade)
    }
}
