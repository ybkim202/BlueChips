using BlueChips.Models.Trading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlueChips.Models.Reporting
{
    public class TradeLogEntry {
        public DateTimeOffset Ts { get; set; }
        public string Market { get; set; } = "";
        public OrderSide Side { get; set; }
        public decimal Price { get; set; }
        public decimal Quantity { get; set; }
        public decimal Fee { get; set; }
        public string? Note { get; set; }
    }
}
