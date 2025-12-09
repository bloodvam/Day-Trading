// ChartEngine/Rendering/Painters/TimeAxisPainter.cs
using System;
using System.Drawing;
using ChartEngine.Rendering;
using ChartEngine.Styles.Core;

namespace ChartEngine.Rendering.Painters
{
    /// <summary>
    /// 时间轴绘制器
    /// </summary>
    public class TimeAxisPainter
    {
        private readonly RenderResourcePool _resourcePool;  // 🔥 新增

        // 🔥 新增构造函数
        public TimeAxisPainter(RenderResourcePool resourcePool = null)
        {
            _resourcePool = resourcePool ?? new RenderResourcePool();
        }

        public void Render(ChartRenderContext ctx, AxisStyle style)
        {
            var g = ctx.Graphics;
            var priceArea = ctx.PriceArea;
            var volumeArea = ctx.VolumeArea;

            Rectangle axisRect = CalculateAxisRect(priceArea, volumeArea, style);

            DrawBackground(g, axisRect, style);
            DrawSeparatorLine(g, axisRect, style);
            DrawTimeTicks(g, ctx, axisRect, style);
        }

        private Rectangle CalculateAxisRect(Rectangle priceArea, Rectangle volumeArea, AxisStyle style)
        {
            int top = volumeArea.Bottom;

            return new Rectangle(
                priceArea.Left,
                top,
                priceArea.Width,
                style.TimeAxisHeight
            );
        }

        private void DrawBackground(Graphics g, Rectangle axisRect, AxisStyle style)
        {
            // 🔥 使用 ResourcePool
            var brush = _resourcePool.GetBrush(style.BackgroundColor);
            g.FillRectangle(brush, axisRect);
        }

        private void DrawSeparatorLine(Graphics g, Rectangle axisRect, AxisStyle style)
        {
            // 🔥 使用 ResourcePool
            var pen = _resourcePool.GetPen(style.AxisLineColor, 1f);
            g.DrawLine(pen, axisRect.Left, axisRect.Top, axisRect.Right, axisRect.Top);
        }

        private void DrawTimeTicks(Graphics g, ChartRenderContext ctx, Rectangle axisRect, AxisStyle style)
        {
            var visibleRange = ctx.VisibleRange;
            if (visibleRange.Count <= 0)
                return;

            int barInterval = CalculateBarInterval(visibleRange.Count);

            int startIndex = (visibleRange.StartIndex / barInterval) * barInterval;
            if (startIndex < visibleRange.StartIndex)
                startIndex += barInterval;

            // 🔥 使用 ResourcePool
            var tickPen = _resourcePool.GetPen(style.TickLineColor, 1f);
            var textBrush = _resourcePool.GetBrush(style.LabelTextColor);

            for (int index = startIndex; index <= visibleRange.EndIndex; index += barInterval)
            {
                float x = ctx.Transform.IndexToX(index, ctx.PriceArea);

                if (x < axisRect.Left || x > axisRect.Right)
                    continue;

                g.DrawLine(tickPen, x, axisRect.Top, x, axisRect.Top + style.TickLength);

                string timeText = GenerateTimeLabel(index, style.TimeFormat);
                var textSize = g.MeasureString(timeText, style.LabelFont);

                float textX = x - textSize.Width / 2;
                float textY = axisRect.Top + style.TickLength + style.LabelPadding;

                if (textX < axisRect.Left)
                    textX = axisRect.Left;
                if (textX + textSize.Width > axisRect.Right)
                    textX = axisRect.Right - textSize.Width;

                g.DrawString(timeText, style.LabelFont, textBrush, textX, textY);
            }
        }

        private int CalculateBarInterval(int visibleCount)
        {
            if (visibleCount <= 0)
                return 1;

            int targetLabels = 8;
            int baseInterval = Math.Max(1, visibleCount / targetLabels);

            int[] niceNumbers = { 1, 2, 5, 10, 20, 50, 100, 200, 500, 1000 };

            foreach (int num in niceNumbers)
            {
                if (num >= baseInterval)
                    return num;
            }

            return baseInterval;
        }

        private string GenerateTimeLabel(int barIndex, string timeFormat)
        {
            DateTime startTime = new DateTime(2024, 1, 1, 9, 30, 0);
            DateTime barTime = startTime.AddMinutes(barIndex);

            return barTime.ToString(timeFormat);
        }
    }
}