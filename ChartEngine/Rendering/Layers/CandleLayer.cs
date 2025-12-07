using System;
using ChartEngine.Interfaces;
using ChartEngine.Rendering;
using ChartEngine.Rendering.Painters;
using ChartEngine.Utils;

namespace ChartEngine.Rendering.Layers
{
    /// <summary>
    /// K线图层（带完整边界检查）
    /// </summary>
    public class CandleLayer : IChartLayer
    {
        public string Name => "Candles";
        public bool IsVisible { get; set; } = true;
        public int ZOrder { get; set; } = 20;

        private readonly CandlePainter _painter;

        public CandleLayer(CandlePainter painter = null)
        {
            _painter = painter ?? new CandlePainter();
        }

        public void Render(ChartRenderContext ctx)
        {
            if (!IsVisible)
                return;

            // 🔥 优化点：全面的空值和边界检查
            if (ctx?.Series?.Bars == null)
            {
                // 可以在这里添加日志
                return;
            }

            if (ctx.Series.Count == 0)
                return;

            var transform = ctx.Transform;
            var bars = ctx.Series.Bars;
            var range = ctx.VisibleRange;
            var area = ctx.PriceArea;
            var style = ctx.CandleStyle;
            var g = ctx.Graphics;

            if (range.Count <= 0)
                return;

            // 🔥 优化点：获取安全的索引范围
            var (safeStart, safeEnd) = BoundsChecker.GetSafeIndexRange(
                range.StartIndex,
                range.EndIndex,
                bars.Count
            );

            if (safeStart > safeEnd)
                return;

            float barWidth = (float)area.Width / range.Count;

            // 渲染循环
            for (int i = safeStart; i <= safeEnd; i++)
            {
                try
                {
                    var bar = bars[i];

                    // 🔥 优化点：验证K线数据有效性
                    if (!BoundsChecker.IsValidBar(bar))
                    {
                        continue; // 跳过无效数据
                    }

                    float xCenter = transform.IndexToX(i, area);

                    float yOpen = transform.PriceToY(bar.Open, area);
                    float yClose = transform.PriceToY(bar.Close, area);
                    float yHigh = transform.PriceToY(bar.High, area);
                    float yLow = transform.PriceToY(bar.Low, area);

                    // 🔥 优化点：检查坐标是否在合理范围内
                    if (float.IsNaN(yOpen) || float.IsNaN(yClose) ||
                        float.IsNaN(yHigh) || float.IsNaN(yLow))
                    {
                        continue;
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
                }
                catch (Exception ex)
                {
                    // 🔥 优化点：单个K线渲染失败不影响整体
                    // 这里可以添加日志记录
                    // _logger?.LogError(ex, $"Error rendering candle at index {i}");
                    continue;
                }
            }
        }
    }
}