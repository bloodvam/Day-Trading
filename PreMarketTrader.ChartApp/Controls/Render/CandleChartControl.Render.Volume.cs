using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using PreMarketTrader.Models;

namespace PreMarketTrader.ChartApp.Controls
{
    public partial class CandleChartControl
    {
        /// <summary>
        /// 绘制成交量柱图，完全使用 Style 样式系统
        /// </summary>
        private void DrawVolume(Graphics g, List<Bar5s> visibleBars, RectangleF volumeRect)
        {
            if (visibleBars.Count == 0)
                return;

            float width = volumeRect.Width;
            float height = volumeRect.Height;

            int count = visibleBars.Count;
            float barWidth = System.Math.Max(2f, width / count);

            long maxVolume = visibleBars.Max(b => b.Volume);
            if (maxVolume <= 0) maxVolume = 1;

            foreach (var (bar, index) in visibleBars.Select((b, i) => (b, i)))
            {
                float x = volumeRect.Left + index * barWidth;

                float ratio = (float)bar.Volume / maxVolume;
                float volHeight = ratio * height;

                float y = volumeRect.Bottom - volHeight;

                bool isUp = bar.Close >= bar.Open;

                // 使用 Style 中的颜色
                Color baseColor = isUp ? Style.VolumeUpColor : Style.VolumeDownColor;

                // 使用 Style.VolumeOpacity（0~1）
                Color color = Color.FromArgb(
                    (int)(Style.VolumeOpacity * 255),
                    baseColor.R,
                    baseColor.G,
                    baseColor.B
                );

                using Brush brush = new SolidBrush(color);

                // 让 Volume 柱更美观（TradingView 风格）
                g.FillRectangle(
                    brush,
                    x + barWidth * 0.2f,
                    y,
                    barWidth * 0.6f,
                    volHeight
                );
            }
        }
    }
}
