using TradingEngine.Models;
using TradingEngine.Parsers;

namespace TradingEngine.Managers
{
    /// <summary>
    /// 管理账户信息、持仓、订单
    /// _positions: 只存储 Quantity != 0 的持仓
    /// _orders: 只存储 Accepted 状态的订单
    /// </summary>
    public class AccountManager
    {
        private readonly DasClient _client;
        private readonly Dictionary<string, Position> _positions = new();
        private readonly Dictionary<int, Order> _orders = new();
        private readonly object _lock = new();

        public AccountInfo AccountInfo { get; private set; } = new();

        public event Action<AccountInfo>? AccountInfoChanged;
        public event Action<Position>? PositionChanged;
        public event Action<Order>? OrderAdded;      // 新订单 Accepted
        public event Action<Order>? OrderRemoved;    // 订单移除 (Canceled/Executed/Rejected/Closed)
        public event Action<Order>? OrderExecuted;   // 订单成交

        public AccountManager(DasClient client)
        {
            _client = client;
            SubscribeEvents();
        }

        private void SubscribeEvents()
        {
            _client.PosUpdate += OnPosUpdate;
            _client.OrderUpdate += OnOrderUpdate;
            _client.AccountInfoUpdate += OnAccountInfoUpdate;
            _client.BPUpdate += OnBPUpdate;
        }

        #region Event Handlers

        private void OnPosUpdate(string line)
        {
            var pos = MessageParser.ParsePosition(line);
            if (pos == null) return;

            lock (_lock)
            {
                if (pos.Quantity != 0)
                {
                    // 有持仓，添加或更新
                    _positions[pos.Symbol] = pos;
                }
                else
                {
                    // 没有持仓，移除
                    _positions.Remove(pos.Symbol);
                }
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
                    // Accepted 状态，添加到 dictionary
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
                    // 这些状态，从 dictionary 移除
                    if (existsInDict)
                    {
                        _orders.Remove(order.OrderId);
                        wasRemoved = true;
                    }
                }
                // Partial, Sending 等状态保持原样
                else if (existsInDict)
                {
                    _orders[order.OrderId] = order;
                }
            }

            // 触发事件
            if (wasAdded)
            {
                OrderAdded?.Invoke(order);
            }
            if (wasRemoved)
            {
                OrderRemoved?.Invoke(order);
            }
            if (order.Status == OrderStatus.Executed)
            {
                OrderExecuted?.Invoke(order);
            }
        }

        private void OnAccountInfoUpdate(string line)
        {
            var info = MessageParser.ParseAccountInfo(line);
            if (info == null) return;

            // 保留BP信息
            info.BuyingPower = AccountInfo.BuyingPower;
            info.OvernightBP = AccountInfo.OvernightBP;

            AccountInfo = info;
            AccountInfoChanged?.Invoke(info);
        }

        private void OnBPUpdate(string line)
        {
            var bp = MessageParser.ParseBuyingPower(line);
            if (bp == null) return;

            AccountInfo.BuyingPower = bp.Value.bp;
            AccountInfo.OvernightBP = bp.Value.overnightBp;
            AccountInfo.UpdateTime = DateTime.Now;
            AccountInfoChanged?.Invoke(AccountInfo);
        }

        #endregion

        #region Public Methods

        public Position? GetPosition(string symbol)
        {
            lock (_lock)
            {
                return _positions.TryGetValue(symbol, out var pos) ? pos : null;
            }
        }

        /// <summary>
        /// 获取所有持仓（都是 Quantity != 0 的）
        /// </summary>
        public List<Position> GetAllPositions()
        {
            lock (_lock)
            {
                return _positions.Values.ToList();
            }
        }

        public Order? GetOrder(int orderId)
        {
            lock (_lock)
            {
                return _orders.TryGetValue(orderId, out var order) ? order : null;
            }
        }

        /// <summary>
        /// 获取所有挂单（都是 Accepted 状态的）
        /// </summary>
        public List<Order> GetAllOrders()
        {
            lock (_lock)
            {
                return _orders.Values.ToList();
            }
        }

        /// <summary>
        /// 查找指定 symbol 的止损单
        /// </summary>
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
            await _client.GetBuyingPower();
            await _client.GetPositions();
            await _client.GetOrders();
        }

        #endregion
    }
}