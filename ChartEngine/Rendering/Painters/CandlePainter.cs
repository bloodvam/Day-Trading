using System.Drawing;
using ChartEngine.Interfaces;
using ChartEngine.Styles;

namespace ChartEngine.Rendering.Painters
{

    public class CandlePainter : ICandleRenderer
    {
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

            using Pen pen = new Pen(color, style.WickWidth);
            using Brush brush = new SolidBrush(color);

            // 画影线
            g.DrawLine(pen, xCenter, yHigh, xCenter, yLow);

            // 画实体
            g.FillRectangle(brush, left, top, bodyWidth, bodyHeight);
        }
    }
}
