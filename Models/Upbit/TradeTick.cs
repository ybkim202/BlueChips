using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlueChips.Models.Upbit
{
    public class TradeTick {
        public string Market { get; set; } = "";
        public DateTimeOffset TimestampUtc { get; set; }
        public decimal Price { get; set; }
        public decimal Volume { get; set; }
        public string AskBid { get; set; } = "";   // "ASK" or "BID"
    }
}
