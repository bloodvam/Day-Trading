namespace TradingEngine.Models
{
    /// <summary>
    /// 单笔成交（来自 $T&S）
    /// </summary>
    public class Tick
    {
        public string Symbol { get; set; } = "";
        public double Price { get; set; }
        public int Volume { get; set; }
        public char Side { get; set; }          // B/S/I
        public DateTime Time { get; set; }
        public int Condition { get; set; }
        public string Exchange { get; set; } = "";

        /// <summary>
        /// 是否为有效的Last价格（用于构建K线）
        /// </summary>
        public bool IsValidForLastPrice => (Condition & 0x20) != 0;

        /// <summary>
        /// 是否计入成交量
        /// </summary>
        public bool IsValidForVolume => (Condition & 0x40) != 0;

        public override string ToString()
        {
            return $"{Symbol} {Time:HH:mm:ss} {Price} x {Volume} ({Side})";
        }
    }
}