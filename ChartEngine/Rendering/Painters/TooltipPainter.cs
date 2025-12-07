using System;
using System.Drawing;
using ChartEngine.Rendering;
using ChartEngine.Styles;
using ChartEngine.Models;

namespace ChartEngine.Rendering.Painters
{
    /// <summary>
    /// Tooltip位置枚举
    /// </summary>
    public enum TooltipPosition
    {
        TopLeft,
        TopRight
    }

    /// <summary>
    /// Tooltip绘制器
    /// 负责绘制K线信息悬浮框,支持智能避让
    /// </summary>
    public class TooltipPainter
    {
        /// <summary>
        /// 绘制Tooltip
        /// </summary>
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

            // 获取前一根K线用于计算涨跌
            IBar prevBar = barIndex > 0 ? ctx.Series.Bars[barIndex - 1] : currentBar;

            // 格式化Tooltip内容
            var lines = FormatTooltipContent(currentBar, prevBar);

            // 计算Tooltip尺寸和位置
            var tooltipRect = CalculateTooltipRect(g, ctx, style, lines, position);

            // 绘制背景和边框
            DrawBackground(g, tooltipRect, style);

            // 绘制内容
            DrawContent(g, tooltipRect, style, lines, currentBar, prevBar);
        }

        /// <summary>
        /// 格式化Tooltip内容
        /// </summary>
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

        /// <summary>
        /// 计算Tooltip矩形区域
        /// </summary>
        private Rectangle CalculateTooltipRect(
            Graphics g,
            ChartRenderContext ctx,
            CrosshairStyle style,
            string[] lines,
            TooltipPosition position)
        {
            // 计算最大文字宽度和总高度
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
                // 左上角
                x = ctx.PriceArea.Left + margin;
                y = ctx.PriceArea.Top + margin;
            }
            else
            {
                // 右上角
                x = ctx.PriceArea.Right - width - margin;
                y = ctx.PriceArea.Top + margin;
            }

            return new Rectangle(x, y, width, height);
        }

        /// <summary>
        /// 绘制背景和边框
        /// </summary>
        private void DrawBackground(Graphics g, Rectangle rect, CrosshairStyle style)
        {
            // 绘制背景
            using (var brush = new SolidBrush(style.TooltipBackColor))
            {
                g.FillRectangle(brush, rect);
            }

            // 绘制边框
            using (var pen = new Pen(style.TooltipBorderColor, 1f))
            {
                g.DrawRectangle(pen, rect);
            }
        }

        /// <summary>
        /// 绘制内容 (带颜色高亮)
        /// </summary>
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

                // 根据行内容决定颜色
                if (line.Contains("最高:"))
                {
                    textColor = style.HighColor; // 绿色
                }
                else if (line.Contains("最低:"))
                {
                    textColor = style.LowColor; // 红色
                }
                else if (line.Contains("涨跌额:") || line.Contains("涨跌幅:"))
                {
                    double change = currentBar.Close - prevBar.Close;
                    textColor = change >= 0 ? style.UpColor : style.DownColor;
                }

                // 绘制文字
                using (var brush = new SolidBrush(textColor))
                {
                    g.DrawString(line, style.TooltipFont, brush, x, y);
                }

                var size = g.MeasureString(line, style.TooltipFont);
                y += size.Height + style.TooltipLineSpacing;
            }
        }

        /// <summary>
        /// 格式化成交量
        /// </summary>
        private string FormatVolume(double volume)
        {
            if (volume >= 10000)
                return $"{volume / 10000:F2}万";
            else if (volume >= 1000)
                return $"{volume / 1000:F2}千";
            else
                return $"{volume:F0}";
        }

        /// <summary>
        /// 格式化成交额
        /// </summary>
        private string FormatAmount(double amount)
        {
            if (amount >= 100000000) // 亿
                return $"{amount / 100000000:F2}亿";
            else if (amount >= 10000) // 万
                return $"{amount / 10000:F2}万";
            else if (amount >= 1000) // 千
                return $"{amount / 1000:F2}千";
            else
                return $"{amount:F0}";
        }

        /// <summary>
        /// 检测鼠标是否在Tooltip区域内
        /// </summary>
        public bool IsMouseOverTooltip(Point mousePos, Rectangle tooltipRect)
        {
            return tooltipRect.Contains(mousePos);
        }

        /// <summary>
        /// 计算Tooltip应该显示的位置 (用于智能避让)
        /// </summary>
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