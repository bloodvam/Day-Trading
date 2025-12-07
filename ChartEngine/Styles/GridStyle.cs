using System.Drawing;
using System.Drawing.Drawing2D;

namespace ChartEngine.Styles
{
    /// <summary>
    /// 网格线样式配置
    /// </summary>
    public class GridStyle
    {
        /// <summary>
        /// 主网格线颜色 (对应整数价格,如 100.0, 101.0)
        /// </summary>
        public Color MajorLineColor { get; set; } = Color.FromArgb(50, 255, 255, 255);

        /// <summary>
        /// 次网格线颜色 (对应半数价格,如 100.5)
        /// </summary>
        public Color MinorLineColor { get; set; } = Color.FromArgb(25, 255, 255, 255);

        /// <summary>
        /// 主网格线宽度
        /// </summary>
        public float MajorLineWidth { get; set; } = 1f;

        /// <summary>
        /// 次网格线宽度
        /// </summary>
        public float MinorLineWidth { get; set; } = 1f;

        /// <summary>
        /// 网格线样式 (实线/虚线/点线)
        /// </summary>
        public DashStyle LineStyle { get; set; } = DashStyle.Dot;

        /// <summary>
        /// 是否显示横向网格线
        /// </summary>
        public bool ShowHorizontalLines { get; set; } = true;

        /// <summary>
        /// 是否显示纵向网格线
        /// </summary>
        public bool ShowVerticalLines { get; set; } = true;

        /// <summary>
        /// 是否显示次网格线
        /// </summary>
        public bool ShowMinorLines { get; set; } = true;

        /// <summary>
        /// 目标网格线数量 (用于自动计算间距)
        /// </summary>
        public int TargetLineCount { get; set; } = 8;

        /// <summary>
        /// 获取暗色主题的默认网格样式
        /// </summary>
        public static GridStyle GetDarkThemeDefault()
        {
            return new GridStyle
            {
                MajorLineColor = Color.FromArgb(50, 255, 255, 255),
                MinorLineColor = Color.FromArgb(25, 255, 255, 255),
                MajorLineWidth = 1f,
                MinorLineWidth = 1f,
                LineStyle = DashStyle.Dot,
                ShowHorizontalLines = true,
                ShowVerticalLines = true,
                ShowMinorLines = true,
                TargetLineCount = 8
            };
        }

        /// <summary>
        /// 获取亮色主题的默认网格样式
        /// </summary>
        public static GridStyle GetLightThemeDefault()
        {
            return new GridStyle
            {
                MajorLineColor = Color.FromArgb(40, 0, 0, 0),
                MinorLineColor = Color.FromArgb(20, 0, 0, 0),
                MajorLineWidth = 1f,
                MinorLineWidth = 1f,
                LineStyle = DashStyle.Dot,
                ShowHorizontalLines = true,
                ShowVerticalLines = true,
                ShowMinorLines = true,
                TargetLineCount = 8
            };
        }

        /// <summary>
        /// TradingView 样式网格
        /// </summary>
        public static GridStyle GetTradingViewStyle()
        {
            return new GridStyle
            {
                MajorLineColor = Color.FromArgb(45, 255, 255, 255),
                MinorLineColor = Color.FromArgb(22, 255, 255, 255),
                MajorLineWidth = 1f,
                MinorLineWidth = 1f,
                LineStyle = DashStyle.Dot,
                ShowHorizontalLines = true,
                ShowVerticalLines = true,
                ShowMinorLines = true,
                TargetLineCount = 10
            };
        }
    }
}