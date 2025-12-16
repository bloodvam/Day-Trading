using TradingEngine.Config;
using TradingEngine.Managers;
using TradingEngine.Models;

namespace TradingEngine.Core
{
    /// <summary>
    /// 交易控制器 - 协调所有Manager，处理业务逻辑
    /// </summary>
    public class TradingController : IDisposable
    {
        private readonly DasClient _client;
        private readonly BarAggregator _barAggregator;
        private readonly AccountManager _accountManager;
        private readonly SubscriptionManager _subscriptionManager;
        private readonly OrderManager _orderManager;

        private HotkeyManager? _hotkeyManager;

        #region Properties

        public bool IsConnected => _client.IsConnected;
        public bool IsLoggedIn => _client.IsLoggedIn;
        public string? CurrentSymbol => _subscriptionManager.CurrentSymbol;
        public AccountInfo AccountInfo => _accountManager.AccountInfo;

        #endregion

        #region Events - Connection

        public event Action? Connected;
        public event Action? Disconnected;
        public event Action? LoginSuccess;
        public event Action<string>? LoginFailed;

        #endregion

        #region Events - Market Data

        public event Action<string>? SymbolSubscribed;
        public event Action<string>? SymbolUnsubscribed;
        public event Action<Quote>? QuoteUpdated;
        public event Action<Bar>? BarUpdated;
        public event Action<Bar>? BarCompleted;

        #endregion

        #region Events - Account

        public event Action<AccountInfo>? AccountInfoChanged;
        public event Action<Position>? PositionChanged;
        public event Action<Order>? OrderAdded;
        public event Action<Order>? OrderRemoved;

        #endregion

        #region Events - Logging

        public event Action<string>? Log;
        public event Action<string>? RawMessage;
        public event Action<string>? CommandSent;

        public void LogMessage(string message)
        {
            Log?.Invoke(message);
        }

        #endregion

        public TradingController()
        {
            // 初始化所有Manager
            _client = new DasClient();
            _barAggregator = new BarAggregator(AppConfig.Instance.Trading.DefaultBarInterval);
            _accountManager = new AccountManager(_client);
            _subscriptionManager = new SubscriptionManager(_client, _barAggregator);
            _orderManager = new OrderManager(_client, _accountManager, _subscriptionManager, _barAggregator);

            SubscribeInternalEvents();
        }

        private void SubscribeInternalEvents()
        {
            // Connection events
            _client.Connected += () => Connected?.Invoke();
            _client.Disconnected += () => Disconnected?.Invoke();
            _client.LoginSuccess += () =>
            {
                LoginSuccess?.Invoke();
                _ = _accountManager.RefreshAll();
            };
            _client.LoginFailed += (msg) => LoginFailed?.Invoke(msg);

            // Subscription events
            _subscriptionManager.SymbolSubscribed += (s) => SymbolSubscribed?.Invoke(s);
            _subscriptionManager.SymbolUnsubscribed += (s) => SymbolUnsubscribed?.Invoke(s);
            _subscriptionManager.QuoteUpdated += (q) => QuoteUpdated?.Invoke(q);

            // Bar events
            _barAggregator.BarUpdated += (b) => BarUpdated?.Invoke(b);
            _barAggregator.BarCompleted += (b) => BarCompleted?.Invoke(b);

            // Account events
            _accountManager.AccountInfoChanged += (a) => AccountInfoChanged?.Invoke(a);
            _accountManager.PositionChanged += (p) => PositionChanged?.Invoke(p);
            _accountManager.OrderAdded += (o) => OrderAdded?.Invoke(o);
            _accountManager.OrderRemoved += (o) => OrderRemoved?.Invoke(o);

            // Logging
            _orderManager.Log += (msg) => Log?.Invoke(msg);
            _client.RawMessage += (msg) => RawMessage?.Invoke(msg);
            _client.CommandSent += (msg) => CommandSent?.Invoke(msg);
        }

        #region Connection

        public async Task ConnectAsync()
        {
            await _client.ConnectAsync();
            await _client.LoginAsync();
        }

        public void Disconnect()
        {
            _client.Disconnect();
        }

        #endregion

        #region Subscription

        public async Task SubscribeAsync(string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol)) return;
            await _subscriptionManager.SubscribeAsync(symbol);
            Log?.Invoke($"Subscribed to {symbol}");
        }

        public async Task UnsubscribeAsync()
        {
            var symbol = _subscriptionManager.CurrentSymbol;
            if (string.IsNullOrEmpty(symbol)) return;
            await _subscriptionManager.UnsubscribeCurrentAsync();
            Log?.Invoke($"Unsubscribed from {symbol}");
        }

        #endregion

        #region Trading

        public async Task BuyOneR()
        {
            Log?.Invoke("Hotkey: Buy 1R triggered");
            await _orderManager.BuyOneR();
        }

        public async Task SellAll()
        {
            Log?.Invoke("Hotkey: Sell All triggered");
            await _orderManager.SellAll();
        }

        public async Task SellHalf()
        {
            Log?.Invoke("Hotkey: Sell Half triggered");
            await _orderManager.SellHalf();
        }

        public async Task Sell70Percent()
        {
            Log?.Invoke("Hotkey: Sell 70% triggered");
            await _orderManager.Sell70Percent();
        }

        public async Task AddPositionBreakeven()
        {
            Log?.Invoke("Hotkey: Add Position (Breakeven) triggered");
            await _orderManager.AddPositionBreakeven();
        }

        public async Task AddPositionHalfProfit()
        {
            Log?.Invoke("Hotkey: Add Position (Keep 50% Profit) triggered");
            await _orderManager.AddPositionHalfProfit();
        }

        public async Task MoveStopToBreakeven()
        {
            Log?.Invoke("Hotkey: Move Stop to Breakeven triggered");
            await _orderManager.MoveStopToBreakeven();
        }

        public async Task CancelAllOrders()
        {
            Log?.Invoke("Cancel all orders");
            await _orderManager.CancelAllOrders();
        }

        #endregion

        #region Hotkeys

        /// <summary>
        /// 设置热键，返回 (allSuccess, failedKey)
        /// </summary>
        public (bool allSuccess, string? failedKey) SetupHotkeys(Form form)
        {
            _hotkeyManager = new HotkeyManager(form);

            var hotkeys = new List<(Keys key, string name, Action action)>
            {
                (Keys.D1 | Keys.Shift, "Shift+1", async () => await BuyOneR()),
                (Keys.D1 | Keys.Alt, "Alt+1", async () => await SellAll()),
                (Keys.D2 | Keys.Alt, "Alt+2", async () => await SellHalf()),
                (Keys.D3 | Keys.Alt, "Alt+3", async () => await Sell70Percent()),
                (Keys.Q | Keys.Shift, "Shift+Q", async () => await AddPositionBreakeven()),
                (Keys.W | Keys.Shift, "Shift+W", async () => await AddPositionHalfProfit()),
                (Keys.Space, "Space", async () => await MoveStopToBreakeven())
            };

            foreach (var (key, name, action) in hotkeys)
            {
                if (!_hotkeyManager.RegisterHotkey(key, action))
                {
                    return (false, name);
                }
            }

            return (true, null);
        }

        public void EnableHotkeys(bool enabled)
        {
            if (_hotkeyManager == null) return;

            if (enabled)
                _hotkeyManager.ReregisterAll();
            else
                _hotkeyManager.UnregisterAll();
        }

        public bool IsHotkeysEnabled => _hotkeyManager?.IsRegistered ?? false;

        #endregion

        #region Data Access

        public Quote? GetCurrentQuote()
        {
            return _subscriptionManager.GetCurrentQuote();
        }

        public Bar? GetCurrentBar()
        {
            var symbol = _subscriptionManager.CurrentSymbol;
            if (string.IsNullOrEmpty(symbol)) return null;
            return _barAggregator.GetCurrentBar(symbol);
        }

        /// <summary>
        /// 获取所有持仓（Quantity != 0）
        /// </summary>
        public List<Position> GetAllPositions()
        {
            return _accountManager.GetAllPositions();
        }

        /// <summary>
        /// 获取所有挂单（Accepted 状态）
        /// </summary>
        public List<Order> GetAllOrders()
        {
            return _accountManager.GetAllOrders();
        }

        /// <summary>
        /// 获取指定时间周期的BarSeries
        /// </summary>
        public BarSeries? GetBarSeries(int intervalSeconds)
        {
            var symbol = _subscriptionManager.CurrentSymbol;
            if (string.IsNullOrEmpty(symbol)) return null;
            return _barAggregator.GetBarSeries(symbol, intervalSeconds);
        }

        /// <summary>
        /// 获取主时间周期的BarSeries
        /// </summary>
        public BarSeries? GetBarSeries()
        {
            var symbol = _subscriptionManager.CurrentSymbol;
            if (string.IsNullOrEmpty(symbol)) return null;
            return _barAggregator.GetBarSeries(symbol);
        }

        /// <summary>
        /// 启用一个时间周期
        /// </summary>
        public void EnableBarInterval(BarInterval interval)
        {
            _barAggregator.EnableInterval(interval);
        }

        /// <summary>
        /// 设置主时间周期
        /// </summary>
        public void SetPrimaryBarInterval(int intervalSeconds)
        {
            _barAggregator.SetPrimaryInterval(intervalSeconds);
        }

        #endregion

        public void Dispose()
        {
            _hotkeyManager?.Dispose();
            _client.Dispose();
        }
    }
}