using System.Drawing;
using ChartEngine.Styles;

namespace ChartEngine.ChartControls
{
    /// <summary>
    /// ChartControl 的样式管理部分（partial）。
    /// 统一管理 BackgroundStyle / CandleStyle / VolumeStyle 等。
    /// </summary>
    public partial class ChartControl
    {
        /// <summary>背景样式（主图 & 成交量区）</summary>
        public BackgroundStyle BackgroundStyle { get; private set; }

        /// <summary>K 线样式</summary>
        public CandleStyle CandleStyle { get; private set; }

        /// <summary>成交量柱样式</summary>
        public VolumeStyle VolumeStyle { get; private set; }

        /// <summary>
        /// 初始化默认样式。
        /// </summary>
        private void InitializeStyles()
        {
            BackgroundStyle = new BackgroundStyle
            {
                PriceAreaBackColor = Color.FromArgb(245, 245, 245),
                VolumeAreaBackColor = Color.FromArgb(235, 235, 235)
            };

            CandleStyle = new CandleStyle
            {
                UpColor = Color.LimeGreen,
                DownColor = Color.Red,
                WickColor = Color.Black,
                BodyBorderColor = Color.Black,
                MinBodyWidth = 1f,
                MaxBodyWidth = 30f,
                WickWidth = 1f
            };

            VolumeStyle = new VolumeStyle
            {
                UpColor = Color.FromArgb(80, 180, 80),
                DownColor = Color.FromArgb(200, 80, 80),
                BorderColor = Color.FromArgb(60, 60, 60),
                MinBarWidth = 1f,
                MaxBarWidth = 30f
            };
        }

        /// <summary>
        /// 应用一个简单的暗色主题示例。
        /// 你可以在外部调用：chart.ApplyDarkTheme();
        /// </summary>
        public void ApplyDarkTheme()
        {
            BackgroundStyle.PriceAreaBackColor = Color.FromArgb(20, 20, 20);
            BackgroundStyle.VolumeAreaBackColor = Color.FromArgb(15, 15, 15);

            CandleStyle.UpColor = Color.Lime;
            CandleStyle.DownColor = Color.Red;
            CandleStyle.WickColor = Color.LightGray;
            CandleStyle.BodyBorderColor = Color.LightGray;

            VolumeStyle.UpColor = Color.FromArgb(0, 150, 0);
            VolumeStyle.DownColor = Color.FromArgb(150, 0, 0);
            VolumeStyle.BorderColor = Color.FromArgb(80, 80, 80);

            Invalidate();
        }
    }
}
