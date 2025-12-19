using TradingEngine.Config;
using TradingEngine.Models;
using TradingEngine.Parsers;

namespace TradingEngine.Managers
{
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
    /// 订单管理 - 处理下单逻辑和自动止损
    /// </summary>
    public class OrderManager
    {
        private readonly DasClient _client;
        private readonly AccountManager _accountManager;
        private readonly SubscriptionManager _subscriptionManager;
        private readonly BarAggregator _barAggregator;

        private readonly object _lock = new();

        // 追踪待挂止损的买入订单: Token -> (StopPrice, EntryBar)
        private readonly Dictionary<int, (double stopPrice, Bar entryBar)> _pendingStops = new();

        // 追踪待卖出操作: Symbol -> PendingSellInfo
        // 流程: 保存止损价 → Cancel止损单 → OrderRemoved触发 → 发卖出单 → PositionChanged触发 → 重挂止损
        private readonly Dictionary<string, PendingSellInfo> _pendingSell = new();

        // 追踪 MoveStopToBreakeven: 标记哪个 symbol 需要在止损单取消后挂新止损
        private string? _pendingMoveStopSymbol;

        // 追踪最后一次买入的成交价
        private readonly LastBuyInfo _lastBuy = new();

        public event Action<string>? Log;
        public event Action<int>? NewOrderSent;  // 发送 NEWORDER 时触发，参数是 token

        public OrderManager(
            DasClient client,
            AccountManager accountManager,
            SubscriptionManager subscriptionManager,
            BarAggregator barAggregator)
        {
            _client = client;
            _accountManager = accountManager;
            _subscriptionManager = subscriptionManager;
            _barAggregator = barAggregator;

            // 监听订单成交，自动挂止损（买入成交后）
            _accountManager.OrderExecuted += OnOrderExecuted;

            // 监听订单移除（止损单取消后发卖出单）
            _accountManager.OrderRemoved += OnOrderRemoved;

            // 监听持仓变化（卖出后重挂止损）
            _accountManager.PositionChanged += OnPositionChanged;

            // 监听成交，追踪买入成交价
            _client.TradeUpdate += OnTradeUpdate;
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
            if (order.Side != OrderSide.Buy) return;

            // 记录最后一次买入的 OrderId
            lock (_lock)
            {
                if (order.Token == _lastBuy.Token && _lastBuy.OrderId == 0)
                {
                    _lastBuy.OrderId = order.OrderId;
                    Log?.Invoke($"LastBuy: Token {order.Token} -> OrderId {order.OrderId}");
                }
            }

            (double stopPrice, Bar entryBar) pendingStop;
            lock (_lock)
            {
                if (!_pendingStops.TryGetValue(order.Token, out pendingStop))
                    return;

                _pendingStops.Remove(order.Token);
            }

            // 获取当前总持仓
            var position = _accountManager.GetPosition(order.Symbol);
            int totalShares = position?.Quantity ?? order.FilledQuantity;

            Log?.Invoke($"Buy order {order.OrderId} executed, placing stop for {totalShares} shares at {pendingStop.stopPrice:F2}");

            // 挂止损单
            await PlaceStopOrder(order.Symbol, totalShares, pendingStop.stopPrice);
        }

        /// <summary>
        /// 订单移除时（止损单Canceled → 发卖出单或挂新止损单）
        /// </summary>
        private async void OnOrderRemoved(Order order)
        {
            // 只处理止损单被取消的情况
            if (order.Status != OrderStatus.Canceled) return;
            if (order.Type != OrderType.StopLimit &&
                order.Type != OrderType.StopLimitPost &&
                order.Type != OrderType.StopMarket) return;

            // 先检查是不是 MoveStopToBreakeven
            bool isMoveStop = false;
            lock (_lock)
            {
                if (_pendingMoveStopSymbol == order.Symbol)
                {
                    _pendingMoveStopSymbol = null;
                    isMoveStop = true;
                }
            }

            if (isMoveStop)
            {
                // MoveStopToBreakeven 流程
                var position = _accountManager.GetPosition(order.Symbol);
                if (position != null && position.Quantity > 0)
                {
                    // 优先使用最后一次买入的成交均价，如果没有则用持仓均价
                    double breakevenPrice = _lastBuy.AvgPrice > 0 ? _lastBuy.AvgPrice : position.AvgCost;
                    Log?.Invoke($"Stop order {order.OrderId} canceled, placing new stop at breakeven {breakevenPrice:F2}");
                    await PlaceStopOrder(order.Symbol, position.Quantity, breakevenPrice);
                }
                return;
            }

            // 否则是卖出流程
            PendingSellInfo? pendingSell;
            lock (_lock)
            {
                if (!_pendingSell.TryGetValue(order.Symbol, out pendingSell))
                    return;
                // 不要移除 _pendingSell，等 PositionChanged 后再移除
            }

            Log?.Invoke($"Stop order {order.OrderId} canceled, now placing sell order");

            // 发卖出单
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

        /// <summary>
        /// 持仓变化时（卖出后 → 重挂止损）
        /// </summary>
        private async void OnPositionChanged(Position pos)
        {
            PendingSellInfo? pendingSell;
            lock (_lock)
            {
                if (!_pendingSell.TryGetValue(pos.Symbol, out pendingSell))
                    return;

                // 移除 _pendingSell
                _pendingSell.Remove(pos.Symbol);
            }

            // 如果是全部卖出，不需要重挂止损
            if (pendingSell.IsSellAll)
            {
                Log?.Invoke($"Sell all completed for {pos.Symbol}, no stop order needed");
                return;
            }

            // 如果还有剩余持仓，重新挂止损
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

                lock (_lock)
                {
                    if (trade.OrderId == _lastBuy.OrderId && _lastBuy.OrderId != 0)
                    {
                        _lastBuy.AddTrade(trade.Price, trade.Quantity);
                        Log?.Invoke($"LastBuy Trade: OrderId={trade.OrderId}, Price={trade.Price:F2}, Qty={trade.Quantity}, AvgPrice={_lastBuy.AvgPrice:F2}");
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

        /// <summary>
        /// 计算止损价 - 智能判断使用当前bar还是前一根bar的Low
        /// </summary>
        private (double stopPrice, bool usedLastBar) GetSmartStopPrice(string symbol, double bid)
        {
            var currentBar = _barAggregator.GetCurrentBar(symbol);
            var lastBar = _barAggregator.GetLastCompletedBar(symbol);
            var quote = _subscriptionManager.GetCurrentQuote();

            if (currentBar == null || currentBar.Low <= 0)
            {
                return (0, false);
            }

            double currentLow = currentBar.Low;
            double stopPrice = currentLow;
            bool usedLastBar = false;

            // 条件1：任何时候，bid <= currentBar.Low
            if (bid <= currentLow)
            {
                if (lastBar != null && lastBar.Low > 0)
                {
                    stopPrice = lastBar.Low;
                    usedLastBar = true;
                    Log?.Invoke($"[StopLogic] bid({bid:F2}) <= currentLow({currentLow:F2}), using lastBar.Low({stopPrice:F2})");
                }
            }
            // 条件2：新bar的第0秒内，bid - currentBar.Low <= 0.01
            else if (lastBar != null && lastBar.Low > 0 && quote != null)
            {
                int barInterval = currentBar.IntervalSeconds;
                double quoteSeconds = quote.UpdateTime.TimeOfDay.TotalSeconds;
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
        /// 买入1R仓位
        /// </summary>
        public async Task<bool> BuyOneR()
        {
            var config = AppConfig.Instance.Trading;
            string? symbol = _subscriptionManager.CurrentSymbol;

            if (string.IsNullOrEmpty(symbol))
            {
                Log?.Invoke("No symbol subscribed");
                return false;
            }

            var quote = _subscriptionManager.GetCurrentQuote();
            if (quote == null || quote.Ask <= 0 || quote.Bid <= 0)
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

            double askPrice = quote.Ask;
            double bidPrice = quote.Bid;

            // 智能计算止损价
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

            // 计算股数: 风险金额 / 每股风险
            int shares = (int)(config.RiskAmount / riskPerShare);
            if (shares <= 0)
            {
                Log?.Invoke($"Risk too high: ${riskPerShare:F2}/share > ${config.RiskAmount} budget");
                return false;
            }

            // 限价 = Ask * (1 + spread)
            double limitPrice = Math.Round(askPrice * (1 + config.SpreadPercent), 2);

            int token = GetNextToken();

            // 记录待挂止损信息和最后一次买入
            lock (_lock)
            {
                _pendingStops[token] = (stopPrice, currentBar.Clone());
                _lastBuy.Reset(token);
            }

            string barInfo = usedLastBar ? "LastBar" : "CurrentBar";
            Log?.Invoke($"BUY {symbol} {shares}@{limitPrice:F2} (Ask={askPrice:F2}, Stop={stopPrice:F2} [{barInfo}], Token={token})");

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
        /// 卖出全部持仓
        /// </summary>
        public async Task<bool> SellAll()
        {
            var config = AppConfig.Instance.Trading;
            string? symbol = _subscriptionManager.CurrentSymbol;

            if (string.IsNullOrEmpty(symbol))
            {
                Log?.Invoke("No symbol subscribed");
                return false;
            }

            var position = _accountManager.GetPosition(symbol);
            if (position == null || position.Quantity <= 0)
            {
                Log?.Invoke($"No position for {symbol}");
                return false;
            }

            var quote = _subscriptionManager.GetCurrentQuote();
            if (quote == null || quote.Bid <= 0)
            {
                Log?.Invoke($"No valid quote for {symbol}");
                return false;
            }

            // 限价 = Bid * (1 - spread)
            double limitPrice = Math.Round(quote.Bid * (1 - config.SpreadPercent), 2);
            int shares = position.Quantity;

            Log?.Invoke($"SELL ALL {symbol} {shares}@{limitPrice:F2} (Bid={quote.Bid:F2})");

            // 查找止损单
            var stopOrder = _accountManager.GetStopOrder(symbol);
            if (stopOrder != null)
            {
                // 保存卖出信息，等止损单取消后执行
                lock (_lock)
                {
                    _pendingSell[symbol] = new PendingSellInfo
                    {
                        SellShares = shares,
                        SellLimitPrice = limitPrice,
                        StopTriggerPrice = 0,  // 全部卖出，不需要重挂止损
                        StopLimitPrice = 0,
                        IsSellAll = true
                    };
                }

                // 取消止损单（会触发 OnOrderRemoved）
                Log?.Invoke($"Canceling stop order {stopOrder.OrderId} before sell");
                await _client.CancelOrder(stopOrder.OrderId);
            }
            else
            {
                // 没有止损单，直接卖
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

        /// <summary>
        /// 卖出一半持仓
        /// </summary>
        public async Task<bool> SellHalf()
        {
            return await SellPercent(50, "HALF");
        }

        /// <summary>
        /// 卖出70%持仓
        /// </summary>
        public async Task<bool> Sell70Percent()
        {
            return await SellPercent(70, "70%");
        }

        /// <summary>
        /// 卖出指定百分比持仓
        /// </summary>
        private async Task<bool> SellPercent(int percent, string label)
        {
            var config = AppConfig.Instance.Trading;
            string? symbol = _subscriptionManager.CurrentSymbol;

            if (string.IsNullOrEmpty(symbol))
            {
                Log?.Invoke("No symbol subscribed");
                return false;
            }

            var position = _accountManager.GetPosition(symbol);
            if (position == null || position.Quantity <= 0)
            {
                Log?.Invoke($"No position for {symbol}");
                return false;
            }

            var quote = _subscriptionManager.GetCurrentQuote();
            if (quote == null || quote.Bid <= 0)
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

            double limitPrice = Math.Round(quote.Bid * (1 - config.SpreadPercent), 2);

            Log?.Invoke($"SELL {label} {symbol} {shares}@{limitPrice:F2} (Bid={quote.Bid:F2})");

            // 查找止损单
            var stopOrder = _accountManager.GetStopOrder(symbol);
            if (stopOrder != null)
            {
                // 保存卖出信息和止损价，等止损单取消后执行
                lock (_lock)
                {
                    _pendingSell[symbol] = new PendingSellInfo
                    {
                        SellShares = shares,
                        SellLimitPrice = limitPrice,
                        StopTriggerPrice = stopOrder.StopPrice,
                        StopLimitPrice = stopOrder.Price,
                        IsSellAll = false
                    };
                }

                // 取消止损单
                Log?.Invoke($"Canceling stop order {stopOrder.OrderId} (Trigger={stopOrder.StopPrice:F2}) before sell");
                await _client.CancelOrder(stopOrder.OrderId);
            }
            else
            {
                // 没有止损单，直接卖
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

        /// <summary>
        /// 保本加仓
        /// </summary>
        public async Task<bool> AddPositionBreakeven()
        {
            return await AddPositionWithProfitTarget(0);
        }

        /// <summary>
        /// 保半利加仓
        /// </summary>
        public async Task<bool> AddPositionHalfProfit()
        {
            return await AddPositionWithProfitTarget(0.5);
        }

        /// <summary>
        /// 移动止损到成本价
        /// </summary>
        public async Task<bool> MoveStopToBreakeven()
        {
            string? symbol = _subscriptionManager.CurrentSymbol;

            if (string.IsNullOrEmpty(symbol))
            {
                Log?.Invoke("No symbol subscribed");
                return false;
            }

            var position = _accountManager.GetPosition(symbol);
            if (position == null || position.Quantity <= 0)
            {
                Log?.Invoke($"No position for {symbol}");
                return false;
            }

            // 优先使用最后一次买入的成交均价，如果没有则用持仓均价
            double breakevenPrice = _lastBuy.AvgPrice > 0 ? _lastBuy.AvgPrice : position.AvgCost;
            int shares = position.Quantity;

            Log?.Invoke($"Moving stop to breakeven: {symbol} {shares}@{breakevenPrice:F2} (LastBuyAvg={_lastBuy.AvgPrice:F2}, PositionAvg={position.AvgCost:F2})");

            // 查找旧止损单
            var stopOrder = _accountManager.GetStopOrder(symbol);
            if (stopOrder != null)
            {
                // 标记这个 symbol 需要 move stop
                lock (_lock)
                {
                    _pendingMoveStopSymbol = symbol;
                }

                // 取消旧止损单（会触发 OnOrderRemoved）
                await _client.CancelOrder(stopOrder.OrderId);
            }
            else
            {
                // 没有旧止损单，直接挂新的
                await PlaceStopOrder(symbol, shares, breakevenPrice);
            }

            return true;
        }

        /// <summary>
        /// 根据目标利润比例加仓
        /// </summary>
        private async Task<bool> AddPositionWithProfitTarget(double profitRatio)
        {
            var config = AppConfig.Instance.Trading;
            string? symbol = _subscriptionManager.CurrentSymbol;

            if (string.IsNullOrEmpty(symbol))
            {
                Log?.Invoke("No symbol subscribed");
                return false;
            }

            var position = _accountManager.GetPosition(symbol);
            if (position == null || position.Quantity <= 0)
            {
                Log?.Invoke($"No position for {symbol}");
                return false;
            }

            var quote = _subscriptionManager.GetCurrentQuote();
            if (quote == null || quote.Ask <= 0 || quote.Bid <= 0)
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
            double bidPrice = quote.Bid;
            double askPrice = quote.Ask;

            // 智能计算止损价
            var (stopPrice, usedLastBar) = GetSmartStopPrice(symbol, bidPrice);
            if (stopPrice <= 0)
            {
                Log?.Invoke($"Cannot determine stop price for {symbol}");
                return false;
            }

            // 当前浮盈
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

            // 记录待挂止损信息和最后一次买入
            lock (_lock)
            {
                _pendingStops[token] = (stopPrice, currentBar.Clone());
                _lastBuy.Reset(token);
            }

            string mode = profitRatio == 0 ? "BREAKEVEN" : $"KEEP {profitRatio * 100:F0}% PROFIT";
            string barInfo = usedLastBar ? "LastBar" : "CurrentBar";
            Log?.Invoke($"ADD {symbol} {sharesToAdd}@{limitPrice:F2} ({mode}, Token={token})");
            Log?.Invoke($"  Current: {currentQty}@{avgCost:F2}, Profit={currentProfit:F2}");
            Log?.Invoke($"  New Stop={stopPrice:F2} [{barInfo}], TargetProfit={targetProfit:F2}");

            // 先取消旧止损单
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

        /// <summary>
        /// 挂止损单
        /// </summary>
        private async Task PlaceStopOrder(string symbol, int shares, double stopPrice)
        {
            var config = AppConfig.Instance.Trading;

            // 止损限价 = StopPrice * (1 - spread)
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

        /// <summary>
        /// 取消所有挂单
        /// </summary>
        public async Task CancelAllOrders()
        {
            await _client.CancelAll();

            lock (_lock)
            {
                _pendingStops.Clear();
                _pendingSell.Clear();
                _pendingMoveStopSymbol = null;
            }
        }

        #endregion
    }
}