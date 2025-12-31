using TradingEngine.Models;

namespace TradingEngine.Managers
{
    /// <summary>
    /// 追踪最后一次买入的成交信息
    /// </summary>
    public class LastBuyInfo
    {
        public int Token { get; set; }
        public int OrderId { get; set; }
        public double TotalCost { get; set; }
        public int TotalQty { get; set; }
        public double AvgPrice { get; set; }

        public void Reset(int token)
        {
            Token = token;
            OrderId = 0;
            TotalCost = 0;
            TotalQty = 0;
            // 不清空 AvgPrice，保留上一次的值
        }

        public void AddTrade(double price, int qty)
        {
            TotalCost += price * qty;
            TotalQty += qty;
            AvgPrice = TotalCost / TotalQty;
        }
    }

    /// <summary>
    /// 待卖出信息
    /// </summary>
    public class PendingSellInfo
    {
        public int SellShares { get; set; }
        public double SellLimitPrice { get; set; }
        public double StopTriggerPrice { get; set; }
        public double StopLimitPrice { get; set; }
        public bool IsSellAll { get; set; }
    }

    /// <summary>
    /// 日志条目
    /// </summary>
    public class LogEntry
    {
        public string Message { get; set; } = "";
        public int ColorArgb { get; set; }  // 用 int 存颜色，避免依赖 System.Drawing
        public DateTime Time { get; set; }
    }

    /// <summary>
    /// 每个 Symbol 的完整状态
    /// </summary>
    public class SymbolState
    {
        public string Symbol { get; }

        // 行情数据
        public Quote Quote { get; set; }

        // 持仓（同步自 AccountManager，用于 UI 高亮）
        public Position? Position { get; set; }

        // 订单追踪
        public LastBuyInfo LastBuy { get; } = new();
        public PendingSellInfo? PendingSell { get; set; }
        public bool PendingMoveStop { get; set; }

        // 日志
        public List<LogEntry> Logs { get; } = new();

        public SymbolState(string symbol)
        {
            Symbol = symbol;
            Quote = new Quote { Symbol = symbol };
        }

        /// <summary>
        /// 清空日志
        /// </summary>
        public void ClearLogs()
        {
            Logs.Clear();
        }

        /// <summary>
        /// 添加日志
        /// </summary>
        public void AddLog(string message, int colorArgb)
        {
            Logs.Add(new LogEntry
            {
                Message = message,
                ColorArgb = colorArgb,
                Time = DateTime.Now
            });
        }
    }

    /// <summary>
    /// 统一管理所有 Symbol 的数据
    /// </summary>
    public class SymbolDataManager
    {
        private readonly Dictionary<string, SymbolState> _symbols = new();
        private readonly object _lock = new();

        private string? _activeSymbol;

        /// <summary>
        /// 当前选中的 Symbol
        /// </summary>
        public string? ActiveSymbol
        {
            get => _activeSymbol;
            set
            {
                if (_activeSymbol == value) return;

                // 验证 symbol 存在
                if (value != null)
                {
                    lock (_lock)
                    {
                        if (!_symbols.ContainsKey(value)) return;
                    }
                }

                _activeSymbol = value;
                ActiveSymbolChanged?.Invoke(value);
            }
        }

        /// <summary>
        /// 获取当前选中的 SymbolState
        /// </summary>
        public SymbolState? ActiveState
        {
            get
            {
                if (string.IsNullOrEmpty(_activeSymbol)) return null;
                return Get(_activeSymbol);
            }
        }

        /// <summary>
        /// 所有已订阅的 Symbol
        /// </summary>
        public IReadOnlyCollection<string> Symbols
        {
            get
            {
                lock (_lock)
                {
                    return _symbols.Keys.ToList();
                }
            }
        }

        // 事件
        public event Action<string?>? ActiveSymbolChanged;
        public event Action<string>? SymbolAdded;
        public event Action<string>? SymbolRemoved;

        /// <summary>
        /// 获取或创建 SymbolState
        /// </summary>
        public SymbolState GetOrCreate(string symbol)
        {
            symbol = symbol.ToUpper().Trim();

            lock (_lock)
            {
                if (!_symbols.TryGetValue(symbol, out var state))
                {
                    state = new SymbolState(symbol);
                    _symbols[symbol] = state;
                    SymbolAdded?.Invoke(symbol);
                }
                return state;
            }
        }

        /// <summary>
        /// 获取 SymbolState（不创建）
        /// </summary>
        public SymbolState? Get(string symbol)
        {
            symbol = symbol.ToUpper().Trim();

            lock (_lock)
            {
                return _symbols.TryGetValue(symbol, out var state) ? state : null;
            }
        }

        /// <summary>
        /// 移除 Symbol
        /// </summary>
        public void Remove(string symbol)
        {
            symbol = symbol.ToUpper().Trim();

            lock (_lock)
            {
                if (_symbols.Remove(symbol))
                {
                    // 如果移除的是当前选中的，清空
                    if (_activeSymbol == symbol)
                    {
                        _activeSymbol = null;
                        ActiveSymbolChanged?.Invoke(null);
                    }
                    SymbolRemoved?.Invoke(symbol);
                }
            }
        }

        /// <summary>
        /// 检查 Symbol 是否存在
        /// </summary>
        public bool Contains(string symbol)
        {
            symbol = symbol.ToUpper().Trim();

            lock (_lock)
            {
                return _symbols.ContainsKey(symbol);
            }
        }

        /// <summary>
        /// 获取所有 SymbolState
        /// </summary>
        public List<SymbolState> GetAll()
        {
            lock (_lock)
            {
                return _symbols.Values.ToList();
            }
        }

        /// <summary>
        /// 遍历所有 SymbolState（线程安全）
        /// </summary>
        public void ForEach(Action<SymbolState> action)
        {
            List<SymbolState> snapshot;
            lock (_lock)
            {
                snapshot = _symbols.Values.ToList();
            }
            foreach (var state in snapshot)
            {
                action(state);
            }
        }
    }
}