using TradingEngine.Config;
using TradingEngine.Models;

namespace TradingEngine.Managers
{
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

        // 追踪已Accept的订单: Token -> OrderId
        private readonly Dictionary<int, int> _acceptedOrders = new();

        // 追踪止损单: Symbol -> Token
        private readonly Dictionary<string, int> _stopOrderTokens = new();

        public event Action<string>? Log;
        public event Action<Order>? OrderPlaced;
        public event Action<Order>? StopOrderPlaced;

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

            // 监听订单成交，自动挂止损
            _accountManager.OrderExecuted += OnOrderExecuted;

            // 监听 %OrderAct 事件来追踪订单状态
            _accountManager.OrderActionReceived += OnOrderActionReceived;
        }

        private int GetNextToken()
        {
            return (int)(DateTime.UtcNow.Ticks % int.MaxValue);
        }

        /// <summary>
        /// 处理 %OrderAct 事件
        /// </summary>
        private void OnOrderActionReceived(int orderId, string actionType, int qty, double price, int token)
        {
            lock (_lock)
            {
                if (token == 0) return;

                if (actionType == "Accept")
                {
                    // 订单被接受，记录到 _acceptedOrders
                    _acceptedOrders[token] = orderId;
                }
                else if (actionType == "Execute" || actionType == "Canceled" ||
                         actionType == "Close" || actionType == "Send_Rej")
                {
                    // 订单完成，从 _acceptedOrders 移除
                    _acceptedOrders.Remove(token);

                    // 如果是止损单，也从 _stopOrderTokens 移除
                    var symbolToRemove = _stopOrderTokens
                        .FirstOrDefault(kvp => kvp.Value == token).Key;
                    if (symbolToRemove != null)
                    {
                        _stopOrderTokens.Remove(symbolToRemove);
                    }
                }
            }
        }

        /// <summary>
        /// 获取所有已Accept且当前仍是Accepted状态的OrderId
        /// </summary>
        public HashSet<int> GetPendingOrderIds()
        {
            lock (_lock)
            {
                var result = new HashSet<int>();
                var toRemove = new List<int>();

                foreach (var kvp in _acceptedOrders)
                {
                    int token = kvp.Key;
                    int orderId = kvp.Value;

                    if (orderId > 0)
                    {
                        // 再次检查订单当前状态
                        var order = _accountManager.GetOrder(orderId);
                        if (order != null && order.Status == OrderStatus.Accepted)
                        {
                            result.Add(orderId);
                        }
                        else
                        {
                            // 订单已不是Accepted状态，标记移除
                            toRemove.Add(token);
                        }
                    }
                }

                // 清理
                foreach (var token in toRemove)
                {
                    _acceptedOrders.Remove(token);
                }

                return result;
            }
        }

        /// <summary>
        /// 买入1R仓位（热键触发）
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
            if (quote == null || quote.Ask <= 0)
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
            double stopPrice = currentBar.Low;
            double riskPerShare = askPrice - stopPrice;

            if (riskPerShare <= 0)
            {
                Log?.Invoke($"Invalid risk: Ask={askPrice}, BarLow={stopPrice}");
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

            // 记录待挂止损信息
            lock (_lock)
            {
                _pendingStops[token] = (stopPrice, currentBar.Clone());
            }

            Log?.Invoke($"BUY {symbol} {shares}@{limitPrice:F2} (Ask={askPrice:F2}, Stop={stopPrice:F2}, Token={token})");

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
        /// 卖出全部持仓（热键触发）
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
            int token = GetNextToken();

            Log?.Invoke($"SELL ALL {symbol} {shares}@{limitPrice:F2} (Bid={quote.Bid:F2})");

            // 先取消该symbol的止损单
            await CancelStopOrders(symbol);

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
        /// 卖出一半持仓（Alt+2）
        /// </summary>
        public async Task<bool> SellHalf()
        {
            return await SellPercent(50, "HALF");
        }

        /// <summary>
        /// 卖出70%持仓（Alt+3）
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
            int token = GetNextToken();

            Log?.Invoke($"SELL {label} {symbol} {shares}@{limitPrice:F2} (Bid={quote.Bid:F2})");

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
        /// 保本加仓（Shift+Q）- 用全部盈利加仓，止损后保本
        /// </summary>
        public async Task<bool> AddPositionBreakeven()
        {
            return await AddPositionWithProfitTarget(0);
        }

        /// <summary>
        /// 保半利加仓（Shift+W）- 用一半盈利加仓，止损后保住一半利润
        /// </summary>
        public async Task<bool> AddPositionHalfProfit()
        {
            return await AddPositionWithProfitTarget(0.5);
        }

        /// <summary>
        /// 移动止损到成本价（Space）- 把止损点移到average cost
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

            double avgCost = position.AvgCost;
            int shares = position.Quantity;

            Log?.Invoke($"Moving stop to breakeven: {symbol} {shares}@{avgCost:F2}");

            // 取消旧止损单，挂新止损单
            await UpdateStopOrder(symbol, shares, avgCost);

            return true;
        }

        /// <summary>
        /// 根据目标利润比例加仓
        /// </summary>
        /// <param name="profitRatio">止损后保留的利润比例（0=保本，0.5=保一半）</param>
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
            double stopPrice = currentBar.Low;

            // 当前浮盈
            double currentProfit = (bidPrice - avgCost) * currentQty;
            if (currentProfit <= 0)
            {
                Log?.Invoke($"No profit to add position: currentProfit={currentProfit:F2}");
                return false;
            }

            // 检查止损价是否有效
            if (stopPrice >= askPrice)
            {
                Log?.Invoke($"Invalid stop price: Stop={stopPrice:F2} >= Ask={askPrice:F2}");
                return false;
            }

            // 用 Ask 计算加仓数量，限价用 Ask * (1 + spread)
            double entryPriceForCalc = askPrice;  // 计算用 Ask
            double limitPrice = Math.Round(askPrice * (1 + config.SpreadPercent), 2);  // 下单限价

            // 计算最大加仓数量
            // 止损时：原仓位盈亏 + 新仓位亏损 >= 目标利润
            // (stopPrice - avgCost) * currentQty + (stopPrice - entryPrice) * addShares >= targetProfit
            // addShares <= ((stopPrice - avgCost) * currentQty - targetProfit) / (entryPrice - stopPrice)

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
                Log?.Invoke($"Cannot add position: maxShares={maxShares:F2}, profitAtStop={profitAtStop:F2}, targetProfit={targetProfit:F2}");
                return false;
            }

            int token = GetNextToken();

            // 记录待挂止损信息
            lock (_lock)
            {
                _pendingStops[token] = (stopPrice, currentBar.Clone());
            }

            string mode = profitRatio == 0 ? "BREAKEVEN" : $"KEEP {profitRatio * 100:F0}% PROFIT";
            Log?.Invoke($"ADD {symbol} {sharesToAdd}@{limitPrice:F2} ({mode}, Token={token})");
            Log?.Invoke($"  Current: {currentQty}@{avgCost:F2}, Profit={currentProfit:F2}");
            Log?.Invoke($"  New Stop={stopPrice:F2}, TargetProfit={targetProfit:F2}");

            // 先取消旧止损单，避免加仓期间被止损出场变成空单
            await CancelStopOrders(symbol);

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

        /// <summary>
        /// 挂止损单
        /// </summary>
        private async Task PlaceStopOrder(string symbol, int shares, double stopPrice)
        {
            var config = AppConfig.Instance.Trading;

            // 止损限价 = StopPrice * (1 - spread)
            double limitPrice = Math.Round(stopPrice * (1 - config.SpreadPercent), 2);
            int token = GetNextToken();

            // 记录这个symbol的止损单token
            lock (_lock)
            {
                _stopOrderTokens[symbol] = token;
            }

            Log?.Invoke($"STOP {symbol} {shares}@{stopPrice:F2} (Limit={limitPrice:F2}, Token={token})");

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
        /// 更新止损单（取消旧的，挂新的）
        /// </summary>
        private async Task UpdateStopOrder(string symbol, int totalShares, double newStopPrice)
        {
            // 先取消旧止损单
            await CancelStopOrders(symbol);

            // 挂新止损单
            await PlaceStopOrder(symbol, totalShares, newStopPrice);
        }

        /// <summary>
        /// 取消指定symbol的止损单
        /// </summary>
        private async Task CancelStopOrders(string symbol)
        {
            int orderId = 0;
            int token = 0;

            lock (_lock)
            {
                // 找到这个symbol的止损单token
                if (_stopOrderTokens.TryGetValue(symbol, out token))
                {
                    // 通过token找到orderId
                    if (_acceptedOrders.TryGetValue(token, out orderId))
                    {
                        // 从追踪中移除
                        _acceptedOrders.Remove(token);
                    }
                    _stopOrderTokens.Remove(symbol);
                }
            }

            if (orderId > 0)
            {
                Log?.Invoke($"Cancel stop order {orderId} (Token={token})");
                await _client.CancelOrder(orderId);
            }
        }

        /// <summary>
        /// 订单成交时自动挂止损
        /// </summary>
        private async void OnOrderExecuted(Order order)
        {
            // 只处理买入订单
            if (order.Side != OrderSide.Buy) return;

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

            Log?.Invoke($"Order {order.OrderId} executed, updating stop for {totalShares} shares at {pendingStop.stopPrice:F2}");

            // 更新止损单（取消旧的，挂新的覆盖全部仓位）
            await UpdateStopOrder(order.Symbol, totalShares, pendingStop.stopPrice);
        }

        /// <summary>
        /// 取消所有挂单
        /// </summary>
        public async Task CancelAllOrders()
        {
            await _client.CancelAll();

            // 清理追踪
            lock (_lock)
            {
                _acceptedOrders.Clear();
                _stopOrderTokens.Clear();
            }
        }
    }
}