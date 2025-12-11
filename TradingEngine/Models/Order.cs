namespace TradingEngine.Models
{
    public enum OrderSide
    {
        Buy,
        Sell,
        Short
    }

    public enum OrderType
    {
        Limit,
        Market,
        StopMarket,
        StopLimit,
        StopLimitPost    // STOPLMTP - 支持盘前盘后
    }

    public enum OrderStatus
    {
        Pending,
        Sending,
        Accepted,
        Partial,
        Executed,
        Canceled,
        Rejected,
        Closed
    }

    /// <summary>
    /// 订单
    /// </summary>
    public class Order
    {
        public int OrderId { get; set; }            // DAS返回的订单ID
        public int Token { get; set; }              // 本地Token，用于追踪
        public string Symbol { get; set; } = "";
        public OrderSide Side { get; set; }
        public OrderType Type { get; set; }
        public int Quantity { get; set; }
        public int FilledQuantity { get; set; }
        public int LeftQuantity { get; set; }
        public int CanceledQuantity { get; set; }
        public double Price { get; set; }           // 限价
        public double StopPrice { get; set; }       // 止损触发价
        public string Route { get; set; } = "";
        public OrderStatus Status { get; set; }
        public DateTime Time { get; set; }
        public string Account { get; set; } = "";
        public string Notes { get; set; } = "";

        // 关联的止损单Token（买入单成交后挂的止损单）
        public int? LinkedStopToken { get; set; }

        public override string ToString()
        {
            return $"[{OrderId}] {Symbol} {Side} {Quantity}@{Price} {Status}";
        }
    }
}