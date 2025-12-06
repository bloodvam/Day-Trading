using System.Drawing;

namespace ChartEngine.Interfaces
{
    /// <summary>
    /// 所有可绘制组件的基础接口（K线层、成交量层、网格层、坐标轴层…）
    /// </summary>
    public interface IChartComponentRenderer
    {
        /// <summary>
        /// 绘制该组件
        /// </summary>
       // void Render(Graphics g, ChartRenderContext ctx);
    }
}
