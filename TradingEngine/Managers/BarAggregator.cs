using TradingEngine.Models;

namespace TradingEngine.Managers
{
    public enum BarInterval
    {
        Seconds5 = 5,
        Seconds10 = 10,
        Minute1 = 60,
        Minute5 = 300,
        Minute15 = 900,
        Minute30 = 1800,
        Hour1 = 3600,
        Day = 86400
    }

    /// <summary>
    /// K线聚合器 - 支持多时间周期
    /// </summary>
    public class BarAggregator
    {
        // symbol -> (intervalSeconds -> BarSeries)
        private readonly Dictionary<string, Dictionary<int, BarSeries>> _seriesMap = new();

        // symbol -> (intervalSeconds -> currentBar)
        private readonly Dictionary<string, Dictionary<int, Bar>> _currentBars = new();

        private readonly object _lock = new();

        /// <summary>
        /// 启用的时间周期列表
        /// </summary>
        public List<int> EnabledIntervals { get; } = new();

        /// <summary>
        /// 主时间周期（用于UI显示）
        /// </summary>
        public int PrimaryInterval { get; set; }

        /// <summary>
        /// Bar完成时触发
        /// </summary>
        public event Action<Bar>? BarCompleted;

        /// <summary>
        /// 当前Bar更新时触发（只触发主时间周期）
        /// </summary>
        public event Action<Bar>? BarUpdated;

        public BarAggregator(int primaryInterval = 5)
        {
            PrimaryInterval = primaryInterval;
            EnabledIntervals.Add(primaryInterval);
        }

        /// <summary>
        /// 启用一个时间周期
        /// </summary>
        public void EnableInterval(BarInterval interval)
        {
            EnableInterval((int)interval);
        }

        public void EnableInterval(int intervalSeconds)
        {
            lock (_lock)
            {
                if (!EnabledIntervals.Contains(intervalSeconds))
                {
                    EnabledIntervals.Add(intervalSeconds);
                }
            }
        }

        /// <summary>
        /// 禁用一个时间周期
        /// </summary>
        public void DisableInterval(int intervalSeconds)
        {
            lock (_lock)
            {
                EnabledIntervals.Remove(intervalSeconds);

                // 清理该周期的数据
                foreach (var symbolSeries in _seriesMap.Values)
                {
                    symbolSeries.Remove(intervalSeconds);
                }
                foreach (var symbolBars in _currentBars.Values)
                {
                    symbolBars.Remove(intervalSeconds);
                }
            }
        }

        /// <summary>
        /// 设置主时间周期
        /// </summary>
        public void SetPrimaryInterval(int intervalSeconds)
        {
            PrimaryInterval = intervalSeconds;
            EnableInterval(intervalSeconds);
        }

        /// <summary>
        /// 处理新的Tick数据
        /// </summary>
        public void ProcessTick(Tick tick)
        {
            if (!tick.IsValidForLastPrice) return;

            lock (_lock)
            {
                string symbol = tick.Symbol;
                EnsureSymbol(symbol);

                // 对每个启用的时间周期处理
                foreach (int interval in EnabledIntervals)
                {
                    ProcessTickForInterval(tick, symbol, interval);
                }
            }
        }

        private void ProcessTickForInterval(Tick tick, string symbol, int intervalSeconds)
        {
            DateTime barTime = GetBarStartTime(tick.Time, intervalSeconds);

            var currentBars = _currentBars[symbol];
            var series = _seriesMap[symbol];

            if (!series.ContainsKey(intervalSeconds))
            {
                series[intervalSeconds] = new BarSeries(symbol, intervalSeconds);
            }

            if (!currentBars.TryGetValue(intervalSeconds, out var currentBar))
            {
                // 创建新Bar
                currentBar = CreateNewBar(symbol, barTime, tick, intervalSeconds);
                currentBars[intervalSeconds] = currentBar;
            }
            else if (barTime > currentBar.Time)
            {
                // 当前Bar完成
                currentBar.IsComplete = true;
                series[intervalSeconds].AddBar(currentBar);
                BarCompleted?.Invoke(currentBar.Clone());

                // 创建新Bar
                currentBar = CreateNewBar(symbol, barTime, tick, intervalSeconds);
                currentBars[intervalSeconds] = currentBar;
            }
            else
            {
                // 更新当前Bar
                UpdateBar(currentBar, tick);
            }

            // 更新BarSeries的CurrentBar
            series[intervalSeconds].SetCurrentBar(currentBar);

            // 只对主时间周期触发BarUpdated
            if (intervalSeconds == PrimaryInterval)
            {
                BarUpdated?.Invoke(currentBar.Clone());
            }
        }

        /// <summary>
        /// 获取指定symbol和时间周期的BarSeries
        /// </summary>
        public BarSeries? GetBarSeries(string symbol, int intervalSeconds)
        {
            lock (_lock)
            {
                if (_seriesMap.TryGetValue(symbol, out var symbolSeries))
                {
                    if (symbolSeries.TryGetValue(intervalSeconds, out var series))
                    {
                        return series;
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// 获取主时间周期的BarSeries
        /// </summary>
        public BarSeries? GetBarSeries(string symbol)
        {
            return GetBarSeries(symbol, PrimaryInterval);
        }

        /// <summary>
        /// 获取当前Bar（主时间周期）
        /// </summary>
        public Bar? GetCurrentBar(string symbol)
        {
            return GetCurrentBar(symbol, PrimaryInterval);
        }

        /// <summary>
        /// 获取指定时间周期的当前Bar
        /// </summary>
        public Bar? GetCurrentBar(string symbol, int intervalSeconds)
        {
            lock (_lock)
            {
                if (_currentBars.TryGetValue(symbol, out var symbolBars))
                {
                    if (symbolBars.TryGetValue(intervalSeconds, out var bar))
                    {
                        return bar.Clone();
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// 获取最后一根完成的Bar（主时间周期）
        /// </summary>
        public Bar? GetLastCompletedBar(string symbol)
        {
            return GetBarSeries(symbol)?.Last;
        }

        /// <summary>
        /// 获取最后N根完成的Bar（主时间周期）
        /// </summary>
        public List<Bar> GetLastBars(string symbol, int count)
        {
            return GetBarSeries(symbol)?.GetLastBars(count) ?? new List<Bar>();
        }

        /// <summary>
        /// 清除指定symbol的所有数据
        /// </summary>
        public void Clear(string symbol)
        {
            lock (_lock)
            {
                _seriesMap.Remove(symbol);
                _currentBars.Remove(symbol);
            }
        }

        /// <summary>
        /// 清除所有数据
        /// </summary>
        public void ClearAll()
        {
            lock (_lock)
            {
                _seriesMap.Clear();
                _currentBars.Clear();
            }
        }

        #region Private Methods

        private void EnsureSymbol(string symbol)
        {
            if (!_seriesMap.ContainsKey(symbol))
            {
                _seriesMap[symbol] = new Dictionary<int, BarSeries>();
            }
            if (!_currentBars.ContainsKey(symbol))
            {
                _currentBars[symbol] = new Dictionary<int, Bar>();
            }
        }

        private DateTime GetBarStartTime(DateTime tickTime, int intervalSeconds)
        {
            if (intervalSeconds >= 86400)
            {
                return tickTime.Date;
            }

            long ticks = tickTime.Ticks;
            long intervalTicks = TimeSpan.FromSeconds(intervalSeconds).Ticks;
            long barTicks = (ticks / intervalTicks) * intervalTicks;
            return new DateTime(barTicks, tickTime.Kind);
        }

        private Bar CreateNewBar(string symbol, DateTime time, Tick tick, int intervalSeconds)
        {
            return new Bar
            {
                Symbol = symbol,
                Time = time,
                Open = tick.Price,
                High = tick.Price,
                Low = tick.Price,
                Close = tick.Price,
                Volume = tick.IsValidForVolume ? tick.Volume : 0,
                IntervalSeconds = intervalSeconds,
                IsComplete = false
            };
        }

        private void UpdateBar(Bar bar, Tick tick)
        {
            if (tick.Price > bar.High) bar.High = tick.Price;
            if (tick.Price < bar.Low) bar.Low = tick.Price;
            bar.Close = tick.Price;

            if (tick.IsValidForVolume)
                bar.Volume += tick.Volume;
        }

        #endregion
    }
}