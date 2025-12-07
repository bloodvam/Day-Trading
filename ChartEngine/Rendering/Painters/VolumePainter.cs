using System.Drawing;
using ChartEngine.Interfaces;
using ChartEngine.Styles;

namespace ChartEngine.Rendering.Painters
{
    /// <summary>
    /// 成交量绘制器（使用对象池优化）
    /// </summary>
    public class VolumePainter : IVolumeRenderer
    {
        private readonly RenderResourcePool _resourcePool;

        public VolumePainter(RenderResourcePool resourcePool = null)
        {
            _resourcePool = resourcePool ?? new RenderResourcePool();
        }

        public void RenderVolumeBar(
            Graphics g,
            VolumeStyle style,
            float xCenter,
            float barWidth,
            float yTop,
            float yBottom,
            bool isUp
        )
        {
            Color color = isUp ? style.UpColor : style.DownColor;

            float width = barWidth * style.BarWidthRatio;
            float left = xCenter - width / 2f;

            float height = yBottom - yTop;
            if (height < style.MinBarHeight)
                height = style.MinBarHeight;

            // 🔥 优化点：使用对象池
            var brush = _resourcePool.GetBrush(color);

            g.FillRectangle(
                brush,
                left,
                yTop,
                width,
                height
            );

            // 注意：不再需要 dispose
        }
    }
}