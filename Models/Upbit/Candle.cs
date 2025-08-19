using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlueChips.Models.Upbit
{
    public class Candle {
        public string Market { get; set; } = "";                 // KRW-BTC
        public DateTimeOffset CandleTimeUtc { get; set; }        // 바 시각(UTC)
        public decimal OpenPrice { get; set; }
        public decimal HighPrice { get; set; }
        public decimal LowPrice { get; set; }
        public decimal ClosePrice { get; set; }                  // 종가
        public decimal Volume { get; set; }                      // 거래량
    }
}
