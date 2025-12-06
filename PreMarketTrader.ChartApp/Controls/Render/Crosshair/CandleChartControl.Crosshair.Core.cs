using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using PreMarketTrader.Models;

namespace PreMarketTrader.ChartApp.Controls
{
    public partial class CandleChartControl
    {
        private int _mouseX = -1;
        private int _mouseY = -1;
        private int _hoverIndex = -1;

        /// <summary>
        /// 初始化 Crosshair
        /// </summary>
        private void SetupCrosshair()
        {
            this.MouseMove += OnMouseMoveCrosshair;
            this.MouseLeave += OnMouseLeaveCrosshair;
        }

        private void OnMouseMoveCrosshair(object? sender, MouseEventArgs e)
        {
            // 右键拖动时不显示 crosshair
            if (e.Button == MouseButtons.Right)
                return;

            _mouseX = e.X;
            _mouseY = e.Y;

            _hoverIndex = FindHoveredBar(e.X);

            Invalidate();
        }

        private void OnMouseLeaveCrosshair(object? sender, EventArgs e)
        {
            _mouseX = -1;
            _mouseY = -1;
            _hoverIndex = -1;

            Invalidate();
        }

        /// <summary>
        /// 绘制 Crosshair + Tooltip + Bubbles + VolumeHighlight
        /// </summary>
        private void DrawCrosshair(
            Graphics g,
            System.Collections.Generic.List<Bar5s> visibleBars,
            RectangleF candleRect,
            RectangleF volumeRect,
            RectangleF contentRect)
        {
            if (_hoverIndex < 0 || _hoverIndex >= visibleBars.Count)
                return;

            var bar = visibleBars[_hoverIndex];

            float barWidth = candleRect.Width / visibleBars.Count;
            float snappedX = candleRect.Left + _hoverIndex * barWidth + barWidth / 2f;

            float x = snappedX;
            float y = _mouseY;

            // ==============================
            // 虚线 Crosshair
            // ==============================
            using (Pen pen = new Pen(Style.CrosshairLineColor, 1))
            {
                pen.DashPattern = Style.CrosshairDashPattern;

                // 垂直线
                g.DrawLine(pen, x, candleRect.Top, x, volumeRect.Bottom);

                // 水平线
                g.DrawLine(pen, contentRect.Left, y, contentRect.Right, y);
            }

            // ==============================
            // Tooltip（左上角）
            // ==============================
            DrawCrosshairTooltip(g, bar);

            // ==============================
            // 右侧价格气泡
            // ==============================
            DrawPriceBubble(g, y, candleRect, visibleBars);

            // ==============================
            // 底部时间气泡
            // ==============================
            DrawTimeBubble(g, bar, x);

            // ==============================
            // Hover 高亮
            // ==============================

            // ❌ 不再高亮蜡烛（你要求去掉）
            // HighlightCandle(g, bar, _hoverIndex, candleRect, visibleBars);

            // ✔ 只高亮 Volume
            HighlightVolume(g, _hoverIndex, visibleBars, volumeRect);
        }

        /// <summary>
        /// 根据鼠标 X 位置找到鼠标悬停的 Bar index
        /// </summary>
        private int FindHoveredBar(int mouseX)
        {
            var candleRect = GetCandleAreaRect(VolumeAreaHeightRatio);
            var bars = GetVisibleBars();

            if (bars.Count == 0)
                return -1;

            float barWidth = candleRect.Width / bars.Count;

            int index = (int)((mouseX - candleRect.Left) / barWidth);

            if (index < 0 || index >= bars.Count)
                return -1;

            return index;
        }
    }
}
