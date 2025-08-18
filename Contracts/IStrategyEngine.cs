using BlueChips.Models.AutoTrade;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlueChips.Contracts
{
    public interface IStrategyEngine {
        Task<string> SaveAsync(Strategy strategy, CancellationToken ct = default); // returns Id
        Task<Strategy?> GetAsync(string id, CancellationToken ct = default);
        Task<IReadOnlyList<Strategy>> ListAsync(CancellationToken ct = default);

        Task<bool> StartAsync(string id, CancellationToken ct = default);
        Task<bool> StopAsync(string id, CancellationToken ct = default);
        Task<bool> DeleteAsync(string id, CancellationToken ct = default);

        event Action<string, string>? StatusChanged; // (strategyId, newState)
    }
}
