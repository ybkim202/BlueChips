using BlueChips.Contracts;
using BlueChips.Models.Trading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlueChips.Services.Trading
{
    public sealed class SimExecutionEngine : ISimExecutionEngine {
        private readonly ITradeRepository _repo;

        public event Action<Order>? OrderUpdated;
        public event Action<Fill>? FillCreated;

        public SimExecutionEngine(ITradeRepository repo) => _repo = repo;

        public async Task<Guid> SubmitAsync(Order order, CancellationToken ct = default) {
            await _repo.SaveOrderAsync(order, ct);
            OrderUpdated?.Invoke(order);
            // MVP: 체결 로직은 이후 PriceFeed/슬리피지/수수료와 연동
            return order.Id;
        }

        public async Task CancelAsync(Guid orderId, CancellationToken ct = default) {
            var o = await _repo.GetOrderAsync(orderId, ct);
            if (o is null) return;
            o.Status = OrderStatus.Canceled;
            await _repo.UpdateOrderAsync(o, ct);
            OrderUpdated?.Invoke(o);
        }
    }
}
