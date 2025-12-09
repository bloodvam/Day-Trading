using System.Drawing;
using ChartEngine.Styles.Core;

namespace ChartEngine.Styles.Themes
{
    public class DarkTheme : IChartTheme
    {
        public string Name => "Dark";

        public CandleStyle CandleStyle { get; }
        public VolumeStyle VolumeStyle { get; }

        public DarkTheme()
        {
            CandleStyle = new CandleStyle
            {
                UpColor = Color.FromArgb(0, 200, 100),
                DownColor = Color.FromArgb(255, 80, 80),
                WickColor = Color.FromArgb(150, 150, 150),
                BodyBorderColor = Color.FromArgb(200, 200, 200),
                WickWidth = 1f,
                MinBodyWidth = 1f,
                MaxBodyWidth = 30f,
                BodyWidthRatio = 0.7f
            };

            VolumeStyle = new VolumeStyle
            {
                UpColor = Color.FromArgb(0, 150, 80),
                DownColor = Color.FromArgb(200, 60, 60),
                BorderColor = Color.FromArgb(80, 80, 80),
                BorderWidth = 1f,
                BarWidthRatio = 0.7f,
                MinBarWidth = 1f,
                MaxBarWidth = 30f,
                MinBarHeight = 1f
            };
        }
    }
}