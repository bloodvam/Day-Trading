// ChartEngine/Rendering/Layers/CrosshairLayer.cs
using System;
using System.Drawing;
using System.Windows.Forms;
using ChartEngine.Interfaces;
using ChartEngine.Rendering;
using ChartEngine.Rendering.Painters;
using ChartEngine.Styles.Core;

namespace ChartEngine.Rendering.Layers
{
    /// <summary>
    /// 十字光标图层
    /// </summary>
    public class CrosshairLayer : IChartLayer
    {
        public string Name => "Crosshair";
        public bool IsVisible { get; set; } = true;
        public int ZOrder { get; set; } = 100;

        private readonly CrosshairPainter _crosshairPainter;
        private readonly TooltipPainter _tooltipPainter;
        private CrosshairStyle _style;

        private Point _mousePosition;
        private bool _isMouseInChart = false;
        private int _snappedBarIndex = -1;

        private TooltipPosition _tooltipPosition = TooltipPosition.TopLeft;
        private Rectangle _lastTooltipRect = Rectangle.Empty;

        // 🔥 修改构造函数
        public CrosshairLayer(CrosshairStyle style, RenderResourcePool resourcePool)
        {
            _style = style ?? CrosshairStyle.GetDarkThemeDefault();
            _crosshairPainter = new CrosshairPainter(resourcePool);
            _tooltipPainter = new TooltipPainter(resourcePool);
        }

        public void Render(ChartRenderContext ctx)
        {
            if (!IsVisible || !_isMouseInChart)
                return;

            if (_snappedBarIndex < 0 || _snappedBarIndex >= ctx.Series.Count)
                return;

            var g = ctx.Graphics;

            _crosshairPainter.Render(
                g,
                ctx,
                _style,
                _mousePosition,
                _snappedBarIndex
            );

            _tooltipPainter.Render(
                g,
                ctx,
                _style,
                _snappedBarIndex,
                _tooltipPosition
            );
        }

        public void OnMouseMove(Point mousePosition, ChartRenderContext ctx)
        {
            _mousePosition = mousePosition;
            _isMouseInChart = true;

            _snappedBarIndex = CalculateSnappedBar(mousePosition, ctx);

            if (_snappedBarIndex >= 0 && _snappedBarIndex < ctx.Series.Count)
            {
                _lastTooltipRect = _tooltipPainter.GetTooltipRect(
                    ctx.Graphics,
                    ctx,
                    _style,
                    _snappedBarIndex,
                    _tooltipPosition
                );

                UpdateTooltipPosition(mousePosition);
            }
        }

        public void OnMouseLeave()
        {
            _isMouseInChart = false;
            _snappedBarIndex = -1;
            _tooltipPosition = TooltipPosition.TopLeft;
            _lastTooltipRect = Rectangle.Empty;
        }

        private int CalculateSnappedBar(Point mousePos, ChartRenderContext ctx)
        {
            if (!ctx.PriceArea.Contains(mousePos) && !ctx.VolumeArea.Contains(mousePos))
                return -1;

            int index = ctx.Transform.XToIndex(mousePos.X, ctx.PriceArea);

            if (index < ctx.VisibleRange.StartIndex)
                index = ctx.VisibleRange.StartIndex;
            if (index > ctx.VisibleRange.EndIndex)
                index = ctx.VisibleRange.EndIndex;

            if (index < 0)
                index = 0;
            if (index >= ctx.Series.Count)
                index = ctx.Series.Count - 1;

            return index;
        }

        private void UpdateTooltipPosition(Point mousePos)
        {
            bool isMouseOverTooltip = _lastTooltipRect.Contains(mousePos);

            if (isMouseOverTooltip)
            {
                if (_tooltipPosition == TooltipPosition.TopLeft)
                {
                    _tooltipPosition = TooltipPosition.TopRight;
                }
            }
        }

        public void UpdateStyle(CrosshairStyle style)
        {
            if (style == null)
                return;

            _style = style;
        }

        public CrosshairStyle GetStyle()
        {
            return _style;
        }

        public int GetSnappedBarIndex()
        {
            return _snappedBarIndex;
        }

        public bool IsMouseInChart()
        {
            return _isMouseInChart;
        }
    }
}