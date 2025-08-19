using BlueChips.Contracts;
using BlueChips.Models.Upbit;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlueChips.Services.Upbit
{
    public sealed class PriceFeed : IPriceFeed {
        private readonly IUpbitRestService _rest;
        private readonly Services.SettingsProvider _settings;
        private CancellationTokenSource? _cts;
        private Task? _runner;

        private readonly ConcurrentDictionary<string, Ticker> _tickers = new();
        private readonly ConcurrentDictionary<string, Orderbook> _orderbooks = new();
        private readonly ConcurrentDictionary<string, List<TradeTick>> _trades = new();

        public event Action<string, Ticker>? TickerUpdated;
        public event Action<string, Orderbook>? OrderbookUpdated;
        public event Action<string, TradeTick>? TradeTicked;

        public PriceFeed(IUpbitRestService rest, Services.SettingsProvider settings) {
            _rest = rest;
            _settings = settings;
        }

        public Task StartAsync(CancellationToken ct = default) {
            if (_runner != null) return Task.CompletedTask;
            _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            _runner = Task.Run(async () =>
            {
                var pollMs = _settings.Get().Feed?.PollIntervalMs ?? 1000;
                var markets = await _rest.GetMarketsAsync(false, _cts.Token);
                var top = markets.Select(m => m.Symbol).Take(20).ToArray(); // MVP: 상위 20개만

                while (!_cts.IsCancellationRequested) {
                    try {
                        var tks = await _rest.GetTickersAsync(top, _cts.Token);
                        foreach (var t in tks) {
                            _tickers[t.Market] = t;
                            TickerUpdated?.Invoke(t.Market, t);
                        }
                    }
                    catch { /* TODO: 로깅 */ }

                    await Task.Delay(pollMs, _cts.Token);
                }
            }, _cts.Token);
            return Task.CompletedTask;
        }

        public async Task StopAsync() {
            _cts?.Cancel();
            if (_runner != null) try { await _runner; } catch { }
            _runner = null;
            _cts = null;
        }

        public decimal? GetLastPrice(string market)
            => _tickers.TryGetValue(market, out var t) ? t.TradePrice : null;

        public Orderbook? GetOrderbookSnapshot(string market)
            => _orderbooks.TryGetValue(market, out var ob) ? ob : null;

        public IReadOnlyList<TradeTick> GetRecentTrades(string market, int take = 50)
            => _trades.TryGetValue(market, out var list) ? list.TakeLast(take).ToList() : Array.Empty<TradeTick>();
    }
}
