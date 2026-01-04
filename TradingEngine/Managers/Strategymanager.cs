using TradingEngine.Models;
using TradingEngine.Utils;

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
        }

        /// <summary>
        /// 启动策略
        /// </summary>
        public void StartStrategy(string symbol, double triggerPrice)
        {
            var state = _dataManager.Get(symbol);
            if (state == null) return;

            // 更新 TriggerPrice
            state.TriggerPrice = triggerPrice;
            state.StrategyEnabled = true;
            state.HasTriggeredBuy = false;   // 允许新的买入触发
            state.HasTriggeredSell = false;  // 允许卖出

            // 检查是否有持仓
            var position = _accountManager.GetPosition(symbol);
            bool hasPosition = position != null && position.Quantity > 0;

            if (!hasPosition)
            {
                // 没有持仓，清除 StopPrice
                state.StopPrice = 0;
                Log?.Invoke($"[Strategy] Started for {symbol}: TriggerPrice={triggerPrice:F3}");
            }
            else
            {
                // 有持仓，保留 StopPrice，新买入触发后才更新
                Log?.Invoke($"[Strategy] Updated for {symbol}: TriggerPrice={triggerPrice:F3}, StopPrice={state.StopPrice:F3} (preserved until new buy)");
            }
        }

        /// <summary>
        /// 停止策略
        /// </summary>
        public void StopStrategy(string symbol)
        {
            var state = _dataManager.Get(symbol);
            if (state == null) return;

            state.StrategyEnabled = false;
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
                // 检查买入条件
                CheckBuyCondition(state, tick);

                // 检查卖出条件
                CheckSellCondition(state, tick);
            }
        }

        /// <summary>
        /// 是否满足 BarAggregator 的过滤规则
        /// </summary>
        private bool IsValidForAggregator(Tick tick)
        {
            // FADF 交易所的数据，只有 IsValidForLastPrice 才算入
            // 其他交易所的数据全部算入
            if (tick.Exchange == "FADF" && !tick.IsValidForLastPrice) return false;
            return true;
        }

        private async void CheckBuyCondition(SymbolState state, Tick tick)
        {
            // 条件1: 策略已启动
            if (!state.StrategyEnabled) return;

            // 条件2: 未触发过买入
            if (state.HasTriggeredBuy) return;

            // 条件3: Ask >= TriggerPrice
            double ask = state.Quote.Ask;
            if (ask < state.TriggerPrice) return;

            // 条件4: tick.Price >= TriggerPrice
            if (tick.Price < state.TriggerPrice) return;

            // 计算 StopPrice = Max(barLow, triggerPrice - 0.51)
            var currentBar = _barAggregator.GetCurrentBar(state.Symbol);
            double barLow = currentBar?.Low ?? 0;
            double triggerStop = state.TriggerPrice - 0.51;
            double stopPrice = Math.Max(barLow, triggerStop);

            // 保存 StopPrice
            state.StopPrice = stopPrice;

            // 触发买入
            state.HasTriggeredBuy = true;
            Log?.Invoke($"[Strategy] {state.Symbol} BUY triggered! Ask={ask:F3} >= Trigger={state.TriggerPrice:F3}, TickPrice={tick.Price:F3}, StopPrice={stopPrice:F3}");

            try
            {
                await _orderManager.BuyOneR();
            }
            catch (Exception ex)
            {
                Log?.Invoke($"[Strategy] {state.Symbol} BuyOneR failed: {ex.Message}");
                state.HasTriggeredBuy = false;  // 允许重试
            }
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

            // 触发卖出
            state.HasTriggeredSell = true;
            state.StrategyEnabled = false;  // 一轮结束
            Log?.Invoke($"[Strategy] {state.Symbol} SELL triggered! Bid={bid:F3} < StopPrice={state.StopPrice:F3}, TickPrice={tick.Price:F3}");

            try
            {
                await _orderManager.SellAll();
            }
            catch (Exception ex)
            {
                Log?.Invoke($"[Strategy] {state.Symbol} SellAll failed: {ex.Message}");
                state.HasTriggeredSell = false;
                state.StrategyEnabled = true;  // 允许重试
            }
        }

        public void Dispose()
        {
            _client.TsReceived -= OnTsReceived;
        }
    }
}