using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlueChips.Models.Upbit
{
    public class Orderbook {
        public string Market { get; set; } = "";
        public DateTimeOffset TimestampUtc { get; set; }
        public List<OrderbookLevel> Bids { get; set; } = new();
        public List<OrderbookLevel> Asks { get; set; } = new();
    }

    public class OrderbookLevel {
        public decimal Price { get; set; }
        public decimal Size { get; set; }
    }
}
