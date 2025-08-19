using BlueChips.Models.Reporting;
using BlueChips.Models.Trading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlueChips.Models.AutoTrade
{
    public class BacktestResult {
        public DateTimeOffset Start { get; set; }
        public DateTimeOffset End { get; set; }
        public List<EquityPoint> EquityCurve { get; set; } = new();
        public List<TradeLogEntry> Trades { get; set; } = new();
        public List<Fill> Fills { get; set; } = new();

        // 요약 지표(간단 버전)
        public decimal TotalReturnPct { get; set; }
        public decimal MaxDrawdownPct { get; set; }
        public int TradeCount { get; set; }
    }
}
