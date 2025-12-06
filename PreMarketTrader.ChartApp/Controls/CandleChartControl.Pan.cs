using System;
using System.Windows.Forms;

namespace PreMarketTrader.ChartApp.Controls
{
    public partial class CandleChartControl
    {
        private bool _isPanning = false;
        private int _panStartX = 0;
        private int _panStartVisibleStart = 0;
        private int _panStartVisibleEnd = 0;

        /// <summary>
        /// 初始化拖拽事件（在主构造函数里调用）
        /// </summary>
        private void SetupPan()
        {
            this.MouseDown += CandleChartControl_MouseDown;
            this.MouseMove += CandleChartControl_MouseMove;
            this.MouseUp += CandleChartControl_MouseUp;
        }

        private void CandleChartControl_MouseDown(object? sender, MouseEventArgs e)
        {
            if (Bars.Count == 0) return;

            // 🔥 改成右键启动平移
            if (e.Button == MouseButtons.Right)
            {
                _isPanning = true;
                _panStartX = e.X;

                // 记录拖拽开始时的可见窗口
                _panStartVisibleStart = _visibleStart;
                _panStartVisibleEnd = _visibleEnd;

                this.Cursor = Cursors.SizeWE; // 右键拖动时显示左右箭头鼠标
            }
        }

        private void CandleChartControl_MouseMove(object? sender, MouseEventArgs e)
        {
            if (!_isPanning || Bars.Count <= 1)
                return;

            int visibleCount = _panStartVisibleEnd - _panStartVisibleStart + 1;
            if (visibleCount <= 1) return;

            int dx = e.X - _panStartX;
            int width = ClientRectangle.Width;
            if (width <= 0) return;

            // 🔥 TradingView式：平移速度随缩放动态变化
            float barsPerPixel = visibleCount / (float)width;
            int barsToShift = (int)Math.Round(-dx * barsPerPixel);

            int newStart = _panStartVisibleStart + barsToShift;
            int newEnd = _panStartVisibleEnd + barsToShift;

            // 🔥 使用 clamp 避免抖动
            if (newStart < 0)
            {
                newStart = 0;
                newEnd = visibleCount - 1;
            }

            if (newEnd >= Bars.Count)
            {
                newEnd = Bars.Count - 1;
                newStart = Bars.Count - visibleCount;
                if (newStart < 0) newStart = 0;
            }

            _visibleStart = newStart;
            _visibleEnd = newEnd;

            Invalidate();
        }

        private void CandleChartControl_MouseUp(object? sender, MouseEventArgs e)
        {
            // 🔥 只处理右键释放
            if (e.Button == MouseButtons.Right)
            {
                _isPanning = false;
                this.Cursor = Cursors.Default;
            }
        }
    }
}
