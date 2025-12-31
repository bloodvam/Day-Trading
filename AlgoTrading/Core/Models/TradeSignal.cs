namespace AlgoTrading.Core.Models
{
    /// <summary>
    /// 交易信号类型
    /// </summary>
    public enum SignalType
    {
        Buy,            // 买入
        SellHalf,       // 卖出一半
        SellAll,        // 全部卖出
        AddPosition     // 加仓
    }

    /// <summary>
    /// 交易信号
    /// </summary>
    public class TradeSignal
    {
        /// <summary>
        /// 信号类型
        /// </summary>
        public SignalType Type { get; set; }

        /// <summary>
        /// 信号触发价格
        /// </summary>
        public double Price { get; set; }

        /// <summary>
        /// 信号触发时间
        /// </summary>
        public DateTime Time { get; set; }

        /// <summary>
        /// 触发原因（如 "突破 $10.00"）
        /// </summary>
        public string Reason { get; set; } = string.Empty;

        /// <summary>
        /// 股数（可选，策略可指定）
        /// </summary>
        public int? Shares { get; set; }

        /// <summary>
        /// 入场价格（用于计算 PnL，多仓位模式下每个 unit 有不同的入场价）
        /// </summary>
        public double? EntryPrice { get; set; }

        public override string ToString()
        {
            return $"[{Time:HH:mm:ss.fff}] {Type} @ {Price:F2} - {Reason}";
        }
    }
}