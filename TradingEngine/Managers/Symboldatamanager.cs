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
    /// 日志面板类型
    /// </summary>
    public enum LogPanelType
    {
        Order,
        Strategy,
        Agent
    }

    /// <summary>
    /// 加仓模式
    /// </summary>
    public enum AddPositionMode
    {
        None,       // 无
        Open,       // 开仓
        AddAll,     // 加仓 All Profit
        AddHalf     // 加仓 1/2 Profit
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

        // 技术指标（由 IndicatorManager 更新）
        public double ATR14 { get; set; }
        public double EMA20 { get; set; }
        public bool IsAboveEMA20 { get; set; }  // 当前价格是否在 EMA20 上方

        // VWAP 计算
        public bool VwapEnabled { get; set; }
        public double CumulativeValue { get; set; }   // 累计成交额
        public long CumulativeVolume { get; set; }    // 累计成交量
        public double VWAP { get; set; }
        public bool IsAboveVWAP { get; set; }         // 当前价格是否在 VWAP 上方

        // Session High
        public double SessionHigh { get; set; }

        // 策略状态
        public double TriggerPrice { get; set; }
        public double StopPrice { get; set; }
        public bool StrategyEnabled { get; set; }
        public bool HasTriggeredBuy { get; set; }
        public bool HasTriggeredSell { get; set; }
        public int StopVolumeAccumulated { get; set; }    // 止损累计成交量
        public bool IsHighBreakout { get; set; }       // 开盘突破策略（止损只用 barLow - 0.01）
        public AddPositionMode PositionMode { get; set; }  // 当前等待触发的模式

        // Trailing Stop 状态
        public double TrailingSinceHigh { get; set; }      // 买入后最高价
        public double TrailHalf { get; set; }              // 第二大波动 (High - Low)
        public double TrailAll { get; set; }               // TrailHalf × 1.25
        public List<double> BarRanges { get; } = new();    // 买入后各 bar 的波动幅度
        public bool HasTriggeredTrailHalf { get; set; }    // 是否已触发 Trail½ 卖出
        public int RemainingShares { get; set; }           // SellHalf 后剩余股数（供 SellAll 使用）
        public DateTime LastBarTime { get; set; }          // 最后添加的 bar 时间，避免重复
        public bool TrailingStopActive { get; set; }       // Trailing Stop 是否已激活

        // Agent Mode 状态
        public bool AgentEnabled { get; set; }
        public double AgentTriggerPrice { get; set; }      // Agent 计算的买入价格
        public double AgentPreviousPrice { get; set; }     // 上一笔 tick 价格
        public double AgentBreakedLevel { get; set; }      // 已突破的关键位
        public double AgentCurrentBarHigh { get; set; }    // 当前 bar 最高价
        public double AgentLastTriggeredPrice { get; set; } // 上次触发 StartOpen 的价格

        // 分类日志
        public List<LogEntry> OrderLogs { get; } = new();
        public List<LogEntry> StrategyLogs { get; } = new();
        public List<LogEntry> AgentLogs { get; } = new();

        public SymbolState(string symbol)
        {
            Symbol = symbol;
            Quote = new Quote { Symbol = symbol };
        }

        /// <summary>
        /// 清空所有日志
        /// </summary>
        public void ClearLogs()
        {
            OrderLogs.Clear();
            StrategyLogs.Clear();
            AgentLogs.Clear();
        }

        /// <summary>
        /// 清空指定类型的日志
        /// </summary>
        public void ClearLogs(LogPanelType type)
        {
            switch (type)
            {
                case LogPanelType.Order:
                    OrderLogs.Clear();
                    break;
                case LogPanelType.Strategy:
                    StrategyLogs.Clear();
                    break;
                case LogPanelType.Agent:
                    AgentLogs.Clear();
                    break;
            }
        }

        /// <summary>
        /// 添加日志
        /// </summary>
        public void AddLog(LogPanelType type, string message, int colorArgb)
        {
            var entry = new LogEntry
            {
                Message = message,
                ColorArgb = colorArgb,
                Time = DateTime.Now
            };

            switch (type)
            {
                case LogPanelType.Order:
                    OrderLogs.Add(entry);
                    break;
                case LogPanelType.Strategy:
                    StrategyLogs.Add(entry);
                    break;
                case LogPanelType.Agent:
                    AgentLogs.Add(entry);
                    break;
            }
        }

        /// <summary>
        /// 获取指定类型的日志
        /// </summary>
        public List<LogEntry> GetLogs(LogPanelType type)
        {
            return type switch
            {
                LogPanelType.Order => OrderLogs,
                LogPanelType.Strategy => StrategyLogs,
                LogPanelType.Agent => AgentLogs,
                _ => OrderLogs
            };
        }

        /// <summary>
        /// 清除策略和 Trailing Stop 状态（全部清仓后调用）
        /// </summary>
        public void ClearStrategyState()
        {
            // 策略状态
            TriggerPrice = 0;
            StopPrice = 0;
            StrategyEnabled = false;
            HasTriggeredBuy = false;
            HasTriggeredSell = false;
            StopVolumeAccumulated = 0;
            IsHighBreakout = false;
            PositionMode = AddPositionMode.None;

            // Trailing Stop 状态
            TrailingSinceHigh = 0;
            TrailHalf = 0;
            TrailAll = 0;
            BarRanges.Clear();
            HasTriggeredTrailHalf = false;
            RemainingShares = 0;
            LastBarTime = DateTime.MinValue;
            TrailingStopActive = false;
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
                    state.VwapEnabled = true;  // 自动启用 VWAP 计算
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