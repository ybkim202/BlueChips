using BlueChips.Models.Trading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlueChips.Contracts
{
    public interface IPortfolioService {
        Task<decimal> GetCashAsync(CancellationToken ct = default);
        Task<IReadOnlyList<Holding>> GetHoldingsAsync(CancellationToken ct = default);
        Task<PortfolioSnapshot> TakeSnapshotAsync(CancellationToken ct = default);

        Task<decimal> GetUnrealizedPnlAsync(string market, decimal? markPrice = null, CancellationToken ct = default);
        Task<decimal> GetRealizedPnlAsync(DateTimeOffset? from = null, DateTimeOffset? to = null, CancellationToken ct = default);
    }
}
