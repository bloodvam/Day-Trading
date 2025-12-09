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
    /// 只存储状态，在 OnPaint 时统一绘制
    /// </summary>
    public class CrosshairLayer : IChartLayer
    {
        public string Name => "Crosshair";
        public bool IsVisible { get; set; } = true;
        public int ZOrder { get; set; } = 100;

        private readonly CrosshairPainter _crosshairPainter;
        private readonly TooltipPainter _tooltipPainter;
        private CrosshairStyle _style;

        // ========================================
        // 状态字段
        // ========================================
        private Point _mousePosition;
        private bool _isMouseInChart = false;
        private int _snappedBarIndex = -1;
        private TooltipPosition _tooltipPosition = TooltipPosition.TopLeft;
        private Rectangle _lastTooltipRect = Rectangle.Empty;

        public CrosshairLayer(CrosshairStyle style, RenderResourcePool resourcePool)
        {
            _style = style ?? CrosshairStyle.GetDarkThemeDefault();
            _crosshairPainter = new CrosshairPainter(resourcePool);
            _tooltipPainter = new TooltipPainter(resourcePool);
        }

        // ========================================
        // 渲染方法（在 OnPaint 中调用）
        // ========================================
        public void Render(ChartRenderContext ctx)
        {
            if (!IsVisible || !_isMouseInChart)
                return;

            // 🔥 在 Render 时才计算吸附索引（延迟计算）
            _snappedBarIndex = CalculateSnappedBar(_mousePosition, ctx);

            if (_snappedBarIndex < 0 || _snappedBarIndex >= ctx.Series.Count)
                return;

            var g = ctx.Graphics;

            // 绘制十字线
            _crosshairPainter.Render(
                g,
                ctx,
                _style,
                _mousePosition,
                _snappedBarIndex
            );

            // 绘制 Tooltip
            _tooltipPainter.Render(
                g,
                ctx,
                _style,
                _snappedBarIndex,
                _tooltipPosition
            );

            // 更新 Tooltip 位置（避免遮挡）
            _lastTooltipRect = _tooltipPainter.GetTooltipRect(
                g,
                ctx,
                _style,
                _snappedBarIndex,
                _tooltipPosition
            );

            UpdateTooltipPosition(_mousePosition);
        }

        // ========================================
        // 🔥 新增：状态更新方法（在 InputHandler 中调用）
        // ========================================

        /// <summary>
        /// 更新鼠标位置（不绘制，只存储状态）
        /// </summary>
        public void UpdateMousePosition(Point mousePosition)
        {
            _mousePosition = mousePosition;
            _isMouseInChart = true;
        }

        /// <summary>
        /// 隐藏 Crosshair
        /// </summary>
        public void Hide()
        {
            _isMouseInChart = false;
            _snappedBarIndex = -1;
            _tooltipPosition = TooltipPosition.TopLeft;
            _lastTooltipRect = Rectangle.Empty;
        }

        // ========================================
        // 辅助方法
        // ========================================

        /// <summary>
        /// 计算吸附的 K 线索引
        /// </summary>
        private int CalculateSnappedBar(Point mousePos, ChartRenderContext ctx)
        {
            // 检查鼠标是否在图表区域内
            if (!ctx.PriceArea.Contains(mousePos) && !ctx.VolumeArea.Contains(mousePos))
                return -1;

            // 将鼠标 X 坐标转换为 K 线索引
            int index = ctx.Transform.XToIndex(mousePos.X, ctx.PriceArea);

            // 限制在可视范围内
            if (index < ctx.VisibleRange.StartIndex)
                index = ctx.VisibleRange.StartIndex;
            if (index > ctx.VisibleRange.EndIndex)
                index = ctx.VisibleRange.EndIndex;

            // 限制在数据范围内
            if (index < 0)
                index = 0;
            if (index >= ctx.Series.Count)
                index = ctx.Series.Count - 1;

            return index;
        }

        /// <summary>
        /// 更新 Tooltip 位置（避免遮挡鼠标）
        /// </summary>
        private void UpdateTooltipPosition(Point mousePos)
        {
            bool isMouseOverTooltip = _lastTooltipRect.Contains(mousePos);

            if (isMouseOverTooltip)
            {
                // 如果鼠标在 Tooltip 上，切换到另一侧
                if (_tooltipPosition == TooltipPosition.TopLeft)
                {
                    _tooltipPosition = TooltipPosition.TopRight;
                }
            }
        }

        // ========================================
        // 样式管理
        // ========================================

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

        // ========================================
        // 查询方法
        // ========================================

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