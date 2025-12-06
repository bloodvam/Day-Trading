using System.Drawing;

namespace PreMarketTrader.ChartApp.Controls
{
    /// <summary>
    /// 全局图表样式管理（类似 TradingView Theme 系统）
    /// 所有渲染模块从此处读取样式，而不在各自文件硬编码颜色/宽度
    /// </summary>
    public class ChartStyle
    {
        // ========== 背景 ==========
        public Color BackgroundColor { get; set; } = Color.Black;

        // ========== 蜡烛图 ==========
        public float CandleBodyWidthRatio { get; set; } = 0.6f;
        public Color CandleUpColor { get; set; } = Color.Lime;
        public Color CandleDownColor { get; set; } = Color.Red;

        // ========== Volume ==========
        public float VolumeOpacity { get; set; } = 0.5f;
        public Color VolumeUpColor { get; set; } = Color.Lime;
        public Color VolumeDownColor { get; set; } = Color.Red;

        // ========== 网格线 ==========
        public int GridHorizontalCount { get; set; } = 6;
        public int GridVerticalCount { get; set; } = 6;
        public Color GridLineColor { get; set; } = Color.FromArgb(40, 255, 255, 255);

        // ========== 坐标轴 ==========
        public Font AxisFont { get; set; } = new Font("Segoe UI", 8);
        public Color AxisTextColor { get; set; } = Color.FromArgb(200, 200, 200, 200);
        public Color AxisLineColor { get; set; } = Color.FromArgb(120, 200, 200, 200);
        public Brush AxisTextBrush => new SolidBrush(AxisTextColor);

        // ========== Crosshair ==========
        public Color CrosshairLineColor { get; set; } = Color.FromArgb(180, 200, 200, 200);
        public float[] CrosshairDashPattern { get; set; } = new float[] { 4, 4 };

        // ========== Tooltip ==========
        public Font TooltipFont { get; set; } = new Font("Segoe UI", 8);
        public Color TooltipBackgroundColor { get; set; } = Color.FromArgb(200, 25, 25, 25);
        public Color TooltipShadowColor { get; set; } = Color.FromArgb(80, 0, 0, 0);

        // ========== 价格泡泡 & 时间泡泡 ==========
        public Color BubbleBackground { get; set; } = Color.FromArgb(160, 40, 40, 40);
        public Color BubbleShadow { get; set; } = Color.FromArgb(80, 0, 0, 0);
        public Color BubbleTextColor { get; set; } = Color.White;

        // ========== 高亮效果 ==========
        public Color HighlightColor { get; set; } = Color.FromArgb(80, 255, 255, 255);
    }
}
