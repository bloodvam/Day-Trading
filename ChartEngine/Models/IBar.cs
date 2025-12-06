namespace ChartEngine.Models
{
    /// <summary>
    /// 表示一根标准 OHLCV K线。
    /// </summary>
    public interface IBar
    {
        double Open { get; }
        double High { get; }
        double Low { get; }
        double Close { get; }
        double Volume { get; }
    }
}
