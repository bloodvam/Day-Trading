using System.Windows.Forms;

namespace PreMarketTrader.ChartApp.Controls
{
    public partial class CandleChartControl
    {
        /// <summary>
        /// 初始化缩放事件（在主构造函数里调用）
        /// </summary>
        private void SetupZoom()
        {
            this.MouseWheel += CandleChartControl_MouseWheel;
        }

        /// <summary>
        /// 鼠标滚轮缩放
        /// </summary>
        private void CandleChartControl_MouseWheel(object? sender, MouseEventArgs e)
        {
            if (Bars.Count == 0) return;

            int visibleCount = _visibleEnd - _visibleStart + 1;
            if (visibleCount <= 0) return;

            // 每次缩放改变的bar数量（当前可见数量的10%）
            int zoomAmount = visibleCount / 10;
            if (zoomAmount < 1) zoomAmount = 1;

            if (e.Delta > 0)  // 放大（滚轮向上）
            {
                if (visibleCount <= MinBarsVisible)
                    return;

                _visibleStart += zoomAmount;
                _visibleEnd -= zoomAmount;
            }
            else              // 缩小（滚轮向下）
            {
                if (visibleCount >= MaxBarsVisible || visibleCount >= Bars.Count)
                    return;

                _visibleStart -= zoomAmount;
                _visibleEnd += zoomAmount;

                if (_visibleStart < 0) _visibleStart = 0;
                if (_visibleEnd >= Bars.Count) _visibleEnd = Bars.Count - 1;
            }

            // 防止 start >= end 的异常情况
            if (_visibleStart >= _visibleEnd)
                _visibleStart = System.Math.Max(0, _visibleEnd - 1);

            Invalidate(); // 触发重绘
        }
    }
}
