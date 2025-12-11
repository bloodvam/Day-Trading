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

        private int _tokenCounter = 1000;
        private readonly object _tokenLock = new();

        // 追踪待挂止损的买入订单: Token -> (StopPrice, EntryBar)
        private readonly Dictionary<int, (double stopPrice, Bar entryBar)> _pendingStops = new();
        private readonly object _stopLock = new();

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
        }

        private int GetNextToken()
        {
            lock (_tokenLock)
            {
                return ++_tokenCounter;
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
            lock (_stopLock)
            {
                _pendingStops[token] = (stopPrice, currentBar.Clone());
            }

            Log?.Invoke($"BUY {symbol} {shares}@{limitPrice:F2} (Ask={askPrice:F2}, Stop={stopPrice:F2})");

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

            Log?.Invoke($"SELL {symbol} {shares}@{limitPrice:F2} (Bid={quote.Bid:F2})");

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
        /// 挂止损单
        /// </summary>
        private async Task PlaceStopOrder(string symbol, int shares, double stopPrice)
        {
            var config = AppConfig.Instance.Trading;

            // 止损限价 = StopPrice * (1 - spread)
            double limitPrice = Math.Round(stopPrice * (1 - config.SpreadPercent), 2);
            int token = GetNextToken();

            Log?.Invoke($"STOP {symbol} {shares}@{stopPrice:F2} (Limit={limitPrice:F2})");

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
        /// 取消指定symbol的止损单
        /// </summary>
        private async Task CancelStopOrders(string symbol)
        {
            var config = AppConfig.Instance.Trading;
            var openOrders = _accountManager.GetOpenOrders();

            foreach (var order in openOrders)
            {
                if (order.Symbol == symbol &&
                    order.Route == config.StopRoute &&
                    (order.Type == OrderType.StopLimit || order.Type == OrderType.StopLimitPost))
                {
                    Log?.Invoke($"Cancel stop order {order.OrderId}");
                    await _client.CancelOrder(order.OrderId);
                }
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
            lock (_stopLock)
            {
                if (!_pendingStops.TryGetValue(order.Token, out pendingStop))
                    return;

                _pendingStops.Remove(order.Token);
            }

            Log?.Invoke($"Order {order.OrderId} executed, placing stop at {pendingStop.stopPrice:F2}");

            await PlaceStopOrder(order.Symbol, order.FilledQuantity, pendingStop.stopPrice);
        }

        /// <summary>
        /// 取消所有挂单
        /// </summary>
        public async Task CancelAllOrders()
        {
            await _client.CancelAll();
        }
    }
}