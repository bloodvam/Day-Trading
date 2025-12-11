using TradingEngine.Models;
using TradingEngine.Parsers;

namespace TradingEngine.Managers
{
    /// <summary>
    /// 管理行情订阅
    /// </summary>
    public class SubscriptionManager
    {
        private readonly DasClient _client;
        private readonly BarAggregator _barAggregator;
        private readonly Dictionary<string, Quote> _quotes = new();
        private readonly object _lock = new();

        private string? _currentSymbol;

        public string? CurrentSymbol => _currentSymbol;

        public event Action<Quote>? QuoteUpdated;
        public event Action<Tick>? TickReceived;
        public event Action<string>? SymbolSubscribed;
        public event Action<string>? SymbolUnsubscribed;

        public SubscriptionManager(DasClient client, BarAggregator barAggregator)
        {
            _client = client;
            _barAggregator = barAggregator;
            SubscribeEvents();
        }

        private void SubscribeEvents()
        {
            _client.QuoteReceived += OnQuoteReceived;
            _client.TsReceived += OnTsReceived;
        }

        private void OnQuoteReceived(string line)
        {
            lock (_lock)
            {
                // 解析到现有Quote对象或创建新的
                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2) return;

                string symbol = parts[1];
                if (!_quotes.TryGetValue(symbol, out var quote))
                {
                    quote = new Quote { Symbol = symbol };
                    _quotes[symbol] = quote;
                }

                MessageParser.ParseQuote(line, quote);
                QuoteUpdated?.Invoke(quote);
            }
        }

        private void OnTsReceived(string line)
        {
            var tick = MessageParser.ParseTick(line);
            if (tick == null) return;

            // 传递给BarAggregator
            _barAggregator.ProcessTick(tick);

            TickReceived?.Invoke(tick);
        }

        /// <summary>
        /// 订阅股票（单股票模式，会自动取消之前的订阅）
        /// </summary>
        public async Task SubscribeAsync(string symbol)
        {
            symbol = symbol.ToUpper().Trim();
            if (string.IsNullOrEmpty(symbol)) return;

            // 先取消之前的订阅
            if (!string.IsNullOrEmpty(_currentSymbol) && _currentSymbol != symbol)
            {
                await UnsubscribeCurrentAsync();
            }

            _currentSymbol = symbol;

            // 初始化Quote
            lock (_lock)
            {
                if (!_quotes.ContainsKey(symbol))
                {
                    _quotes[symbol] = new Quote { Symbol = symbol };
                }
            }

            // 订阅Lv1和T&S
            await _client.SubscribeLv1(symbol);
            await _client.SubscribeTimeSales(symbol);

            SymbolSubscribed?.Invoke(symbol);
        }

        /// <summary>
        /// 取消当前订阅
        /// </summary>
        public async Task UnsubscribeCurrentAsync()
        {
            if (string.IsNullOrEmpty(_currentSymbol)) return;

            string symbol = _currentSymbol;
            _currentSymbol = null;

            await _client.UnsubscribeLv1(symbol);
            await _client.UnsubscribeTimeSales(symbol);

            // 清除Bar数据
            _barAggregator.Clear(symbol);

            lock (_lock)
            {
                _quotes.Remove(symbol);
            }

            SymbolUnsubscribed?.Invoke(symbol);
        }

        /// <summary>
        /// 获取当前报价
        /// </summary>
        public Quote? GetCurrentQuote()
        {
            if (string.IsNullOrEmpty(_currentSymbol)) return null;

            lock (_lock)
            {
                return _quotes.TryGetValue(_currentSymbol, out var quote) ? quote : null;
            }
        }

        /// <summary>
        /// 获取指定symbol的报价
        /// </summary>
        public Quote? GetQuote(string symbol)
        {
            lock (_lock)
            {
                return _quotes.TryGetValue(symbol, out var quote) ? quote : null;
            }
        }
    }
}