using System;

namespace PreMarketTrader.Models
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
        public DateTime Time { get; set; }      // 本地时间
        public int Condition { get; set; }      // condition 字节

        public override string ToString()
        {
            return $"{Symbol} {Time:HH:mm:ss} {Price} x {Volume} ({Side}) cond={Condition}";
        }
    }
}
