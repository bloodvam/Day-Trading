using System.Drawing;

namespace ChartEngine.Styles.Core
{
    public class VolumeStyle
    {
        /// <summary>
        /// 成交量柱上涨颜色
        /// </summary>
        public Color UpColor { get; set; } = Color.FromArgb(80, 180, 80);

        /// <summary>
        /// 成交量柱下跌颜色
        /// </summary>
        public Color DownColor { get; set; } = Color.FromArgb(200, 80, 80);

        /// <summary>
        /// Volume bar 边框颜色
        /// </summary>
        public Color BorderColor { get; set; } = Color.FromArgb(60, 60, 60);

        /// <summary>
        /// 边框宽度
        /// </summary>
        public float BorderWidth { get; set; } = 1f;

        /// <summary>
        /// Volume bar 宽度占槽位宽度的比例（0~1）
        /// 例如 0.7f 表示占 70%
        /// </summary>
        public float BarWidthRatio { get; set; } = 0.7f;

        /// <summary>
        /// Volume bar 最小宽度
        /// </summary>
        public float MinBarWidth { get; set; } = 1f;

        /// <summary>
        /// Volume bar 最大宽度
        /// </summary>
        public float MaxBarWidth { get; set; } = 30f;

        /// <summary>
        /// Volume bar 最小高度（避免 volume=0 时不可见）
        /// </summary>
        public float MinBarHeight { get; set; } = 1f;
    }
}
