using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlueChips.Contracts
{
    public interface IFeePolicy {
        /** Methods **/
        decimal CalcFee(string market, BlueChips.Models.Trading.OrderSide side, decimal price, decimal quantity);
    }
}
