using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using PreMarketTrader.Models;

namespace PreMarketTrader.ChartApp.Controls
{
    public partial class CandleChartControl
    {
        /// <summary>
        /// 绘制网格线（水平 + 垂直）
        /// 使用 ChartStyle 配置（颜色 + 数量）
        /// </summary>
        private void DrawGrid(Graphics g, List<Bar5s> visibleBars, RectangleF candleRect)
        {
            if (visibleBars.Count == 0)
                return;

            float width = candleRect.Width;
            float height = candleRect.Height;

            int hCount = Style.GridHorizontalCount;
            int vCount = Style.GridVerticalCount;

            using Pen pen = new Pen(Style.GridLineColor, 1);

            // ======================================================
            // 1) 水平网格线（价格方向）
            // ======================================================
            for (int i = 0; i <= hCount; i++)
            {
                float y = candleRect.Top + height * (i / (float)hCount);
                g.DrawLine(pen, candleRect.Left, y, candleRect.Right, y);
            }

            // ======================================================
            // 2) 垂直网格线（时间方向）
            // ======================================================
            int count = visibleBars.Count;
            float barWidth = width / count;

            // 自动选择步长，使得网格线视觉密度固定
            int step = System.Math.Max(1, count / vCount);

            for (int i = 0; i < count; i += step)
            {
                float x = candleRect.Left + i * barWidth + barWidth / 2;
                g.DrawLine(pen, x, candleRect.Top, x, candleRect.Bottom);
            }
        }
    }
}
