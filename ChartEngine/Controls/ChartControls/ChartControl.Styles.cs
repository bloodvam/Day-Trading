using System.Drawing;
using ChartEngine.Rendering.Layers;
using ChartEngine.Styles.Core;

namespace ChartEngine.Controls.ChartControls
{
    /// <summary>
    /// ChartControl 的样式管理部分（partial）。
    /// 统一管理 BackgroundStyle / CandleStyle / VolumeStyle / GridStyle 等。
    /// </summary>
    public partial class ChartControl
    {
        /// <summary>背景样式（主图 & 成交量区）</summary>
        public BackgroundStyle BackgroundStyle { get; private set; }

        /// <summary>K 线样式</summary>
        public CandleStyle CandleStyle { get; private set; }

        /// <summary>成交量柱样式</summary>
        public VolumeStyle VolumeStyle { get; private set; }

        /// <summary>网格样式</summary>
        public GridStyle GridStyle { get; private set; }

        /// <summary>坐标轴样式</summary>
        public AxisStyle AxisStyle { get; private set; }

        /// <summary>十字光标样式</summary>
        public CrosshairStyle CrosshairStyle { get; private set; }

        /// <summary>
        /// 初始化默认样式。
        /// </summary>
        private void InitializeStyles()
        {
            BackgroundStyle = new BackgroundStyle
            {
                PriceAreaBackColor = Color.FromArgb(20, 20, 20),      // 深色背景
                VolumeAreaBackColor = Color.FromArgb(15, 15, 15)
            };

            CandleStyle = new CandleStyle
            {
                UpColor = Color.FromArgb(0, 200, 100),
                DownColor = Color.FromArgb(255, 80, 80),
                WickColor = Color.FromArgb(150, 150, 150),
                BodyBorderColor = Color.FromArgb(200, 200, 200),
                MinBodyWidth = 1f,
                MaxBodyWidth = 30f,
                WickWidth = 1f,
                BodyWidthRatio = 0.7f
            };

            VolumeStyle = new VolumeStyle
            {
                UpColor = Color.FromArgb(0, 150, 80),
                DownColor = Color.FromArgb(200, 60, 60),
                BorderColor = Color.FromArgb(80, 80, 80),
                MinBarWidth = 1f,
                MaxBarWidth = 30f,
                BarWidthRatio = 0.7f,
                BorderWidth = 1f,
                MinBarHeight = 1f
            };

            // 初始化网格样式
            GridStyle = GridStyle.GetDarkThemeDefault();

            // 初始化坐标轴样式
            AxisStyle = AxisStyle.GetDarkThemeDefault();

            // 初始化十字光标样式
            CrosshairStyle = CrosshairStyle.GetDarkThemeDefault();
        }

        /// <summary>
        /// 应用暗色主题。
        /// </summary>
        public void ApplyDarkTheme()
        {
            BackgroundStyle.PriceAreaBackColor = Color.FromArgb(20, 20, 20);
            BackgroundStyle.VolumeAreaBackColor = Color.FromArgb(15, 15, 15);

            CandleStyle.UpColor = Color.FromArgb(0, 200, 100);
            CandleStyle.DownColor = Color.FromArgb(255, 80, 80);
            CandleStyle.WickColor = Color.FromArgb(150, 150, 150);
            CandleStyle.BodyBorderColor = Color.FromArgb(200, 200, 200);

            VolumeStyle.UpColor = Color.FromArgb(0, 150, 80);
            VolumeStyle.DownColor = Color.FromArgb(200, 60, 60);
            VolumeStyle.BorderColor = Color.FromArgb(80, 80, 80);

            UpdateGridStyle(GridStyle.GetDarkThemeDefault());
            UpdateAxisStyle(AxisStyle.GetDarkThemeDefault());
            UpdateCrosshairStyle(CrosshairStyle.GetDarkThemeDefault());

            Invalidate();
        }

        /// <summary>
        /// 应用亮色主题。
        /// </summary>
        public void ApplyLightTheme()
        {
            BackgroundStyle.PriceAreaBackColor = Color.FromArgb(255, 255, 255);
            BackgroundStyle.VolumeAreaBackColor = Color.FromArgb(250, 250, 250);

            CandleStyle.UpColor = Color.FromArgb(34, 139, 34);
            CandleStyle.DownColor = Color.FromArgb(220, 20, 60);
            CandleStyle.WickColor = Color.FromArgb(80, 80, 80);
            CandleStyle.BodyBorderColor = Color.FromArgb(60, 60, 60);

            VolumeStyle.UpColor = Color.FromArgb(80, 180, 80);
            VolumeStyle.DownColor = Color.FromArgb(200, 80, 80);
            VolumeStyle.BorderColor = Color.FromArgb(120, 120, 120);

            UpdateGridStyle(GridStyle.GetLightThemeDefault());
            UpdateAxisStyle(AxisStyle.GetLightThemeDefault());
            UpdateCrosshairStyle(CrosshairStyle.GetLightThemeDefault());

            Invalidate();
        }

        /// <summary>
        /// 应用 TradingView 风格主题。
        /// </summary>
        public void ApplyTradingViewTheme()
        {
            BackgroundStyle.PriceAreaBackColor = Color.FromArgb(19, 23, 34);
            BackgroundStyle.VolumeAreaBackColor = Color.FromArgb(19, 23, 34);

            CandleStyle.UpColor = Color.FromArgb(38, 166, 154);
            CandleStyle.DownColor = Color.FromArgb(239, 83, 80);
            CandleStyle.WickColor = Color.FromArgb(120, 123, 134);
            CandleStyle.BodyBorderColor = Color.FromArgb(120, 123, 134);

            VolumeStyle.UpColor = Color.FromArgb(38, 166, 154, 100);  // 半透明
            VolumeStyle.DownColor = Color.FromArgb(239, 83, 80, 100);
            VolumeStyle.BorderColor = Color.FromArgb(60, 60, 60);

            UpdateGridStyle(GridStyle.GetTradingViewStyle());
            UpdateAxisStyle(AxisStyle.GetTradingViewStyle());

            Invalidate();
        }

        /// <summary>
        /// 更新网格样式
        /// </summary>
        public void UpdateGridStyle(GridStyle style)
        {
            if (style == null) return;

            GridStyle = style;

            var gridLayer = GetLayer<GridLayer>();
            if (gridLayer != null)
            {
                gridLayer.UpdateStyle(style);
                Invalidate();
            }
        }

        /// <summary>
        /// 更新蜡烛样式
        /// </summary>
        public void UpdateCandleStyle(CandleStyle style)
        {
            if (style == null) return;
            CandleStyle = style;
            Invalidate();
        }

        /// <summary>
        /// 更新成交量样式
        /// </summary>
        public void UpdateVolumeStyle(VolumeStyle style)
        {
            if (style == null) return;
            VolumeStyle = style;
            Invalidate();
        }

        /// <summary>
        /// 更新背景样式
        /// </summary>
        public void UpdateBackgroundStyle(BackgroundStyle style)
        {
            if (style == null) return;
            BackgroundStyle = style;
            Invalidate();
        }

        /// <summary>
        /// 更新坐标轴样式
        /// </summary>
        public void UpdateAxisStyle(AxisStyle style)
        {
            if (style == null) return;

            AxisStyle = style;

            var axisLayer = GetLayer<AxisLayer>();
            if (axisLayer != null)
            {
                axisLayer.UpdateStyle(style);
                Invalidate();
            }
        }

        /// <summary>
        /// 更新十字光标样式
        /// </summary>
        public void UpdateCrosshairStyle(CrosshairStyle style)
        {
            if (style == null) return;

            CrosshairStyle = style;

            var crosshairLayer = GetLayer<CrosshairLayer>();
            if (crosshairLayer != null)
            {
                crosshairLayer.UpdateStyle(style);
                Invalidate();
            }
        }
    }
}