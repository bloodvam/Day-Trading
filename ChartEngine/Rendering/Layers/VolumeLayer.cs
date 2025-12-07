using ChartEngine.Interfaces;
using ChartEngine.Rendering;
using ChartEngine.Rendering.Painters;

namespace ChartEngine.Rendering.Layers
{
    /// <summary>
    /// 绘制成交量柱的 Layer。
    /// </summary>
    public class VolumeLayer : IChartLayer
    {
        public string Name => "Volume";
        public bool IsVisible { get; set; } = true;
        public int ZOrder { get; set; } = 10; // 成交量在网格之上,K线之下

        private readonly VolumePainter _painter = new VolumePainter();

        public void Render(ChartRenderContext ctx)
        {
            if (!IsVisible)
                return;

            var transform = ctx.Transform;
            var bars = ctx.Series.Bars;
            var range = ctx.VisibleRange;
            var area = ctx.VolumeArea;
            var style = ctx.VolumeStyle;

            var g = ctx.Graphics;

            if (range.Count <= 0)
                return;

            float barWidth = (float)area.Width / range.Count;

            for (int i = range.StartIndex; i <= range.EndIndex; i++)
            {
                var bar = bars[i];
                bool isUp = bar.Close >= bar.Open;

                float xCenter = transform.IndexToX(i, area);

                float yTop = transform.VolumeToY(bar.Volume, area);
                float yBottom = area.Bottom;

                _painter.RenderVolumeBar(
                    g,
                    style,
                    xCenter,
                    barWidth,
                    yTop,
                    yBottom,
                    isUp
                );
            }
        }
    }
}