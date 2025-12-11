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
        public event Action<Order>? OrderChanged;
        public event Action<Trade>? TradeReceived;

        #endregion

        #region Events - Logging

        public event Action<string>? Log;
        public event Action<string>? RawMessage;

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
            _accountManager.OrderChanged += (o) => OrderChanged?.Invoke(o);
            _accountManager.TradeReceived += (t) => TradeReceived?.Invoke(t);

            // Logging
            _orderManager.Log += (msg) => Log?.Invoke(msg);
            _client.RawMessage += (msg) => RawMessage?.Invoke(msg);
            _client.CommandSent += (msg) => Log?.Invoke(msg);  // 打印发送的命令
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

        public async Task CancelAllOrders()
        {
            Log?.Invoke("Cancel all orders");
            await _orderManager.CancelAllOrders();
        }

        #endregion

        #region Hotkeys

        public void SetupHotkeys(Form form)
        {
            _hotkeyManager = new HotkeyManager(form);
            _hotkeyManager.Log += (msg) => Log?.Invoke(msg);

            // Shift+1: Buy 1R
            _hotkeyManager.RegisterHotkey(Keys.D1 | Keys.Shift, async () =>
            {
                await BuyOneR();
            });

            // Alt+1: Sell All
            _hotkeyManager.RegisterHotkey(Keys.D1 | Keys.Alt, async () =>
            {
                await SellAll();
            });
        }

        public void EnableHotkeys(bool enabled)
        {
            if (_hotkeyManager != null)
                _hotkeyManager.IsEnabled = enabled;
        }

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

        public List<Position> GetAllPositions()
        {
            return _accountManager.GetAllPositions();
        }

        public List<Order> GetAllOrders()
        {
            return _accountManager.GetAllOrders();
        }

        public List<Order> GetOpenOrders()
        {
            return _accountManager.GetOpenOrders();
        }

        public List<Trade> GetAllTrades()
        {
            return _accountManager.GetAllTrades();
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