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

        // 追踪待挂止损的买入订单: Token -> (StopPrice, EntryBar, Symbol)
        private readonly Dictionary<int, (double stopPrice, Bar entryBar, string symbol)> _pendingStops = new();

        public event Action<string>? Log;
        public event Action<int>? NewOrderSent;

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
        /// 订单成交时（买入成交 → 挂止损单）
        /// </summary>
        private async void OnOrderExecuted(Order order)
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
                        Log?.Invoke($"LastBuy [{order.Symbol}]: Token {order.Token} -> OrderId {order.OrderId}");
                    }
                }

                (double stopPrice, Bar entryBar, string symbol) pendingStop;
                lock (_lock)
                {
                    if (!_pendingStops.TryGetValue(order.Token, out pendingStop))
                        return;

                    _pendingStops.Remove(order.Token);
                }

                var position = _accountManager.GetPosition(order.Symbol);
                int totalShares = position?.Quantity ?? order.FilledQuantity;

                Log?.Invoke($"Buy order {order.OrderId} executed, placing stop for {totalShares} shares at {pendingStop.stopPrice:F2}");

                // TODO: 策略测试期间暂时不挂止损，由策略控制卖出
                // await PlaceStopOrder(order.Symbol, totalShares, pendingStop.stopPrice);
            }
            catch (Exception ex)
            {
                Log?.Invoke($"OnOrderExecuted error: {ex.Message}");
            }
        }

        /// <summary>
        /// 订单移除时（止损单Canceled → 发卖出单或挂新止损单）
        /// </summary>
        private async void OnOrderRemoved(Order order)
        {
            try
            {
                // 买入单被取消/拒绝时，清理 _pendingStops
                if (order.Side == OrderSide.Buy &&
                    (order.Status == OrderStatus.Canceled || order.Status == OrderStatus.Rejected))
                {
                    lock (_lock)
                    {
                        if (_pendingStops.ContainsKey(order.Token))
                        {
                            _pendingStops.Remove(order.Token);
                            Log?.Invoke($"Buy order {order.OrderId} {order.Status}, removed pending stop (Token={order.Token})");
                        }
                    }
                    return;
                }

                // 以下是止损单处理逻辑
                if (order.Status != OrderStatus.Canceled) return;
                if (order.Type != OrderType.StopLimit &&
                    order.Type != OrderType.StopLimitPost &&
                    order.Type != OrderType.StopMarket) return;

                var state = _dataManager.Get(order.Symbol);
                if (state == null) return;

                // 先检查是不是 MoveStopToBreakeven
                bool isMoveStop = false;
                lock (_lock)
                {
                    if (state.PendingMoveStop)
                    {
                        state.PendingMoveStop = false;
                        isMoveStop = true;
                    }
                }

                if (isMoveStop)
                {
                    var position = _accountManager.GetPosition(order.Symbol);
                    if (position != null && position.Quantity > 0)
                    {
                        double breakevenPrice = state.LastBuy.AvgPrice > 0 ? state.LastBuy.AvgPrice : position.AvgCost;
                        Log?.Invoke($"Stop order {order.OrderId} canceled, placing new stop at breakeven {breakevenPrice:F2}");
                        await PlaceStopOrder(order.Symbol, position.Quantity, breakevenPrice);
                    }
                    return;
                }

                // 否则是卖出流程
                var pendingSell = state.PendingSell;
                if (pendingSell == null) return;

                Log?.Invoke($"Stop order {order.OrderId} canceled, now placing sell order");

                var config = AppConfig.Instance.Trading;
                int token = GetNextToken();

                NewOrderSent?.Invoke(token);
                await _client.PlaceLimitOrder(
                    token,
                    "S",
                    order.Symbol,
                    config.SellRoute,
                    pendingSell.SellShares,
                    pendingSell.SellLimitPrice,
                    "DAY+"
                );
            }
            catch (Exception ex)
            {
                Log?.Invoke($"OnOrderRemoved error: {ex.Message}");
            }
        }

        /// <summary>
        /// 持仓变化时（卖出后 → 重挂止损）
        /// </summary>
        private async void OnPositionChanged(Position pos)
        {
            try
            {
                var state = _dataManager.Get(pos.Symbol);
                if (state == null) return;

                var pendingSell = state.PendingSell;
                if (pendingSell == null) return;

                // 清除 PendingSell
                state.PendingSell = null;

                if (pendingSell.IsSellAll)
                {
                    Log?.Invoke($"Sell all completed for {pos.Symbol}, no stop order needed");
                    return;
                }

                if (pos.Quantity > 0 && pendingSell.StopTriggerPrice > 0)
                {
                    Log?.Invoke($"Position updated for {pos.Symbol}: {pos.Quantity} shares, re-placing stop at {pendingSell.StopTriggerPrice:F2}");
                    await PlaceStopOrder(pos.Symbol, pos.Quantity, pendingSell.StopTriggerPrice);
                }
                else
                {
                    Log?.Invoke($"Position cleared for {pos.Symbol}, no stop order needed");
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
                        Log?.Invoke($"LastBuy Trade [{trade.Symbol}]: OrderId={trade.OrderId}, Price={trade.Price:F2}, Qty={trade.Quantity}, AvgPrice={state.LastBuy.AvgPrice:F2}");
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

        public async Task<bool> BuyOneR()
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
            double bidPrice = state.Quote.Bid;

            var (stopPrice, usedLastBar) = GetSmartStopPrice(symbol, bidPrice);
            if (stopPrice <= 0)
            {
                Log?.Invoke($"Cannot determine stop price for {symbol}");
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

            double limitPrice = Math.Round(askPrice * (1 + config.SpreadPercent), 2);

            int token = GetNextToken();

            lock (_lock)
            {
                _pendingStops[token] = (stopPrice, currentBar.Clone(), symbol);
                state.LastBuy.Reset(token);
            }

            string barInfo = usedLastBar ? "LastBar" : "CurrentBar";
            //Log?.Invoke($"BUY {symbol} {shares}@{limitPrice:F2} (Ask={askPrice:F2}, Stop={stopPrice:F2} [{barInfo}], Token={token})");

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

            Log?.Invoke($"SELL ALL {symbol} {shares}@{limitPrice:F2} (Bid={state.Quote.Bid:F2})");

            var stopOrder = _accountManager.GetStopOrder(symbol);
            if (stopOrder != null)
            {
                state.PendingSell = new PendingSellInfo
                {
                    SellShares = shares,
                    SellLimitPrice = limitPrice,
                    StopTriggerPrice = 0,
                    StopLimitPrice = 0,
                    IsSellAll = true
                };

                Log?.Invoke($"Canceling stop order {stopOrder.OrderId} before sell");
                await _client.CancelOrder(stopOrder.OrderId);
            }
            else
            {
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
            }

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

            Log?.Invoke($"SELL {label} {symbol} {shares}@{limitPrice:F2} (Bid={state.Quote.Bid:F2})");

            var stopOrder = _accountManager.GetStopOrder(symbol);
            if (stopOrder != null)
            {
                state.PendingSell = new PendingSellInfo
                {
                    SellShares = shares,
                    SellLimitPrice = limitPrice,
                    StopTriggerPrice = stopOrder.StopPrice,
                    StopLimitPrice = stopOrder.Price,
                    IsSellAll = false
                };

                Log?.Invoke($"Canceling stop order {stopOrder.OrderId} (Trigger={stopOrder.StopPrice:F2}) before sell");
                await _client.CancelOrder(stopOrder.OrderId);
            }
            else
            {
                Log?.Invoke($"No stop order found, selling directly");
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
            }

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

            var stopOrder = _accountManager.GetStopOrder(symbol);
            if (stopOrder != null)
            {
                if (state != null)
                {
                    state.PendingMoveStop = true;
                }

                await _client.CancelOrder(stopOrder.OrderId);
            }
            else
            {
                await PlaceStopOrder(symbol, shares, breakevenPrice);
            }

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

            double entryPriceForCalc = askPrice;
            double limitPrice = Math.Round(askPrice * (1 + config.SpreadPercent), 2);

            double targetProfit = currentProfit * profitRatio;
            double profitAtStop = (stopPrice - avgCost) * currentQty;
            double lossPerShareOnAdd = entryPriceForCalc - stopPrice;

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

            int token = GetNextToken();

            lock (_lock)
            {
                _pendingStops[token] = (stopPrice, currentBar.Clone(), symbol);
                state.LastBuy.Reset(token);
            }

            string mode = profitRatio == 0 ? "BREAKEVEN" : $"KEEP {profitRatio * 100:F0}% PROFIT";
            string barInfo = usedLastBar ? "LastBar" : "CurrentBar";
            Log?.Invoke($"ADD {symbol} {sharesToAdd}@{limitPrice:F2} ({mode}, Token={token})");
            Log?.Invoke($"  Current: {currentQty}@{avgCost:F2}, Profit={currentProfit:F2}");
            Log?.Invoke($"  New Stop={stopPrice:F2} [{barInfo}], TargetProfit={targetProfit:F2}");

            var stopOrder = _accountManager.GetStopOrder(symbol);
            if (stopOrder != null)
            {
                await _client.CancelOrder(stopOrder.OrderId);
            }

            NewOrderSent?.Invoke(token);
            await _client.PlaceLimitOrder(
                token,
                "B",
                symbol,
                config.BuyRoute,
                sharesToAdd,
                limitPrice,
                "DAY+"
            );

            return true;
        }

        #endregion

        #region Helper Methods

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

            lock (_lock)
            {
                _pendingStops.Clear();
            }

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
            lock (_lock)
            {
                // 清理 _pendingStops 中该 symbol 的记录
                var tokensToRemove = _pendingStops
                    .Where(kvp => kvp.Value.symbol == symbol)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var token in tokensToRemove)
                {
                    _pendingStops.Remove(token);
                }
            }

            // SymbolState 中的 PendingSell 和 PendingMoveStop 会随 SymbolDataManager.Remove 一起被清除
        }

        #endregion
    }
}