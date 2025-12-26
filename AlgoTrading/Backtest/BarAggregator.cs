using AlgoTrading.Core.Models;

namespace AlgoTrading.Backtest
{
    /// <summary>
    /// K线聚合器（回测用）
    /// </summary>
    public class BarAggregator
    {
        private readonly int _intervalSeconds;
        private Bar? _currentBar;

        /// <summary>
        /// 当前正在形成的 Bar
        /// </summary>
        public Bar? CurrentBar => _currentBar?.Clone();

        /// <summary>
        /// Bar 完成时触发
        /// </summary>
        public event Action<Bar>? BarCompleted;

        /// <summary>
        /// 新 Bar 开始时触发
        /// </summary>
        public event Action<Bar>? BarStarted;

        public BarAggregator(int intervalSeconds = 5)
        {
            _intervalSeconds = intervalSeconds;
        }

        /// <summary>
        /// 处理一笔 Trade
        /// </summary>
        public void ProcessTrade(Trade trade)
        {
            var barTime = GetBarStartTime(trade.SipTimestampEt);

            if (_currentBar == null)
            {
                // 创建新 Bar
                _currentBar = CreateNewBar(barTime, trade);
                BarStarted?.Invoke(_currentBar.Clone());
            }
            else if (barTime > _currentBar.Time)
            {
                // 当前 Bar 完成
                _currentBar.IsComplete = true;
                BarCompleted?.Invoke(_currentBar.Clone());

                // 创建新 Bar
                _currentBar = CreateNewBar(barTime, trade);
                BarStarted?.Invoke(_currentBar.Clone());
            }
            else
            {
                // 更新当前 Bar
                UpdateBar(_currentBar, trade);
            }
        }

        /// <summary>
        /// 获取 Bar 开始时间
        /// </summary>
        private DateTime GetBarStartTime(DateTime tickTime)
        {
            var totalSeconds = (long)tickTime.TimeOfDay.TotalSeconds;
            var barSeconds = (totalSeconds / _intervalSeconds) * _intervalSeconds;
            return tickTime.Date.AddSeconds(barSeconds);
        }

        /// <summary>
        /// 创建新 Bar
        /// </summary>
        private Bar CreateNewBar(DateTime barTime, Trade trade)
        {
            return new Bar
            {
                Time = barTime,
                Open = trade.Price,
                High = trade.Price,
                Low = trade.Price,
                Close = trade.Price,
                Volume = trade.Size,
                IntervalSeconds = _intervalSeconds,
                IsComplete = false
            };
        }

        /// <summary>
        /// 更新 Bar
        /// </summary>
        private void UpdateBar(Bar bar, Trade trade)
        {
            if (trade.Price > bar.High) bar.High = trade.Price;
            if (trade.Price < bar.Low) bar.Low = trade.Price;
            bar.Close = trade.Price;
            bar.Volume += trade.Size;
        }

        /// <summary>
        /// 重置
        /// </summary>
        public void Reset()
        {
            _currentBar = null;
        }

        /// <summary>
        /// 强制完成当前 Bar（session 结束时调用）
        /// </summary>
        public void FlushCurrentBar()
        {
            if (_currentBar != null)
            {
                _currentBar.IsComplete = true;
                BarCompleted?.Invoke(_currentBar.Clone());
                _currentBar = null;
            }
        }
    }
}