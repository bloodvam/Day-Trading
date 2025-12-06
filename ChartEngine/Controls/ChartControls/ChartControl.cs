using System;
using System.Windows.Forms;
using ChartEngine.Models;

namespace ChartEngine.ChartControls
{
    /// <summary>
    /// 主图表控件入口（partial），其他功能拆分在 ChartControl.*.cs 中。
    /// </summary>
    public partial class ChartControl : Control
    {
        /// <summary>
        /// 当前使用的数据序列（K 线）。
        /// </summary>
        private ISeries _series;

        /// <summary>
        /// 对外暴露的序列属性。设置后会自动重绘并更新可视区。
        /// </summary>
        public ISeries Series
        {
            get => _series;
            set
            {
                _series = value;
                UpdateAutoRanges();
                Invalidate();
            }
        }

        public ChartControl()
        {
            DoubleBuffered = true;

            InitializeStyles();
            InitializeTransform();
            InitializeLayers();

            // 如果外部没有设置 Series，就先用一组测试数据
            _series = GenerateTestSeries();
            



            // 调试信息
            System.Diagnostics.Debug.WriteLine($"=== ChartControl 初始化 ===");
            System.Diagnostics.Debug.WriteLine($"Series Count: {_series.Count}");
            System.Diagnostics.Debug.WriteLine($"First bar: O={_series.Bars[0].Open}, C={_series.Bars[0].Close}");
            System.Diagnostics.Debug.WriteLine($"Last bar: O={_series.Bars[_series.Count - 1].Open}, C={_series.Bars[_series.Count - 1].Close}");

            _transform.SetVisibleRange(0, _series.Count - 1);
            UpdateAutoRanges();
        }

        /// <summary>
        /// WinForms 绘制入口：只负责调用内部渲染管线。
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (_series == null || _series.Count == 0)
                return;

            RenderAll(e.Graphics);
        }

        /// <summary>
        /// 尺寸变化时，重新布局+重绘。
        /// </summary>
        /// <param name="e"></param>
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            Invalidate();
        }

        /// <summary>
        /// 生成一组简单的模拟 K 线数据用于测试。
        /// </summary>
        private ISeries GenerateTestSeries()
        {
            var s = new SimpleSeries();
            var rnd = new Random();

            double price = 100;

            for (int i = 0; i < 50; i++)
            {
                double open = price;
                double close = open + rnd.Next(-3, 4);
                double high = Math.Max(open, close) + rnd.Next(1, 5);
                double low = Math.Min(open, close) - rnd.Next(1, 5);
                double vol = rnd.Next(500, 5000);

                s.AddBar(open, high, low, close, vol);
                price = close;
            }

            return s;
        }
    }
}
