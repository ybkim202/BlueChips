using BlueChips.Contracts;
using BlueChips.Models.Trading;
using BlueChips.Models.Upbit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlueChips.Services.Trading
{
    public sealed class NoSlippageModel : ISlippageModel {
        public decimal AdjustPrice(string market, OrderSide side, decimal intendedPrice, decimal lastPrice, Orderbook? orderbook = null)
            => intendedPrice; // 슬리피지 없음
    }
}
