using System.Drawing;

namespace ChartEngine.Styles.Core
{
    public class CandleStyle
    {
        /// <summary>
        /// 上涨 K 线实体颜色
        /// </summary>
        public Color UpColor { get; set; } = Color.LimeGreen;

        /// <summary>
        /// 下跌 K 线实体颜色
        /// </summary>
        public Color DownColor { get; set; } = Color.Red;

        /// <summary>
        /// 影线颜色
        /// </summary>
        public Color WickColor { get; set; } = Color.Black;

        /// <summary>
        /// 实体边框颜色
        /// </summary>
        public Color BodyBorderColor { get; set; } = Color.Black;

        /// <summary>
        /// 影线线宽
        /// </summary>
        public float WickWidth { get; set; } = 1f;

        /// <summary>
        /// 实体最小宽度（像素）
        /// </summary>
        public float MinBodyWidth { get; set; } = 1f;

        /// <summary>
        /// 实体最大宽度（像素）
        /// </summary>
        public float MaxBodyWidth { get; set; } = 30f;

        /// <summary>
        /// 实体宽度相对于“bar槽位宽度”的比例（0~1）
        /// 例如 0.7 表示蜡烛实体占整个槽位宽度的 70%
        /// </summary>
        public float BodyWidthRatio { get; set; } = 0.7f;
    }
}
