using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlueChips.Models.Reporting
{
    public class EquityPoint {
        public DateTimeOffset Ts { get; set; }
        public decimal Equity { get; set; }
    }
}
