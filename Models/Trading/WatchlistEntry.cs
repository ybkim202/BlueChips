using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlueChips.Models.Trading
{
    public class WatchlistEntry {
        public string Symbol { get; set; } = "";   // KRW-BTC
        public int Sort { get; set; } = 0;
        public string? Note { get; set; }
    }
}
