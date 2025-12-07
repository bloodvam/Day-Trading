using System.Drawing;
using System.Drawing.Drawing2D;

namespace ChartEngine.Styles.Core
{
    /// <summary>
    /// 十字光标样式配置
    /// </summary>
    public class CrosshairStyle
    {
        // ========== 十字线样式 ==========

        /// <summary>十字线颜色</summary>
        public Color LineColor { get; set; } = Color.FromArgb(150, 150, 150);

        /// <summary>十字线宽度</summary>
        public float LineWidth { get; set; } = 1f;

        /// <summary>十字线样式</summary>
        public DashStyle LineStyle { get; set; } = DashStyle.Dot;

        // ========== 价格气泡样式 ==========

        /// <summary>价格气泡背景色</summary>
        public Color PriceBubbleBackColor { get; set; } = Color.FromArgb(80, 80, 80);

        /// <summary>价格气泡文字颜色</summary>
        public Color PriceBubbleTextColor { get; set; } = Color.White;

        /// <summary>价格气泡边框颜色</summary>
        public Color PriceBubbleBorderColor { get; set; } = Color.FromArgb(100, 100, 100);

        /// <summary>价格气泡字体</summary>
        public Font PriceBubbleFont { get; set; } = new Font("Arial", 9);

        /// <summary>价格气泡内边距</summary>
        public int PriceBubblePadding { get; set; } = 4;

        // ========== 时间气泡样式 ==========

        /// <summary>时间气泡背景色</summary>
        public Color TimeBubbleBackColor { get; set; } = Color.FromArgb(80, 80, 80);

        /// <summary>时间气泡文字颜色</summary>
        public Color TimeBubbleTextColor { get; set; } = Color.White;

        /// <summary>时间气泡边框颜色</summary>
        public Color TimeBubbleBorderColor { get; set; } = Color.FromArgb(100, 100, 100);

        /// <summary>时间气泡字体</summary>
        public Font TimeBubbleFont { get; set; } = new Font("Arial", 9);

        /// <summary>时间气泡内边距</summary>
        public int TimeBubblePadding { get; set; } = 4;

        // ========== Tooltip样式 ==========

        /// <summary>Tooltip背景色</summary>
        public Color TooltipBackColor { get; set; } = Color.FromArgb(230, 30, 30, 30);

        /// <summary>Tooltip边框颜色</summary>
        public Color TooltipBorderColor { get; set; } = Color.FromArgb(80, 80, 80);

        /// <summary>Tooltip字体</summary>
        public Font TooltipFont { get; set; } = new Font("Arial", 9);

        /// <summary>Tooltip标签字体 (加粗)</summary>
        public Font TooltipLabelFont { get; set; } = new Font("Arial", 9, FontStyle.Bold);

        /// <summary>Tooltip内边距</summary>
        public int TooltipPadding { get; set; } = 8;

        /// <summary>Tooltip行间距</summary>
        public int TooltipLineSpacing { get; set; } = 4;

        /// <summary>Tooltip宽度</summary>
        public int TooltipWidth { get; set; } = 180;

        // ========== 颜色配置 ==========

        /// <summary>最高价颜色</summary>
        public Color HighColor { get; set; } = Color.FromArgb(0, 200, 100);

        /// <summary>最低价颜色</summary>
        public Color LowColor { get; set; } = Color.FromArgb(255, 80, 80);

        /// <summary>普通文字颜色</summary>
        public Color NormalColor { get; set; } = Color.FromArgb(200, 200, 200);

        /// <summary>标签颜色 (时间、开盘等)</summary>
        public Color LabelColor { get; set; } = Color.FromArgb(150, 150, 150);

        /// <summary>上涨颜色</summary>
        public Color UpColor { get; set; } = Color.FromArgb(0, 200, 100);

        /// <summary>下跌颜色</summary>
        public Color DownColor { get; set; } = Color.FromArgb(255, 80, 80);

        // ========== 预设主题 ==========

        public static CrosshairStyle GetDarkThemeDefault()
        {
            return new CrosshairStyle
            {
                LineColor = Color.FromArgb(150, 150, 150),
                LineWidth = 1f,
                LineStyle = DashStyle.Dot,

                PriceBubbleBackColor = Color.FromArgb(80, 80, 80),
                PriceBubbleTextColor = Color.White,

                TimeBubbleBackColor = Color.FromArgb(80, 80, 80),
                TimeBubbleTextColor = Color.White,

                TooltipBackColor = Color.FromArgb(230, 30, 30, 30),
                TooltipBorderColor = Color.FromArgb(80, 80, 80),

                HighColor = Color.FromArgb(0, 200, 100),
                LowColor = Color.FromArgb(255, 80, 80),
                NormalColor = Color.FromArgb(200, 200, 200),
                UpColor = Color.FromArgb(0, 200, 100),
                DownColor = Color.FromArgb(255, 80, 80)
            };
        }

        public static CrosshairStyle GetLightThemeDefault()
        {
            return new CrosshairStyle
            {
                LineColor = Color.FromArgb(120, 120, 120),
                LineWidth = 1f,
                LineStyle = DashStyle.Dot,

                PriceBubbleBackColor = Color.FromArgb(240, 240, 240),
                PriceBubbleTextColor = Color.Black,

                TimeBubbleBackColor = Color.FromArgb(240, 240, 240),
                TimeBubbleTextColor = Color.Black,

                TooltipBackColor = Color.FromArgb(250, 255, 255, 255),
                TooltipBorderColor = Color.FromArgb(180, 180, 180),

                HighColor = Color.FromArgb(34, 139, 34),
                LowColor = Color.FromArgb(220, 20, 60),
                NormalColor = Color.FromArgb(60, 60, 60),
                UpColor = Color.FromArgb(34, 139, 34),
                DownColor = Color.FromArgb(220, 20, 60)
            };
        }
    }
}