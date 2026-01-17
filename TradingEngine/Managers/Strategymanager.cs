using TradingEngine.Models;
using TradingEngine.Utils;
using TradingEngine.Config;
using System.Drawing;

namespace TradingEngine.Managers
{
    /// <summary>
    /// 策略管理器 - 监听行情触发买卖
    /// </summary>
    public class StrategyManager : IDisposable
    {
        private readonly DasClient _client;
        private readonly SymbolDataManager _dataManager;
        private readonly BarAggregator _barAggregator;
        private readonly OrderManager _orderManager;
        private readonly AccountManager _accountManager;

        public event Action<string>? Log;
        public event Action<string, Color>? LogWithColor;  // 带颜色的日志（打印到 Strategy Log）
        public event Action<string, Color>? TrailingStopLog;  // 带颜色的日志（打印到 Agent Log）
        public event Action<string, double, double>? TrailingStopUpdated;  // symbol, trailHalf, trailAll
        public event Action<string>? BuyTriggered;  // 实际发送买单时触发（symbol）
        public event Action<Tick>? ValidTickReceived;  // 有效 tick（供 AgentStrategy 使用）

        public StrategyManager(
            DasClient client,
            SymbolDataManager dataManager,
            BarAggregator barAggregator,
            OrderManager orderManager,
            AccountManager accountManager)
        {
            _client = client;
            _dataManager = dataManager;
            _barAggregator = barAggregator;
            _orderManager = orderManager;
            _accountManager = accountManager;

            _client.TsReceived += OnTsReceived;
            _barAggregator.BarCompleted += OnBarCompleted;
            _orderManager.BuyOrderFailed += OnBuyOrderFailed;
        }

        private void OnBuyOrderFailed(string symbol)
        {
            var state = _dataManager.Get(symbol);
            if (state == null) return;

            // 订单被拒绝，停止策略
            if (state.StrategyEnabled)
            {
                state.StrategyEnabled = false;
                state.HasTriggeredBuy = false;
                Log?.Invoke($"[Strategy] {symbol} Order rejected, strategy stopped");
            }
        }

        /// <summary>
        /// 开仓
        /// </summary>
        public void StartOpen(string symbol, double triggerPrice)
        {
            var state = _dataManager.Get(symbol);
            if (state == null) return;

            state.TriggerPrice = triggerPrice;
            state.StrategyEnabled = true;
            state.HasTriggeredBuy = false;
            state.HasTriggeredSell = false;
            state.PositionMode = AddPositionMode.Open;
            state.StopPrice = 0;
            state.IsHighBreakout = false;

            Log?.Invoke($"[Strategy] Open started for {symbol}: TriggerPrice={triggerPrice:F3}");
        }

        /// <summary>
        /// 高点突破策略（止损只用 barLow - 0.01）
        /// </summary>
        public void StartHighBreakout(string symbol, double triggerPrice)
        {
            var state = _dataManager.Get(symbol);
            if (state == null) return;

            state.TriggerPrice = triggerPrice;
            state.StrategyEnabled = true;
            state.HasTriggeredBuy = false;
            state.HasTriggeredSell = false;
            state.PositionMode = AddPositionMode.Open;
            state.StopPrice = 0;
            state.IsHighBreakout = true;

            Log?.Invoke($"[Strategy] HighBreakout started for {symbol}: TriggerPrice={triggerPrice:F3}");
        }

        /// <summary>
        /// 加仓 All Profit
        /// </summary>
        public void StartAddAll(string symbol, double triggerPrice)
        {
            StartAddPosition(symbol, triggerPrice, AddPositionMode.AddAll);
        }

        /// <summary>
        /// 加仓 1/2 Profit
        /// </summary>
        public void StartAddHalf(string symbol, double triggerPrice)
        {
            StartAddPosition(symbol, triggerPrice, AddPositionMode.AddHalf);
        }

        private void StartAddPosition(string symbol, double triggerPrice, AddPositionMode mode)
        {
            var state = _dataManager.Get(symbol);
            if (state == null) return;

            // 检查是否有持仓
            var position = _accountManager.GetPosition(symbol);
            if (position == null || position.Quantity <= 0)
            {
                Log?.Invoke($"[Strategy] Cannot add position for {symbol}: No position");
                return;
            }

            state.TriggerPrice = triggerPrice;
            state.StrategyEnabled = true;
            state.HasTriggeredBuy = false;
            state.HasTriggeredSell = false;
            state.PositionMode = mode;
            // 保留原 StopPrice

            string modeStr = mode == AddPositionMode.AddAll ? "All" : "1/2";
            Log?.Invoke($"[Strategy] Add {modeStr} started for {symbol}: TriggerPrice={triggerPrice:F3}, StopPrice={state.StopPrice:F3} (preserved)");
        }

        /// <summary>
        /// 停止策略
        /// </summary>
        public void StopStrategy(string symbol)
        {
            var state = _dataManager.Get(symbol);
            if (state == null) return;

            state.StrategyEnabled = false;
            state.PositionMode = AddPositionMode.None;
            Log?.Invoke($"[Strategy] Stopped for {symbol}");
        }

        private void OnTsReceived(string line)
        {
            var tick = MessageParser.ParseTick(line);
            if (tick == null) return;

            var state = _dataManager.Get(tick.Symbol);
            if (state == null) return;

            // 只处理满足 BarAggregator 过滤规则的 tick
            if (IsValidForAggregator(tick))
            {
                // 通知其他模块（AgentStrategy）
                ValidTickReceived?.Invoke(tick);

                // 更新 Trailing Stop 最高价
                UpdateTrailingSinceHigh(state, tick);

                // 检查买入条件
                CheckBuyCondition(state, tick);

                // 检查止损卖出条件
                CheckSellCondition(state, tick);

                // 检查 Trailing Stop 卖出条件
                CheckTrailingStopCondition(state, tick);
            }
        }

        private void OnBarCompleted(Bar bar)
        {
            var state = _dataManager.Get(bar.Symbol);
            if (state == null) return;

            // 每根 bar 结束重置止损累计量
            state.StopVolumeAccumulated = 0;

            // 只有有持仓时才处理
            var position = _accountManager.GetPosition(bar.Symbol);
            if (position == null || position.Quantity <= 0) return;

            // 数据还没准备好，准备数据
            if (state.TrailHalf <= 0)
            {
                ActivateTrailingStop(state, bar);
                return;
            }

            // 数据已准备好，累积更新（无论是否点了 Start）
            double range = bar.High - bar.Low;

            // 如果是同一个 bar，更新；否则添加
            if (bar.Time == state.LastBarTime && state.BarRanges.Count > 0)
            {
                state.BarRanges[state.BarRanges.Count - 1] = range;
            }
            else
            {
                state.BarRanges.Add(range);
                state.LastBarTime = bar.Time;
            }

            // 更新 TrailHalf（第二大波动）
            double oldTrailHalf = state.TrailHalf;
            double oldTrailAll = state.TrailAll;
            UpdateTrailHalf(state);

            // 只有变化时才打印日志
            if (Math.Abs(state.TrailHalf - oldTrailHalf) > 0.0001 || Math.Abs(state.TrailAll - oldTrailAll) > 0.0001)
            {
                TrailingStopLog?.Invoke($"[TrailingStop] {bar.Symbol} Updated: Range={range:F3}, TrailHalf={state.TrailHalf:F3}, TrailAll={state.TrailAll:F3}", Color.Black);
                TrailingStopUpdated?.Invoke(bar.Symbol, state.TrailHalf, state.TrailAll);
            }
        }

        /// <summary>
        /// 准备 Trailing Stop 数据（买入那根 bar 完成时调用，但不激活）
        /// </summary>
        private void ActivateTrailingStop(SymbolState state, Bar completedBar)
        {
            // 获取过去 5 根 bar（包括刚完成的这根）
            var lastBars = _barAggregator.GetLastBars(state.Symbol, 5);

            // 计算每根 bar 的 range
            state.BarRanges.Clear();
            foreach (var bar in lastBars)
            {
                double range = bar.High - bar.Low;
                state.BarRanges.Add(range);
            }

            // 更新 TrailHalf（第二大波动）
            UpdateTrailHalf(state);

            // 初始化最高价为刚完成的 bar 的 High
            state.TrailingSinceHigh = completedBar.High;
            state.LastBarTime = completedBar.Time;
            // 不设置 TrailingStopActive = true，等用户手动点 Start

            TrailingStopLog?.Invoke($"[TrailingStop] {state.Symbol} Ready: BarCount={state.BarRanges.Count}, TrailHalf={state.TrailHalf:F3}, TrailAll={state.TrailAll:F3}, High={state.TrailingSinceHigh:F3}", Color.Black);
            TrailingStopUpdated?.Invoke(state.Symbol, state.TrailHalf, state.TrailAll);
        }

        /// <summary>
        /// 手动启动 Trailing Stop
        /// </summary>
        public void StartTrailingStop(string symbol)
        {
            var state = _dataManager.Get(symbol);
            if (state == null) return;

            // 必须有 TrailHalf 数据
            if (state.TrailHalf <= 0)
            {
                Log?.Invoke($"[TrailingStop] {symbol} Cannot start: TrailHalf not ready");
                return;
            }

            // 已经激活
            if (state.TrailingStopActive)
            {
                Log?.Invoke($"[TrailingStop] {symbol} Already active");
                return;
            }

            state.TrailingStopActive = true;
            TrailingStopLog?.Invoke($"[TrailingStop] {symbol} Started! TrailHalf={state.TrailHalf:F3}, TrailAll={state.TrailAll:F3}, High={state.TrailingSinceHigh:F3}", Color.Black);
        }

        /// <summary>
        /// 手动停止 Trailing Stop
        /// </summary>
        public void StopTrailingStop(string symbol)
        {
            var state = _dataManager.Get(symbol);
            if (state == null) return;

            if (!state.TrailingStopActive)
            {
                Log?.Invoke($"[TrailingStop] {symbol} Not active");
                return;
            }

            state.TrailingStopActive = false;
            TrailingStopLog?.Invoke($"[TrailingStop] {symbol} Stopped", Color.Black);
        }

        /// <summary>
        /// 切换 Trailing Stop 状态
        /// </summary>
        public bool ToggleTrailingStop(string symbol)
        {
            var state = _dataManager.Get(symbol);
            if (state == null) return false;

            if (state.TrailingStopActive)
            {
                StopTrailingStop(symbol);
                return false;
            }
            else
            {
                StartTrailingStop(symbol);
                return state.TrailingStopActive;
            }
        }

        /// <summary>
        /// 更新 TrailHalf（第二大波动）
        /// </summary>
        private void UpdateTrailHalf(SymbolState state)
        {
            if (state.BarRanges.Count == 0) return;

            var config = AppConfig.Instance.Trading;

            if (state.BarRanges.Count == 1)
            {
                // 只有一根 bar，用这根 bar 的一半
                state.TrailHalf = state.BarRanges[0] / 2;
            }
            else
            {
                // 按波动幅度排序，取第二大
                var sorted = state.BarRanges.OrderByDescending(x => x).ToList();
                state.TrailHalf = sorted[1];
            }

            state.TrailAll = Math.Max(state.TrailHalf * 1.25, config.MinTrailAll);
        }

        /// <summary>
        /// 更新买入后最高价
        /// </summary>
        private void UpdateTrailingSinceHigh(SymbolState state, Tick tick)
        {
            // 只有 Trailing Stop 激活时才更新
            if (!state.TrailingStopActive) return;

            // 只有有持仓时才更新
            var position = _accountManager.GetPosition(state.Symbol);
            if (position == null || position.Quantity <= 0) return;

            if (tick.Price > state.TrailingSinceHigh)
            {
                double oldHigh = state.TrailingSinceHigh;
                state.TrailingSinceHigh = tick.Price;

                // 新高后重置 HasTriggeredTrailHalf，允许再次触发
                if (oldHigh > 0 && state.HasTriggeredTrailHalf)
                {
                    state.HasTriggeredTrailHalf = false;
                    TrailingStopLog?.Invoke($"[TrailingStop] {state.Symbol} New high {tick.Price:F3}, reset TrailHalf trigger", Color.Black);
                }
            }
        }

        /// <summary>
        /// 是否满足 BarAggregator 的过滤规则
        /// </summary>
        private bool IsValidForAggregator(Tick tick)
        {
            if (tick.Exchange == "FADF" && !tick.IsValidForLastPrice) return false;
            return true;
        }

        private async void CheckBuyCondition(SymbolState state, Tick tick)
        {
            // 条件1: 策略已启动
            if (!state.StrategyEnabled) return;

            // 条件2: 未触发过买入
            if (state.HasTriggeredBuy) return;

            // 条件3: 有有效的 PositionMode
            if (state.PositionMode == AddPositionMode.None) return;

            // 条件4: Ask >= TriggerPrice
            double ask = state.Quote.Ask;
            if (ask < state.TriggerPrice) return;

            // 条件5: tick.Price >= TriggerPrice
            if (tick.Price < state.TriggerPrice) return;

            // 计算 StopPrice
            var currentBar = _barAggregator.GetCurrentBar(state.Symbol);
            double barLow = currentBar?.Low ?? 0;
            double newStopPrice;

            if (state.IsHighBreakout)
            {
                // 高点突破策略：只用 barLow - 0.01
                newStopPrice = barLow - 0.01;
            }
            else
            {
                // 普通策略：Max(barLow - 0.01, triggerPrice - 0.51)
                double triggerStop = state.TriggerPrice - 0.51;
                newStopPrice = Math.Max(barLow - 0.01, triggerStop);
            }

            // 检查最小风险限制，如果 ask - stop < MinRiskPerShare，自动调低止损价
            var config = AppConfig.Instance.Trading;
            if (ask - newStopPrice < config.MinRiskPerShare)
            {
                double oldStop = newStopPrice;
                newStopPrice = ask - config.MinRiskPerShare;
                Log?.Invoke($"[Strategy] Stop adjusted: {oldStop:F2} -> {newStopPrice:F2} (min risk = {config.MinRiskPerShare})");
            }

            // 标记已触发
            state.HasTriggeredBuy = true;

            // 通知实际买入触发（用于关闭 Agent 等）
            BuyTriggered?.Invoke(state.Symbol);

            try
            {
                switch (state.PositionMode)
                {
                    case AddPositionMode.Open:
                        state.StopPrice = newStopPrice;

                        // 初始化 Trailing Stop 状态
                        InitTrailingStop(state);

                        LogWithColor?.Invoke($"[Strategy] {state.Symbol} OPEN triggered! Ask={ask:F3} >= Trigger={state.TriggerPrice:F3}, StopPrice={newStopPrice:F3}", Color.DarkGreen);
                        await _orderManager.BuyOneR(newStopPrice);
                        break;

                    case AddPositionMode.AddAll:
                    case AddPositionMode.AddHalf:
                        double riskFactor = state.PositionMode == AddPositionMode.AddAll ? 1.0 : 0.5;
                        string modeStr = state.PositionMode == AddPositionMode.AddAll ? "All" : "1/2";
                        LogWithColor?.Invoke($"[Strategy] {state.Symbol} ADD {modeStr} triggered! Ask={ask:F3} >= Trigger={state.TriggerPrice:F3}, NewStopPrice={newStopPrice:F3}", Color.DarkGreen);
                        await _orderManager.AddPosition(newStopPrice, riskFactor);
                        break;
                }
            }
            catch (Exception ex)
            {
                Log?.Invoke($"[Strategy] {state.Symbol} Buy failed: {ex.Message}");
                state.HasTriggeredBuy = false;  // 允许重试
            }
        }

        /// <summary>
        /// 重置 Trailing Stop 状态（等买入那根 bar 完成后才真正开启）
        /// </summary>
        private void InitTrailingStop(SymbolState state)
        {
            state.TrailingSinceHigh = 0;
            state.BarRanges.Clear();
            state.HasTriggeredTrailHalf = false;
            state.RemainingShares = 0;
            state.LastBarTime = DateTime.MinValue;
            state.TrailingStopActive = false;
            state.TrailHalf = 0;
            state.TrailAll = 0;

            //Log?.Invoke($"[TrailingStop] {state.Symbol} Pending: waiting for current bar to complete");
        }

        private async void CheckSellCondition(SymbolState state, Tick tick)
        {
            // 条件1: 策略已启动
            if (!state.StrategyEnabled) return;

            // 条件2: 未卖出
            if (state.HasTriggeredSell) return;

            // 条件3: 有持仓
            var position = _accountManager.GetPosition(state.Symbol);
            if (position == null || position.Quantity <= 0) return;

            // 条件4: StopPrice 有效
            if (state.StopPrice <= 0) return;

            // 条件5: Bid < StopPrice
            double bid = state.Quote.Bid;
            if (bid >= state.StopPrice) return;

            // 条件6: tick.Price < StopPrice
            if (tick.Price >= state.StopPrice) return;

            // 条件7: 累计成交量 > MinStopVolume
            var config = AppConfig.Instance.Trading;
            state.StopVolumeAccumulated += tick.Volume;
            if (state.StopVolumeAccumulated <= config.MinStopVolume) return;

            // 触发卖出
            state.HasTriggeredSell = true;

            // 判断卖出数量：TrailSellHalf 已触发时只卖剩余
            int sharesToSell = state.RemainingShares > 0 ? state.RemainingShares : 0;
            string sellType = sharesToSell > 0 ? $"SellShares({sharesToSell})" : "SellAll";

            LogWithColor?.Invoke($"[Strategy] {state.Symbol} STOP LOSS triggered! Bid={bid:F3} < StopPrice={state.StopPrice:F3}, TickPrice={tick.Price:F3}, {sellType}", Color.Red);

            try
            {
                if (sharesToSell > 0)
                {
                    await _orderManager.SellShares(sharesToSell);
                }
                else
                {
                    await _orderManager.SellAll();
                }

                // 全部清仓后清除状态
                state.ClearStrategyState();
                TrailingStopUpdated?.Invoke(state.Symbol, 0, 0);
            }
            catch (Exception ex)
            {
                Log?.Invoke($"[Strategy] {state.Symbol} SellAll failed: {ex.Message}");
                state.HasTriggeredSell = false;
            }
        }

        private async void CheckTrailingStopCondition(SymbolState state, Tick tick)
        {
            // 条件0: 还没触发过卖出（避免和 StopLoss 重复卖出）
            if (state.HasTriggeredSell) return;

            // 条件1: Trailing Stop 已激活
            if (!state.TrailingStopActive) return;

            // 条件2: 有持仓
            var position = _accountManager.GetPosition(state.Symbol);
            if (position == null || position.Quantity <= 0) return;

            // 条件3: TrailHalf 有效
            if (state.TrailHalf <= 0 || state.TrailingSinceHigh <= 0) return;

            double bid = state.Quote.Bid;

            // 检查 Sell All 条件
            double sellAllPrice = state.TrailingSinceHigh - state.TrailAll;
            if (bid < sellAllPrice && tick.Price < sellAllPrice)
            {
                // 计算要卖的数量：RemainingShares > 0 说明 SellHalf 已触发
                int sharesToSell = state.RemainingShares > 0
                    ? state.RemainingShares
                    : position.Quantity;

                // 标记已触发卖出（防止 StopLoss 重复卖出）
                state.HasTriggeredSell = true;

                TrailingStopLog?.Invoke($"[TrailingStop] {state.Symbol} SELL ALL triggered! Bid={bid:F3} < SellAllPrice={sellAllPrice:F3}, Shares={sharesToSell}", Color.Blue);

                try
                {
                    await _orderManager.SellShares(sharesToSell);

                    // 全部清仓后清除状态
                    state.ClearStrategyState();
                    TrailingStopUpdated?.Invoke(state.Symbol, 0, 0);
                }
                catch (Exception ex)
                {
                    TrailingStopLog?.Invoke($"[TrailingStop] {state.Symbol} SellAll failed: {ex.Message}", Color.Red);
                    state.HasTriggeredSell = false;  // 失败时重置
                }
                return;
            }

            // 检查 Sell Half 条件
            if (!state.HasTriggeredTrailHalf)
            {
                double sellHalfPrice = state.TrailingSinceHigh - state.TrailHalf;
                if (bid < sellHalfPrice && tick.Price < sellHalfPrice)
                {
                    state.HasTriggeredTrailHalf = true;

                    // 计算并保存剩余股数
                    int sellShares = position.Quantity / 2;
                    state.RemainingShares = position.Quantity - sellShares;

                    TrailingStopLog?.Invoke($"[TrailingStop] {state.Symbol} SELL HALF triggered! Bid={bid:F3} < SellHalfPrice={sellHalfPrice:F3}, Sell={sellShares}, Remaining={state.RemainingShares}", Color.DarkGoldenrod);

                    try
                    {
                        await _orderManager.SellShares(sellShares);
                    }
                    catch (Exception ex)
                    {
                        TrailingStopLog?.Invoke($"[TrailingStop] {state.Symbol} SellHalf failed: {ex.Message}", Color.Red);
                        state.HasTriggeredTrailHalf = false;
                        state.RemainingShares = 0;
                    }
                }
            }
        }

        public void Dispose()
        {
            _client.TsReceived -= OnTsReceived;
            _barAggregator.BarCompleted -= OnBarCompleted;
            _orderManager.BuyOrderFailed -= OnBuyOrderFailed;
        }
    }
}