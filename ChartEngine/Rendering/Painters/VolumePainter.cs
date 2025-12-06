using System.Drawing;
using ChartEngine.Interfaces;
using ChartEngine.Styles;

namespace ChartEngine.Rendering.Painters
{
    /// <summary>
    /// 绘制一根成交量柱（不关心循环和坐标转换）
    /// </summary>
    public class VolumePainter : IVolumeRenderer
    {
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

            using Brush brush = new SolidBrush(color);

            g.FillRectangle(
                brush,
                left,
                yTop,
                width,
                height
            );
        }
    }
}
