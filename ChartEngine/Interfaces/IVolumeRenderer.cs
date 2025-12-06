using System.Drawing;
using ChartEngine.Styles;

namespace ChartEngine.Interfaces
{
    public interface IVolumeRenderer
    {
        void RenderVolumeBar(
             Graphics g,
            VolumeStyle style,
            float xCenter,
             float barWidth,
            float yTop,
            float yBottom,
             bool isUp
        );
    }
}
