using System;
using System.Drawing;

namespace ChartEngine.Transforms
{
    /// <summary>
    /// 价格 ↔ 像素 Y（主图）
    /// </summary>
    public class VerticalScale
    {
        public double Min { get; private set; }
        public double Max { get; private set; }

        public void SetRange(double min, double max)
        {
            if (max <= min)
            {
                max = min + 1e-6;
            }

            Min = min;
            Max = max;
        }

        public float PriceToY(double price, Rectangle area)
        {
            if (Max <= Min)
                return area.Bottom;

            double t = (price - Min) / (Max - Min);
            t = Math.Max(0.0, Math.Min(1.0, t));

            return area.Bottom - (float)(t * area.Height);
        }

        public double YToPrice(float y, Rectangle area)
        {
            if (Max <= Min)
                return Min;

            double t = (area.Bottom - y) / area.Height;
            t = Math.Max(0.0, Math.Min(1.0, t));

            return Min + t * (Max - Min);
        }
    }
}
