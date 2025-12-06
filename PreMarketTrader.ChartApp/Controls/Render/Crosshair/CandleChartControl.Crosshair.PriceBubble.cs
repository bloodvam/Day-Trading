using System.Drawing;
using PreMarketTrader.Models;
using System.Linq;

namespace PreMarketTrader.ChartApp.Controls
{
    public partial class CandleChartControl
    {
        private void DrawPriceBubble(
            Graphics g,
            float mouseY,
            RectangleF candleRect,
            System.Collections.Generic.List<Bar5s> visibleBars)
        {
            float max = (float)visibleBars.Max(b => b.High);
            float min = (float)visibleBars.Min(b => b.Low);
            float range = max - min;

            float rel = (mouseY - candleRect.Top) / candleRect.Height;
            float price = max - rel * range;

            string txt = price.ToString("F2");
            SizeF size = g.MeasureString(txt, Style.TooltipFont);

            RectangleF axis = GetPriceAxisRect();

            float x = axis.Left + 6;
            float y = mouseY - size.Height / 2;

            g.FillRectangle(new SolidBrush(Style.BubbleShadow),
                            x + 3, y + 3, size.Width + 10, size.Height + 4);

            g.FillRectangle(new SolidBrush(Style.BubbleBackground),
                            x, y, size.Width + 10, size.Height + 4);

            g.DrawString(txt, Style.TooltipFont,
                new SolidBrush(Style.BubbleTextColor),
                x + 5, y + 1);
        }
    }
}
