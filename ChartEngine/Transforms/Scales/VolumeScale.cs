using System;
using System.Drawing;

namespace ChartEngine.Transforms.Scales
{
    /// <summary>
    /// 成交量 ↔ 像素 Y（volume 区域）
    /// </summary>
    public class VolumeScale
    {
        public double MaxVolume { get; private set; } = 1.0;

        public void SetMaxVolume(double maxVolume)
        {
            if (maxVolume <= 0)
                maxVolume = 1.0;

            MaxVolume = maxVolume;
        }

        public float VolumeToY(double volume, Rectangle area)
        {
            if (MaxVolume <= 0)
                return area.Bottom;

            double t = volume / MaxVolume;
            t = Math.Max(0.0, Math.Min(1.0, t));

            return area.Bottom - (float)(t * area.Height);
        }
    }
}
