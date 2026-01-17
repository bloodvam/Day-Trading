using TradingEngine.Config;
using TradingEngine.Models;
using TradingEngine.Utils;

namespace TradingEngine.Managers
{
    /// <summary>
    /// 订单管理 - 处理下单逻辑和自动止损
    /// </summary>
    public class OrderManager : IDisposable
    {
        private readonly DasClient _client;
        private readonly AccountManager _accountManager;
        private readonly SymbolDataManager _dataManager;
        private readonly BarAggregator _barAggregator;

        private readonly object _lock = new();

        public event Action<string>? Log;
        public event Action<int>? NewOrderSent;
        public event Action<string>? BuyOrderFailed;  // 买单失败（Canceled/Rejected），参数是 symbol
        public event Action? OrderTriggered;  // 买卖单触发时（清空 Order Log 用）

        public OrderManager(
            DasClient client,
            AccountManager accountManager,
            SymbolDataManager dataManager,
            BarAggregator barAggregator)
        {
            _client = client;
            _accountManager = accountManager;
            _dataManager = dataManager;
            _barAggregator = barAggregator;

            _accountManager.OrderExecuted += OnOrderExecuted;
            _accountManager.OrderRemoved += OnOrderRemoved;
            _accountManager.PositionChanged += OnPositionChanged;
            _client.TradeUpdate += OnTradeUpdate;

            // 监听 symbol 移除事件，自动清理 pending 数据
            _dataManager.SymbolRemoved += OnSymbolRemoved;
        }

        public void Dispose()
        {
            _accountManager.OrderExecuted -= OnOrderExecuted;
            _accountManager.OrderRemoved -= OnOrderRemoved;
            _accountManager.PositionChanged -= OnPositionChanged;
            _client.TradeUpdate -= OnTradeUpdate;
            _dataManager.SymbolRemoved -= OnSymbolRemoved;
        }

        private void OnSymbolRemoved(string symbol)
        {
            ClearSymbolPendingData(symbol);
        }

        private int GetNextToken()
        {
            return (int)(DateTime.UtcNow.Ticks % int.MaxValue);
        }

        #region Event Handlers

        /// <summary>
        /// 订单成交时（记录买入信息）
        /// </summary>
        private void OnOrderExecuted(Order order)
        {
            try
            {
                if (order.Side != OrderSide.Buy) return;

                var state = _dataManager.Get(order.Symbol);
                if (state == null) return;

                // 记录最后一次买入的 OrderId
                lock (_lock)
                {
                    if (order.Token == state.LastBuy.Token && state.LastBuy.OrderId == 0)
                    {
                        state.LastBuy.OrderId = order.OrderId;
                    }
                }
            }
            catch (Exception ex)
            {
                Log?.Invoke($"OnOrderExecuted error: {ex.Message}");
            }
        }

        /// <summary>
        /// 订单移除时 - 只处理买单失败通知
        /// </summary>
        private void OnOrderRemoved(Order order)
        {
            // 买入单被取消/拒绝时，通知买单失败
            if (order.Side == OrderSide.Buy &&
                (order.Status == OrderStatus.Canceled || order.Status == OrderStatus.Rejected))
            {
                BuyOrderFailed?.Invoke(order.Symbol);
            }
        }

        /// <summary>
        /// 持仓变化时
        /// </summary>
        private void OnPositionChanged(Position pos)
        {
            try
            {
                var state = _dataManager.Get(pos.Symbol);
                if (state == null) return;

                // 清除 PendingSell
                if (state.PendingSell != null)
                {
                    state.PendingSell = null;
                }
            }
            catch (Exception ex)
            {
                Log?.Invoke($"OnPositionChanged error: {ex.Message}");
            }
        }

        /// <summary>
        /// 成交时追踪买入成交价
        /// </summary>
        private void OnTradeUpdate(string line)
        {
            try
            {
                var trade = MessageParser.ParseTrade(line);
                if (trade == null) return;
                if (trade.Side != OrderSide.Buy) return;

                var state = _dataManager.Get(trade.Symbol);
                if (state == null) return;

                lock (_lock)
                {
                    if (trade.OrderId == state.LastBuy.OrderId && state.LastBuy.OrderId != 0)
                    {
                        state.LastBuy.AddTrade(trade.Price, trade.Quantity);
                        //Log?.Invoke($"LastBuy Trade [{trade.Symbol}]: OrderId={trade.OrderId}, Price={trade.Price:F2}, Qty={trade.Quantity}, AvgPrice={state.LastBuy.AvgPrice:F2}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log?.Invoke($"OnTradeUpdate error: {ex.Message}");
            }
        }

        #endregion

        #region Smart Stop Price

        private (double stopPrice, bool usedLastBar) GetSmartStopPrice(string symbol, double bid)
        {
            var currentBar = _barAggregator.GetCurrentBar(symbol);
            var lastBar = _barAggregator.GetLastCompletedBar(symbol);
            var state = _dataManager.Get(symbol);

            if (currentBar == null || currentBar.Low <= 0)
            {
                return (0, false);
            }

            double currentLow = currentBar.Low;
            double stopPrice = currentLow;
            bool usedLastBar = false;

            if (bid <= currentLow)
            {
                if (lastBar != null && lastBar.Low > 0)
                {
                    stopPrice = lastBar.Low;
                    usedLastBar = true;
                    Log?.Invoke($"[StopLogic] bid({bid:F2}) <= currentLow({currentLow:F2}), using lastBar.Low({stopPrice:F2})");
                }
            }
            else if (lastBar != null && lastBar.Low > 0 && state?.Quote != null)
            {
                int barInterval = currentBar.IntervalSeconds;
                double quoteSeconds = state.Quote.UpdateTime.TimeOfDay.TotalSeconds;
                double elapsed = quoteSeconds % barInterval;

                bool isFirstSecond = (elapsed == 0);

                if (isFirstSecond && (bid - currentLow) <= 0.01)
                {
                    stopPrice = lastBar.Low;
                    usedLastBar = true;
                    Log?.Invoke($"[StopLogic] In first second, bid-low({bid - currentLow:F4}) <= 0.01, using lastBar.Low({stopPrice:F2})");
                }
            }

            return (stopPrice, usedLastBar);
        }

        #endregion

        #region Trading Actions

        /// <summary>
        /// 核心买入方法
        /// </summary>
        /// <param name="shares">计划买入股数（会根据 BP 和持仓限制调整）</param>
        /// <param name="askPrice">买入参考价（用于计算限价和 BP）</param>
        public async Task<bool> Buy(int shares, double askPrice)
        {
            var config = AppConfig.Instance.Trading;
            string? symbol = _dataManager.ActiveSymbol;

            if (string.IsNullOrEmpty(symbol))
            {
                Log?.Invoke("No symbol subscribed");
                return false;
            }

            if (shares <= 0)
            {
                Log?.Invoke($"Invalid shares: {shares}");
                return false;
            }

            if (askPrice <= 0)
            {
                Log?.Invoke($"Invalid ask price: {askPrice}");
                return false;
            }

            var state = _dataManager.Get(symbol);
            if (state == null)
            {
                Log?.Invoke($"No state for {symbol}");
                return false;
            }

            // BP 限制
            int maxSharesByBP = CalculateMaxSharesByBP(symbol, askPrice);

            // 持仓限制
            var position = _accountManager.GetPosition(symbol);
            int currentShares = position?.Quantity ?? 0;
            int maxSharesByPosition = config.MaxSharesPerSymbol - currentShares;

            // 取较小值
            int maxShares = Math.Min(maxSharesByBP, maxSharesByPosition);

            if (maxShares <= 0)
            {
                Log?.Invoke($"Cannot buy {symbol}: BP limit={maxSharesByBP}, Position limit={maxSharesByPosition} (holding {currentShares})");
                return false;
            }

            if (shares > maxShares)
            {
                Log?.Invoke($"Limit: planned {shares} reduced to {maxShares} (BP={maxSharesByBP}, Pos={maxSharesByPosition})");
                shares = maxShares;
            }

            // 计算限价
            double limitPrice = Math.Round(askPrice * (1 + config.SpreadPercent), 2);

            // 生成 token 并记录 LastBuy
            int token = GetNextToken();
            lock (_lock)
            {
                state.LastBuy.Reset(token);
            }

            // 清空 Order Log
            OrderTriggered?.Invoke();

            // 发送买单
            NewOrderSent?.Invoke(token);
            await _client.PlaceLimitOrder(
                token,
                "B",
                symbol,
                config.BuyRoute,
                shares,
                limitPrice,
                "DAY+"
            );

            return true;
        }

        /// <summary>
        /// 1R 风险买入
        /// </summary>
        public async Task<bool> BuyOneR(double stopPrice)
        {
            var config = AppConfig.Instance.Trading;
            string? symbol = _dataManager.ActiveSymbol;

            if (string.IsNullOrEmpty(symbol))
            {
                Log?.Invoke("No symbol subscribed");
                return false;
            }

            var state = _dataManager.Get(symbol);
            if (state == null || state.Quote.Ask <= 0 || state.Quote.Bid <= 0)
            {
                Log?.Invoke($"No valid quote for {symbol}");
                return false;
            }

            var currentBar = _barAggregator.GetCurrentBar(symbol);
            if (currentBar == null || currentBar.Low <= 0)
            {
                Log?.Invoke($"No bar data for {symbol}");
                return false;
            }

            double askPrice = state.Quote.Ask;

            if (stopPrice <= 0)
            {
                Log?.Invoke($"Invalid stop price: {stopPrice}");
                return false;
            }

            double riskPerShare = askPrice - stopPrice;
            if (riskPerShare <= 0)
            {
                Log?.Invoke($"Invalid risk: Ask={askPrice:F2}, Stop={stopPrice:F2}");
                return false;
            }

            int shares = (int)(config.RiskAmount / riskPerShare);
            if (shares <= 0)
            {
                Log?.Invoke($"Risk too high: ${riskPerShare:F2}/share > ${config.RiskAmount} budget");
                return false;
            }

            return await Buy(shares, askPrice);
        }

        /// <summary>
        /// 加仓
        /// </summary>
        /// <param name="newStopPrice">新的止损价</param>
        /// <param name="riskFactor">风险因子：1.0 = All Profit, 0.5 = 1/2 Profit</param>
        public async Task<bool> AddPosition(double newStopPrice, double riskFactor)
        {
            var config = AppConfig.Instance.Trading;
            string? symbol = _dataManager.ActiveSymbol;

            if (string.IsNullOrEmpty(symbol))
            {
                Log?.Invoke("No symbol subscribed");
                return false;
            }

            var state = _dataManager.Get(symbol);
            if (state == null || state.Quote.Ask <= 0 || state.Quote.Bid <= 0)
            {
                Log?.Invoke($"No valid quote for {symbol}");
                return false;
            }

            var position = _accountManager.GetPosition(symbol);
            if (position == null || position.Quantity <= 0)
            {
                Log?.Invoke($"No position for {symbol}");
                return false;
            }

            double askPrice = state.Quote.Ask;
            double bidPrice = state.Quote.Bid;
            int Q1 = position.Quantity;
            double P1 = position.AvgCost;

            // 计算当前利润（用 Bid）
            double profit = Q1 * (bidPrice - P1);
            Log?.Invoke($"[AddPosition] Q1={Q1}, P1={P1:F3}, Bid={bidPrice:F3}, Profit={profit:F2}");

            // 计算原仓位在止损价的盈亏
            double gainOnOriginal = Q1 * (newStopPrice - P1);
            Log?.Invoke($"[AddPosition] NewStopPrice={newStopPrice:F3}, GainOnOriginal={gainOnOriginal:F2}");

            // 计算加仓每股亏损
            double lossPerShare = askPrice - newStopPrice;
            if (lossPerShare <= 0)
            {
                Log?.Invoke($"[AddPosition] Invalid: Ask={askPrice:F3} <= StopPrice={newStopPrice:F3}");
                return false;
            }

            // 计算加仓股数
            // Add All:  Q2 = gainOnOriginal / lossPerShare
            // Add 1/2:  Q2 = (gainOnOriginal - profit/2) / lossPerShare
            double targetGain = gainOnOriginal - profit * (1 - riskFactor);
            int Q2 = (int)(targetGain / lossPerShare);

            string modeStr = riskFactor >= 1.0 ? "All" : "1/2";
            Log?.Invoke($"[AddPosition] Mode={modeStr}, TargetGain={targetGain:F2}, LossPerShare={lossPerShare:F3}, Q2={Q2}");

            // 如果 Q2 <= 0，不买入
            if (Q2 <= 0)
            {
                Log?.Invoke($"[AddPosition] Q2={Q2} <= 0, skip buy");
                return false;
            }

            // 最小仓位 1R
            int minShares = (int)(config.RiskAmount / lossPerShare);
            if (minShares > 0 && Q2 < minShares)
            {
                Log?.Invoke($"[AddPosition] Q2={Q2} < 1R({minShares}), use 1R");
                Q2 = minShares;
            }

            // 更新 StopPrice
            state.StopPrice = newStopPrice;

            Log?.Invoke($"[AddPosition] ADD {symbol} Q2={Q2} (Ask={askPrice:F2}, NewStop={newStopPrice:F2}, Mode={modeStr})");

            return await Buy(Q2, askPrice);
        }

        public async Task<bool> SellAll()
        {
            var config = AppConfig.Instance.Trading;
            string? symbol = _dataManager.ActiveSymbol;

            if (string.IsNullOrEmpty(symbol))
            {
                Log?.Invoke("No symbol subscribed");
                return false;
            }

            var state = _dataManager.Get(symbol);
            var position = _accountManager.GetPosition(symbol);
            if (position == null || position.Quantity <= 0)
            {
                Log?.Invoke($"No position for {symbol}");
                return false;
            }

            if (state == null || state.Quote.Bid <= 0)
            {
                Log?.Invoke($"No valid quote for {symbol}");
                return false;
            }

            double limitPrice = Math.Round(state.Quote.Bid * (1 - config.SpreadPercent), 2);
            int shares = position.Quantity;

            //Log?.Invoke($"SELL ALL {symbol} {shares}@{limitPrice:F2} (Bid={state.Quote.Bid:F2})");

            // 如果有止损单，先取消
            var stopOrder = _accountManager.GetStopOrder(symbol);
            if (stopOrder != null)
            {
                Log?.Invoke($"Canceling stop order {stopOrder.OrderId} before sell");
                await _client.CancelOrder(stopOrder.OrderId);
            }

            // 清空 Order Log
            OrderTriggered?.Invoke();

            // 直接发卖出订单
            int token = GetNextToken();
            NewOrderSent?.Invoke(token);
            await _client.PlaceLimitOrder(
                token,
                "S",
                symbol,
                config.SellRoute,
                shares,
                limitPrice,
                "DAY+"
            );

            return true;
        }

        /// <summary>
        /// 卖出指定数量的股票
        /// </summary>
        public async Task<bool> SellShares(int sharesToSell)
        {
            var config = AppConfig.Instance.Trading;
            string? symbol = _dataManager.ActiveSymbol;

            if (string.IsNullOrEmpty(symbol))
            {
                Log?.Invoke("No symbol subscribed");
                return false;
            }

            if (sharesToSell <= 0)
            {
                Log?.Invoke($"Invalid shares to sell: {sharesToSell}");
                return false;
            }

            var state = _dataManager.Get(symbol);
            if (state == null || state.Quote.Bid <= 0)
            {
                Log?.Invoke($"No valid quote for {symbol}");
                return false;
            }

            double limitPrice = Math.Round(state.Quote.Bid * (1 - config.SpreadPercent), 2);

            //Log?.Invoke($"SELL {symbol} {sharesToSell}@{limitPrice:F2} (Bid={state.Quote.Bid:F2})");

            // 如果有止损单，先取消
            var stopOrder = _accountManager.GetStopOrder(symbol);
            if (stopOrder != null)
            {
                Log?.Invoke($"Canceling stop order {stopOrder.OrderId} before sell");
                await _client.CancelOrder(stopOrder.OrderId);
            }

            // 清空 Order Log
            OrderTriggered?.Invoke();

            // 直接发卖出订单
            int token = GetNextToken();
            NewOrderSent?.Invoke(token);
            await _client.PlaceLimitOrder(
                token,
                "S",
                symbol,
                config.SellRoute,
                sharesToSell,
                limitPrice,
                "DAY+"
            );

            return true;
        }

        public async Task<bool> SellHalf()
        {
            return await SellPercent(50, "HALF");
        }

        public async Task<bool> Sell70Percent()
        {
            return await SellPercent(70, "70%");
        }

        private async Task<bool> SellPercent(int percent, string label)
        {
            var config = AppConfig.Instance.Trading;
            string? symbol = _dataManager.ActiveSymbol;

            if (string.IsNullOrEmpty(symbol))
            {
                Log?.Invoke("No symbol subscribed");
                return false;
            }

            var state = _dataManager.Get(symbol);
            var position = _accountManager.GetPosition(symbol);
            if (position == null || position.Quantity <= 0)
            {
                Log?.Invoke($"No position for {symbol}");
                return false;
            }

            if (state == null || state.Quote.Bid <= 0)
            {
                Log?.Invoke($"No valid quote for {symbol}");
                return false;
            }

            int shares = position.Quantity * percent / 100;
            if (shares <= 0)
            {
                Log?.Invoke($"Position too small to sell {label}: {position.Quantity}");
                return false;
            }

            double limitPrice = Math.Round(state.Quote.Bid * (1 - config.SpreadPercent), 2);

            //Log?.Invoke($"SELL {label} {symbol} {shares}@{limitPrice:F2} (Bid={state.Quote.Bid:F2})");

            // 如果有止损单，先取消
            var stopOrder = _accountManager.GetStopOrder(symbol);
            if (stopOrder != null)
            {
                Log?.Invoke($"Canceling stop order {stopOrder.OrderId} before sell");
                await _client.CancelOrder(stopOrder.OrderId);
            }

            // 清空 Order Log
            OrderTriggered?.Invoke();

            // 直接发卖出订单
            int token = GetNextToken();
            NewOrderSent?.Invoke(token);
            await _client.PlaceLimitOrder(
                token,
                "S",
                symbol,
                config.SellRoute,
                shares,
                limitPrice,
                "DAY+"
            );

            return true;
        }

        public async Task<bool> AddPositionBreakeven()
        {
            return await AddPositionWithProfitTarget(0);
        }

        public async Task<bool> AddPositionHalfProfit()
        {
            return await AddPositionWithProfitTarget(0.5);
        }

        public async Task<bool> MoveStopToBreakeven()
        {
            string? symbol = _dataManager.ActiveSymbol;

            if (string.IsNullOrEmpty(symbol))
            {
                Log?.Invoke("No symbol subscribed");
                return false;
            }

            var state = _dataManager.Get(symbol);
            var position = _accountManager.GetPosition(symbol);
            if (position == null || position.Quantity <= 0)
            {
                Log?.Invoke($"No position for {symbol}");
                return false;
            }

            double breakevenPrice = state?.LastBuy.AvgPrice > 0 ? state.LastBuy.AvgPrice : position.AvgCost;
            int shares = position.Quantity;

            Log?.Invoke($"Moving stop to breakeven: {symbol} {shares}@{breakevenPrice:F2} (LastBuyAvg={state?.LastBuy.AvgPrice:F2}, PositionAvg={position.AvgCost:F2})");

            // 如果有旧止损单，先取消
            var stopOrder = _accountManager.GetStopOrder(symbol);
            if (stopOrder != null)
            {
                await _client.CancelOrder(stopOrder.OrderId);
            }

            // 直接挂新止损单到保本价
            await PlaceStopOrder(symbol, shares, breakevenPrice);

            return true;
        }

        private async Task<bool> AddPositionWithProfitTarget(double profitRatio)
        {
            var config = AppConfig.Instance.Trading;
            string? symbol = _dataManager.ActiveSymbol;

            if (string.IsNullOrEmpty(symbol))
            {
                Log?.Invoke("No symbol subscribed");
                return false;
            }

            var state = _dataManager.Get(symbol);
            var position = _accountManager.GetPosition(symbol);
            if (position == null || position.Quantity <= 0)
            {
                Log?.Invoke($"No position for {symbol}");
                return false;
            }

            if (state == null || state.Quote.Ask <= 0 || state.Quote.Bid <= 0)
            {
                Log?.Invoke($"No valid quote for {symbol}");
                return false;
            }

            var currentBar = _barAggregator.GetCurrentBar(symbol);
            if (currentBar == null || currentBar.Low <= 0)
            {
                Log?.Invoke($"No bar data for {symbol}");
                return false;
            }

            double avgCost = position.AvgCost;
            int currentQty = position.Quantity;
            double bidPrice = state.Quote.Bid;
            double askPrice = state.Quote.Ask;

            var (stopPrice, usedLastBar) = GetSmartStopPrice(symbol, bidPrice);
            if (stopPrice <= 0)
            {
                Log?.Invoke($"Cannot determine stop price for {symbol}");
                return false;
            }

            double currentProfit = (bidPrice - avgCost) * currentQty;
            if (currentProfit <= 0)
            {
                Log?.Invoke($"No profit to add position: currentProfit={currentProfit:F2}");
                return false;
            }

            if (stopPrice >= askPrice)
            {
                Log?.Invoke($"Invalid stop price: Stop={stopPrice:F2} >= Ask={askPrice:F2}");
                return false;
            }

            double targetProfit = currentProfit * profitRatio;
            double profitAtStop = (stopPrice - avgCost) * currentQty;
            double lossPerShareOnAdd = askPrice - stopPrice;

            if (lossPerShareOnAdd <= 0)
            {
                Log?.Invoke($"Invalid calculation: Ask={askPrice:F2} <= stopPrice={stopPrice:F2}");
                return false;
            }

            double maxShares = (profitAtStop - targetProfit) / lossPerShareOnAdd;
            int sharesToAdd = (int)Math.Floor(maxShares);

            if (sharesToAdd <= 0)
            {
                Log?.Invoke($"Cannot add position: maxShares={maxShares:F2}");
                return false;
            }

            string mode = profitRatio == 0 ? "BREAKEVEN" : $"KEEP {profitRatio * 100:F0}% PROFIT";
            string barInfo = usedLastBar ? "LastBar" : "CurrentBar";
            Log?.Invoke($"[AddPositionWithProfitTarget] {symbol} sharesToAdd={sharesToAdd} ({mode})");
            Log?.Invoke($"  Current: {currentQty}@{avgCost:F2}, Profit={currentProfit:F2}");
            Log?.Invoke($"  New Stop={stopPrice:F2} [{barInfo}], TargetProfit={targetProfit:F2}");

            // 如果有旧止损单，先取消
            var stopOrder = _accountManager.GetStopOrder(symbol);
            if (stopOrder != null)
            {
                await _client.CancelOrder(stopOrder.OrderId);
            }

            return await Buy(sharesToAdd, askPrice);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// 计算单股票可买入的最大股数（基于 BP 限制）
        /// 公式：可用 BP = Equity × 3 × 0.97 - 当前持仓成本
        /// </summary>
        private int CalculateMaxSharesByBP(string symbol, double buyPrice)
        {
            var config = AppConfig.Instance.Trading;
            double equity = _accountManager.AccountInfo.CurrentEquity;

            if (equity <= 0 || buyPrice <= 0) return 0;

            // 获取当前持仓成本
            double currentPositionCost = 0;
            var position = _accountManager.GetPosition(symbol);
            if (position != null && position.Quantity > 0)
            {
                currentPositionCost = position.AvgCost * position.Quantity;
            }

            // 可用 BP = Equity × 3 × 0.97 - 当前持仓成本
            double availableBP = equity * config.SingleStockBPMultiplier * 0.97 - currentPositionCost;

            if (availableBP <= 0) return 0;

            int maxShares = (int)(availableBP / buyPrice);
            return maxShares;
        }

        private async Task PlaceStopOrder(string symbol, int shares, double stopPrice)
        {
            var config = AppConfig.Instance.Trading;

            double limitPrice = Math.Round(stopPrice * (1 - config.SpreadPercent), 2);
            int token = GetNextToken();

            Log?.Invoke($"STOP {symbol} {shares}@{stopPrice:F2} (Limit={limitPrice:F2}, Token={token})");

            NewOrderSent?.Invoke(token);
            await _client.PlaceStopLimitPostOrder(
                token,
                "S",
                symbol,
                config.StopRoute,
                shares,
                stopPrice,
                limitPrice,
                "DAY+"
            );
        }

        public async Task CancelAllOrders()
        {
            await _client.CancelAll();

            // 清除所有 symbol 的 pending 状态
            _dataManager.ForEach(state =>
            {
                state.PendingSell = null;
                state.PendingMoveStop = false;
            });
        }

        /// <summary>
        /// 清理指定 symbol 的 pending 数据（SymbolRemoved 事件触发）
        /// </summary>
        private void ClearSymbolPendingData(string symbol)
        {
            // SymbolState 中的 PendingSell 和 PendingMoveStop 会随 SymbolDataManager.Remove 一起被清除
        }

        #endregion
    }
}