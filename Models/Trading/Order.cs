using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlueChips.Models.Trading
{
    public class Order {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTimeOffset Ts { get; set; } = DateTimeOffset.UtcNow;
        public string Market { get; set; } = "";          // KRW-BTC
        public OrderSide Side { get; set; }
        public OrderType Type { get; set; }
        public TimeInForce TimeInForce { get; set; } = TimeInForce.GTC;

        public decimal? Price { get; set; }               // Limit/Stop 기준가
        public decimal? StopPrice { get; set; }           // Stop 트리거
        public decimal Quantity { get; set; }
        public decimal FilledQty { get; set; }
        public OrderStatus Status { get; set; } = OrderStatus.New;
        public string? Note { get; set; }
    }
}
