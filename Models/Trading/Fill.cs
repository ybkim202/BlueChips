using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlueChips.Models.Trading {
    public class Fill {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public Guid OrderId { get; set; }
        public string Market { get; set; } = "";
        public OrderSide Side { get; set; }
        public DateTimeOffset Ts { get; set; }
        public decimal Price { get; set; }
        public decimal Quantity { get; set; }
        public decimal Fee { get; set; }
    }
}