using ChartEngine.Rendering;

namespace ChartEngine.Interfaces
{
    /// <summary>
    /// 图表中的一层（Layer），包含一个 Renderer 和 Layer 级别的属性。
    /// </summary>
    public interface IChartLayer
    {
        /// <summary>图层名称</summary>
        string Name { get; }

        /// <summary>是否可见</summary>
        bool IsVisible { get; set; }

        /// <summary>
        /// 渲染顺序 (Z-Order)
        /// 数值越小越先渲染(越底层)，数值越大越后渲染(越上层)
        /// 
        /// 推荐顺序:
        /// 0 = BackgroundLayer (背景)
        /// 1 = GridLayer (网格)
        /// 10 = VolumeLayer (成交量)
        /// 20 = CandleLayer (K线)
        /// 30 = IndicatorLayer (指标)
        /// 100 = CrosshairLayer (十字光标)
        /// 200 = TooltipLayer (提示框)
        /// </summary>
        int ZOrder { get; set; }

        /// <summary>
        /// 调用内部的 Renderer 来绘制该层内容
        /// </summary>
        void Render(ChartRenderContext ctx);
    }
}