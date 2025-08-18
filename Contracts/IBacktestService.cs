using BlueChips.Enums;
using BlueChips.Models.AutoTrade;
using BlueChips.Services.AutoTrade;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlueChips.Contracts
{
    public interface IBacktestService {

        /** Methods **/
        public interface IBacktestService {
            Task<BacktestResult> RunAsync(
                Strategy strategy,
                string market,
                string timeframe,
                DateTimeOffset from,
                DateTimeOffset to,
                CancellationToken ct = default);
        }
    }
