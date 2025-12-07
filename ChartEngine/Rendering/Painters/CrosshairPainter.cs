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
    /// 负责绘制十字线、价格气泡和时间气泡
    /// </summary>
    public class CrosshairPainter
    {
        /// <summary>
        /// 绘制十字光标
        /// </summary>
        public void Render(
            Graphics g,
            ChartRenderContext ctx,
            CrosshairStyle style,
            Point mousePosition,
            int snappedBarIndex)
        {
            if (snappedBarIndex < 0 || snappedBarIndex >= ctx.Series.Count)
                return;

            // 计算十字线的X坐标 (吸附到K线中心)
            float crosshairX = ctx.Transform.IndexToX(snappedBarIndex, ctx.PriceArea);

            // 十字线的Y坐标 (跟随鼠标)
            float crosshairY = mousePosition.Y;

            // 确保Y坐标在主图区域内
            if (crosshairY < ctx.PriceArea.Top)
                crosshairY = ctx.PriceArea.Top;
            if (crosshairY > ctx.PriceArea.Bottom)
                crosshairY = ctx.PriceArea.Bottom;

            // 1. 绘制十字线
            DrawCrosshairLines(g, ctx, style, crosshairX, crosshairY);

            // 2. 绘制价格气泡
            DrawPriceBubble(g, ctx, style, crosshairY);

            // 3. 绘制时间气泡
            DrawTimeBubble(g, ctx, style, snappedBarIndex, crosshairX);
        }

        /// <summary>
        /// 绘制十字线
        /// </summary>
        private void DrawCrosshairLines(
            Graphics g,
            ChartRenderContext ctx,
            CrosshairStyle style,
            float crosshairX,
            float crosshairY)
        {
            var priceArea = ctx.PriceArea;
            var volumeArea = ctx.VolumeArea;

            using (var pen = new Pen(style.LineColor, style.LineWidth))
            {
                pen.DashStyle = style.LineStyle;

                // 横线 (贯穿主图和成交量区)
                int top = Math.Min(priceArea.Top, volumeArea.Top);
                int bottom = Math.Max(priceArea.Bottom, volumeArea.Bottom);
                g.DrawLine(pen, priceArea.Left, crosshairY, priceArea.Right, crosshairY);

                // 竖线 (贯穿主图和成交量区)
                g.DrawLine(pen, crosshairX, top, crosshairX, bottom);
            }
        }

        /// <summary>
        /// 绘制价格气泡 (右侧)
        /// </summary>
        private void DrawPriceBubble(
            Graphics g,
            ChartRenderContext ctx,
            CrosshairStyle style,
            float crosshairY)
        {
            // 将Y坐标转换为价格
            double price = ctx.Transform.YToPrice(crosshairY, ctx.PriceArea);

            // 根据价格值决定精度
            string priceText = FormatPrice(price);

            // 测量文字尺寸
            var textSize = g.MeasureString(priceText, style.PriceBubbleFont);

            // 计算气泡位置 (在价格轴区域内,右侧)
            int bubbleX = ctx.PriceArea.Right;
            int bubbleWidth = 70; // 价格轴宽度
            int bubbleHeight = (int)textSize.Height + style.PriceBubblePadding * 2;
            int bubbleY = (int)(crosshairY - bubbleHeight / 2);

            Rectangle bubbleRect = new Rectangle(
                bubbleX,
                bubbleY,
                bubbleWidth,
                bubbleHeight
            );

            // 绘制背景
            using (var brush = new SolidBrush(style.PriceBubbleBackColor))
            {
                g.FillRectangle(brush, bubbleRect);
            }

            // 绘制边框
            using (var pen = new Pen(style.PriceBubbleBorderColor, 1f))
            {
                g.DrawRectangle(pen, bubbleRect);
            }

            // 绘制价格文字 (居中)
            using (var textBrush = new SolidBrush(style.PriceBubbleTextColor))
            {
                float textX = bubbleRect.Left + (bubbleRect.Width - textSize.Width) / 2;
                float textY = bubbleRect.Top + style.PriceBubblePadding;
                g.DrawString(priceText, style.PriceBubbleFont, textBrush, textX, textY);
            }
        }

        /// <summary>
        /// 绘制时间气泡 (底部)
        /// </summary>
        private void DrawTimeBubble(
            Graphics g,
            ChartRenderContext ctx,
            CrosshairStyle style,
            int barIndex,
            float crosshairX)
        {
            var bar = ctx.Series.Bars[barIndex];

            // 格式化时间 (需要从外部传入 TimeFrame)
            string timeText = FormatTime(bar.Timestamp);

            // 测量文字尺寸
            var textSize = g.MeasureString(timeText, style.TimeBubbleFont);

            // 计算气泡位置 (在时间轴区域内)
            int bubbleWidth = (int)textSize.Width + style.TimeBubblePadding * 2;
            int bubbleHeight = 25; // 时间轴高度
            int bubbleX = (int)(crosshairX - bubbleWidth / 2);
            int bubbleY = ctx.VolumeArea.Bottom;

            // 确保不超出边界
            if (bubbleX < ctx.PriceArea.Left)
                bubbleX = ctx.PriceArea.Left;
            if (bubbleX + bubbleWidth > ctx.PriceArea.Right)
                bubbleX = ctx.PriceArea.Right - bubbleWidth;

            Rectangle bubbleRect = new Rectangle(
                bubbleX,
                bubbleY,
                bubbleWidth,
                bubbleHeight
            );

            // 绘制背景
            using (var brush = new SolidBrush(style.TimeBubbleBackColor))
            {
                g.FillRectangle(brush, bubbleRect);
            }

            // 绘制边框
            using (var pen = new Pen(style.TimeBubbleBorderColor, 1f))
            {
                g.DrawRectangle(pen, bubbleRect);
            }

            // 绘制时间文字 (居中)
            using (var textBrush = new SolidBrush(style.TimeBubbleTextColor))
            {
                float textX = bubbleRect.Left + style.TimeBubblePadding;
                float textY = bubbleRect.Top + (bubbleRect.Height - textSize.Height) / 2;
                g.DrawString(timeText, style.TimeBubbleFont, textBrush, textX, textY);
            }
        }

        /// <summary>
        /// 格式化价格 (根据价格值决定精度)
        /// </summary>
        private string FormatPrice(double price)
        {
            if (price >= 1.0)
                return price.ToString("F3");  // >= 1: 3位小数
            else
                return price.ToString("F4");  // < 1: 4位小数
        }

        /// <summary>
        /// 格式化时间 (简化版,实际应根据 TimeFrame 调整)
        /// </summary>
        private string FormatTime(DateTime timestamp)
        {
            DateTime today = DateTime.Today;

            if (timestamp.Date == today)
            {
                // 当天: 只显示时间
                return timestamp.ToString("HH:mm");
            }
            else
            {
                // 非当天: 显示日期+时间
                return timestamp.ToString("MM/dd HH:mm");
            }
        }
    }
}