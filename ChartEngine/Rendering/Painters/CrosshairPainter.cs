// ChartEngine/Rendering/Painters/CrosshairPainter.cs
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using ChartEngine.Rendering;
using ChartEngine.Data.Models;
using ChartEngine.Styles.Core;

namespace ChartEngine.Rendering.Painters
{
    /// <summary>
    /// 十字光标绘制器
    /// </summary>
    public class CrosshairPainter
    {
        private readonly RenderResourcePool _resourcePool;  // 🔥 新增

        // 🔥 新增构造函数
        public CrosshairPainter(RenderResourcePool resourcePool = null)
        {
            _resourcePool = resourcePool ?? new RenderResourcePool();
        }

        public void Render(
            Graphics g,
            ChartRenderContext ctx,
            CrosshairStyle style,
            Point mousePosition,
            int snappedBarIndex)
        {
            if (snappedBarIndex < 0 || snappedBarIndex >= ctx.Series.Count)
                return;

            float crosshairX = ctx.Transform.IndexToX(snappedBarIndex, ctx.PriceArea);
            float crosshairY = mousePosition.Y;

            if (crosshairY < ctx.PriceArea.Top)
                crosshairY = ctx.PriceArea.Top;
            if (crosshairY > ctx.PriceArea.Bottom)
                crosshairY = ctx.PriceArea.Bottom;

            DrawCrosshairLines(g, ctx, style, crosshairX, crosshairY);
            DrawPriceBubble(g, ctx, style, crosshairY);
            DrawTimeBubble(g, ctx, style, snappedBarIndex, crosshairX);
        }

        private void DrawCrosshairLines(
            Graphics g,
            ChartRenderContext ctx,
            CrosshairStyle style,
            float crosshairX,
            float crosshairY)
        {
            var priceArea = ctx.PriceArea;
            var volumeArea = ctx.VolumeArea;

            // 🔥 使用 ResourcePool
            var pen = _resourcePool.GetStyledPen(
                style.LineColor,
                style.LineWidth,
                style.LineStyle);

            int top = Math.Min(priceArea.Top, volumeArea.Top);
            int bottom = Math.Max(priceArea.Bottom, volumeArea.Bottom);
            g.DrawLine(pen, priceArea.Left, crosshairY, priceArea.Right, crosshairY);
            g.DrawLine(pen, crosshairX, top, crosshairX, bottom);
        }

        private void DrawPriceBubble(
            Graphics g,
            ChartRenderContext ctx,
            CrosshairStyle style,
            float crosshairY)
        {
            double price = ctx.Transform.YToPrice(crosshairY, ctx.PriceArea);
            string priceText = FormatPrice(price);

            var textSize = g.MeasureString(priceText, style.PriceBubbleFont);

            int bubbleX = ctx.PriceArea.Right;
            int bubbleWidth = 70;
            int bubbleHeight = (int)textSize.Height + style.PriceBubblePadding * 2;
            int bubbleY = (int)(crosshairY - bubbleHeight / 2);

            Rectangle bubbleRect = new Rectangle(bubbleX, bubbleY, bubbleWidth, bubbleHeight);

            // 🔥 使用 ResourcePool
            var brush = _resourcePool.GetBrush(style.PriceBubbleBackColor);
            g.FillRectangle(brush, bubbleRect);

            var pen = _resourcePool.GetPen(style.PriceBubbleBorderColor, 1f);
            g.DrawRectangle(pen, bubbleRect);

            var textBrush = _resourcePool.GetBrush(style.PriceBubbleTextColor);
            float textX = bubbleRect.Left + (bubbleRect.Width - textSize.Width) / 2;
            float textY = bubbleRect.Top + style.PriceBubblePadding;
            g.DrawString(priceText, style.PriceBubbleFont, textBrush, textX, textY);
        }

        private void DrawTimeBubble(
            Graphics g,
            ChartRenderContext ctx,
            CrosshairStyle style,
            int barIndex,
            float crosshairX)
        {
            var bar = ctx.Series.Bars[barIndex];
            string timeText = FormatTime(bar.Timestamp);

            var textSize = g.MeasureString(timeText, style.TimeBubbleFont);

            int bubbleWidth = (int)textSize.Width + style.TimeBubblePadding * 2;
            int bubbleHeight = 25;
            int bubbleX = (int)(crosshairX - bubbleWidth / 2);
            int bubbleY = ctx.VolumeArea.Bottom;

            if (bubbleX < ctx.PriceArea.Left)
                bubbleX = ctx.PriceArea.Left;
            if (bubbleX + bubbleWidth > ctx.PriceArea.Right)
                bubbleX = ctx.PriceArea.Right - bubbleWidth;

            Rectangle bubbleRect = new Rectangle(bubbleX, bubbleY, bubbleWidth, bubbleHeight);

            // 🔥 使用 ResourcePool
            var brush = _resourcePool.GetBrush(style.TimeBubbleBackColor);
            g.FillRectangle(brush, bubbleRect);

            var pen = _resourcePool.GetPen(style.TimeBubbleBorderColor, 1f);
            g.DrawRectangle(pen, bubbleRect);

            var textBrush = _resourcePool.GetBrush(style.TimeBubbleTextColor);
            float textX = bubbleRect.Left + style.TimeBubblePadding;
            float textY = bubbleRect.Top + (bubbleRect.Height - textSize.Height) / 2;
            g.DrawString(timeText, style.TimeBubbleFont, textBrush, textX, textY);
        }

        private string FormatPrice(double price)
        {
            if (price >= 1.0)
                return price.ToString("F3");
            else
                return price.ToString("F4");
        }

        private string FormatTime(DateTime timestamp)
        {
            DateTime today = DateTime.Today;

            if (timestamp.Date == today)
            {
                return timestamp.ToString("HH:mm");
            }
            else
            {
                return timestamp.ToString("MM/dd HH:mm");
            }
        }
    }
}