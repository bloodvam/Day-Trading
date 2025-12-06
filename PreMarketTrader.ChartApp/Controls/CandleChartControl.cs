using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using PreMarketTrader.Models;

namespace PreMarketTrader.ChartApp.Controls
{
    public partial class CandleChartControl : Control
    {
        public ChartStyle Style { get; set; } = new ChartStyle();

        // ---------------------------
        // 当前所有 K 线数据
        // ---------------------------
        public List<Bar5s> Bars { get; private set; } = new();

        // ---------------------------
        // 可见区域（由缩放 & 拖拽控制）
        // ---------------------------
        private const float CandleBodyWidthRatio = 0.6f;
        private int _visibleStart = 0;
        private int _visibleEnd = 0;

        // 最小 / 最大显示 K 线数量
        private const int MinBarsVisible = 20;
        private const int MaxBarsVisible = 500;

        // ---------------------------
        // 构造函数
        // ---------------------------
        public CandleChartControl()
        {
            DoubleBuffered = true;   // 防止闪烁
            BackColor = Color.Black;

            // 初始化交互功能
            SetupZoom();   // 在 CandleChartControl.Zoom.cs 里
            SetupPan();  // 以后拖拽功能会启用
            SetupCrosshair(); // 以后十字线功能会启用
        }

        // ---------------------------
        // 添加一根新的 K 线数据
        // ---------------------------
        public void AddBar(Bar5s bar)
        {
            Bars.Add(bar);

            // 每次添加自动让可见范围右移到底部
            if (Bars.Count == 1)
            {
                _visibleStart = 0;
                _visibleEnd = 0;
            }
            else
            {
                _visibleEnd = Bars.Count - 1;
            }

            Invalidate(); // 重绘
        }

        // ---------------------------
        // 主绘图入口
        // ---------------------------
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            // 渲染由 Render.cs 控制
            DrawChart(e.Graphics);
        }
    }
}
