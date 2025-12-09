// ChartEngine/Data/Models/IBar.cs
using System;

namespace ChartEngine.Data.Models
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

        /// <summary>K线时间戳</summary>
        DateTime Timestamp { get; }

        /// <summary>K线时间周期</summary>
        TimeFrame TimeFrame { get; }  // 🔥 新增
    }
}