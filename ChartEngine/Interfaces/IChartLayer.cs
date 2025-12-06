using System.Drawing;
using ChartEngine.Rendering;

namespace ChartEngine.Interfaces
{
    /// <summary>
    /// 图表中的一层（Layer），包含一个 Renderer 和 Layer 级别的属性。
    /// </summary>
    public interface IChartLayer
    {
        string Name { get; }
        bool IsVisible { get; set; }

        /// <summary>
        /// 调用内部的 Renderer 来绘制该层内容
        /// </summary>
        void Render(ChartRenderContext ctx);
    }
}
