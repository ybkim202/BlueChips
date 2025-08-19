// Services/Reporting/PngChartExporter.cs
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BlueChips.Contracts;
using BlueChips.Models.Reporting;

namespace BlueChips.Services.Reporting {
    public sealed class PngChartExporter : IReportExporter {
        public string Name => "PNG";
        public string FileExtension => ".png";

        public Task<string> ExportAsync(
            IEnumerable<TradeLogEntry> trades,
            IEnumerable<EquityPoint> equityCurve,
            IEnumerable<ReturnByAsset> breakdown,
            string destinationPath,
            CancellationToken ct = default) {
            var path = Path.ChangeExtension(destinationPath, ".png");
            var points = equityCurve?.OrderBy(x => x.Ts).ToList() ?? new List<EquityPoint>();

            const int W = 1000;
            const int H = 500;
            const int PAD = 40;

            var dv = new DrawingVisual();
            using (var dc = dv.RenderOpen()) {
                // 배경
                dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, W, H));

                if (points.Count >= 2) {
                    var min = points.Min(p => p.Equity);
                    var max = points.Max(p => p.Equity);
                    if (max <= min) max = min + 1;

                    // 축(간단)
                    var axisPen = new Pen(Brushes.Gray, 1);
                    dc.DrawLine(axisPen, new Point(PAD, H - PAD), new Point(W - PAD, H - PAD)); // X축
                    dc.DrawLine(axisPen, new Point(PAD, PAD), new Point(PAD, H - PAD));     // Y축

                    // 에쿼티 폴리라인
                    var geo = new StreamGeometry();
                    using (var ctx = geo.Open()) {
                        for (int i = 0; i < points.Count; i++) {
                            double x = PAD + i * (W - 2.0 * PAD) / (points.Count - 1);
                            double y = H - PAD - (double)((points[i].Equity - min) / (max - min)) * (H - 2.0 * PAD);

                            if (i == 0) ctx.BeginFigure(new Point(x, y), false, false);
                            else ctx.LineTo(new Point(x, y), true, false);
                        }
                    }
                    geo.Freeze();
                    dc.DrawGeometry(null, new Pen(Brushes.Black, 2), geo);
                }
            }

            var rtb = new RenderTargetBitmap(W, H, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(dv);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(rtb));

            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write))
                encoder.Save(fs);

            return Task.FromResult(path);
        }
    }
}
