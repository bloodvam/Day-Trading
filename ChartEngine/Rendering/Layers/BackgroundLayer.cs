using ChartEngine.Styles;
using ChartEngine.Interfaces;

namespace ChartEngine.Rendering.Layers
{
    public class BackgroundLayer : IChartLayer
    {
        public string Name => "Background";
        public bool IsVisible { get; set; } = true;

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
