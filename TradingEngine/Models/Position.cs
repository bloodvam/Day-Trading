namespace TradingEngine.Models
{
    public enum PositionType
    {
        Cash = 1,
        Margin = 2,
        Short = 3
    }

    /// <summary>
    /// 持仓
    /// </summary>
    public class Position
    {
        public string Symbol { get; set; } = "";
        public PositionType Type { get; set; }
        public int Quantity { get; set; }
        public double AvgCost { get; set; }
        public int InitQuantity { get; set; }       // 隔夜持仓数量
        public double InitPrice { get; set; }       // 隔夜持仓成本
        public double RealizedPL { get; set; }
        public double UnrealizedPL { get; set; }
        public DateTime CreateTime { get; set; }

        public double MarketValue => Quantity * AvgCost;

        public override string ToString()
        {
            return $"{Symbol} {Quantity}@{AvgCost:F2} PL:{RealizedPL:F2}/{UnrealizedPL:F2}";
        }
    }
}