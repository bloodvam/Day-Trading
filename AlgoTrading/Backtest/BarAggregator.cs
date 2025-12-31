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

        // ATR 相关
        private int _atrPeriod = 14;
        private readonly List<double> _trueRanges = new();
        private double _prevClose = 0;
        private double _currentATR = 0;

        /// <summary>
        /// ATR 周期（可配置）
        /// </summary>
        public int AtrPeriod
        {
            get => _atrPeriod;
            set => _atrPeriod = value > 0 ? value : 14;
        }

        /// <summary>
        /// 当前 ATR 值
        /// </summary>
        public double CurrentATR => _currentATR;

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

                // 计算 ATR
                CalculateATR(_currentBar);

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
        /// 计算 ATR（Bar 完成时调用）
        /// </summary>
        private void CalculateATR(Bar bar)
        {
            // 计算 True Range
            double tr;
            if (_prevClose == 0)
            {
                // 第一个 Bar：TR = High - Low
                tr = bar.High - bar.Low;
            }
            else
            {
                // TR = max(High-Low, |High-PrevClose|, |Low-PrevClose|)
                tr = Math.Max(bar.High - bar.Low,
                     Math.Max(Math.Abs(bar.High - _prevClose),
                              Math.Abs(bar.Low - _prevClose)));
            }

            _trueRanges.Add(tr);
            _prevClose = bar.Close;

            int count = _trueRanges.Count;

            if (count < _atrPeriod)
            {
                // Bar 数量 < 周期：用已有的 TR 求平均
                _currentATR = _trueRanges.Average();
            }
            else if (count == _atrPeriod)
            {
                // 刚好等于周期：SMA，作为 Wilder's 初始值
                _currentATR = _trueRanges.Average();
            }
            else
            {
                // Bar 数量 > 周期：Wilder's Smoothing
                _currentATR = (_currentATR * (_atrPeriod - 1) + tr) / _atrPeriod;
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
            _trueRanges.Clear();
            _prevClose = 0;
            _currentATR = 0;
        }

        /// <summary>
        /// 强制完成当前 Bar（session 结束时调用）
        /// </summary>
        public void FlushCurrentBar()
        {
            if (_currentBar != null)
            {
                _currentBar.IsComplete = true;
                CalculateATR(_currentBar);
                BarCompleted?.Invoke(_currentBar.Clone());
                _currentBar = null;
            }
        }
    }
}