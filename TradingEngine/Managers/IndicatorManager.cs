using TradingEngine.Models;
using TradingEngine.Utils;

namespace TradingEngine.Managers
{
    /// <summary>
    /// 指标管理器 - 监听 Bar 完成事件，更新 SymbolState 中的指标值
    /// </summary>
    public class IndicatorManager : IDisposable
    {
        private readonly DasClient _client;
        private readonly BarAggregator _barAggregator;
        private readonly SymbolDataManager _dataManager;

        public event Action<string, double, double>? IndicatorsUpdated;  // symbol, atr14, ema20
        public event Action<string, double>? VwapUpdated;                // symbol, vwap
        public event Action<string, double>? SessionHighUpdated;         // symbol, sessionHigh

        public IndicatorManager(DasClient client, BarAggregator barAggregator, SymbolDataManager dataManager)
        {
            _client = client;
            _barAggregator = barAggregator;
            _dataManager = dataManager;

            _barAggregator.BarCompleted += OnBarCompleted;
            _client.TsReceived += OnTsReceived;
        }

        private void OnBarCompleted(Bar bar)
        {
            var state = _dataManager.Get(bar.Symbol);
            if (state == null) return;

            var series = _barAggregator.GetBarSeries(bar.Symbol, bar.IntervalSeconds);
            if (series == null || series.Count < 2) return;

            // 获取价格数据
            var highs = series.GetHighs();
            var lows = series.GetLows();
            var closes = series.GetCloses();

            // 计算指标
            double atr14 = Indicators.ATR(highs, lows, closes, 14);
            double ema20 = Indicators.EMA(closes, 20);

            // 更新 SymbolState
            state.ATR14 = atr14;
            state.EMA20 = ema20;

            // 触发事件
            IndicatorsUpdated?.Invoke(bar.Symbol, atr14, ema20);
        }

        private void OnTsReceived(string line)
        {
            var tick = MessageParser.ParseTick(line);
            if (tick == null) return;

            var state = _dataManager.Get(tick.Symbol);
            if (state == null) return;

            // Session High - 使用 BarAggregator 的过滤规则
            if (IsValidForAggregator(tick))
            {
                if (tick.Price > state.SessionHigh)
                {
                    state.SessionHigh = tick.Price;
                    SessionHighUpdated?.Invoke(tick.Symbol, state.SessionHigh);
                }
            }

            // VWAP - 只处理 IsValidForVolume 的 Tick
            if (tick.IsValidForVolume && state.VwapEnabled)
            {
                state.CumulativeValue += tick.Price * tick.Volume;
                state.CumulativeVolume += tick.Volume;

                if (state.CumulativeVolume > 0)
                {
                    state.VWAP = state.CumulativeValue / state.CumulativeVolume;
                    VwapUpdated?.Invoke(tick.Symbol, state.VWAP);
                }
            }
        }

        /// <summary>
        /// 是否满足 BarAggregator 的过滤规则
        /// </summary>
        private bool IsValidForAggregator(Tick tick)
        {
            if (tick.Exchange == "FADF" && !tick.IsValidForLastPrice) return false;
            return true;
        }

        /// <summary>
        /// 重置 VWAP（从 0 开始或指定初始值）
        /// </summary>
        public void ResetVwap(string symbol, double? initialVwap = null)
        {
            var state = _dataManager.Get(symbol);
            if (state == null) return;

            if (initialVwap.HasValue && initialVwap.Value > 0)
            {
                // 有初始值：用初始值 × 当前 Volume 初始化
                long initialVolume = state.Quote.Volume;
                if (initialVolume > 0)
                {
                    state.CumulativeValue = initialVwap.Value * initialVolume;
                    state.CumulativeVolume = initialVolume;
                    state.VWAP = initialVwap.Value;
                    VwapUpdated?.Invoke(symbol, state.VWAP);
                }
            }
            else
            {
                // 无初始值：清零，等下一笔 tick 重新开始
                state.CumulativeValue = 0;
                state.CumulativeVolume = 0;
                state.VWAP = 0;
                VwapUpdated?.Invoke(symbol, 0);
            }
        }

        /// <summary>
        /// 重置 Session High
        /// </summary>
        public void ResetSessionHigh(string symbol, double? initialValue = null)
        {
            var state = _dataManager.Get(symbol);
            if (state == null) return;

            state.SessionHigh = initialValue ?? 0;
            SessionHighUpdated?.Invoke(symbol, state.SessionHigh);
        }

        public void Dispose()
        {
            _barAggregator.BarCompleted -= OnBarCompleted;
            _client.TsReceived -= OnTsReceived;
        }
    }
}