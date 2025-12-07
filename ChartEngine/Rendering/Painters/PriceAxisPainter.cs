using System;
using System.Drawing;
using ChartEngine.Rendering;
using ChartEngine.Styles.Core;

namespace ChartEngine.Rendering.Painters
{
    /// <summary>
    /// 价格轴绘制器
    /// 负责绘制价格刻度、标签和当前价格标签
    /// </summary>
    public class PriceAxisPainter
    {
        /// <summary>
        /// 渲染价格轴
        /// </summary>
        public void Render(ChartRenderContext ctx, AxisStyle style)
        {
            var g = ctx.Graphics;
            var priceArea = ctx.PriceArea;

            // 1. 计算价格轴区域
            Rectangle axisRect = CalculateAxisRect(priceArea, style);

            // 2. 绘制背景
            DrawBackground(g, axisRect, style);

            // 3. 绘制分隔线
            DrawSeparatorLine(g, axisRect, style);

            // 4. 绘制价格刻度和标签
            if (style.ShowPriceTicks)
            {
                DrawPriceTicks(g, ctx, axisRect, style);
            }

            // 5. 绘制当前价格标签 (最上层)
            if (style.ShowCurrentPriceLabel)
            {
                DrawCurrentPriceLabel(g, ctx, axisRect, style);
            }
        }

        /// <summary>
        /// 计算价格轴区域
        /// </summary>
        private Rectangle CalculateAxisRect(Rectangle priceArea, AxisStyle style)
        {
            if (style.PriceAxisPosition == PriceAxisPosition.Right)
            {
                // 右侧价格轴
                return new Rectangle(
                    priceArea.Right,
                    priceArea.Top,
                    style.PriceAxisWidth,
                    priceArea.Height
                );
            }
            else
            {
                // 左侧价格轴
                return new Rectangle(
                    priceArea.Left - style.PriceAxisWidth,
                    priceArea.Top,
                    style.PriceAxisWidth,
                    priceArea.Height
                );
            }
        }

        /// <summary>
        /// 绘制背景
        /// </summary>
        private void DrawBackground(Graphics g, Rectangle axisRect, AxisStyle style)
        {
            using (var brush = new SolidBrush(style.BackgroundColor))
            {
                g.FillRectangle(brush, axisRect);
            }
        }

        /// <summary>
        /// 绘制分隔线 (主图和价格轴之间的竖线)
        /// </summary>
        private void DrawSeparatorLine(Graphics g, Rectangle axisRect, AxisStyle style)
        {
            using (var pen = new Pen(style.AxisLineColor, 1f))
            {
                if (style.PriceAxisPosition == PriceAxisPosition.Right)
                {
                    // 画左边界线
                    g.DrawLine(pen, axisRect.Left, axisRect.Top, axisRect.Left, axisRect.Bottom);
                }
                else
                {
                    // 画右边界线
                    g.DrawLine(pen, axisRect.Right, axisRect.Top, axisRect.Right, axisRect.Bottom);
                }
            }
        }

        /// <summary>
        /// 绘制价格刻度和标签
        /// </summary>
        private void DrawPriceTicks(Graphics g, ChartRenderContext ctx, Rectangle axisRect, AxisStyle style)
        {
            var priceRange = ctx.PriceRange;
            if (!priceRange.IsValid)
                return;

            double minPrice = priceRange.MinPrice;
            double maxPrice = priceRange.MaxPrice;
            double range = maxPrice - minPrice;

            // 计算智能间距 (复用 GridLayer 的算法)
            double interval = CalculateSmartInterval(range);

            // 计算起始价格
            double startPrice = Math.Floor(minPrice / interval) * interval;

            using (var tickPen = new Pen(style.TickLineColor, 1f))
            using (var textBrush = new SolidBrush(style.LabelTextColor))
            {
                for (double price = startPrice; price <= maxPrice; price += interval)
                {
                    if (price < minPrice)
                        continue;

                    // 计算 Y 坐标
                    float y = ctx.Transform.PriceToY(price, ctx.PriceArea);

                    // 确保在可见区域内
                    if (y < ctx.PriceArea.Top || y > ctx.PriceArea.Bottom)
                        continue;

                    // 绘制刻度线
                    if (style.PriceAxisPosition == PriceAxisPosition.Right)
                    {
                        g.DrawLine(tickPen, axisRect.Left, y, axisRect.Left + style.TickLength, y);
                    }
                    else
                    {
                        g.DrawLine(tickPen, axisRect.Right - style.TickLength, y, axisRect.Right, y);
                    }

                    // 绘制价格标签
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
        }

        /// <summary>
        /// 绘制当前价格标签 (红色或绿色标签)
        /// </summary>
        private void DrawCurrentPriceLabel(Graphics g, ChartRenderContext ctx, Rectangle axisRect, AxisStyle style)
        {
            if (ctx.Series == null || ctx.Series.Count == 0)
                return;

            // 获取最新K线
            var lastBar = ctx.Series.Bars[ctx.Series.Count - 1];
            double currentPrice = lastBar.Close;

            // 判断涨跌
            bool isUp = ctx.Series.Count > 1
                ? lastBar.Close >= ctx.Series.Bars[ctx.Series.Count - 2].Close
                : lastBar.Close >= lastBar.Open;

            // 选择背景色
            Color backColor = isUp ? style.CurrentPriceUpBackColor : style.CurrentPriceDownBackColor;

            // 计算 Y 坐标
            float y = ctx.Transform.PriceToY(currentPrice, ctx.PriceArea);

            // 价格文本
            string priceText = currentPrice.ToString($"F{style.PriceDecimals}");
            var textSize = g.MeasureString(priceText, style.CurrentPriceFont);

            // 计算标签矩形
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

            // 绘制背景
            using (var brush = new SolidBrush(backColor))
            {
                g.FillRectangle(brush, labelRect);
            }

            // 绘制边框
            using (var pen = new Pen(backColor, 1f))
            {
                g.DrawRectangle(pen, labelRect);
            }

            // 绘制价格文本 (居中)
            using (var textBrush = new SolidBrush(style.CurrentPriceTextColor))
            {
                float textX = labelRect.Left + (labelRect.Width - textSize.Width) / 2;
                float textY = labelRect.Top + style.LabelVerticalPadding;
                g.DrawString(priceText, style.CurrentPriceFont, textBrush, textX, textY);
            }

            // 绘制指向线 (从标签指向主图)
            using (var pen = new Pen(backColor, 1f))
            {
                if (style.PriceAxisPosition == PriceAxisPosition.Right)
                {
                    // 画左侧三角形指示器
                    PointF[] triangle = {
                        new PointF(axisRect.Left, y),
                        new PointF(axisRect.Left - 5, y - 5),
                        new PointF(axisRect.Left - 5, y + 5)
                    };
                    using (var brush = new SolidBrush(backColor))
                    {
                        g.FillPolygon(brush, triangle);
                    }
                }
            }
        }

        /// <summary>
        /// 智能计算价格间距 (复用 GridLayer 的算法)
        /// </summary>
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