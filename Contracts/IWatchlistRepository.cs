using BlueChips.Models.Trading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlueChips.Contracts
{
    public interface IWatchlistRepository {
        Task<IReadOnlyList<WatchlistEntry>> GetAllAsync(CancellationToken ct = default);
        Task AddAsync(WatchlistEntry entry, CancellationToken ct = default);
        Task RemoveAsync(string symbol, CancellationToken ct = default);
        Task ReorderAsync(IReadOnlyList<string> orderedSymbols, CancellationToken ct = default);
    }
}
