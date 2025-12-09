// ChartEngine/Styles/Core/SessionStyle.cs
using System.Drawing;
using System.Drawing.Drawing2D;

namespace ChartEngine.Styles.Core
{
    /// <summary>
    /// 交易时段样式配置
    /// </summary>
    public class SessionStyle
    {
        // ========== 背景色配置 ==========

        /// <summary>盘前背景色（深蓝）</summary>
        public Color PreMarketBackColor { get; set; } = Color.FromArgb(15, 20, 35);

        /// <summary>盘中背景色（黑色）</summary>
        public Color RegularBackColor { get; set; } = Color.FromArgb(20, 20, 20);

        /// <summary>盘后背景色（深蓝）</summary>
        public Color AfterHoursBackColor { get; set; } = Color.FromArgb(15, 20, 35);

        /// <summary>休市背景色（深灰）</summary>
        public Color ClosedBackColor { get; set; } = Color.FromArgb(10, 10, 10);

        // ========== 日期分隔线配置 ==========

        /// <summary>日期分隔线颜色</summary>
        public Color DateSeparatorColor { get; set; } = Color.FromArgb(60, 60, 60);

        /// <summary>日期分隔线宽度</summary>
        public float DateSeparatorWidth { get; set; } = 1f;

        /// <summary>日期分隔线样式</summary>
        public DashStyle DateSeparatorStyle { get; set; } = DashStyle.Solid;

        // ========== 预设主题 ==========

        public static SessionStyle GetDarkThemeDefault()
        {
            return new SessionStyle();
        }

        public static SessionStyle GetLightThemeDefault()
        {
            return new SessionStyle
            {
                PreMarketBackColor = Color.FromArgb(240, 245, 250),
                RegularBackColor = Color.FromArgb(255, 255, 255),
                AfterHoursBackColor = Color.FromArgb(240, 245, 250),
                ClosedBackColor = Color.FromArgb(245, 245, 245),
                DateSeparatorColor = Color.FromArgb(200, 200, 200)
            };
        }
    }
}