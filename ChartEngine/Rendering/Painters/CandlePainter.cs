using System;
using System.Drawing;
using ChartEngine.Interfaces;
using ChartEngine.Styles;

namespace ChartEngine.Rendering.Painters
{
    /// <summary>
    /// K线绘制器（使用对象池优化）
    /// </summary>
    public class CandlePainter : ICandleRenderer
    {
        private readonly RenderResourcePool _resourcePool;

        public CandlePainter(RenderResourcePool resourcePool = null)
        {
            _resourcePool = resourcePool ?? new RenderResourcePool();
        }

        public void RenderSingleBar(
            Graphics g,
            CandleStyle style,
            float xCenter,
            float barWidth,
            float yOpen,
            float yClose,
            float yHigh,
            float yLow
        )
        {
            bool isUp = yClose < yOpen;
            Color color = isUp ? style.UpColor : style.DownColor;

            float bodyWidth = barWidth * style.BodyWidthRatio;
            float left = xCenter - bodyWidth / 2f;

            float top = Math.Min(yOpen, yClose);
            float bodyHeight = Math.Abs(yClose - yOpen);
            if (bodyHeight < 1f)
                bodyHeight = 1f;

            // 🔥 优化点：使用对象池，避免每次创建 Pen 和 Brush
            var pen = _resourcePool.GetPen(color, style.WickWidth);
            var brush = _resourcePool.GetBrush(color);

            // 画影线
            g.DrawLine(pen, xCenter, yHigh, xCenter, yLow);

            // 画实体
            g.FillRectangle(brush, left, top, bodyWidth, bodyHeight);

            // 注意：不再需要 using/dispose，对象池会统一管理
        }
    }
}