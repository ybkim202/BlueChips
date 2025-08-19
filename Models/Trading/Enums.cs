using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlueChips.Models.Trading
{
    public enum OrderSide { Buy, Sell }
    public enum OrderType { Market, Limit, Stop, StopLimit }
    public enum OrderStatus { New, PartiallyFilled, Filled, Canceled, Rejected }
    public enum TimeInForce { GTC, IOC, FOK }
}
