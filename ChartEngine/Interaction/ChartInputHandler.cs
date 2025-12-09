// ChartEngine/Interaction/ChartInputHandler.cs
using System;
using System.Drawing;
using System.Windows.Forms;
using ChartEngine.Interfaces;
using ChartEngine.Rendering.Layers;

namespace ChartEngine.Interaction
{
    /// <summary>
    /// 图表输入处理器
    /// 处理鼠标和键盘事件，实现拖动、缩放、十字光标等交互功能
    /// </summary>
    public class ChartInputHandler : IInputHandler
    {
        private readonly Controls.ChartControls.ChartControl _chart;

        // ========================================
        // 鼠标状态
        // ========================================
        private Point _lastMousePosition;
        private bool _isPanning = false;
        private bool _isMouseInside = false;

        // ========================================
        // 平移相关
        // ========================================
        private int _panStartIndex;
        private int _panEndIndex;
        private float _accumulatedPanOffset = 0f;  // 🔥 新增：累积小数偏移

        // ========================================
        // 缩放相关
        // ========================================
        private const float ZoomSpeed = 0.1f;
        private const int MinVisibleBars = 10;
        private const int MaxVisibleBars = 1000;

        // ========================================
        // Crosshair 相关
        // ========================================
        private CrosshairLayer _crosshairLayer;  // 🔥 新增

        public ChartInputHandler(Controls.ChartControls.ChartControl chart)
        {
            _chart = chart ?? throw new ArgumentNullException(nameof(chart));

            // 🔥 获取 CrosshairLayer 引用（避免每次查找）
            _crosshairLayer = _chart.GetLayer<CrosshairLayer>();
        }

        // ========================================
        // 鼠标事件处理
        // ========================================

        /// <summary>
        /// 处理鼠标按下事件
        /// </summary>
        public void OnMouseDown(MouseEventArgs e)
        {
            // 🔥 改进：改为右键拖动
            if (e.Button == MouseButtons.Right)
            {
                // 开始拖动
                _isPanning = true;
                _lastMousePosition = e.Location;

                // 记录当前可视范围
                var visibleRange = _chart.Transform.VisibleRange;
                _panStartIndex = visibleRange.StartIndex;
                _panEndIndex = visibleRange.EndIndex;

                // 🔥 改进：使用手型光标
                _chart.Cursor = Cursors.Hand;
            }
        }

        /// <summary>
        /// 处理鼠标移动事件
        /// </summary>
        public void OnMouseMove(MouseEventArgs e)
        {
            _isMouseInside = true;

            if (_isPanning)
            {
                // ========================================
                // 拖动模式：执行平移
                // ========================================
                int deltaX = e.X - _lastMousePosition.X;

                if (deltaX != 0)
                {
                    PerformPan(deltaX);
                    _lastMousePosition = e.Location;  // 🔥 改进：实时更新
                }
            }
            else
            {
                // ========================================
                // 非拖动模式：更新 Crosshair
                // ========================================
                UpdateCrosshair(e.Location);
            }
        }

        /// <summary>
        /// 处理鼠标抬起事件
        /// </summary>
        public void OnMouseUp(MouseEventArgs e)
        {
            // 🔥 改进：改为右键
            if (e.Button == MouseButtons.Right)
            {
                _isPanning = false;
                _chart.Cursor = Cursors.Default;

                // 🔥 改进：清零累积偏移
                _accumulatedPanOffset = 0f;
            }
        }

        /// <summary>
        /// 处理鼠标滚轮事件（缩放）
        /// </summary>
        public void OnMouseWheel(MouseEventArgs e)
        {
            // Delta > 0: 向上滚动（放大）
            // Delta < 0: 向下滚动（缩小）
            int delta = e.Delta;

            if (delta != 0)
            {
                // 以鼠标位置为中心进行缩放
                PerformZoom(delta > 0, e.Location);
            }
        }

        /// <summary>
        /// 🔥 新增：处理鼠标离开事件
        /// </summary>
        public void OnMouseLeave()
        {
            _isMouseInside = false;

            // 隐藏 Crosshair
            if (_crosshairLayer != null)
            {
                _crosshairLayer.Hide();
                _chart.Invalidate();
            }
        }

        /// <summary>
        /// 处理键盘按下事件
        /// </summary>
        public void OnKeyDown(KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Left:
                    // 向左平移
                    PanByBars(-5);
                    e.Handled = true;
                    break;

                case Keys.Right:
                    // 向右平移
                    PanByBars(5);
                    e.Handled = true;
                    break;

                case Keys.Home:
                    // 回到最开始
                    PanToStart();
                    e.Handled = true;
                    break;

                case Keys.End:
                    // 跳到最末尾
                    PanToEnd();
                    e.Handled = true;
                    break;

                case Keys.Add:
                case Keys.Oemplus:
                    // 放大（+键）
                    PerformZoom(true, Point.Empty);
                    e.Handled = true;
                    break;

                case Keys.Subtract:
                case Keys.OemMinus:
                    // 缩小（-键）
                    PerformZoom(false, Point.Empty);
                    e.Handled = true;
                    break;
            }
        }

        /// <summary>
        /// 处理键盘抬起事件
        /// </summary>
        public void OnKeyUp(KeyEventArgs e)
        {
            // 当前不需要处理
        }

        // ========================================
        // Crosshair 更新逻辑
        // ========================================
        #region Crosshair

        /// <summary>
        /// 🔥 新增：更新 Crosshair 位置
        /// </summary>
        private void UpdateCrosshair(Point mousePosition)
        {
            if (_crosshairLayer == null || !_crosshairLayer.IsVisible)
                return;

            // 只更新状态，不绘制
            _crosshairLayer.UpdateMousePosition(mousePosition);

            // 触发重绘（在 OnPaint 中统一绘制）
            _chart.Invalidate();
        }

        #endregion

        // ========================================
        // 平移功能
        // ========================================
        #region Panning

        /// <summary>
        /// 🔥 改进：执行平移操作（支持累积小数偏移）
        /// </summary>
        private void PerformPan(int deltaX)
        {
            var priceArea = _chart.GetPriceArea();
            if (priceArea.Width <= 0)
                return;

            var visibleRange = _chart.Transform.VisibleRange;
            int visibleCount = visibleRange.Count;
            if (visibleCount <= 0)
                return;

            // 计算每个像素代表多少根 K 线
            float pixelsPerBar = (float)priceArea.Width / visibleCount;

            // 🔥 改进：保留小数，累积偏移
            float barOffsetFloat = -deltaX / pixelsPerBar;
            _accumulatedPanOffset += barOffsetFloat;

            // 🔥 改进：只有累积到整数时才触发平移
            int barOffset = (int)_accumulatedPanOffset;

            if (barOffset != 0)
            {
                PanByBars(barOffset);
                _accumulatedPanOffset -= barOffset;  // 减去已处理部分
            }
        }

        /// <summary>
        /// 按 K 线数量平移
        /// </summary>
        private void PanByBars(int barCount)
        {
            var series = _chart.Series;
            if (series == null || series.Count == 0)
                return;

            var visibleRange = _chart.Transform.VisibleRange;
            int newStart = visibleRange.StartIndex + barCount;
            int newEnd = visibleRange.EndIndex + barCount;

            // 边界检查
            if (newStart < 0)
            {
                int offset = -newStart;
                newStart = 0;
                newEnd += offset;
            }

            if (newEnd >= series.Count)
            {
                int offset = newEnd - series.Count + 1;
                newEnd = series.Count - 1;
                newStart -= offset;
            }

            newStart = Math.Max(0, newStart);
            newEnd = Math.Min(series.Count - 1, newEnd);

            // 更新可视范围
            _chart.Transform.SetVisibleRange(newStart, newEnd);
            _chart.Invalidate();
        }

        /// <summary>
        /// 平移到最开始
        /// </summary>
        private void PanToStart()
        {
            var series = _chart.Series;
            if (series == null || series.Count == 0)
                return;

            var visibleRange = _chart.Transform.VisibleRange;
            int count = visibleRange.Count;

            _chart.Transform.SetVisibleRange(0, Math.Min(count - 1, series.Count - 1));
            _chart.Invalidate();
        }

        /// <summary>
        /// 平移到最末尾
        /// </summary>
        private void PanToEnd()
        {
            var series = _chart.Series;
            if (series == null || series.Count == 0)
                return;

            var visibleRange = _chart.Transform.VisibleRange;
            int count = visibleRange.Count;

            int newEnd = series.Count - 1;
            int newStart = Math.Max(0, newEnd - count + 1);

            _chart.Transform.SetVisibleRange(newStart, newEnd);
            _chart.Invalidate();
        }

        #endregion

        // ========================================
        // 缩放功能
        // ========================================
        #region Zooming

        /// <summary>
        /// 执行缩放操作
        /// </summary>
        private void PerformZoom(bool zoomIn, Point mousePosition)
        {
            var series = _chart.Series;
            if (series == null || series.Count == 0)
                return;

            var visibleRange = _chart.Transform.VisibleRange;
            int currentCount = visibleRange.Count;

            // 计算新的可视 K 线数量
            int newCount;
            if (zoomIn)
            {
                // 放大：减少可视 K 线数量
                newCount = Math.Max(MinVisibleBars, (int)(currentCount * (1 - ZoomSpeed)));
            }
            else
            {
                // 缩小：增加可视 K 线数量
                newCount = Math.Min(MaxVisibleBars, (int)(currentCount * (1 + ZoomSpeed)));
                newCount = Math.Min(newCount, series.Count);
            }

            if (newCount == currentCount)
                return;

            // 计算缩放中心点（如果有鼠标位置，以鼠标为中心；否则以中心为中心）
            float centerRatio = 0.5f; // 默认居中

            if (mousePosition != Point.Empty)
            {
                var priceArea = _chart.GetPriceArea();
                if (priceArea.Width > 0)
                {
                    centerRatio = (float)(mousePosition.X - priceArea.Left) / priceArea.Width;
                    centerRatio = Math.Max(0, Math.Min(1, centerRatio));
                }
            }

            // 计算新的起始和结束索引
            int centerIndex = visibleRange.StartIndex + (int)(currentCount * centerRatio);
            int newStart = centerIndex - (int)(newCount * centerRatio);
            int newEnd = newStart + newCount - 1;

            // 边界检查
            if (newStart < 0)
            {
                newStart = 0;
                newEnd = newCount - 1;
            }

            if (newEnd >= series.Count)
            {
                newEnd = series.Count - 1;
                newStart = newEnd - newCount + 1;
            }

            newStart = Math.Max(0, newStart);
            newEnd = Math.Min(series.Count - 1, newEnd);

            // 更新可视范围
            _chart.Transform.SetVisibleRange(newStart, newEnd);
            _chart.Invalidate();
        }

        #endregion
    }
}