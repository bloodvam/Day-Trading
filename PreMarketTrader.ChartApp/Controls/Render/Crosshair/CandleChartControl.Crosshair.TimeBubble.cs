using System.Drawing;
using PreMarketTrader.Models;

namespace PreMarketTrader.ChartApp.Controls
{
    public partial class CandleChartControl
    {
        private void DrawTimeBubble(Graphics g, Bar5s bar, float cx)
        {
            string txt = bar.StartTime.ToString("HH:mm:ss");
            SizeF size = g.MeasureString(txt, Style.TooltipFont);

            RectangleF axis = GetTimeAxisRect();

            float x = cx - size.Width / 2;
            float y = axis.Top + 2;

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
