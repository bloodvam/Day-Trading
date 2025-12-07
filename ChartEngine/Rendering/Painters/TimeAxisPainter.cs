using System;
using System.Drawing;
using ChartEngine.Rendering;
using ChartEngine.Styles.Core;

namespace ChartEngine.Rendering.Painters
{
    /// <summary>
    /// 时间轴绘制器
    /// 负责绘制时间刻度和标签
    /// </summary>
    public class TimeAxisPainter
    {
        /// <summary>
        /// 渲染时间轴
        /// </summary>
        public void Render(ChartRenderContext ctx, AxisStyle style)
        {
            var g = ctx.Graphics;
            var priceArea = ctx.PriceArea;
            var volumeArea = ctx.VolumeArea;

            // 1. 计算时间轴区域 (在成交量区域下方)
            Rectangle axisRect = CalculateAxisRect(priceArea, volumeArea, style);

            // 2. 绘制背景
            DrawBackground(g, axisRect, style);

            // 3. 绘制分隔线
            DrawSeparatorLine(g, axisRect, style);

            // 4. 绘制时间刻度和标签
            DrawTimeTicks(g, ctx, axisRect, style);
        }

        /// <summary>
        /// 计算时间轴区域
        /// </summary>
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
        /// 绘制分隔线 (成交量区和时间轴之间的横线)
        /// </summary>
        private void DrawSeparatorLine(Graphics g, Rectangle axisRect, AxisStyle style)
        {
            using (var pen = new Pen(style.AxisLineColor, 1f))
            {
                g.DrawLine(pen, axisRect.Left, axisRect.Top, axisRect.Right, axisRect.Top);
            }
        }

        /// <summary>
        /// 绘制时间刻度和标签
        /// </summary>
        private void DrawTimeTicks(Graphics g, ChartRenderContext ctx, Rectangle axisRect, AxisStyle style)
        {
            var visibleRange = ctx.VisibleRange;
            if (visibleRange.Count <= 0)
                return;

            // 计算合适的 K 线间隔
            int barInterval = CalculateBarInterval(visibleRange.Count);

            // 计算起始索引
            int startIndex = (visibleRange.StartIndex / barInterval) * barInterval;
            if (startIndex < visibleRange.StartIndex)
                startIndex += barInterval;

            using (var tickPen = new Pen(style.TickLineColor, 1f))
            using (var textBrush = new SolidBrush(style.LabelTextColor))
            {
                for (int index = startIndex; index <= visibleRange.EndIndex; index += barInterval)
                {
                    // 计算 X 坐标
                    float x = ctx.Transform.IndexToX(index, ctx.PriceArea);

                    // 确保在可见区域内
                    if (x < axisRect.Left || x > axisRect.Right)
                        continue;

                    // 绘制刻度线
                    g.DrawLine(tickPen, x, axisRect.Top, x, axisRect.Top + style.TickLength);

                    // 绘制时间标签
                    string timeText = GenerateTimeLabel(index, style.TimeFormat);
                    var textSize = g.MeasureString(timeText, style.LabelFont);

                    float textX = x - textSize.Width / 2;
                    float textY = axisRect.Top + style.TickLength + style.LabelPadding;

                    // 确保文字不超出边界
                    if (textX < axisRect.Left)
                        textX = axisRect.Left;
                    if (textX + textSize.Width > axisRect.Right)
                        textX = axisRect.Right - textSize.Width;

                    g.DrawString(timeText, style.LabelFont, textBrush, textX, textY);
                }
            }
        }

        /// <summary>
        /// 计算 K 线间隔
        /// </summary>
        private int CalculateBarInterval(int visibleCount)
        {
            if (visibleCount <= 0)
                return 1;

            // 目标: 显示 6-10 条时间标签
            int targetLabels = 8;
            int baseInterval = Math.Max(1, visibleCount / targetLabels);

            // 调整到"好看"的数字
            int[] niceNumbers = { 1, 2, 5, 10, 20, 50, 100, 200, 500, 1000 };

            foreach (int num in niceNumbers)
            {
                if (num >= baseInterval)
                    return num;
            }

            return baseInterval;
        }

        /// <summary>
        /// 生成时间标签文本
        /// 注意: 这里使用索引模拟时间,实际应用中应该从数据中获取真实时间
        /// </summary>
        private string GenerateTimeLabel(int barIndex, string timeFormat)
        {
            // 模拟: 假设从 09:30 开始,每根K线代表1分钟
            // 实际应用中,应该从 IBar 中获取真实时间戳

            DateTime startTime = new DateTime(2024, 1, 1, 9, 30, 0);
            DateTime barTime = startTime.AddMinutes(barIndex);

            return barTime.ToString(timeFormat);
        }
    }
}