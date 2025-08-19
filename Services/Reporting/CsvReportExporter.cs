using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BlueChips.Contracts;
using BlueChips.Models.Reporting;

namespace BlueChips.Services.Reporting {
    public sealed class CsvReportExporter : IReportExporter {
        public string Name => "CSV";
        public string FileExtension => ".csv";

        public async Task<string> ExportAsync(IEnumerable<TradeLogEntry> trades, IEnumerable<EquityPoint> equityCurve, IEnumerable<ReturnByAsset> breakdown, string destinationPath, CancellationToken ct = default) {
            // MVP: 트레이드 로그만 CSV로 저장
            var path = Path.ChangeExtension(destinationPath, ".csv");
            var sb = new StringBuilder();
            sb.AppendLine("Timestamp,Market,Side,Price,Quantity,Fee,Note");

            var ci = CultureInfo.InvariantCulture;
            foreach (var t in trades)
                sb.AppendLine($"{t.Ts:O},{t.Market},{t.Side},{t.Price.ToString(ci)},{t.Quantity.ToString(ci)},{t.Fee.ToString(ci)},{t.Note}");

            await File.WriteAllTextAsync(path, sb.ToString(), ct);
            return path;
        }
    }
}
