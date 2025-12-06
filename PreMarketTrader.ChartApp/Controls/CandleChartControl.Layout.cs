using System.Drawing;

namespace PreMarketTrader.ChartApp.Controls
{
    public partial class CandleChartControl
    {
        // ====== 图表布局参数（统一管理）======

        // 未来你想调整坐标轴大小，只改这里即可
        private const int Layout_PriceAxisWidth = 48;
        private const int Layout_TimeAxisHeight = 24;

        // 未来可以加入顶部指标区（如 MA、VWAP 标题区）
        private const int Layout_TopPadding = 0;

        // 左侧留白（以后可能放 Volume Axis）
        private const int Layout_LeftPadding = 0;


        // ====== 返回统一的布局区域定义 ======

        /// <summary>
        /// 整个图表的内容区域（除去右侧价格轴 + 底部时间轴）
        /// </summary>
        public RectangleF GetContentRect()
        {
            float x = Layout_LeftPadding;
            float y = Layout_TopPadding;

            float width = Width - Layout_LeftPadding - Layout_PriceAxisWidth;
            float height = Height - Layout_TopPadding - Layout_TimeAxisHeight;

            if (width < 0) width = 0;
            if (height < 0) height = 0;

            return new RectangleF(x, y, width, height);
        }

        /// <summary>
        /// K 线主图区域（内容区域的上半部分）
        /// </summary>
        public RectangleF GetCandleAreaRect(float volumeRatio)
        {
            RectangleF content = GetContentRect();

            float candleHeight = content.Height * (1f - volumeRatio);

            return new RectangleF(
                content.X,
                content.Y,
                content.Width,
                candleHeight
            );
        }

        /// <summary>
        /// 成交量区域（内容区域的下半部分）
        /// </summary>
        public RectangleF GetVolumeAreaRect(float volumeRatio)
        {
            RectangleF content = GetContentRect();

            float candleHeight = content.Height * (1f - volumeRatio);
            float volumeHeight = content.Height - candleHeight;

            return new RectangleF(
                content.X,
                content.Y + candleHeight,
                content.Width,
                volumeHeight
            );
        }

        /// <summary>
        /// 右侧价格轴的区域
        /// </summary>
        public RectangleF GetPriceAxisRect()
        {
            return new RectangleF(
                Width - Layout_PriceAxisWidth,
                Layout_TopPadding,
                Layout_PriceAxisWidth,
                Height - Layout_TimeAxisHeight - Layout_TopPadding
            );
        }

        /// <summary>
        /// 底部时间轴区域
        /// </summary>
        public RectangleF GetTimeAxisRect()
        {
            return new RectangleF(
                Layout_LeftPadding,
                Height - Layout_TimeAxisHeight,
                Width - Layout_LeftPadding - Layout_PriceAxisWidth,
                Layout_TimeAxisHeight
            );
        }
    }
}
