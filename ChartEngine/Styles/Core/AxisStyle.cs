using System.Drawing;

namespace ChartEngine.Styles.Core
{
    /// <summary>
    /// 坐标轴样式配置
    /// </summary>
    public class AxisStyle
    {
        // ========== 通用样式 ==========

        /// <summary>坐标轴背景颜色</summary>
        public Color BackgroundColor { get; set; } = Color.FromArgb(30, 30, 30);

        /// <summary>坐标轴线颜色</summary>
        public Color AxisLineColor { get; set; } = Color.FromArgb(60, 60, 60);

        /// <summary>刻度线颜色</summary>
        public Color TickLineColor { get; set; } = Color.FromArgb(50, 50, 50);

        /// <summary>标签文字颜色</summary>
        public Color LabelTextColor { get; set; } = Color.FromArgb(180, 180, 180);

        /// <summary>标签字体</summary>
        public Font LabelFont { get; set; } = new Font("Arial", 9);

        // ========== 价格轴配置 ==========

        /// <summary>是否显示价格轴</summary>
        public bool ShowPriceAxis { get; set; } = true;

        /// <summary>价格轴位置</summary>
        public PriceAxisPosition PriceAxisPosition { get; set; } = PriceAxisPosition.Right;

        /// <summary>价格轴宽度</summary>
        public int PriceAxisWidth { get; set; } = 70;

        /// <summary>价格标签格式 (小数位数)</summary>
        public int PriceDecimals { get; set; } = 2;

        /// <summary>是否显示刻度线</summary>
        public bool ShowPriceTicks { get; set; } = true;

        /// <summary>刻度线长度</summary>
        public int TickLength { get; set; } = 5;

        // ========== 时间轴配置 ==========

        /// <summary>是否显示时间轴</summary>
        public bool ShowTimeAxis { get; set; } = true;

        /// <summary>时间轴高度</summary>
        public int TimeAxisHeight { get; set; } = 25;

        /// <summary>时间格式</summary>
        public string TimeFormat { get; set; } = "HH:mm";

        // ========== 当前价格标签 ==========

        /// <summary>是否显示当前价格标签</summary>
        public bool ShowCurrentPriceLabel { get; set; } = true;

        /// <summary>当前价格标签背景色 (上涨)</summary>
        public Color CurrentPriceUpBackColor { get; set; } = Color.FromArgb(0, 200, 100);

        /// <summary>当前价格标签背景色 (下跌)</summary>
        public Color CurrentPriceDownBackColor { get; set; } = Color.FromArgb(255, 80, 80);

        /// <summary>当前价格标签文字颜色</summary>
        public Color CurrentPriceTextColor { get; set; } = Color.White;

        /// <summary>当前价格标签字体</summary>
        public Font CurrentPriceFont { get; set; } = new Font("Arial", 9, FontStyle.Bold);

        // ========== 边距 ==========

        /// <summary>标签左右边距</summary>
        public int LabelPadding { get; set; } = 5;

        /// <summary>标签上下边距</summary>
        public int LabelVerticalPadding { get; set; } = 2;

        // ========== 预设主题 ==========

        /// <summary>获取暗色主题坐标轴样式</summary>
        public static AxisStyle GetDarkThemeDefault()
        {
            return new AxisStyle
            {
                BackgroundColor = Color.FromArgb(30, 30, 30),
                AxisLineColor = Color.FromArgb(60, 60, 60),
                TickLineColor = Color.FromArgb(50, 50, 50),
                LabelTextColor = Color.FromArgb(180, 180, 180),
                LabelFont = new Font("Arial", 9),
                ShowPriceAxis = true,
                ShowTimeAxis = true,
                ShowCurrentPriceLabel = true,
                PriceAxisWidth = 70,
                TimeAxisHeight = 25,
                PriceDecimals = 2,
                TimeFormat = "HH:mm"
            };
        }

        /// <summary>获取亮色主题坐标轴样式</summary>
        public static AxisStyle GetLightThemeDefault()
        {
            return new AxisStyle
            {
                BackgroundColor = Color.FromArgb(245, 245, 245),
                AxisLineColor = Color.FromArgb(200, 200, 200),
                TickLineColor = Color.FromArgb(220, 220, 220),
                LabelTextColor = Color.FromArgb(80, 80, 80),
                LabelFont = new Font("Arial", 9),
                ShowPriceAxis = true,
                ShowTimeAxis = true,
                ShowCurrentPriceLabel = true,
                PriceAxisWidth = 70,
                TimeAxisHeight = 25,
                PriceDecimals = 2,
                TimeFormat = "HH:mm",
                CurrentPriceUpBackColor = Color.FromArgb(34, 139, 34),
                CurrentPriceDownBackColor = Color.FromArgb(220, 20, 60)
            };
        }

        /// <summary>获取 TradingView 风格坐标轴样式</summary>
        public static AxisStyle GetTradingViewStyle()
        {
            return new AxisStyle
            {
                BackgroundColor = Color.FromArgb(19, 23, 34),
                AxisLineColor = Color.FromArgb(40, 44, 52),
                TickLineColor = Color.FromArgb(35, 39, 47),
                LabelTextColor = Color.FromArgb(130, 134, 142),
                LabelFont = new Font("Arial", 9),
                ShowPriceAxis = true,
                ShowTimeAxis = true,
                ShowCurrentPriceLabel = true,
                PriceAxisWidth = 70,
                TimeAxisHeight = 25,
                PriceDecimals = 2,
                TimeFormat = "HH:mm",
                CurrentPriceUpBackColor = Color.FromArgb(38, 166, 154),
                CurrentPriceDownBackColor = Color.FromArgb(239, 83, 80)
            };
        }
    }

    /// <summary>
    /// 价格轴位置
    /// </summary>
    public enum PriceAxisPosition
    {
        /// <summary>左侧</summary>
        Left,
        /// <summary>右侧</summary>
        Right
    }
}