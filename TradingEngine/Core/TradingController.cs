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
        private readonly SymbolDataManager _dataManager;
        private readonly BarAggregator _barAggregator;
        private readonly AccountManager _accountManager;
        private readonly SubscriptionManager _subscriptionManager;
        private readonly OrderManager _orderManager;

        private HotkeyManager? _hotkeyManager;

        #region Properties

        public bool IsConnected => _client.IsConnected;
        public bool IsLoggedIn => _client.IsLoggedIn;
        public string? ActiveSymbol => _dataManager.ActiveSymbol;
        public IReadOnlyCollection<string> SubscribedSymbols => _dataManager.Symbols;
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
        public event Action<string?>? ActiveSymbolChanged;
        public event Action<Quote>? QuoteUpdated;
        public event Action<Quote>? AnyQuoteUpdated;
        public event Action<Bar>? BarUpdated;
        public event Action<Bar>? BarCompleted;

        #endregion

        #region Events - Account

        public event Action<AccountInfo>? AccountInfoChanged;
        public event Action<Position>? PositionChanged;
        public event Action<Order>? OrderAdded;
        public event Action<Order>? OrderRemoved;

        #endregion

        #region Events - Order Tracking

        public event Action? NewOperationStarted;
        public event Action<int>? NewOrderSent;

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
            // 初始化核心数据管理
            _client = new DasClient();
            _dataManager = new SymbolDataManager();
            _barAggregator = new BarAggregator(_dataManager, AppConfig.Instance.Trading.DefaultBarInterval);

            // 初始化业务管理器
            _accountManager = new AccountManager(_client, _dataManager);
            _subscriptionManager = new SubscriptionManager(_client, _dataManager, _barAggregator);
            _orderManager = new OrderManager(_client, _accountManager, _dataManager, _barAggregator);

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

            // SymbolDataManager events
            _dataManager.SymbolAdded += (s) => SymbolSubscribed?.Invoke(s);
            _dataManager.SymbolRemoved += (s) => SymbolUnsubscribed?.Invoke(s);
            _dataManager.ActiveSymbolChanged += (s) => ActiveSymbolChanged?.Invoke(s);

            // Subscription events
            _subscriptionManager.QuoteUpdated += (q) => QuoteUpdated?.Invoke(q);
            _subscriptionManager.AnyQuoteUpdated += (q) => AnyQuoteUpdated?.Invoke(q);

            // Bar events - 只触发 ActiveSymbol 的
            _barAggregator.BarUpdated += (b) =>
            {
                if (b.Symbol == _dataManager.ActiveSymbol)
                {
                    BarUpdated?.Invoke(b);
                }
            };
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

            // Order tracking
            _orderManager.NewOrderSent += (token) => NewOrderSent?.Invoke(token);
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
        }

        public async Task UnsubscribeAsync(string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol)) return;
            await _subscriptionManager.UnsubscribeAsync(symbol);
        }

        public void SetActiveSymbol(string? symbol)
        {
            _dataManager.ActiveSymbol = symbol;
        }

        #endregion

        #region Trading

        public async Task BuyOneR()
        {
            try
            {
                Log?.Invoke("Hotkey: Buy 1R triggered");
                NewOperationStarted?.Invoke();
                await _orderManager.BuyOneR();
            }
            catch (Exception ex)
            {
                Log?.Invoke($"BuyOneR error: {ex.Message}");
            }
        }

        public async Task SellAll()
        {
            try
            {
                Log?.Invoke("Hotkey: Sell All triggered");
                NewOperationStarted?.Invoke();
                await _orderManager.SellAll();
            }
            catch (Exception ex)
            {
                Log?.Invoke($"SellAll error: {ex.Message}");
            }
        }

        public async Task SellHalf()
        {
            try
            {
                Log?.Invoke("Hotkey: Sell Half triggered");
                NewOperationStarted?.Invoke();
                await _orderManager.SellHalf();
            }
            catch (Exception ex)
            {
                Log?.Invoke($"SellHalf error: {ex.Message}");
            }
        }

        public async Task Sell70Percent()
        {
            try
            {
                Log?.Invoke("Hotkey: Sell 70% triggered");
                NewOperationStarted?.Invoke();
                await _orderManager.Sell70Percent();
            }
            catch (Exception ex)
            {
                Log?.Invoke($"Sell70Percent error: {ex.Message}");
            }
        }

        public async Task AddPositionBreakeven()
        {
            try
            {
                Log?.Invoke("Hotkey: Add Position (Breakeven) triggered");
                NewOperationStarted?.Invoke();
                await _orderManager.AddPositionBreakeven();
            }
            catch (Exception ex)
            {
                Log?.Invoke($"AddPositionBreakeven error: {ex.Message}");
            }
        }

        public async Task AddPositionHalfProfit()
        {
            try
            {
                Log?.Invoke("Hotkey: Add Position (Half Profit) triggered");
                NewOperationStarted?.Invoke();
                await _orderManager.AddPositionHalfProfit();
            }
            catch (Exception ex)
            {
                Log?.Invoke($"AddPositionHalfProfit error: {ex.Message}");
            }
        }

        public async Task MoveStopToBreakeven()
        {
            try
            {
                Log?.Invoke("Hotkey: Move Stop to Breakeven triggered");
                NewOperationStarted?.Invoke();
                await _orderManager.MoveStopToBreakeven();
            }
            catch (Exception ex)
            {
                Log?.Invoke($"MoveStopToBreakeven error: {ex.Message}");
            }
        }

        public async Task CancelAllOrders()
        {
            try
            {
                Log?.Invoke("Cancel all orders");
                await _orderManager.CancelAllOrders();
            }
            catch (Exception ex)
            {
                Log?.Invoke($"CancelAllOrders error: {ex.Message}");
            }
        }

        #endregion

        #region Hotkeys

        public (bool allSuccess, string? failedKey) SetupHotkeys(Form form)
        {
            _hotkeyManager = new HotkeyManager(form);

            var hotkeys = new List<(Keys key, string name, Action action)>
            {
                (Keys.D1 | Keys.Shift, "Shift+1", () => _ = Task.Run(BuyOneR)),
                (Keys.D1 | Keys.Alt, "Alt+1", () => _ = Task.Run(SellAll)),
                (Keys.D2 | Keys.Alt, "Alt+2", () => _ = Task.Run(SellHalf)),
                (Keys.D3 | Keys.Alt, "Alt+3", () => _ = Task.Run(Sell70Percent)),
                (Keys.Q | Keys.Shift, "Shift+Q", () => _ = Task.Run(AddPositionBreakeven)),
                (Keys.W | Keys.Shift, "Shift+W", () => _ = Task.Run(AddPositionHalfProfit)),
                (Keys.Space, "Space", () => _ = Task.Run(MoveStopToBreakeven))
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
            return _dataManager.ActiveState?.Quote;
        }

        public Quote? GetQuote(string symbol)
        {
            return _dataManager.Get(symbol)?.Quote;
        }

        public Bar? GetCurrentBar()
        {
            var symbol = _dataManager.ActiveSymbol;
            if (string.IsNullOrEmpty(symbol)) return null;
            return _barAggregator.GetCurrentBar(symbol);
        }

        public Bar? GetCurrentBar(string symbol)
        {
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

        public BarSeries? GetBarSeries(int intervalSeconds)
        {
            var symbol = _dataManager.ActiveSymbol;
            if (string.IsNullOrEmpty(symbol)) return null;
            return _barAggregator.GetBarSeries(symbol, intervalSeconds);
        }

        public BarSeries? GetBarSeries()
        {
            var symbol = _dataManager.ActiveSymbol;
            if (string.IsNullOrEmpty(symbol)) return null;
            return _barAggregator.GetBarSeries(symbol);
        }

        public void EnableBarInterval(BarInterval interval)
        {
            _barAggregator.EnableInterval(interval);
        }

        public void SetPrimaryBarInterval(int intervalSeconds)
        {
            _barAggregator.SetPrimaryInterval(intervalSeconds);
        }

        /// <summary>
        /// 获取 SymbolState（用于 UI 访问日志等）
        /// </summary>
        public SymbolState? GetSymbolState(string symbol)
        {
            return _dataManager.Get(symbol);
        }

        #endregion

        public void Dispose()
        {
            _hotkeyManager?.Dispose();
            _orderManager.Dispose();
            _barAggregator.Dispose();
            _client.Dispose();
        }
    }
}