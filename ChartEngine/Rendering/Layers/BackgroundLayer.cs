using System.Drawing;
using ChartEngine.Interfaces;
using ChartEngine.Styles.Core;

namespace ChartEngine.Rendering.Layers
{
    public class BackgroundLayer : IChartLayer
    {
        public string Name => "Background";
        public bool IsVisible { get; set; } = true;
        public int ZOrder { get; set; } = 0; // 最底层

        public BackgroundStyle Style { get; set; }

        public BackgroundLayer(BackgroundStyle style)
        {
            Style = style;
        }

        public void Render(ChartRenderContext ctx)
        {
            var g = ctx.Graphics;

            // 主图背景
            using (var brush = new SolidBrush(Style.PriceAreaBackColor))
                g.FillRectangle(brush, ctx.PriceArea);

            // 成交量背景
            using (var brush = new SolidBrush(Style.VolumeAreaBackColor))
                g.FillRectangle(brush, ctx.VolumeArea);
        }
    }
}