using BlueChips.Contracts;
using BlueChips.Models.Trading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlueChips.Services.Trading
{
    public sealed class PortfolioService : IPortfolioService {
        private readonly ITradeRepository _repo;
        private readonly IPriceFeed _feed;

        public PortfolioService(ITradeRepository repo, IPriceFeed feed) {
            _repo = repo;
            _feed = feed;
        }

        public async Task<decimal> GetCashAsync(CancellationToken ct = default) {
            var h = await _repo.GetHoldingsAsync(ct);
            var krw = h.FirstOrDefault(x => x.Asset == "KRW");
            return krw?.Free ?? 0m;
        }

        public Task<IReadOnlyList<Holding>> GetHoldingsAsync(CancellationToken ct = default)
            => _repo.GetHoldingsAsync(ct);

        public async Task<PortfolioSnapshot> TakeSnapshotAsync(CancellationToken ct = default) {
            var h = await _repo.GetHoldingsAsync(ct);
            decimal cash = h.FirstOrDefault(x => x.Asset == "KRW")?.Free ?? 0m;

            decimal equity = cash;
            foreach (var a in h.Where(x => x.Asset != "KRW")) {
                var price = _feed.GetLastPrice($"KRW-{a.Asset}") ?? 0m;
                equity += a.Free * price;
            }

            return new PortfolioSnapshot
            {
                Ts = DateTimeOffset.UtcNow,
                Cash = cash,
                Equity = equity,
                UnrealizedPnl = 0m, // TODO: 평균단가 기준 계산
                RealizedPnl = 0m
            };
        }

        public Task<decimal> GetUnrealizedPnlAsync(string market, decimal? markPrice = null, CancellationToken ct = default)
            => Task.FromResult(0m); // TODO

        public Task<decimal> GetRealizedPnlAsync(DateTimeOffset? from = null, DateTimeOffset? to = null, CancellationToken ct = default)
            => Task.FromResult(0m); // TODO
    }
}
