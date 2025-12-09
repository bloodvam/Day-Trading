using System.Drawing;
using ChartEngine.Styles.Core;

namespace ChartEngine.Styles.Themes
{
    public class LightTheme : IChartTheme
    {
        public string Name => "Light";
        public CandleStyle CandleStyle { get; }
        public VolumeStyle VolumeStyle { get; }

        public LightTheme()
        {
            CandleStyle = new CandleStyle
            {
                UpColor = Color.FromArgb(34, 139, 34),
                DownColor = Color.FromArgb(220, 20, 60),
                WickColor = Color.FromArgb(80, 80, 80),
                BodyBorderColor = Color.FromArgb(60, 60, 60),
                WickWidth = 1f,
                MinBodyWidth = 1f,
                MaxBodyWidth = 30f,
                BodyWidthRatio = 0.7f
            };

            VolumeStyle = new VolumeStyle
            {
                UpColor = Color.FromArgb(80, 180, 80),
                DownColor = Color.FromArgb(200, 80, 80),
                BorderColor = Color.FromArgb(120, 120, 120),
                BorderWidth = 1f,
                BarWidthRatio = 0.7f,
                MinBarWidth = 1f,
                MaxBarWidth = 30f,
                MinBarHeight = 1f
            };
        }
    }
}