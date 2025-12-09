// ChartEngine/Rendering/Painters/PriceAxisPainter.cs
using System;
using System.Drawing;
using ChartEngine.Rendering;
using ChartEngine.Styles.Core;

namespace ChartEngine.Rendering.Painters
{
    /// <summary>
    /// 价格轴绘制器
    /// </summary>
    public class PriceAxisPainter
    {
        private readonly RenderResourcePool _resourcePool;  // 🔥 新增

        // 🔥 新增构造函数
        public PriceAxisPainter(RenderResourcePool resourcePool = null)
        {
            _resourcePool = resourcePool ?? new RenderResourcePool();
        }

        public void Render(ChartRenderContext ctx, AxisStyle style)
        {
            var g = ctx.Graphics;
            var priceArea = ctx.PriceArea;

            Rectangle axisRect = CalculateAxisRect(priceArea, style);

            DrawBackground(g, axisRect, style);
            DrawSeparatorLine(g, axisRect, style);

            if (style.ShowPriceTicks)
            {
                DrawPriceTicks(g, ctx, axisRect, style);
            }

            if (style.ShowCurrentPriceLabel)
            {
                DrawCurrentPriceLabel(g, ctx, axisRect, style);
            }
        }

        private Rectangle CalculateAxisRect(Rectangle priceArea, AxisStyle style)
        {
            if (style.PriceAxisPosition == PriceAxisPosition.Right)
            {
                return new Rectangle(
                    priceArea.Right,
                    priceArea.Top,
                    style.PriceAxisWidth,
                    priceArea.Height
                );
            }
            else
            {
                return new Rectangle(
                    priceArea.Left - style.PriceAxisWidth,
                    priceArea.Top,
                    style.PriceAxisWidth,
                    priceArea.Height
                );
            }
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

            if (style.PriceAxisPosition == PriceAxisPosition.Right)
            {
                g.DrawLine(pen, axisRect.Left, axisRect.Top, axisRect.Left, axisRect.Bottom);
            }
            else
            {
                g.DrawLine(pen, axisRect.Right, axisRect.Top, axisRect.Right, axisRect.Bottom);
            }
        }

        private void DrawPriceTicks(Graphics g, ChartRenderContext ctx, Rectangle axisRect, AxisStyle style)
        {
            var priceRange = ctx.PriceRange;
            if (!priceRange.IsValid)
                return;

            double minPrice = priceRange.MinPrice;
            double maxPrice = priceRange.MaxPrice;
            double range = maxPrice - minPrice;

            double interval = CalculateSmartInterval(range);
            double startPrice = Math.Floor(minPrice / interval) * interval;

            // 🔥 使用 ResourcePool
            var tickPen = _resourcePool.GetPen(style.TickLineColor, 1f);
            var textBrush = _resourcePool.GetBrush(style.LabelTextColor);

            for (double price = startPrice; price <= maxPrice; price += interval)
            {
                if (price < minPrice)
                    continue;

                float y = ctx.Transform.PriceToY(price, ctx.PriceArea);

                if (y < ctx.PriceArea.Top || y > ctx.PriceArea.Bottom)
                    continue;

                if (style.PriceAxisPosition == PriceAxisPosition.Right)
                {
                    g.DrawLine(tickPen, axisRect.Left, y, axisRect.Left + style.TickLength, y);
                }
                else
                {
                    g.DrawLine(tickPen, axisRect.Right - style.TickLength, y, axisRect.Right, y);
                }

                string priceText = price.ToString($"F{style.PriceDecimals}");
                var textSize = g.MeasureString(priceText, style.LabelFont);

                float textX;
                if (style.PriceAxisPosition == PriceAxisPosition.Right)
                {
                    textX = axisRect.Left + style.TickLength + style.LabelPadding;
                }
                else
                {
                    textX = axisRect.Right - style.TickLength - style.LabelPadding - textSize.Width;
                }

                float textY = y - textSize.Height / 2;

                g.DrawString(priceText, style.LabelFont, textBrush, textX, textY);
            }
        }

        private void DrawCurrentPriceLabel(Graphics g, ChartRenderContext ctx, Rectangle axisRect, AxisStyle style)
        {
            if (ctx.Series == null || ctx.Series.Count == 0)
                return;

            var lastBar = ctx.Series.Bars[ctx.Series.Count - 1];
            double currentPrice = lastBar.Close;

            bool isUp = ctx.Series.Count > 1
                ? lastBar.Close >= ctx.Series.Bars[ctx.Series.Count - 2].Close
                : lastBar.Close >= lastBar.Open;

            Color backColor = isUp ? style.CurrentPriceUpBackColor : style.CurrentPriceDownBackColor;

            float y = ctx.Transform.PriceToY(currentPrice, ctx.PriceArea);

            string priceText = currentPrice.ToString($"F{style.PriceDecimals}");
            var textSize = g.MeasureString(priceText, style.CurrentPriceFont);

            float labelHeight = textSize.Height + style.LabelVerticalPadding * 2;
            float labelY = y - labelHeight / 2;

            Rectangle labelRect;
            if (style.PriceAxisPosition == PriceAxisPosition.Right)
            {
                labelRect = new Rectangle(
                    axisRect.Left,
                    (int)labelY,
                    axisRect.Width,
                    (int)labelHeight
                );
            }
            else
            {
                labelRect = new Rectangle(
                    axisRect.Left,
                    (int)labelY,
                    axisRect.Width,
                    (int)labelHeight
                );
            }

            // 🔥 使用 ResourcePool
            var brush = _resourcePool.GetBrush(backColor);
            g.FillRectangle(brush, labelRect);

            var pen = _resourcePool.GetPen(backColor, 1f);
            g.DrawRectangle(pen, labelRect);

            var textBrush = _resourcePool.GetBrush(style.CurrentPriceTextColor);
            float textX = labelRect.Left + (labelRect.Width - textSize.Width) / 2;
            float textY = labelRect.Top + style.LabelVerticalPadding;
            g.DrawString(priceText, style.CurrentPriceFont, textBrush, textX, textY);

            if (style.PriceAxisPosition == PriceAxisPosition.Right)
            {
                PointF[] triangle = {
                    new PointF(axisRect.Left, y),
                    new PointF(axisRect.Left - 5, y - 5),
                    new PointF(axisRect.Left - 5, y + 5)
                };

                g.FillPolygon(brush, triangle);
            }
        }

        private double CalculateSmartInterval(double range)
        {
            if (range <= 0)
                return 1;

            double magnitude = Math.Pow(10, Math.Floor(Math.Log10(range)));
            double normalized = range / magnitude;

            double multiplier;
            if (normalized <= 1.5)
                multiplier = 0.2;
            else if (normalized <= 3)
                multiplier = 0.5;
            else if (normalized <= 7)
                multiplier = 1;
            else
                multiplier = 2;

            double interval = multiplier * magnitude;

            int estimatedLines = (int)(range / interval);
            if (estimatedLines < 4)
                interval /= 2;
            if (estimatedLines > 15)
                interval *= 2;

            return interval;
        }
    }
}