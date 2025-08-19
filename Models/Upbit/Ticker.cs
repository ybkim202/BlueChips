using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlueChips.Models.Upbit
{
    public class Ticker {
        public string Market { get; set; } = "";
        public decimal TradePrice { get; set; }                 // 현재가(종가)
        public decimal ChangeRate24h { get; set; }              // 24h 등락률(비율)
        public decimal AccVolume24h { get; set; }               // 24h 거래량
        public decimal AccTradePrice24h { get; set; }           // 24h 거래대금
        public DateTimeOffset TimestampUtc { get; set; }
    }
}
