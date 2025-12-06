using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using PreMarketTrader.Models;

namespace PreMarketTrader.ChartApp.Controls
{
    public partial class CandleChartControl
    {
        private void DrawCrosshairTooltip(Graphics g, Bar5s bar)
        {
            double diff = bar.Close - bar.Open;
            double pct = bar.Open != 0 ? diff / bar.Open * 100.0 : 0;
            double volWan = bar.Volume / 10000.0;
            double amtWan = (bar.Close * bar.Volume) / 10000.0;

            string[] lines =
            {
                $"Time: {bar.StartTime:HH:mm:ss}",
                $"O: {bar.Open:F2}",
                $"H: {bar.High:F2}",
                $"L: {bar.Low:F2}",
                $"C: {bar.Close:F2}",
                $"Δ: {diff:+0.00;-0.00} ({pct:+0.00;-0.00}%)",
                $"Vol: {volWan:F2} 万",
                $"Amt: {amtWan:F2} 万"
            };

            float padding = 8f;
            float lh = g.MeasureString("A", Style.TooltipFont).Height;

            float maxW = lines.Max(t => g.MeasureString(t, Style.TooltipFont).Width);
            float boxW = maxW + padding * 2;
            float boxH = lh * lines.Length + padding * 2;

            float x = 8;
            float y = 8;

            g.FillRectangle(new SolidBrush(Style.TooltipShadowColor),
                            x + 3, y + 3, boxW, boxH);

            using GraphicsPath path = RoundedRect(new RectangleF(x, y, boxW, boxH), 8);
            using Brush bg = new SolidBrush(Style.TooltipBackgroundColor);
            g.FillPath(bg, path);

            float ty = y + padding;

            foreach (string line in lines)
            {
                Brush brush = Style.AxisTextBrush;

                if (line.StartsWith("H:")) brush = Brushes.Lime;
                if (line.StartsWith("L:")) brush = Brushes.Red;

                g.DrawString(line, Style.TooltipFont, brush, x + padding, ty);
                ty += lh;
            }
        }

        private GraphicsPath RoundedRect(RectangleF r, float radius)
        {
            float d = radius * 2;
            GraphicsPath path = new GraphicsPath();

            path.AddArc(r.Left, r.Top, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Top, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.Left, r.Bottom - d, d, d, 90, 90);

            path.CloseFigure();
            return path;
        }
    }
}
