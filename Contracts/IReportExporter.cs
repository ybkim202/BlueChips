using BlueChips.Models.Reporting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlueChips.Contracts
{
    public interface IReportExporter {
        string Name { get; }          // 예: "CSV", "PNG"
        string FileExtension { get; } // 예: ".csv", ".png"

        Task<string> ExportAsync(
            IEnumerable<TradeLogEntry> trades,
            IEnumerable<EquityPoint> equityCurve,
            IEnumerable<ReturnByAsset> breakdown,
            string destinationPath,
            CancellationToken ct = default);
    }
}
