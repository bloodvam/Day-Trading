namespace TradingEngine.Models
{
    /// <summary>
    /// K线序列 - 管理单个时间周期的K线数据
    /// </summary>
    public class BarSeries
    {
        private readonly List<Bar> _bars = new();
        private readonly object _lock = new();

        public string Symbol { get; }
        public int IntervalSeconds { get; }
        public int MaxBars { get; set; } = 4000;

        /// <summary>
        /// 当前正在形成的Bar（未完成）
        /// </summary>
        public Bar? CurrentBar { get; private set; }

        public int Count
        {
            get { lock (_lock) return _bars.Count; }
        }

        /// <summary>
        /// 索引访问，0是最早的，Count-1是最新的
        /// </summary>
        public Bar? this[int index]
        {
            get
            {
                lock (_lock)
                {
                    if (index < 0 || index >= _bars.Count) return null;
                    return _bars[index].Clone();
                }
            }
        }

        /// <summary>
        /// 最后一根完成的Bar
        /// </summary>
        public Bar? Last
        {
            get
            {
                lock (_lock)
                {
                    return _bars.Count > 0 ? _bars[^1].Clone() : null;
                }
            }
        }

        /// <summary>
        /// 倒数第N根Bar（1=最后一根，2=倒数第二根）
        /// </summary>
        public Bar? FromEnd(int n)
        {
            lock (_lock)
            {
                int index = _bars.Count - n;
                if (index < 0 || index >= _bars.Count) return null;
                return _bars[index].Clone();
            }
        }

        public BarSeries(string symbol, int intervalSeconds)
        {
            Symbol = symbol;
            IntervalSeconds = intervalSeconds;
        }

        /// <summary>
        /// 添加完成的Bar
        /// </summary>
        internal void AddBar(Bar bar)
        {
            lock (_lock)
            {
                _bars.Add(bar.Clone());

                while (_bars.Count > MaxBars)
                {
                    _bars.RemoveAt(0);
                }
            }
        }

        /// <summary>
        /// 设置当前正在形成的Bar
        /// </summary>
        internal void SetCurrentBar(Bar? bar)
        {
            CurrentBar = bar?.Clone();
        }

        /// <summary>
        /// 获取最后N根完成的Bar
        /// </summary>
        public List<Bar> GetLastBars(int count)
        {
            lock (_lock)
            {
                return _bars.TakeLast(count).Select(b => b.Clone()).ToList();
            }
        }

        /// <summary>
        /// 获取所有完成的Bar
        /// </summary>
        public List<Bar> GetAllBars()
        {
            lock (_lock)
            {
                return _bars.Select(b => b.Clone()).ToList();
            }
        }

        /// <summary>
        /// 清空
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _bars.Clear();
                CurrentBar = null;
            }
        }

        #region 技术指标用 - 获取价格数组

        public double[] GetOpens(int count = 0)
        {
            lock (_lock)
            {
                var source = count > 0 ? _bars.TakeLast(count) : _bars;
                return source.Select(b => b.Open).ToArray();
            }
        }

        public double[] GetHighs(int count = 0)
        {
            lock (_lock)
            {
                var source = count > 0 ? _bars.TakeLast(count) : _bars;
                return source.Select(b => b.High).ToArray();
            }
        }

        public double[] GetLows(int count = 0)
        {
            lock (_lock)
            {
                var source = count > 0 ? _bars.TakeLast(count) : _bars;
                return source.Select(b => b.Low).ToArray();
            }
        }

        public double[] GetCloses(int count = 0)
        {
            lock (_lock)
            {
                var source = count > 0 ? _bars.TakeLast(count) : _bars;
                return source.Select(b => b.Close).ToArray();
            }
        }

        public long[] GetVolumes(int count = 0)
        {
            lock (_lock)
            {
                var source = count > 0 ? _bars.TakeLast(count) : _bars;
                return source.Select(b => b.Volume).ToArray();
            }
        }

        #endregion
    }
}