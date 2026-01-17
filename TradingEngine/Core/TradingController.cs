using TradingEngine.Config;
using TradingEngine.Managers;
using TradingEngine.Models;
using System.Drawing;

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
        private readonly IndicatorManager _indicatorManager;
        private readonly StrategyManager _strategyManager;
        private readonly AgentStrategy _agentStrategy;

        private HotkeyManager? _hotkeyManager;

        #region Properties

        public bool IsConnected => _client.IsConnected;
        public bool IsLoggedIn => _client.IsLoggedIn;
        public string? ActiveSymbol => _dataManager.ActiveSymbol;
        public IReadOnlyCollection<string> SubscribedSymbols => _dataManager.Symbols;
        public AccountInfo AccountInfo => _accountManager.AccountInfo;
        public Position? GetPosition(string symbol) => _accountManager.GetPosition(symbol);

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
        public event Action<string, double, double>? IndicatorsUpdated;  // symbol, atr14, ema20
        public event Action<string, double>? VwapUpdated;                // symbol, vwap
        public event Action<string, double>? SessionHighUpdated;         // symbol, sessionHigh
        public event Action<string, bool>? EMA20Crossed;                 // symbol, isAbove
        public event Action<string, bool>? VWAPCrossed;                  // symbol, isAbove
        public event Action<string, double, double>? TrailingStopUpdated; // symbol, trailHalf, trailAll
        public event Action<string, double>? AgentTriggerPriceUpdated;   // symbol, triggerPrice
        public event Action<string, bool>? AgentStateChanged;            // symbol, isEnabled
        public event Action<string, double>? LevelCrossed;               // symbol, ceilLevel (价格跨越关键位)

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
        public event Action<string>? OrderLog;
        public event Action<string>? StrategyLog;
        public event Action<string, Color>? StrategyLogWithColor;  // 带颜色的 Strategy 日志
        public event Action<string>? AgentLog;
        public event Action<string, Color>? AgentLogWithColor;  // 带颜色的 Agent 日志
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
            _indicatorManager = new IndicatorManager(_client, _barAggregator, _dataManager);
            _strategyManager = new StrategyManager(_client, _dataManager, _barAggregator, _orderManager, _accountManager);
            _agentStrategy = new AgentStrategy(_dataManager, _barAggregator);

            SubscribeInternalEvents();
        }

        private void SubscribeInternalEvents()
        {
            // Connection events
            _client.Connected += () => Connected?.Invoke();
            _client.Disconnected += () => Disconnected?.Invoke();
            _client.LoginSuccess += async () =>
            {
                LoginSuccess?.Invoke();
                try
                {
                    await _accountManager.RefreshAll();
                }
                catch (Exception ex)
                {
                    Log?.Invoke($"RefreshAll error: {ex.Message}");
                }
            };
            _client.LoginFailed += (msg) => LoginFailed?.Invoke(msg);

            // SymbolDataManager events
            _dataManager.SymbolAdded += (s) => SymbolSubscribed?.Invoke(s);
            _dataManager.SymbolRemoved += (s) => SymbolUnsubscribed?.Invoke(s);
            _dataManager.ActiveSymbolChanged += (s) => ActiveSymbolChanged?.Invoke(s);

            // Subscription events
            _subscriptionManager.QuoteUpdated += (q) => QuoteUpdated?.Invoke(q);
            _subscriptionManager.AnyQuoteUpdated += (q) => AnyQuoteUpdated?.Invoke(q);
            _subscriptionManager.SessionHighUpdated += (symbol, high) =>
            {
                if (symbol == _dataManager.ActiveSymbol)
                {
                    SessionHighUpdated?.Invoke(symbol, high);
                }
            };

            // Bar events - 只触发 ActiveSymbol 的
            _barAggregator.BarUpdated += (b) =>
            {
                if (b.Symbol == _dataManager.ActiveSymbol)
                {
                    BarUpdated?.Invoke(b);
                }
            };
            _barAggregator.BarCompleted += (b) => BarCompleted?.Invoke(b);

            // Indicator events - 只触发 ActiveSymbol 的
            _indicatorManager.IndicatorsUpdated += (symbol, atr, ema) =>
            {
                if (symbol == _dataManager.ActiveSymbol)
                {
                    IndicatorsUpdated?.Invoke(symbol, atr, ema);
                }
            };
            _indicatorManager.VwapUpdated += (symbol, vwap) =>
            {
                if (symbol == _dataManager.ActiveSymbol)
                {
                    VwapUpdated?.Invoke(symbol, vwap);
                }
            };
            _indicatorManager.SessionHighUpdated += (symbol, high) =>
            {
                if (symbol == _dataManager.ActiveSymbol)
                {
                    SessionHighUpdated?.Invoke(symbol, high);
                }
            };
            _indicatorManager.EMA20Crossed += (symbol, isAbove) =>
            {
                if (symbol == _dataManager.ActiveSymbol)
                {
                    EMA20Crossed?.Invoke(symbol, isAbove);
                }
            };
            _indicatorManager.VWAPCrossed += (symbol, isAbove) =>
            {
                if (symbol == _dataManager.ActiveSymbol)
                {
                    VWAPCrossed?.Invoke(symbol, isAbove);
                }
            };

            // Account events
            _accountManager.AccountInfoChanged += (a) => AccountInfoChanged?.Invoke(a);
            _accountManager.PositionChanged += (p) => PositionChanged?.Invoke(p);
            _accountManager.OrderAdded += (o) => OrderAdded?.Invoke(o);
            _accountManager.OrderRemoved += (o) => OrderRemoved?.Invoke(o);

            // Logging - 分类日志
            _orderManager.Log += (msg) => OrderLog?.Invoke(msg);
            _strategyManager.Log += (msg) => StrategyLog?.Invoke(msg);
            _strategyManager.LogWithColor += (msg, color) => StrategyLogWithColor?.Invoke(msg, color);
            _strategyManager.TrailingStopUpdated += (symbol, trailHalf, trailAll) =>
            {
                if (symbol == _dataManager.ActiveSymbol)
                {
                    TrailingStopUpdated?.Invoke(symbol, trailHalf, trailAll);
                }
            };
            _strategyManager.TrailingStopLog += (msg, color) => AgentLogWithColor?.Invoke(msg, color);
            _strategyManager.ValidTickReceived += (tick) => _agentStrategy.OnValidTickReceived(tick);

            // OrderManager - 订单触发时清空 Order Log
            _orderManager.OrderTriggered += () => NewOperationStarted?.Invoke();

            // Agent Strategy
            _agentStrategy.Log += (msg) => AgentLog?.Invoke(msg);
            _agentStrategy.TriggerPriceUpdated += (symbol, price) =>
            {
                if (symbol == _dataManager.ActiveSymbol)
                {
                    AgentTriggerPriceUpdated?.Invoke(symbol, price);
                }
            };
            _agentStrategy.AgentStateChanged += (symbol, isEnabled) =>
            {
                if (symbol == _dataManager.ActiveSymbol)
                {
                    AgentStateChanged?.Invoke(symbol, isEnabled);
                }
            };
            _agentStrategy.LevelCrossed += (symbol, ceilLevel) =>
            {
                if (symbol == _dataManager.ActiveSymbol)
                {
                    LevelCrossed?.Invoke(symbol, ceilLevel);
                }
            };
            _agentStrategy.OpenSignalTriggered += (symbol, triggerPrice) =>
            {
                StartOpen(symbol, triggerPrice);
            };

            // 实际发送买单时关闭 Agent
            _strategyManager.BuyTriggered += (symbol) =>
            {
                if (_agentStrategy.IsEnabled(symbol))
                {
                    _agentStrategy.Disable(symbol);
                }
            };

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

                // Hotkey 触发时，用 currentBar.Low 作为止损价
                string? symbol = _dataManager.ActiveSymbol;
                if (string.IsNullOrEmpty(symbol)) return;

                var currentBar = _barAggregator.GetCurrentBar(symbol);
                if (currentBar == null) return;

                double stopPrice = currentBar.Low;

                // 更新 state.StopPrice
                var state = _dataManager.Get(symbol);
                if (state != null)
                {
                    state.StopPrice = stopPrice;
                    state.StrategyEnabled = true;
                }

                await _orderManager.BuyOneR(stopPrice);
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
                await _orderManager.MoveStopToBreakeven();
            }
            catch (Exception ex)
            {
                Log?.Invoke($"MoveStopToBreakeven error: {ex.Message}");
            }
        }

        public void ToggleTrailingStopForActiveSymbol()
        {
            string? symbol = ActiveSymbol;
            if (string.IsNullOrEmpty(symbol))
            {
                Log?.Invoke("Hotkey: Toggle TrailingStop - No symbol selected");
                return;
            }

            bool isActive = ToggleTrailingStop(symbol);
            Log?.Invoke($"Hotkey: TrailingStop {(isActive ? "Started" : "Stopped")} for {symbol}");

            // 触发事件更新 UI
            var state = _dataManager.Get(symbol);
            if (state != null)
            {
                TrailingStopUpdated?.Invoke(symbol, state.TrailHalf, state.TrailAll);
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

        public async Task CancelOrder(int orderId)
        {
            try
            {
                Log?.Invoke($"Cancel order {orderId}");
                await _client.CancelOrder(orderId);
            }
            catch (Exception ex)
            {
                Log?.Invoke($"CancelOrder error: {ex.Message}");
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
                (Keys.Space, "Space", () => _ = Task.Run(MoveStopToBreakeven)),
                (Keys.D1 | Keys.Control, "Ctrl+1", ToggleTrailingStopForActiveSymbol)
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

        public void ResetVwap(string symbol, double? initialVwap = null)
        {
            _indicatorManager.ResetVwap(symbol, initialVwap);
            Log?.Invoke($"VWAP reset for {symbol}" + (initialVwap.HasValue ? $": Initial={initialVwap:F3}" : ""));
        }

        public void ResetSessionHigh(string symbol, double? initialValue = null)
        {
            _indicatorManager.ResetSessionHigh(symbol, initialValue);
            Log?.Invoke($"Session High reset for {symbol}" + (initialValue.HasValue ? $": Initial={initialValue:F3}" : ""));
        }

        public void StartOpen(string symbol, double triggerPrice)
        {
            _strategyManager.StartOpen(symbol, triggerPrice);
        }

        public void StartHighBreakout(string symbol, double triggerPrice)
        {
            _strategyManager.StartHighBreakout(symbol, triggerPrice);
        }

        public void StartAddAll(string symbol, double triggerPrice)
        {
            _strategyManager.StartAddAll(symbol, triggerPrice);
        }

        public void StartAddHalf(string symbol, double triggerPrice)
        {
            _strategyManager.StartAddHalf(symbol, triggerPrice);
        }

        public void StopStrategy(string symbol)
        {
            _strategyManager.StopStrategy(symbol);
        }

        #region Agent Mode

        public void EnableAgent(string symbol)
        {
            _agentStrategy.Enable(symbol);
        }

        public void DisableAgent(string symbol)
        {
            _agentStrategy.Disable(symbol);
        }

        public bool IsAgentEnabled(string symbol)
        {
            return _agentStrategy.IsEnabled(symbol);
        }

        public double GetAgentTriggerPrice(string symbol)
        {
            return _agentStrategy.GetTriggerPrice(symbol);
        }

        public void SetAgentBreakedLevel(string symbol, double level)
        {
            _agentStrategy.SetBreakedLevel(symbol, level);
        }

        public void SetTrailHalf(string symbol, double value)
        {
            var state = _dataManager.Get(symbol);
            if (state == null) return;

            state.TrailHalf = value;
            state.TrailAll = Math.Max(value * 1.25, AppConfig.Instance.Trading.MinTrailAll);
        }

        public void SetTrailAll(string symbol, double value)
        {
            var state = _dataManager.Get(symbol);
            if (state == null) return;

            state.TrailAll = value;
        }

        public void StartTrailingStop(string symbol)
        {
            _strategyManager.StartTrailingStop(symbol);
        }

        public void StopTrailingStop(string symbol)
        {
            _strategyManager.StopTrailingStop(symbol);
        }

        public bool ToggleTrailingStop(string symbol)
        {
            return _strategyManager.ToggleTrailingStop(symbol);
        }

        /// <summary>
        /// AutoFill 开仓：无持仓时触发 StartOpen，有持仓时只返回 true
        /// </summary>
        /// <returns>true 表示只填充，false 表示已触发 StartOpen</returns>
        public bool AutoFillOpen(string symbol, double price)
        {
            if (string.IsNullOrEmpty(symbol)) return true;

            var pos = _accountManager.GetPosition(symbol);
            bool hasPosition = pos != null && pos.Quantity > 0;

            if (hasPosition)
            {
                Log?.Invoke($"AutoFill: {price:F2}");
                return true;  // 只填充
            }
            else
            {
                StartOpen(symbol, price);
                Log?.Invoke($"AutoFill: StartOpen at {price:F2}");
                return false;  // 已触发
            }
        }

        /// <summary>
        /// 获取 AutoFill 价格（ceil1, ceil2, ceilSessionHigh）
        /// </summary>
        public (double ceil1, double ceil2, double ceilSessionHigh) GetAutoFillPrices(string symbol)
        {
            var state = _dataManager.Get(symbol);
            if (state == null) return (0, 0, 0);

            double lastPrice = state.Quote.Last;
            double sessionHigh = state.SessionHigh;

            double ceil1 = lastPrice > 0 ? AgentStrategy.CeilLevel(lastPrice) : 0;
            double ceil2 = ceil1 > 0 ? ceil1 + 0.5 : 0;
            double ceilSessionHigh = sessionHigh > 0 ? AgentStrategy.CeilLevel(sessionHigh) : 0;

            return (ceil1, ceil2, ceilSessionHigh);
        }

        #endregion

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
            _strategyManager.Dispose();
            _agentStrategy.Dispose();
            _indicatorManager.Dispose();
            _orderManager.Dispose();
            _barAggregator.Dispose();
            _subscriptionManager.Dispose();
            _accountManager.Dispose();
            _client.Dispose();
        }
    }
}