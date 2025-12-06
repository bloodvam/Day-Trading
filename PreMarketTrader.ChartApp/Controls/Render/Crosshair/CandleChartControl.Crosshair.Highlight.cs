using System.Drawing;
using System.Linq;
using PreMarketTrader.Models;

namespace PreMarketTrader.ChartApp.Controls
{
    public partial class CandleChartControl
    {
        private void HighlightCandle(Graphics g,
            Bar5s bar,
            int index,
            RectangleF candleRect,
            System.Collections.Generic.List<Bar5s> visibleBars)
        {
            float barWidth = candleRect.Width / visibleBars.Count;
            float x = candleRect.Left + index * barWidth;

            // 坐标映射
            float max = (float)visibleBars.Max(b => b.High);
            float min = (float)visibleBars.Min(b => b.Low);
            float range = max - min;

            float yHigh = candleRect.Top + (float)((max - bar.High) / range * candleRect.Height);
            float yLow = candleRect.Top + (float)((max - bar.Low) / range * candleRect.Height);
            float yOpen = candleRect.Top + (float)((max - bar.Open) / range * candleRect.Height);
            float yClose = candleRect.Top + (float)((max - bar.Close) / range * candleRect.Height);

            float bodyWidth = barWidth * Style.CandleBodyWidthRatio;
            float bodyX = x + (barWidth - bodyWidth) / 2f;

            float bodyY = Math.Min(yOpen, yClose);
            float bodyHeight = Math.Abs(yOpen - yClose);

            using Brush hl = new SolidBrush(Style.HighlightColor);

            // 影线高亮
            g.FillRectangle(hl, x + barWidth / 2f - 1, yHigh, 2, yLow - yHigh);

            // 实体高亮
            g.FillRectangle(hl, bodyX, bodyY, bodyWidth, Math.Max(1f, bodyHeight));
        }


        private void HighlightVolume(Graphics g,
            int index,
            List<Bar5s> bars,
            RectangleF volumeRect)
        {
            if (index < 0 || index >= bars.Count)
                return;

            float barWidth = volumeRect.Width / bars.Count;
            float x = volumeRect.Left + index * barWidth;

            long maxVolume = bars.Max(b => b.Volume);
            if (maxVolume <= 0) return;

            // 当前柱子的高度
            float volRatio = bars[index].Volume / (float)maxVolume;
            float volHeight = volRatio * volumeRect.Height;

            // Volume 柱子的绘制区域
            float vx = x + barWidth * 0.2f;
            float vw = barWidth * 0.6f;
            float vy = volumeRect.Bottom - volHeight;

            // 只高亮柱子本体，不画白色矩形
            using Brush hl = new SolidBrush(Color.FromArgb(120, 255, 255, 255));

            g.FillRectangle(hl, vx, vy, vw, volHeight);
        }


    }
}
