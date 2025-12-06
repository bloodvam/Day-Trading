using System.Collections.Generic;
using System.Drawing;
using PreMarketTrader.Models;

namespace PreMarketTrader.ChartApp.Controls
{
    public partial class CandleChartControl
    {
        private const float VolumeAreaHeightRatio = 0.25f;

        /// <summary>
        /// 主绘图入口（由 OnPaint 调用）
        /// </summary>
        private void DrawChart(Graphics g)
        {
            // Layer 0：背景
            DrawBackground(g);

            if (Bars.Count == 0)
                return;

            // 当前可见 K 线
            List<Bar5s> visibleBars = GetVisibleBars();
            if (visibleBars.Count == 0)
                return;

            // ===== 使用 Layout 得到全部区域 =====
            RectangleF contentRect = GetContentRect();
            RectangleF candleRect = GetCandleAreaRect(VolumeAreaHeightRatio);
            RectangleF volumeRect = GetVolumeAreaRect(VolumeAreaHeightRatio);

            // 内容区域非法，不渲染
            if (contentRect.Width <= 0 || contentRect.Height <= 0)
                return;

            // Layer 1：网格（仅在 candleRect 内）
            DrawGrid(g, visibleBars, candleRect);

            // Layer 2：蜡烛图（candleRect）
            DrawCandles(g, visibleBars, candleRect);

            // Layer 3：成交量（volumeRect）
            DrawVolume(g, visibleBars, volumeRect);

            // Layer 4：坐标轴（使用布局模块的 AxisRect）
            DrawAxes(g, visibleBars, candleRect);

            // Layer 5：十字光标（需要全部区域）
            DrawCrosshair(g, visibleBars, candleRect, volumeRect, contentRect);
        }

        /// <summary>
        /// 获取当前可见 Bar 列表
        /// </summary>

    }
}
