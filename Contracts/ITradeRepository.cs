using BlueChips.Models.Trading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlueChips.Contracts
{
    public interface ITradeRepository {
        // Orders
        Task SaveOrderAsync(Order o, CancellationToken ct = default);
        Task UpdateOrderAsync(Order o, CancellationToken ct = default);
        Task<Order?> GetOrderAsync(Guid id, CancellationToken ct = default);
        Task<IReadOnlyList<Order>> QueryOrdersAsync(
            string market, DateTimeOffset? from = null, DateTimeOffset? to = null,
            OrderStatus? status = null, CancellationToken ct = default);

        // Fills
        Task SaveFillAsync(Fill f, CancellationToken ct = default);
        Task<IReadOnlyList<Fill>> QueryFillsAsync(
            string market, DateTimeOffset? from = null, DateTimeOffset? to = null, CancellationToken ct = default);

        // Holdings
        Task UpsertHoldingAsync(Holding h, CancellationToken ct = default);
        Task<IReadOnlyList<Holding>> GetHoldingsAsync(CancellationToken ct = default);

        // Utility (트랜잭션 경계 실행)
        Task RunInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken ct = default);
    }
}
