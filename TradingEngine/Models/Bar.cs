namespace TradingEngine.Models
{
    /// <summary>
    /// K线数据
    /// </summary>
    public class Bar
    {
        public string Symbol { get; set; } = "";
        public DateTime Time { get; set; }          // Bar开始时间
        public double Open { get; set; }
        public double High { get; set; }
        public double Low { get; set; }
        public double Close { get; set; }
        public long Volume { get; set; }
        public int IntervalSeconds { get; set; }    // 时间周期（秒）

        public bool IsComplete { get; set; }

        public override string ToString()
        {
            return $"{Symbol} {Time:HH:mm:ss} O:{Open} H:{High} L:{Low} C:{Close} V:{Volume}";
        }

        public Bar Clone()
        {
            return new Bar
            {
                Symbol = Symbol,
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
    }
}