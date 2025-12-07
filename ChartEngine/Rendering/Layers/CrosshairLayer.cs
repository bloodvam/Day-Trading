using System;
using System.Drawing;
using System.Windows.Forms;
using ChartEngine.Interfaces;
using ChartEngine.Rendering;
using ChartEngine.Rendering.Painters;
using ChartEngine.Styles;

namespace ChartEngine.Rendering.Layers
{
    /// <summary>
    /// 十字光标图层
    /// 负责协调十字线、气泡和Tooltip的绘制,处理鼠标交互
    /// </summary>
    public class CrosshairLayer : IChartLayer
    {
        public string Name => "Crosshair";
        public bool IsVisible { get; set; } = true;
        public int ZOrder { get; set; } = 100; // 最上层

        private readonly CrosshairPainter _crosshairPainter;
        private readonly TooltipPainter _tooltipPainter;
        private CrosshairStyle _style;

        // 鼠标状态
        private Point _mousePosition;
        private bool _isMouseInChart = false;
        private int _snappedBarIndex = -1;

        // Tooltip状态
        private TooltipPosition _tooltipPosition = TooltipPosition.TopLeft;
        private Rectangle _lastTooltipRect = Rectangle.Empty;

        public CrosshairLayer(CrosshairStyle style)
        {
            _style = style ?? CrosshairStyle.GetDarkThemeDefault();
            _crosshairPainter = new CrosshairPainter();
            _tooltipPainter = new TooltipPainter();
        }

        /// <summary>
        /// 渲染十字光标和Tooltip
        /// </summary>
        public void Render(ChartRenderContext ctx)
        {
            if (!IsVisible || !_isMouseInChart)
                return;

            if (_snappedBarIndex < 0 || _snappedBarIndex >= ctx.Series.Count)
                return;

            var g = ctx.Graphics;

            // 1. 绘制十字线和气泡
            _crosshairPainter.Render(
                g,
                ctx,
                _style,
                _mousePosition,
                _snappedBarIndex
            );

            // 2. 绘制Tooltip
            _tooltipPainter.Render(
                g,
                ctx,
                _style,
                _snappedBarIndex,
                _tooltipPosition
            );
        }

        /// <summary>
        /// 鼠标移动事件处理
        /// </summary>
        public void OnMouseMove(Point mousePosition, ChartRenderContext ctx)
        {
            _mousePosition = mousePosition;
            _isMouseInChart = true;

            // 1. 计算吸附到的K线索引
            _snappedBarIndex = CalculateSnappedBar(mousePosition, ctx);

            // 2. 计算当前Tooltip矩形 (用于检测碰撞)
            if (_snappedBarIndex >= 0 && _snappedBarIndex < ctx.Series.Count)
            {
                _lastTooltipRect = _tooltipPainter.GetTooltipRect(
                    ctx.Graphics,
                    ctx,
                    _style,
                    _snappedBarIndex,
                    _tooltipPosition
                );

                // 3. 检测是否需要智能避让
                UpdateTooltipPosition(mousePosition);
            }
        }

        /// <summary>
        /// 鼠标离开事件处理
        /// </summary>
        public void OnMouseLeave()
        {
            _isMouseInChart = false;
            _snappedBarIndex = -1;

            // 重置Tooltip位置
            _tooltipPosition = TooltipPosition.TopLeft;
            _lastTooltipRect = Rectangle.Empty;
        }

        /// <summary>
        /// 计算吸附到的K线索引
        /// </summary>
        private int CalculateSnappedBar(Point mousePos, ChartRenderContext ctx)
        {
            // 检查鼠标是否在主图区域内
            if (!ctx.PriceArea.Contains(mousePos) && !ctx.VolumeArea.Contains(mousePos))
                return -1;

            // 将X坐标转换为K线索引
            int index = ctx.Transform.XToIndex(mousePos.X, ctx.PriceArea);

            // 限制在可视范围内
            if (index < ctx.VisibleRange.StartIndex)
                index = ctx.VisibleRange.StartIndex;
            if (index > ctx.VisibleRange.EndIndex)
                index = ctx.VisibleRange.EndIndex;

            // 确保在数据范围内
            if (index < 0)
                index = 0;
            if (index >= ctx.Series.Count)
                index = ctx.Series.Count - 1;

            return index;
        }

        /// <summary>
        /// 更新Tooltip位置 (智能避让)
        /// </summary>
        private void UpdateTooltipPosition(Point mousePos)
        {
            // 检测鼠标是否在当前Tooltip区域内
            bool isMouseOverTooltip = _lastTooltipRect.Contains(mousePos);

            if (isMouseOverTooltip)
            {
                // 鼠标进入Tooltip区域,切换到另一侧
                if (_tooltipPosition == TooltipPosition.TopLeft)
                {
                    _tooltipPosition = TooltipPosition.TopRight;
                }
                // 如果已经在右侧,保持在右侧
            }
            else
            {
                // 鼠标不在Tooltip区域
                // 如果当前在右侧,保持在右侧 (除非鼠标离开图表)
                // 这样实现"一直挂在右上角直到光标移出整个chart"的需求
            }
        }

        /// <summary>
        /// 更新样式
        /// </summary>
        public void UpdateStyle(CrosshairStyle style)
        {
            if (style == null)
                return;

            _style = style;
        }

        /// <summary>
        /// 获取当前样式
        /// </summary>
        public CrosshairStyle GetStyle()
        {
            return _style;
        }

        /// <summary>
        /// 获取当前吸附的K线索引
        /// </summary>
        public int GetSnappedBarIndex()
        {
            return _snappedBarIndex;
        }

        /// <summary>
        /// 检查鼠标是否在图表内
        /// </summary>
        public bool IsMouseInChart()
        {
            return _isMouseInChart;
        }
    }
}