using System;

namespace ChartEngine.Models
{
    public class SimpleBar : IBar
    {
        public double Open { get; }
        public double High { get; }
        public double Low { get; }
        public double Close { get; }
        public double Volume { get; }
        public DateTime Timestamp { get; }

        public SimpleBar(double o, double h, double l, double c, double v, DateTime timestamp)
        {
            Open = o;
            High = h;
            Low = l;
            Close = c;
            Volume = v;
            Timestamp = timestamp;
        }
    }
}