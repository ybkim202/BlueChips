using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlueChips.Contracts
{
    public interface ISettingsRepository {
        Task SetAsync<T>(string key, T value, CancellationToken ct = default);
        Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
        Task RemoveAsync(string key, CancellationToken ct = default);
        Task<IReadOnlyDictionary<string, string>> GetAllAsync(CancellationToken ct = default);
    }
}
