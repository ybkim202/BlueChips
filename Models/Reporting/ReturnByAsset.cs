using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlueChips.Models.Reporting
{
    public class ReturnByAsset {
        public string Asset { get; set; } = "";
        public decimal ReturnPct { get; set; }     // 0.1234 == +12.34%
        public decimal Pnl { get; set; }
    }
}
