using System;
using ChartEngine.Interfaces;
using ChartEngine.Rendering;
using ChartEngine.Rendering.Painters;
using ChartEngine.Utils;

namespace ChartEngine.Rendering.Layers
{
    /// <summary>
    /// 成交量图层（带完整边界检查）
    /// </summary>
    public class VolumeLayer : IChartLayer
    {
        public string Name => "Volume";
        public bool IsVisible { get; set; } = true;
        public int ZOrder { get; set; } = 10;

        private readonly VolumePainter _painter;

        public VolumeLayer(VolumePainter painter = null)
        {
            _painter = painter ?? new VolumePainter();
        }

        public void Render(ChartRenderContext ctx)
        {
            if (!IsVisible)
                return;

            // 🔥 优化点：全面的空值和边界检查
            if (ctx?.Series?.Bars == null)
                return;

            if (ctx.Series.Count == 0)
                return;

            var transform = ctx.Transform;
            var bars = ctx.Series.Bars;
            var range = ctx.VisibleRange;
            var area = ctx.VolumeArea;
            var style = ctx.VolumeStyle;
            var g = ctx.Graphics;

            if (range.Count <= 0)
                return;

            // 检查成交量区域是否有效
            if (area.Width <= 0 || area.Height <= 0)
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

            for (int i = safeStart; i <= safeEnd; i++)
            {
                try
                {
                    var bar = bars[i];

                    // 🔥 优化点：验证K线数据有效性
                    if (!BoundsChecker.IsValidBar(bar))
                        continue;

                    // 验证成交量
                    if (!BoundsChecker.IsValidVolume(bar.Volume))
                        continue;

                    bool isUp = bar.Close >= bar.Open;

                    float xCenter = transform.IndexToX(i, area);
                    float yTop = transform.VolumeToY(bar.Volume, area);
                    float yBottom = area.Bottom;

                    // 检查坐标有效性
                    if (float.IsNaN(yTop) || float.IsInfinity(yTop))
                        continue;

                    // 确保 yTop 在合理范围内
                    yTop = BoundsChecker.Clamp(yTop, (float)area.Top, (float)area.Bottom);

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
                catch (Exception ex)
                {
                    // 🔥 优化点：单个柱子渲染失败不影响整体
                    // _logger?.LogError(ex, $"Error rendering volume bar at index {i}");
                    continue;
                }
            }
        }
    }
}