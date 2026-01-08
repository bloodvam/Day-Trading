using TradingEngine.Config;
using TradingEngine.Models;
using TradingEngine.Utils;

namespace TradingEngine.Managers
{
    /// <summary>
    /// 管理账户信息、持仓、订单
    /// BP = Equity * Leverage - Σ(持仓股数 * avgCost)
    /// </summary>
    /// <summary>
    /// 持仓信息（包含 Position 和 LastAvgCost）
    /// </summary>
    public class PositionInfo
    {
        public Position? Position { get; set; }
        public double LastAvgCost { get; set; }  // 即使 Position 清零也保留，用于计算卖出 PL
    }

    public class AccountManager : IDisposable
    {
        private readonly DasClient _client;
        private readonly SymbolDataManager _dataManager;
        private readonly Dictionary<int, Order> _orders = new();
        private readonly object _lock = new();

        // 独立存储 Position 和 AvgCost（不受 unsubscribe 影响）
        private readonly Dictionary<string, PositionInfo> _positionInfos = new();

        // 自己维护的 Equity 和 BP
        private double _equity;
        private double _buyingPower;
        private bool _equityInitialized;  // 是否已从 $AccountInfo 初始化

        public AccountInfo AccountInfo { get; private set; } = new();

        public event Action<AccountInfo>? AccountInfoChanged;
        public event Action<Position>? PositionChanged;
        public event Action<Order>? OrderAdded;
        public event Action<Order>? OrderRemoved;
        public event Action<Order>? OrderExecuted;

        public AccountManager(DasClient client, SymbolDataManager dataManager)
        {
            _client = client;
            _dataManager = dataManager;
            SubscribeEvents();
        }

        private void SubscribeEvents()
        {
            _client.PosUpdate += OnPosUpdate;
            _client.OrderUpdate += OnOrderUpdate;
            _client.AccountInfoUpdate += OnAccountInfoUpdate;
            _client.TradeUpdate += OnTradeUpdate;
        }

        #region Event Handlers

        private void OnPosUpdate(string line)
        {
            var pos = MessageParser.ParsePosition(line);
            if (pos == null) return;

            lock (_lock)
            {
                // 获取或创建 PositionInfo
                if (!_positionInfos.TryGetValue(pos.Symbol, out var info))
                {
                    info = new PositionInfo();
                    _positionInfos[pos.Symbol] = info;
                }

                // 更新 Position 和 LastAvgCost
                if (pos.Quantity != 0)
                {
                    info.Position = pos;
                    info.LastAvgCost = pos.AvgCost;
                }
                else
                {
                    info.Position = null;
                }

                // BP 由 %TRADE 事件更新，这里不再调用 RecalculateBuyingPower
            }

            // 如果订阅了，同步更新 SymbolState（用于 UI 高亮等）
            var state = _dataManager.Get(pos.Symbol);
            if (state != null)
            {
                state.Position = pos.Quantity != 0 ? pos : null;
            }

            PositionChanged?.Invoke(pos);
        }

        private void OnOrderUpdate(string line)
        {
            var order = MessageParser.ParseOrder(line);
            if (order == null) return;

            bool wasAdded = false;
            bool wasRemoved = false;

            lock (_lock)
            {
                bool existsInDict = _orders.ContainsKey(order.OrderId);

                if (order.Status == OrderStatus.Accepted)
                {
                    if (!existsInDict)
                    {
                        wasAdded = true;
                    }
                    _orders[order.OrderId] = order;
                }
                else if (order.Status == OrderStatus.Canceled ||
                         order.Status == OrderStatus.Executed ||
                         order.Status == OrderStatus.Rejected ||
                         order.Status == OrderStatus.Closed ||
                         order.Status == OrderStatus.Hold)
                {
                    if (existsInDict)
                    {
                        _orders.Remove(order.OrderId);
                        wasRemoved = true;
                    }
                }
                else if (existsInDict)
                {
                    _orders[order.OrderId] = order;
                }
            }

            if (wasAdded) OrderAdded?.Invoke(order);
            if (wasRemoved) OrderRemoved?.Invoke(order);
            if (order.Status == OrderStatus.Executed) OrderExecuted?.Invoke(order);
        }

        private void OnTradeUpdate(string line)
        {
            var trade = MessageParser.ParseTrade(line);
            if (trade == null) return;

            lock (_lock)
            {
                // 只有初始化后才处理
                if (!_equityInitialized) return;

                double leverage = AppConfig.Instance.Trading.Leverage;
                double previousEquity = _equity;
                double previousBP = _buyingPower;

                if (trade.Side == OrderSide.Buy)
                {
                    // 买入：Equity 不变，BP 减少 price * qty
                    _buyingPower = previousBP - trade.Price * trade.Quantity;
                }
                else if (trade.Side == OrderSide.Sell)
                {
                    // 卖出：Equity 加上 PL
                    _equity = previousEquity + trade.PL;

                    // BP = current_equity * leverage - (previous_equity * leverage - previous_bp - price * qty)
                    double previousPositionCost = previousEquity * leverage - previousBP;
                    double currentPositionCost = previousPositionCost - trade.Price * trade.Quantity;
                    _buyingPower = _equity * leverage - currentPositionCost;
                }

                UpdateAccountInfo();
            }
        }

        private void OnAccountInfoUpdate(string line)
        {
            var info = MessageParser.ParseAccountInfo(line);
            if (info == null) return;

            lock (_lock)
            {
                if (!_equityInitialized && info.CurrentEquity > 0)
                {
                    _equity = info.CurrentEquity;
                    _equityInitialized = true;
                    RecalculateBuyingPower();
                }
                UpdateAccountInfo();
            }
        }

        #endregion

        #region BP Calculation

        private void RecalculateBuyingPower()
        {
            double leverage = AppConfig.Instance.Trading.Leverage;

            // 计算所有持仓的成本
            double totalPositionCost = 0;
            foreach (var info in _positionInfos.Values)
            {
                if (info.Position != null && info.Position.Quantity > 0)
                {
                    totalPositionCost += info.Position.Quantity * info.Position.AvgCost;
                }
            }

            _buyingPower = _equity * leverage - totalPositionCost;
        }

        private void UpdateAccountInfo()
        {
            AccountInfo.CurrentEquity = _equity;
            AccountInfo.BuyingPower = _buyingPower;
            AccountInfo.UpdateTime = DateTime.Now;
            AccountInfoChanged?.Invoke(AccountInfo);
        }

        #endregion

        #region Public Methods

        public Position? GetPosition(string symbol)
        {
            lock (_lock)
            {
                return _positionInfos.TryGetValue(symbol, out var info) ? info.Position : null;
            }
        }

        public List<Position> GetAllPositions()
        {
            lock (_lock)
            {
                return _positionInfos.Values
                    .Where(info => info.Position != null && info.Position.Quantity != 0)
                    .Select(info => info.Position!)
                    .ToList();
            }
        }

        public Order? GetOrder(int orderId)
        {
            lock (_lock)
            {
                return _orders.TryGetValue(orderId, out var order) ? order : null;
            }
        }

        public List<Order> GetAllOrders()
        {
            lock (_lock)
            {
                return _orders.Values.ToList();
            }
        }

        public Order? GetStopOrder(string symbol)
        {
            lock (_lock)
            {
                return _orders.Values.FirstOrDefault(o =>
                    o.Symbol == symbol &&
                    (o.Type == OrderType.StopLimit ||
                     o.Type == OrderType.StopLimitPost ||
                     o.Type == OrderType.StopMarket));
            }
        }

        public async Task RefreshAll()
        {
            await _client.GetAccountInfo();
            await _client.GetPositions();
            await _client.GetOrders();
        }

        public void SetEquity(double equity)
        {
            lock (_lock)
            {
                _equity = equity;
                _equityInitialized = true;
                RecalculateBuyingPower();
                UpdateAccountInfo();
            }
        }

        #endregion

        public void Dispose()
        {
            _client.PosUpdate -= OnPosUpdate;
            _client.OrderUpdate -= OnOrderUpdate;
            _client.AccountInfoUpdate -= OnAccountInfoUpdate;
            _client.TradeUpdate -= OnTradeUpdate;
        }
    }
}