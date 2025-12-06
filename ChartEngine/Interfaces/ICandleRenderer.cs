using System.Drawing;
//using ChartEngine.Models;
using ChartEngine.Styles;

namespace ChartEngine.Interfaces
{
    /// <summary>
    /// 单根 K 线的渲染器（不负责循环、不负责可见范围）
    /// </summary>
    public interface ICandleRenderer
    {
        void RenderSingleBar(
            Graphics g,
            CandleStyle style,
            float xCenter,
            float barWidth,
            float yOpen,
            float yClose,
            float yHigh,
            float yLow
        );
    }
}
