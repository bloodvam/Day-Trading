using System;
using System.Windows.Forms;
using ChartEngine.Data.Models;

namespace ChartEngine.Controls.ChartControls
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
        /// 当前时间周期
        /// </summary>
        public TimeFrame CurrentTimeFrame { get; set; } = TimeFrame.Minute1;

        /// <summary>
        /// 对外暴露的序列属性。设置后会自动重绘并更新可视区。
        /// </summary>
        public ISeries Series
        {
            get => _series;
            set
            {
                _series = value;

                // 设置可视范围
                if (_series != null && _series.Count > 0)
                {
                    _transform.SetVisibleRange(0, _series.Count - 1);
                }

                UpdateAutoRanges();
                Invalidate();
            }
        }

        public ChartControl()
        {
            DoubleBuffered = true;
            TabStop = true; // 允许接收键盘焦点

            InitializeStyles();
            InitializeRenderingEngine();
            InitializeLayers();
            InitializeInputHandler();

            // 如果外部没有设置 Series，就先用一组测试数据
            _series = GenerateTestSeries();

            // 设置可视范围
            if (_series != null && _series.Count > 0)
            {
                _transform.SetVisibleRange(0, _series.Count - 1);
            }

            UpdateAutoRanges();
        }

        /// <summary>
        /// WinForms 绘制入口：只负责调用内部渲染管线。
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (_series == null || _series.Count == 0)
            {
                DrawNoDataMessage(e.Graphics);
                return;
            }

            RenderAll(e.Graphics);
        }

        /// <summary>
        /// 尺寸变化时，重新布局+重绘。
        /// </summary>
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            Invalidate();
        }

        /// <summary>
        /// 绘制无数据提示
        /// </summary>
        private void DrawNoDataMessage(System.Drawing.Graphics g)
        {
            string message = "No Data";
            using (var font = new System.Drawing.Font("Arial", 14))
            using (var brush = new System.Drawing.SolidBrush(System.Drawing.Color.Gray))
            {
                var size = g.MeasureString(message, font);
                float x = (Width - size.Width) / 2;
                float y = (Height - size.Height) / 2;
                g.DrawString(message, font, brush, x, y);
            }
        }

        /// <summary>
        /// 生成一组简单的模拟 K 线数据用于测试。
        /// </summary>
        private ISeries GenerateTestSeries()
        {
            var s = new SimpleSeries();
            var rnd = new Random();

            double price = 100;
            DateTime startTime = DateTime.Now.Date.AddHours(9).AddMinutes(30); // 09:30

            for (int i = 0; i < 50; i++)
            {
                double open = price;
                double close = open + rnd.Next(-3, 4);
                double high = Math.Max(open, close) + rnd.Next(1, 5);
                double low = Math.Min(open, close) - rnd.Next(1, 5);
                double vol = rnd.Next(500, 5000);

                // 根据 TimeFrame 计算时间
                DateTime barTime = startTime.AddMinutes(i * 1); // 1分钟K线

                s.AddBar(open, high, low, close, vol, barTime, CurrentTimeFrame);
                price = close;
            }

            return s;
        }

        /// <summary>
        /// 加载数据
        /// </summary>
        public void LoadData(ISeries series)
        {
            Series = series;
        }

        /// <summary>
        /// 清除数据
        /// </summary>
        public void ClearData()
        {
            _series = null;
            Invalidate();
        }

        /// <summary>
        /// 刷新图表
        /// </summary>
        public void RefreshChart()
        {
            UpdateAutoRanges();
            Invalidate();
        }
    }
}