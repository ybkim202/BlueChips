using BlueChips.Contracts;
using BlueChips.Models.Trading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlueChips.Services.Trading
{
    public sealed class FixedRateFeePolicy : IFeePolicy {
        private readonly Services.SettingsProvider _settings;
        public FixedRateFeePolicy(Services.SettingsProvider settings) => _settings = settings;

        public decimal CalcFee(string market, OrderSide side, decimal price, decimal quantity) {
            var rate = _settings.Get().Trading?.TakerFeeRate ?? 0.0005m; // 0.05%
            return price * quantity * rate;
        }
    }
}
