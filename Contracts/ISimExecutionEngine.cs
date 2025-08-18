using BlueChips.Models.Trading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlueChips.Contracts
{
    public interface ISimExecutionEngine {
        Task<Guid> SubmitAsync(Order order, CancellationToken ct = default);
        Task CancelAsync(Guid orderId, CancellationToken ct = default);

        event Action<Order>? OrderUpdated;
        event Action<Fill>? FillCreated;
    }
}
