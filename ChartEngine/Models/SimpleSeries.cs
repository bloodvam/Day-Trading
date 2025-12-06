using System.Collections.Generic;

namespace ChartEngine.Models
{
    public class SimpleSeries : ISeries
    {
        private readonly List<IBar> _bars = new List<IBar>();

        public IReadOnlyList<IBar> Bars => _bars;
        public int Count => _bars.Count;

        public void AddBar(double o, double h, double l, double c, double v)
        {
            _bars.Add(new SimpleBar(o, h, l, c, v));
        }
    }
}
