using BlueChips.Models.Trading;
using BlueChips.Models.Upbit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlueChips.Contracts
{
    public interface ISlippageModel {
        decimal AdjustPrice(string market,
                            OrderSide side,
                            decimal intendedPrice,
                            decimal lastPrice,
                            Orderbook? orderbook = null);
    }
}
