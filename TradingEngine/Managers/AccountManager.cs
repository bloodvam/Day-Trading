using TradingEngine.Models;
using TradingEngine.Parsers;

namespace TradingEngine.Managers
{
    /// <summary>
    /// 管理账户信息、持仓、订单、成交
    /// </summary>
    public class AccountManager
    {
        private readonly DasClient _client;
        private readonly Dictionary<string, Position> _positions = new();
        private readonly Dictionary<int, Order> _orders = new();
        private readonly Dictionary<int, Trade> _trades = new();
        private readonly object _lock = new();

        public AccountInfo AccountInfo { get; private set; } = new();

        public event Action<AccountInfo>? AccountInfoChanged;
        public event Action<Position>? PositionChanged;
        public event Action<Order>? OrderChanged;
        public event Action<Trade>? TradeReceived;
        public event Action<Order>? OrderExecuted;  // 订单完全成交

        public AccountManager(DasClient client)
        {
            _client = client;
            SubscribeEvents();
        }

        private void SubscribeEvents()
        {
            _client.PosUpdate += OnPosUpdate;
            _client.OrderUpdate += OnOrderUpdate;
            _client.OrderAction += OnOrderAction;
            _client.TradeUpdate += OnTradeUpdate;
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
                _positions[pos.Symbol] = pos;
            }
            PositionChanged?.Invoke(pos);
        }

        private void OnOrderUpdate(string line)
        {
            var order = MessageParser.ParseOrder(line);

            if (order == null) return;

            lock (_lock)
            {
                _orders[order.OrderId] = order;
            }
            OrderChanged?.Invoke(order);

            // 检测完全成交
            if (order.Status == OrderStatus.Executed)
            {
                OrderExecuted?.Invoke(order);
            }
        }

        private void OnOrderAction(string line)
        {
            var action = MessageParser.ParseOrderAction(line);
            if (action == null) return;

            // OrderAction 提供更实时的状态更新
            // 主要用于跟踪Execute事件
        }

        private void OnTradeUpdate(string line)
        {
            var trade = MessageParser.ParseTrade(line);
            if (trade == null) return;

            lock (_lock)
            {
                _trades[trade.TradeId] = trade;
            }
            TradeReceived?.Invoke(trade);
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

        public Order? GetOrderByToken(int token)
        {
            lock (_lock)
            {
                return _orders.Values.FirstOrDefault(o => o.Token == token);
            }
        }

        public List<Order> GetOpenOrders()
        {
            lock (_lock)
            {
                return _orders.Values
                    .Where(o => o.Status == OrderStatus.Accepted ||
                                o.Status == OrderStatus.Partial ||
                                o.Status == OrderStatus.Pending ||
                                o.Status == OrderStatus.Sending)
                    .ToList();
            }
        }

        public List<Order> GetAllOrders()
        {
            lock (_lock)
            {
                return _orders.Values.ToList();
            }
        }

        public List<Trade> GetAllTrades()
        {
            lock (_lock)
            {
                return _trades.Values.ToList();
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