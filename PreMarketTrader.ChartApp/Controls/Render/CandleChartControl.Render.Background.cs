using System.Drawing;

namespace PreMarketTrader.ChartApp.Controls
{
    public partial class CandleChartControl
    {
        /// <summary>
        /// 渲染图表背景（最底层）
        /// 使用 ChartStyle.BackgroundColor
        /// </summary>
        private void DrawBackground(Graphics g)
        {
            g.Clear(Style.BackgroundColor);
        }
    }
}
