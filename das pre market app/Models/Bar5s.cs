using System;

namespace PreMarketTrader.Models
{
    /// <summary>
    /// 5 秒 K 线
    /// </summary>
    public class Bar5s
    {
        public string Symbol { get; set; } = "";
        public DateTime StartTime { get; set; }     // 区间起点
        public DateTime EndTime { get; set; }       // 区间终点（Start + 5s）

        public double Open { get; set; }
        public double High { get; set; }
        public double Low { get; set; }
        public double Close { get; set; }
        public long Volume { get; set; }

        public override string ToString()
        {
            return $"{Symbol} {StartTime:HH:mm:ss}-{EndTime:HH:mm:ss} " +
                   $"O:{Open:F2} H:{High:F2} L:{Low:F2} C:{Close:F2} V:{Volume}";
        }
    }
}
