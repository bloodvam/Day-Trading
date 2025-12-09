// ChartEngine/Rendering/Painters/TooltipPainter.cs
using System;
using System.Drawing;
using ChartEngine.Rendering;
using ChartEngine.Data.Models;
using ChartEngine.Styles.Core;

namespace ChartEngine.Rendering.Painters
{
    public enum TooltipPosition
    {
        TopLeft,
        TopRight
    }

    /// <summary>
    /// Tooltip绘制器
    /// </summary>
    public class TooltipPainter
    {
        private readonly RenderResourcePool _resourcePool;  // 🔥 新增

        // 🔥 新增构造函数
        public TooltipPainter(RenderResourcePool resourcePool = null)
        {
            _resourcePool = resourcePool ?? new RenderResourcePool();
        }

        public void Render(
            Graphics g,
            ChartRenderContext ctx,
            CrosshairStyle style,
            int barIndex,
            TooltipPosition position)
        {
            if (barIndex < 0 || barIndex >= ctx.Series.Count)
                return;

            var currentBar = ctx.Series.Bars[barIndex];
            IBar prevBar = barIndex > 0 ? ctx.Series.Bars[barIndex - 1] : currentBar;

            var lines = FormatTooltipContent(currentBar, prevBar);
            var tooltipRect = CalculateTooltipRect(g, ctx, style, lines, position);

            DrawBackground(g, tooltipRect, style);
            DrawContent(g, tooltipRect, style, lines, currentBar, prevBar);
        }

        private string[] FormatTooltipContent(IBar currentBar, IBar prevBar)
        {
            double change = currentBar.Close - prevBar.Close;
            double changePercent = prevBar.Close != 0 ? (change / prevBar.Close) * 100 : 0;

            return new string[]
            {
                $"时间: {currentBar.Timestamp:MM/dd HH:mm}",
                $"开盘: {currentBar.Open:F3}",
                $"最高: {currentBar.High:F3}",
                $"最低: {currentBar.Low:F3}",
                $"收盘: {currentBar.Close:F3}",
                $"涨跌额: {(change >= 0 ? "+" : "")}{change:F3}",
                $"涨跌幅: {(changePercent >= 0 ? "+" : "")}{changePercent:F2}%",
                $"成交量: {FormatVolume(currentBar.Volume)}",
                $"成交额: {FormatAmount(currentBar.Volume * currentBar.Close)}"
            };
        }

        private Rectangle CalculateTooltipRect(
            Graphics g,
            ChartRenderContext ctx,
            CrosshairStyle style,
            string[] lines,
            TooltipPosition position)
        {
            float maxWidth = 0;
            float totalHeight = style.TooltipPadding;

            foreach (var line in lines)
            {
                var size = g.MeasureString(line, style.TooltipFont);
                if (size.Width > maxWidth)
                    maxWidth = size.Width;
                totalHeight += size.Height + style.TooltipLineSpacing;
            }

            int width = Math.Max(style.TooltipWidth, (int)(maxWidth + style.TooltipPadding * 2));
            int height = (int)totalHeight + style.TooltipPadding;

            int x, y;
            int margin = 10;

            if (position == TooltipPosition.TopLeft)
            {
                x = ctx.PriceArea.Left + margin;
                y = ctx.PriceArea.Top + margin;
            }
            else
            {
                x = ctx.PriceArea.Right - width - margin;
                y = ctx.PriceArea.Top + margin;
            }

            return new Rectangle(x, y, width, height);
        }

        private void DrawBackground(Graphics g, Rectangle rect, CrosshairStyle style)
        {
            // 🔥 使用 ResourcePool
            var brush = _resourcePool.GetBrush(style.TooltipBackColor);
            g.FillRectangle(brush, rect);

            var pen = _resourcePool.GetPen(style.TooltipBorderColor, 1f);
            g.DrawRectangle(pen, rect);
        }

        private void DrawContent(
            Graphics g,
            Rectangle rect,
            CrosshairStyle style,
            string[] lines,
            IBar currentBar,
            IBar prevBar)
        {
            float x = rect.Left + style.TooltipPadding;
            float y = rect.Top + style.TooltipPadding;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                Color textColor = style.NormalColor;

                if (line.Contains("最高:"))
                {
                    textColor = style.HighColor;
                }
                else if (line.Contains("最低:"))
                {
                    textColor = style.LowColor;
                }
                else if (line.Contains("涨跌额:") || line.Contains("涨跌幅:"))
                {
                    double change = currentBar.Close - prevBar.Close;
                    textColor = change >= 0 ? style.UpColor : style.DownColor;
                }

                // 🔥 使用 ResourcePool
                var brush = _resourcePool.GetBrush(textColor);
                g.DrawString(line, style.TooltipFont, brush, x, y);

                var size = g.MeasureString(line, style.TooltipFont);
                y += size.Height + style.TooltipLineSpacing;
            }
        }

        private string FormatVolume(double volume)
        {
            if (volume >= 10000)
                return $"{volume / 10000:F2}万";
            else if (volume >= 1000)
                return $"{volume / 1000:F2}千";
            else
                return $"{volume:F0}";
        }

        private string FormatAmount(double amount)
        {
            if (amount >= 100000000)
                return $"{amount / 100000000:F2}亿";
            else if (amount >= 10000)
                return $"{amount / 10000:F2}万";
            else if (amount >= 1000)
                return $"{amount / 1000:F2}千";
            else
                return $"{amount:F0}";
        }

        public bool IsMouseOverTooltip(Point mousePos, Rectangle tooltipRect)
        {
            return tooltipRect.Contains(mousePos);
        }

        public Rectangle GetTooltipRect(
            Graphics g,
            ChartRenderContext ctx,
            CrosshairStyle style,
            int barIndex,
            TooltipPosition position)
        {
            if (barIndex < 0 || barIndex >= ctx.Series.Count)
                return Rectangle.Empty;

            var currentBar = ctx.Series.Bars[barIndex];
            var prevBar = barIndex > 0 ? ctx.Series.Bars[barIndex - 1] : currentBar;
            var lines = FormatTooltipContent(currentBar, prevBar);

            return CalculateTooltipRect(g, ctx, style, lines, position);
        }
    }
}