using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlueChips.Models.Trading
{
    public class Holding {
        public string Asset { get; set; } = "";     // KRW, BTC …
        public decimal Free { get; set; }
        public decimal Locked { get; set; }
    }
}
