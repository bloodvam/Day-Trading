using System;
using System.Collections.Generic;

namespace ChartEngine.Models
{
    public class SimpleSeries : ISeries
    {
        private readonly List<IBar> _bars = new List<IBar>();

        public IReadOnlyList<IBar> Bars => _bars;
        public int Count => _bars.Count;

        public void AddBar(double o, double h, double l, double c, double v, DateTime timestamp)
        {
            _bars.Add(new SimpleBar(o, h, l, c, v, timestamp));
        }

        /// <summary>
        /// 添加K线 (兼容旧代码,使用当前时间)
        /// </summary>
        public void AddBar(double o, double h, double l, double c, double v)
        {
            _bars.Add(new SimpleBar(o, h, l, c, v, DateTime.Now));
        }
    }
}