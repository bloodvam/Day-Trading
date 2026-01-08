using TradingEngine.Models;

namespace TradingEngine.Managers
{
    /// <summary>
    /// Agent 自动交易策略
    /// 监听整数位/半数位突破，自动触发开仓
    /// </summary>
    public class AgentStrategy : IDisposable
    {
        private readonly SymbolDataManager _dataManager;
        private readonly BarAggregator _barAggregator;
        private readonly object _lock = new();

        /// <summary>
        /// Agent 计算的 TriggerPrice 更新（UI 显示用）
        /// </summary>
        public event Action<string, double>? TriggerPriceUpdated;

        /// <summary>
        /// Agent 触发开仓信号
        /// </summary>
        public event Action<string, double>? OpenSignalTriggered;

        /// <summary>
        /// Agent 启用/禁用状态变化（UI 更新按钮用）
        /// </summary>
        public event Action<string, bool>? AgentStateChanged;

        /// <summary>
        /// 价格跨越关键位时触发（UI 更新 AutoFill 按钮用）
        /// </summary>
        public event Action<string, double>? LevelCrossed;  // symbol, ceilLevel

        public event Action<string>? Log;

        public AgentStrategy(
            SymbolDataManager dataManager,
            BarAggregator barAggregator)
        {
            _dataManager = dataManager;
            _barAggregator = barAggregator;

            _barAggregator.BarUpdated += OnBarUpdated;
        }

        #region Public Methods

        /// <summary>
        /// 启用 Agent Mode
        /// </summary>
        public void Enable(string symbol)
        {
            var state = _dataManager.Get(symbol);
            if (state == null) return;

            double triggerPrice = 0;
            bool shouldTrigger = false;

            lock (_lock)
            {
                state.AgentEnabled = true;
                state.AgentLastTriggeredPrice = 0;

                // 如果已有有效的 AgentTriggerPrice，准备触发
                if (state.AgentTriggerPrice > 0)
                {
                    state.AgentLastTriggeredPrice = state.AgentTriggerPrice;
                    triggerPrice = state.AgentTriggerPrice;
                    shouldTrigger = true;
                }
            }

            Log?.Invoke($"[Agent] Enabled for {symbol}");
            AgentStateChanged?.Invoke(symbol, true);

            // 立即触发（在 lock 外面，避免死锁）
            if (shouldTrigger)
            {
                //Log?.Invoke($"[Agent] {symbol} Immediately triggering StartOpen at {triggerPrice:F2}");
                OpenSignalTriggered?.Invoke(symbol, triggerPrice);
            }
        }

        /// <summary>
        /// 禁用 Agent Mode
        /// </summary>
        public void Disable(string symbol)
        {
            var state = _dataManager.Get(symbol);
            if (state == null) return;

            lock (_lock)
            {
                state.AgentEnabled = false;
                state.AgentLastTriggeredPrice = 0;
            }

            Log?.Invoke($"[Agent] Disabled for {symbol}");
            AgentStateChanged?.Invoke(symbol, false);
        }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled(string symbol)
        {
            var state = _dataManager.Get(symbol);
            if (state == null) return false;

            lock (_lock)
            {
                return state.AgentEnabled;
            }
        }

        /// <summary>
        /// 获取当前计算的 TriggerPrice
        /// </summary>
        public double GetTriggerPrice(string symbol)
        {
            var state = _dataManager.Get(symbol);
            if (state == null) return 0;

            lock (_lock)
            {
                return state.AgentTriggerPrice;
            }
        }

        /// <summary>
        /// 重置 BreakedLevel
        /// </summary>
        public void SetBreakedLevel(string symbol, double level)
        {
            var state = _dataManager.Get(symbol);
            if (state == null) return;

            lock (_lock)
            {
                state.AgentBreakedLevel = level;
            }

            Log?.Invoke($"[Agent] {symbol} BreakedLevel reset to {level:F2}");
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// 处理有效的 tick（由 StrategyManager 调用）
        /// </summary>
        public void OnValidTickReceived(Tick tick)
        {
            var state = _dataManager.Get(tick.Symbol);
            if (state == null) return;

            // 在 lock 外计算需要触发的事件
            double triggerPriceForUI = 0;
            double triggerPriceForOpen = 0;
            double ceilLevelForUI = 0;
            string? logSignal = null;
            string? logTrigger = null;

            lock (_lock)
            {
                ProcessTickLocked(state, tick.Price,
                    out triggerPriceForUI, out triggerPriceForOpen, out ceilLevelForUI, out logSignal, out logTrigger);
            }

            // 在 lock 外触发事件（避免死锁）
            if (logSignal != null) Log?.Invoke(logSignal);
            if (triggerPriceForUI != 0) TriggerPriceUpdated?.Invoke(tick.Symbol, triggerPriceForUI > 0 ? triggerPriceForUI : 0);
            if (logTrigger != null) Log?.Invoke(logTrigger);
            if (triggerPriceForOpen > 0) OpenSignalTriggered?.Invoke(tick.Symbol, triggerPriceForOpen);
            if (ceilLevelForUI > 0) LevelCrossed?.Invoke(tick.Symbol, ceilLevelForUI);
        }

        private void OnBarUpdated(Bar bar)
        {
            var state = _dataManager.Get(bar.Symbol);
            if (state == null) return;

            lock (_lock)
            {
                state.AgentCurrentBarHigh = bar.High;
            }
        }

        #endregion

        #region Core Logic

        private void ProcessTickLocked(SymbolState state, double price,
            out double triggerPriceForUI, out double triggerPriceForOpen, out double ceilLevelForUI, out string? logSignal, out string? logTrigger)
        {
            triggerPriceForUI = 0;
            triggerPriceForOpen = 0;
            ceilLevelForUI = 0;
            logSignal = null;
            logTrigger = null;

            // 初始化（第一笔 tick）
            if (state.AgentPreviousPrice == 0)
            {
                state.AgentBreakedLevel = FloorLevel(price);
                state.AgentPreviousPrice = price;
                state.AgentCurrentBarHigh = price;
                ceilLevelForUI = CeilLevel(price);  // 首次也触发更新
                return;
            }

            // SessionHigh 必须有效
            double sessionHigh = state.SessionHigh;
            if (sessionHigh <= 0)
            {
                state.AgentPreviousPrice = price;
                if (price > state.AgentCurrentBarHigh) state.AgentCurrentBarHigh = price;
                return;
            }

            double sessionHighLevel = FloorLevel(sessionHigh);

            // ========== 计算下一个预期入场价格（每个 tick 都计算） ==========
            double nextTriggerPrice;
            if (state.AgentBreakedLevel + 0.5 < sessionHighLevel)
            {
                // 不是新高
                nextTriggerPrice = state.AgentBreakedLevel + 0.5;
            }
            else
            {
                // 创新高
                nextTriggerPrice = sessionHighLevel + 0.5;
            }

            // 更新 UI
            if (Math.Abs(state.AgentTriggerPrice - nextTriggerPrice) > 0.001)
            {
                state.AgentTriggerPrice = nextTriggerPrice;
                triggerPriceForUI = nextTriggerPrice;

                // 如果 Agent 启用，同步更新实际的 TriggerPrice
                if (state.AgentEnabled)
                {
                    state.TriggerPrice = nextTriggerPrice;
                    Log?.Invoke($"[Agent] {state.Symbol} TriggerPrice updated: {nextTriggerPrice:F2}");
                }
            }

            // ========== 检测上穿，触发买入 ==========
            double currentLevel = FloorLevel(price);
            double previousLevel = FloorLevel(state.AgentPreviousPrice);

            bool crossedUp = false;
            bool crossedDown = false;

            if (price > state.AgentPreviousPrice)
            {
                if (state.AgentPreviousPrice < currentLevel && price >= currentLevel)
                {
                    crossedUp = true;
                    ceilLevelForUI = CeilLevel(price);  // 上穿时更新
                }
            }
            else if (price < state.AgentPreviousPrice)
            {
                if (price <= previousLevel)
                {
                    crossedDown = true;
                    ceilLevelForUI = CeilLevel(price);  // 下破时更新
                }
            }

            // 上穿时检查是否触发买入
            if (crossedUp)
            {
                double entryLevel = FloorLevel(price);
                ceilLevelForUI = CeilLevel(price);  // 上穿时更新 AutoFill

                // 触发条件：上穿了 triggerPrice
                if (entryLevel > state.AgentBreakedLevel && Math.Abs(entryLevel - nextTriggerPrice) < 0.001)
                {
                    //string condStr = entryLevel > sessionHighLevel ? "NewHigh" : "Breakout";
                    //logSignal = $"[Agent] {state.Symbol} Signal: {condStr}, EntryLevel={entryLevel:F2}, SessionHigh={sessionHigh:F2}, BreakedLevel={state.AgentBreakedLevel:F2}";

                    // 如果 Agent 启用，触发 StartOpen
                    if (state.AgentEnabled && Math.Abs(state.AgentLastTriggeredPrice - entryLevel) > 0.001)
                    {
                        state.AgentLastTriggeredPrice = entryLevel;
                        triggerPriceForOpen = entryLevel;
                        logTrigger = $"[Agent] {state.Symbol} Triggering StartOpen at {entryLevel:F2}";
                    }
                }

                // 上穿后更新 BreakedLevel
                if (entryLevel > state.AgentBreakedLevel)
                {
                    state.AgentBreakedLevel = entryLevel;
                }
            }
            else if (crossedDown)
            {
                ceilLevelForUI = CeilLevel(price);  // 下破时更新 AutoFill

                // 下破：新关键位 < 当前 BreakedLevel 才更新
                double newLevel = CeilLevel(price);
                if (newLevel < state.AgentBreakedLevel)
                {
                    state.AgentBreakedLevel = newLevel;
                }
            }

            // 更新当前 bar 内最高价
            if (price > state.AgentCurrentBarHigh)
            {
                state.AgentCurrentBarHigh = price;
            }

            state.AgentPreviousPrice = price;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// 向下取整到 0.5
        /// </summary>
        private static double FloorLevel(double price)
        {
            return Math.Floor(Math.Round(price * 2, 2)) / 2;
        }

        /// <summary>
        /// 向上取整到 0.5（公开给其他模块使用）
        /// </summary>
        public static double CeilLevel(double price)
        {
            return Math.Ceiling(Math.Round(price * 2, 2)) / 2;
        }

        #endregion

        public void Dispose()
        {
            _barAggregator.BarUpdated -= OnBarUpdated;
        }
    }
}