// ChartEngine/Data/Models/SimpleSeries.cs
using System;
using System.Collections.Generic;

namespace ChartEngine.Data.Models
{
    public class SimpleSeries : ISeries
    {
        private readonly List<IBar> _bars = new List<IBar>();

        public IReadOnlyList<IBar> Bars => _bars;
        public int Count => _bars.Count;

        // 🔥 修改：添加 timeFrame 参数
        public void AddBar(double o, double h, double l, double c, double v,
                          DateTime timestamp, TimeFrame timeFrame)
        {
            _bars.Add(new SimpleBar(o, h, l, c, v, timestamp, timeFrame));
        }

        // 🔥 保留旧版本以兼容，默认 Minute1
        public void AddBar(double o, double h, double l, double c, double v,
                          DateTime timestamp)
        {
            _bars.Add(new SimpleBar(o, h, l, c, v, timestamp, TimeFrame.Minute1));
        }

        // 🔥 兼容更旧的版本（无时间戳）
        public void AddBar(double o, double h, double l, double c, double v)
        {
            _bars.Add(new SimpleBar(o, h, l, c, v, DateTime.Now, TimeFrame.Minute1));
        }
    }
}