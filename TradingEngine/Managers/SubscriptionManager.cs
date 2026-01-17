using TradingEngine.Models;
using TradingEngine.Utils;

namespace TradingEngine.Managers
{
    /// <summary>
    /// 管理行情订阅
    /// </summary>
    public class SubscriptionManager : IDisposable
    {
        private readonly DasClient _client;
        private readonly SymbolDataManager _dataManager;
        private readonly BarAggregator _barAggregator;

        // 事件
        public event Action<Quote>? QuoteUpdated;           // 只有 ActiveSymbol 的 Quote 更新
        public event Action<Quote>? AnyQuoteUpdated;        // 任何订阅股票的 Quote 更新
        public event Action<Tick>? TickReceived;
        public event Action<string>? SymbolSubscribed;
        public event Action<string>? SymbolUnsubscribed;
        public event Action<string, double>? SessionHighUpdated;  // symbol, newHigh

        public SubscriptionManager(DasClient client, SymbolDataManager dataManager, BarAggregator barAggregator)
        {
            _client = client;
            _dataManager = dataManager;
            _barAggregator = barAggregator;

            _client.QuoteReceived += OnQuoteReceived;
            _client.TsReceived += OnTsReceived;
        }

        private void OnQuoteReceived(string line)
        {
            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) return;

            string symbol = parts[1];
            var state = _dataManager.Get(symbol);
            if (state == null) return;

            MessageParser.ParseQuote(line, state.Quote);

            // 只在 SessionHigh == 0 时用 Quote.High 初始化
            if (state.SessionHigh == 0 && state.Quote.High > 0)
            {
                state.SessionHigh = state.Quote.High;
                SessionHighUpdated?.Invoke(symbol, state.SessionHigh);
            }

            // 所有订阅股票的 Quote 更新都触发
            AnyQuoteUpdated?.Invoke(state.Quote);

            // 只有当前选中的股票才触发 QuoteUpdated
            if (symbol == _dataManager.ActiveSymbol)
            {
                QuoteUpdated?.Invoke(state.Quote);
            }
        }

        private void OnTsReceived(string line)
        {
            var tick = MessageParser.ParseTick(line);
            if (tick == null) return;

            if (!_dataManager.Contains(tick.Symbol)) return;

            // 传递给 BarAggregator
            _barAggregator.ProcessTick(tick);

            // 只有当前选中的股票才触发事件
            if (tick.Symbol == _dataManager.ActiveSymbol)
            {
                TickReceived?.Invoke(tick);
            }
        }

        /// <summary>
        /// 订阅股票
        /// </summary>
        public async Task SubscribeAsync(string symbol)
        {
            symbol = symbol.ToUpper().Trim();
            if (string.IsNullOrEmpty(symbol)) return;

            // 已经订阅过了，直接设为 active
            if (_dataManager.Contains(symbol))
            {
                _dataManager.ActiveSymbol = symbol;
                return;
            }

            // 创建 SymbolState
            _dataManager.GetOrCreate(symbol);

            // 订阅 Lv1 和 T&S
            await _client.SubscribeLv1(symbol);
            await _client.SubscribeTimeSales(symbol);

            SymbolSubscribed?.Invoke(symbol);

            // 最新订阅的自动选中
            _dataManager.ActiveSymbol = symbol;
        }

        /// <summary>
        /// 取消订阅指定股票
        /// </summary>
        public async Task UnsubscribeAsync(string symbol)
        {
            symbol = symbol.ToUpper().Trim();
            if (string.IsNullOrEmpty(symbol)) return;

            if (!_dataManager.Contains(symbol)) return;

            await _client.UnsubscribeLv1(symbol);
            await _client.UnsubscribeTimeSales(symbol);

            // 移除 SymbolState（会触发 SymbolRemoved 事件，其他 Manager 自动清理）
            _dataManager.Remove(symbol);

            SymbolUnsubscribed?.Invoke(symbol);
        }

        /// <summary>
        /// 获取当前选中股票的报价
        /// </summary>
        public Quote? GetCurrentQuote()
        {
            return _dataManager.ActiveState?.Quote;
        }

        /// <summary>
        /// 获取指定股票的报价
        /// </summary>
        public Quote? GetQuote(string symbol)
        {
            return _dataManager.Get(symbol)?.Quote;
        }

        public void Dispose()
        {
            _client.QuoteReceived -= OnQuoteReceived;
            _client.TsReceived -= OnTsReceived;
        }
    }
}