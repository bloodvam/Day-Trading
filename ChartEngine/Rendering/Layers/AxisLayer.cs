using ChartEngine.Interfaces;
using ChartEngine.Rendering;
using ChartEngine.Rendering.Painters;
using ChartEngine.Styles;

namespace ChartEngine.Rendering.Layers
{
    /// <summary>
    /// 坐标轴图层
    /// 负责协调价格轴和时间轴的绘制
    /// </summary>
    public class AxisLayer : IChartLayer
    {
        public string Name => "Axis";
        public bool IsVisible { get; set; } = true;
        public int ZOrder { get; set; } = 30; // 在K线之上,十字光标之下

        private readonly PriceAxisPainter _priceAxisPainter;
        private readonly TimeAxisPainter _timeAxisPainter;
        private AxisStyle _style;

        public AxisLayer(AxisStyle style)
        {
            _style = style ?? AxisStyle.GetDarkThemeDefault();
            _priceAxisPainter = new PriceAxisPainter();
            _timeAxisPainter = new TimeAxisPainter();
        }

        /// <summary>
        /// 渲染坐标轴
        /// </summary>
        public void Render(ChartRenderContext ctx)
        {
            if (!IsVisible)
                return;

            // 绘制价格轴
            if (_style.ShowPriceAxis)
            {
                _priceAxisPainter.Render(ctx, _style);
            }

            // 绘制时间轴
            if (_style.ShowTimeAxis)
            {
                _timeAxisPainter.Render(ctx, _style);
            }
        }

        /// <summary>
        /// 更新坐标轴样式
        /// </summary>
        public void UpdateStyle(AxisStyle style)
        {
            if (style == null)
                return;

            _style = style;
        }

        /// <summary>
        /// 获取当前样式
        /// </summary>
        public AxisStyle GetStyle()
        {
            return _style;
        }

        /// <summary>
        /// 切换价格轴显示
        /// </summary>
        public void TogglePriceAxis()
        {
            _style.ShowPriceAxis = !_style.ShowPriceAxis;
        }

        /// <summary>
        /// 切换时间轴显示
        /// </summary>
        public void ToggleTimeAxis()
        {
            _style.ShowTimeAxis = !_style.ShowTimeAxis;
        }

        /// <summary>
        /// 切换当前价格标签显示
        /// </summary>
        public void ToggleCurrentPriceLabel()
        {
            _style.ShowCurrentPriceLabel = !_style.ShowCurrentPriceLabel;
        }
    }
}