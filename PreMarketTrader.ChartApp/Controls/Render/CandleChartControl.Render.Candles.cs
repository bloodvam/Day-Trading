using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using PreMarketTrader.Models;

namespace PreMarketTrader.ChartApp.Controls
{
    public partial class CandleChartControl
    {
        /// <summary>
        /// 绘制 K 线（蜡烛图），使用 ChartStyle 提供的颜色与比例
        /// </summary>
        private void DrawCandles(Graphics g, List<Bar5s> visibleBars, RectangleF candleRect)
        {
            int count = visibleBars.Count;
            if (count == 0)
                return;

            float barWidth = System.Math.Max(2f, candleRect.Width / count);

            // 计算可视区域最高/最低
            double maxPrice = visibleBars.Max(b => b.High);
            double minPrice = visibleBars.Min(b => b.Low);
            double range = maxPrice - minPrice;
            if (range <= 0) range = 1e-6;

            foreach (var (bar, index) in visibleBars.Select((b, i) => (b, i)))
            {
                float x = candleRect.Left + index * barWidth;

                // 通过价格映射到 Y 坐标
                float yHigh = candleRect.Top + (float)((maxPrice - bar.High) / range * candleRect.Height);
                float yLow = candleRect.Top + (float)((maxPrice - bar.Low) / range * candleRect.Height);
                float yOpen = candleRect.Top + (float)((maxPrice - bar.Open) / range * candleRect.Height);
                float yClose = candleRect.Top + (float)((maxPrice - bar.Close) / range * candleRect.Height);

                bool isUp = bar.Close >= bar.Open;
                Color color = isUp ? Style.CandleUpColor : Style.CandleDownColor;

                using Pen pen = new Pen(color, 1);
                using Brush brush = new SolidBrush(color);

                // ===== 绘制影线（高低）=====
                float cx = x + barWidth / 2f;
                g.DrawLine(pen, cx, yHigh, cx, yLow);

                // ===== 绘制实体（开收）=====
                float bodyWidth = barWidth * Style.CandleBodyWidthRatio;
                float bodyX = x + (barWidth - bodyWidth) / 2f;

                float bodyY = System.Math.Min(yOpen, yClose);
                float bodyHeight = System.Math.Abs(yOpen - yClose);

                // 最小高度确保 >= 1 像素
                bodyHeight = System.Math.Max(1f, bodyHeight);

                g.FillRectangle(brush, bodyX, bodyY, bodyWidth, bodyHeight);
            }
        }
    }
}
