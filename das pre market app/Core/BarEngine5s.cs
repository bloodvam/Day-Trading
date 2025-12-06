using System;
using System.Collections.Generic;
using PreMarketTrader.Models;

namespace PreMarketTrader.Core
{
    /// <summary>
    /// 5 秒 K 线引擎（仅在收到 T&S Tick 时构建）
    /// 没有 Tick 就不生成 bar，符合 DAS 5s 图逻辑。
    /// </summary>
    public class BarEngine5s
    {
        private readonly Dictionary<string, Bar5s> _currentBars = new();
        private readonly Dictionary<string, List<Bar5s>> _history = new();

        public int MaxBarsPerSymbol { get; set; } = 500;

        /// <summary>
        /// 当一根 bar 收盘时触发
        /// </summary>
        public event Action<Bar5s>? BarClosed;

        /// <summary>
        /// 收到 tick（来自 $T&S）
        /// </summary>
        public void OnTick(Tick tick)
        {
            var bucketStart = FloorTo5Seconds(tick.Time);
            var bucketEnd = bucketStart.AddSeconds(5);

            // 当前是否正在构建 bar？
            if (!_currentBars.TryGetValue(tick.Symbol, out var bar))
            {
                // 没 bar → 创建新 bar
                bar = CreateNewBar(tick, bucketStart, bucketEnd);
                _currentBars[tick.Symbol] = bar;
                return;
            }

            // 时间进入新的 5 秒 bucket？→ 收盘上一根 + 新开一根
            if (bar.StartTime != bucketStart)
            {
                CloseBar(bar);

                var newBar = CreateNewBar(tick, bucketStart, bucketEnd);
                _currentBars[tick.Symbol] = newBar;
                return;
            }

            // 在同一根 bar 内 → 更新
            UpdateBar(bar, tick);
        }

        // ---------------------------------------------------------
        // 辅助函数：创建新 bar
        // ---------------------------------------------------------
        private Bar5s CreateNewBar(Tick tick, DateTime bucketStart, DateTime bucketEnd)
        {
            return new Bar5s
            {
                Symbol = tick.Symbol,
                StartTime = bucketStart,
                EndTime = bucketEnd,
                Open = tick.Price,
                High = tick.Price,
                Low = tick.Price,
                Close = tick.Price,
                Volume = tick.Volume
            };
        }

        // ---------------------------------------------------------
        // 辅助函数：更新 bar（同 bucket 内）
        // ---------------------------------------------------------
        private void UpdateBar(Bar5s bar, Tick tick)
        {
            bar.High = Math.Max(bar.High, tick.Price);
            bar.Low = Math.Min(bar.Low, tick.Price);
            bar.Close = tick.Price;
            bar.Volume += tick.Volume;
        }

        // ---------------------------------------------------------
        // 收盘 bar
        // ---------------------------------------------------------
        private void CloseBar(Bar5s bar)
        {
            if (!_history.TryGetValue(bar.Symbol, out var list))
            {
                list = new List<Bar5s>();
                _history[bar.Symbol] = list;
            }

            list.Add(bar);
            if (list.Count > MaxBarsPerSymbol)
                list.RemoveAt(0);

            BarClosed?.Invoke(bar);
        }

        // ---------------------------------------------------------
        // 获取历史 bar
        // ---------------------------------------------------------
        public IReadOnlyList<Bar5s> GetHistory(string symbol)
        {
            if (_history.TryGetValue(symbol, out var list))
                return list;

            return Array.Empty<Bar5s>();
        }

        // ---------------------------------------------------------
        // 将时间对齐到 5 秒起点
        // ---------------------------------------------------------
        private static DateTime FloorTo5Seconds(DateTime t)
        {
            int bucketSec = (t.Second / 5) * 5;
            return new DateTime(t.Year, t.Month, t.Day, t.Hour, t.Minute, bucketSec, t.Kind);
        }
    }
}
