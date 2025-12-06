using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using PreMarketTrader.Models;

namespace PreMarketTrader.ChartApp.Controls
{
    public partial class CandleChartControl
    {
        /// <summary>
        /// 主入口（渲染价格轴 + 时间轴）
        /// </summary>
        private void DrawAxes(Graphics g, List<Bar5s> visibleBars, RectangleF candleRect)
        {
            if (visibleBars.Count == 0)
                return;

            DrawPriceAxis(g, visibleBars, candleRect);
            DrawTimeAxis(g, visibleBars);
        }

        // ============================================================
        // 🧭 右侧价格轴
        // ============================================================
        private void DrawPriceAxis(Graphics g, List<Bar5s> visibleBars, RectangleF candleRect)
        {
            RectangleF axis = GetPriceAxisRect();

            float maxPrice = (float)visibleBars.Max(b => b.High);
            float minPrice = (float)visibleBars.Min(b => b.Low);
            float range = maxPrice - minPrice;
            if (range <= 0) return;

            // 根据当前价格区间生成“漂亮的刻度”
            float step = GetNicePriceStep(range / Style.GridHorizontalCount);
            float start = (float)System.Math.Ceiling(minPrice / step) * step;

            using Pen pen = new Pen(Style.AxisLineColor, 1);
            using Brush textBrush = new SolidBrush(Style.AxisTextColor);

            for (float p = start; p <= maxPrice; p += step)
            {
                float y = candleRect.Top + (float)((maxPrice - p) / range * candleRect.Height);

                // 刻度线
                g.DrawLine(pen, axis.Left, y, axis.Left + 5, y);

                // 文字
                g.DrawString(
                    p.ToString("F2"),
                    Style.AxisFont,
                    textBrush,
                    axis.Left + 8,
                    y - Style.AxisFont.Size
                );
            }
        }

        /// <summary>
        /// 数字变漂亮的算法（0.1, 0.2, 0.5, 1, 2...）
        /// </summary>
        private float GetNicePriceStep(float rawStep)
        {
            float[] steps =
            {
                0.01f, 0.02f, 0.05f,
                0.1f, 0.2f, 0.5f,
                1f, 2f, 5f,
                10f, 20f, 50f, 100f
            };

            foreach (float s in steps)
                if (rawStep <= s) return s;

            return rawStep;
        }

        // ============================================================
        // 🕒 底部时间轴
        // ============================================================
        private void DrawTimeAxis(Graphics g, List<Bar5s> visibleBars)
        {
            RectangleF axis = GetTimeAxisRect();
            RectangleF content = GetContentRect();

            int count = visibleBars.Count;
            float barWidth = content.Width / count;

            // 每屏显示约 GridVerticalCount 个时间刻度
            int step = System.Math.Max(1, count / Style.GridVerticalCount);

            using Pen pen = new Pen(Style.AxisLineColor, 1);
            using Brush textBrush = new SolidBrush(Style.AxisTextColor);

            for (int i = 0; i < count; i += step)
            {
                float x = content.Left + i * barWidth + barWidth / 2;

                string label = visibleBars[i].StartTime.ToString("HH:mm:ss");

                SizeF size = g.MeasureString(label, Style.AxisFont);

                // 小刻度线
                g.DrawLine(pen, x, axis.Top, x, axis.Top + 4);

                // 时间刻度文字（居中）
                g.DrawString(
                    label,
                    Style.AxisFont,
                    textBrush,
                    x - size.Width / 2,
                    axis.Top + 4
                );
            }
        }
    }
}
