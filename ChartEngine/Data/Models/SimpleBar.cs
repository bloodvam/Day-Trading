// ChartEngine/Data/Models/SimpleBar.cs
using System;

namespace ChartEngine.Data.Models
{
    public class SimpleBar : IBar
    {
        public double Open { get; }
        public double High { get; }
        public double Low { get; }
        public double Close { get; }
        public double Volume { get; }
        public DateTime Timestamp { get; }
        public TimeFrame TimeFrame { get; }  // 🔥 新增

        // 🔥 修改构造函数，添加 timeFrame 参数
        public SimpleBar(double o, double h, double l, double c, double v,
                         DateTime timestamp, TimeFrame timeFrame)
        {
            Open = o;
            High = h;
            Low = l;
            Close = c;
            Volume = v;
            Timestamp = timestamp;
            TimeFrame = timeFrame;
        }

        // 🔥 保留旧构造函数以兼容，默认为 Minute1
        public SimpleBar(double o, double h, double l, double c, double v,
                         DateTime timestamp)
            : this(o, h, l, c, v, timestamp, TimeFrame.Minute1)
        {
        }
    }
}