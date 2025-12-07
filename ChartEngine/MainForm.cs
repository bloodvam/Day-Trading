using System;
using System.Windows.Forms;
using ChartEngine.Controls.ChartControls;

namespace ChartEngine   // ← 根据你项目命名空间调整
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();

            // 加载 ChartControl
            var chart = new ChartControl
            {
                Dock = DockStyle.Fill
            };

            this.Controls.Add(chart);
        }
    }
}
