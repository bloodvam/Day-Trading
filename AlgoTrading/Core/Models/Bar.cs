namespace AlgoTrading.Core.Models
{
    /// <summary>
    /// K线数据（回测用）
    /// </summary>
    public class Bar
    {
        public DateTime Time { get; set; }
        public double Open { get; set; }
        public double High { get; set; }
        public double Low { get; set; }
        public double Close { get; set; }
        public long Volume { get; set; }
        public int IntervalSeconds { get; set; }
        public bool IsComplete { get; set; }

        public Bar Clone()
        {
            return new Bar
            {
                Time = Time,
                Open = Open,
                High = High,
                Low = Low,
                Close = Close,
                Volume = Volume,
                IntervalSeconds = IntervalSeconds,
                IsComplete = IsComplete
            };
        }

        public override string ToString()
        {
            return $"[{Time:HH:mm:ss}] O:{Open:F2} H:{High:F2} L:{Low:F2} C:{Close:F2} V:{Volume}";
        }
    }
}