using ChartEngine.Interfaces;
using ChartEngine.Rendering;
using ChartEngine.Rendering.Painters;

namespace ChartEngine.Rendering.Layers
{
    /// <summary>
    /// 绘制整组 K 线的 Layer。
    /// 它只做：遍历可视区间 + 坐标转换 + 调用 CandlePainter。
    /// </summary>
    public class CandleLayer : IChartLayer
    {
        public string Name => "Candles";
        public bool IsVisible { get; set; } = true;

        private readonly CandlePainter _painter = new CandlePainter();

        public void Render(ChartRenderContext ctx)
        {
            if (!IsVisible)
                return;

            var transform = ctx.Transform;
            var bars = ctx.Series.Bars;
            var range = ctx.VisibleRange;
            var area = ctx.PriceArea;
            var style = ctx.CandleStyle;

            var g = ctx.Graphics;

            if (range.Count <= 0)
            {
                System.Diagnostics.Debug.WriteLine("CandleLayer: range.Count <= 0, 不绘制");
                return;
            }
                

            float barWidth = (float)area.Width / range.Count;
            int drawCount = 0;
            for (int i = range.StartIndex; i <= range.EndIndex; i++)
            {
                var bar = bars[i];

                float xCenter = transform.IndexToX(i, area);

                float yOpen = transform.PriceToY(bar.Open, area);
                float yClose = transform.PriceToY(bar.Close, area);
                float yHigh = transform.PriceToY(bar.High, area);
                float yLow = transform.PriceToY(bar.Low, area);
                if (drawCount < 3) // 只打印前3根的坐标
                {
                    System.Diagnostics.Debug.WriteLine($"Bar[{i}]: xCenter={xCenter}, yOpen={yOpen}, yClose={yClose}, yHigh={yHigh}, yLow={yLow}");
                }

                _painter.RenderSingleBar(
                    g,
                    style,
                    xCenter,
                    barWidth,
                    yOpen,
                    yClose,
                    yHigh,
                    yLow
                );
                drawCount++;
            }
            System.Diagnostics.Debug.WriteLine($"实际绘制了 {drawCount} 根K线");
        }
    }
}
