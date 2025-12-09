// ChartEngine/Rendering/Layers/AxisLayer.cs
using ChartEngine.Interfaces;
using ChartEngine.Rendering;
using ChartEngine.Rendering.Painters;
using ChartEngine.Styles.Core;

namespace ChartEngine.Rendering.Layers
{
    /// <summary>
    /// 坐标轴图层
    /// </summary>
    public class AxisLayer : IChartLayer
    {
        public string Name => "Axis";
        public bool IsVisible { get; set; } = true;
        public int ZOrder { get; set; } = 30;

        private readonly PriceAxisPainter _priceAxisPainter;
        private readonly TimeAxisPainter _timeAxisPainter;
        private AxisStyle _style;

        // 🔥 修改构造函数
        public AxisLayer(AxisStyle style, RenderResourcePool resourcePool)
        {
            _style = style ?? AxisStyle.GetDarkThemeDefault();
            _priceAxisPainter = new PriceAxisPainter(resourcePool);
            _timeAxisPainter = new TimeAxisPainter(resourcePool);
        }

        public void Render(ChartRenderContext ctx)
        {
            if (!IsVisible)
                return;

            if (_style.ShowPriceAxis)
            {
                _priceAxisPainter.Render(ctx, _style);
            }

            if (_style.ShowTimeAxis)
            {
                _timeAxisPainter.Render(ctx, _style);
            }
        }

        public void UpdateStyle(AxisStyle style)
        {
            if (style == null)
                return;

            _style = style;
        }

        public AxisStyle GetStyle()
        {
            return _style;
        }

        public void TogglePriceAxis()
        {
            _style.ShowPriceAxis = !_style.ShowPriceAxis;
        }

        public void ToggleTimeAxis()
        {
            _style.ShowTimeAxis = !_style.ShowTimeAxis;
        }

        public void ToggleCurrentPriceLabel()
        {
            _style.ShowCurrentPriceLabel = !_style.ShowCurrentPriceLabel;
        }
    }
}