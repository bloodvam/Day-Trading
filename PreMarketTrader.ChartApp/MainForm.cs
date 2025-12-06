using System;
using System.Windows.Forms;
using PreMarketTrader.ChartApp.Controls;
using PreMarketTrader.Models;

namespace PreMarketTrader.ChartApp
{
    public partial class MainForm : Form
    {
        private CandleChartControl _chart;

        public MainForm()
        {
            InitializeComponent();

            // 初始化图表控件
            _chart = new CandleChartControl
            {
                Dock = DockStyle.Fill
            };

            // 把图表控件放到窗口里
            Controls.Add(_chart);

            // 窗口加载完成后绘制假数据
            Load += (_, __) => LoadTestData();
        }

        private void LoadTestData()
        {
            var rand = new Random();
            double price = 100;

            for (int i = 0; i < 200; i++)
            {
                double open = price;
                double close = open + (rand.NextDouble() - 0.5) * 2;
                double high = Math.Max(open, close) + rand.NextDouble();
                double low = Math.Min(open, close) - rand.NextDouble();
                long vol = rand.Next(1000, 20000);

                _chart.AddBar(new Bar5s
                {
                    Symbol = "TEST",
                    StartTime = DateTime.Now,
                    EndTime = DateTime.Now,
                    Open = open,
                    High = high,
                    Low = low,
                    Close = close,
                    Volume = vol
                });

                price = close;
            }
        }
    }
}
