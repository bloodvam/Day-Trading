namespace TradingEngine.Models
{
    /// <summary>
    /// 成交记录
    /// </summary>
    public class Trade
    {
        public int TradeId { get; set; }
        public string Symbol { get; set; } = "";
        public OrderSide Side { get; set; }
        public int Quantity { get; set; }
        public double Price { get; set; }
        public string Route { get; set; } = "";
        public DateTime Time { get; set; }
        public int OrderId { get; set; }
        public char Liquidity { get; set; }         // R/+/-
        public double ECNFee { get; set; }
        public double PL { get; set; }

        public override string ToString()
        {
            return $"[{TradeId}] {Symbol} {Side} {Quantity}@{Price} PL:{PL:F2}";
        }
    }
}