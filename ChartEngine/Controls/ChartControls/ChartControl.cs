using System;
using System.Windows.Forms;
using ChartEngine.Models;

namespace ChartEngine.ChartControls
{
    /// <summary>
    /// 主图表控件入口（partial），其他功能拆分在 ChartControl.*.cs 中
    /// 
    /// 文件结构:
    /// - ChartControl.cs              (本文件) - 主入口、数据管理
    /// - ChartControl.Layers.cs       - 图层管理
    /// - ChartControl.Layout.cs       - 布局计算
    /// - ChartControl.Render.cs       - 渲染流程
    /// - ChartControl.DataAnalysis.cs - 数据分析
    /// - ChartControl.Styles.cs       - 样式管理
    /// - ChartControl.Input.cs        - 输入处理
    /// </summary>
    public partial class ChartControl : Control
    {
        /// <summary>
        /// 当前使用的数据序列（K 线）
        /// </summary>
        private ISeries _series;

        /// <summary>
        /// 对外暴露的序列属性
        /// 设置后会自动更新可视范围并重绘
        /// </summary>
        public ISeries Series
        {
            get => _series;
            set
            {
                _series = value;

                // 数据变化时，重新初始化可视范围
                if (_series != null && _series.Count > 0)
                {
                    _transform.SetVisibleRange(0, _series.Count - 1);
                }

                Invalidate();
            }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        public ChartControl()
        {
            // 启用双缓冲，消除闪烁
            DoubleBuffered = true;

            // 允许接收键盘事件
            this.SetStyle(ControlStyles.Selectable, true);
            this.TabStop = true;

            // 初始化各个子系统
            InitializeStyles();      // 样式系统
            InitializeTransform();   // 坐标转换系统
            InitializeLayers();      // 图层系统
            InitializeInputHandler(); // 输入处理系统

            // 如果外部没有设置 Series，就先用一组测试数据
            _series = GenerateTestSeries();

            // 设置初始可视范围
            if (_series != null && _series.Count > 0)
            {
                _transform.SetVisibleRange(0, _series.Count - 1);
            }
        }

        /// <summary>
        /// WinForms 绘制入口
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (_series == null || _series.Count == 0)
            {
                // 如果没有数据，绘制提示信息
                DrawNoDataMessage(e.Graphics);
                return;
            }

            // 调用渲染流程
            RenderAll(e.Graphics);
        }

        /// <summary>
        /// 尺寸变化时，重新布局+重绘
        /// </summary>
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            Invalidate();
        }

        /// <summary>
        /// 绘制"无数据"提示信息
        /// </summary>
        private void DrawNoDataMessage(System.Drawing.Graphics g)
        {
            string message = "No Data Available";
            using (var font = new System.Drawing.Font("Arial", 14))
            using (var brush = new System.Drawing.SolidBrush(System.Drawing.Color.Gray))
            {
                var size = g.MeasureString(message, font);
                float x = (this.Width - size.Width) / 2;
                float y = (this.Height - size.Height) / 2;
                g.DrawString(message, font, brush, x, y);
            }
        }

        /// <summary>
        /// 生成一组简单的模拟 K 线数据用于测试
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

        /// <summary>
        /// 加载外部数据
        /// </summary>
        public void LoadData(ISeries series)
        {
            if (series == null)
                throw new ArgumentNullException(nameof(series));

            Series = series;
        }

        /// <summary>
        /// 清除所有数据
        /// </summary>
        public void ClearData()
        {
            _series = null;
            Invalidate();
        }

        /// <summary>
        /// 刷新图表（强制重绘）
        /// </summary>
        public void RefreshChart()
        {
            Invalidate();
        }
    }
}