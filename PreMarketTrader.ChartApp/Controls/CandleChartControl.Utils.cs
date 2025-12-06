using System.Collections.Generic;
using PreMarketTrader.Models;

namespace PreMarketTrader.ChartApp.Controls
{
    public partial class CandleChartControl
    {
        /// <summary>
        /// 取得当前可见的 K 线区间
        /// </summary>
        private List<Bar5s> GetVisibleBars()
        {
            if (Bars.Count == 0)
                return new List<Bar5s>();

            if (_visibleStart < 0) _visibleStart = 0;
            if (_visibleEnd >= Bars.Count) _visibleEnd = Bars.Count - 1;

            int count = _visibleEnd - _visibleStart + 1;

            if (count <= 0)
                return new List<Bar5s>();

            return Bars.GetRange(_visibleStart, count);
        }
    }
}
