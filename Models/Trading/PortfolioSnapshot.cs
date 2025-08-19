using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlueChips.Models.Trading
{
    public class PortfolioSnapshot {
        public DateTimeOffset Ts { get; set; }
        public decimal Equity { get; set; }
        public decimal Cash { get; set; }
        public decimal UnrealizedPnl { get; set; }
        public decimal RealizedPnl { get; set; }
    }
}
