namespace TradingEngine.Utils
{
    /// <summary>
    /// 技术指标计算（纯函数）
    /// </summary>
    public static class Indicators
    {
        /// <summary>
        /// 计算 ATR（Average True Range）
        /// 不足 period 根 bar 时返回 TR 平均值
        /// </summary>
        public static double ATR(double[] highs, double[] lows, double[] closes, int period = 14)
        {
            int count = highs.Length;
            if (count < 2) return 0;

            // 计算 TR 序列
            var trValues = new List<double>();
            for (int i = 1; i < count; i++)
            {
                double tr = TrueRange(highs[i], lows[i], closes[i - 1]);
                trValues.Add(tr);
            }

            if (trValues.Count == 0) return 0;

            // 不足 period 时返回 TR 平均值
            if (trValues.Count < period)
            {
                return trValues.Average();
            }

            // 足够时用 Wilder 平滑（RMA）
            double atr = trValues.Take(period).Average();  // 初始值：前 period 个 TR 的 SMA
            for (int i = period; i < trValues.Count; i++)
            {
                atr = (atr * (period - 1) + trValues[i]) / period;
            }

            return atr;
        }

        /// <summary>
        /// 计算 True Range
        /// </summary>
        public static double TrueRange(double high, double low, double prevClose)
        {
            double hl = high - low;
            double hc = Math.Abs(high - prevClose);
            double lc = Math.Abs(low - prevClose);
            return Math.Max(hl, Math.Max(hc, lc));
        }

        /// <summary>
        /// 计算 EMA（Exponential Moving Average）
        /// 不足 period 根 bar 时返回 Close 平均值
        /// </summary>
        public static double EMA(double[] closes, int period = 20)
        {
            int count = closes.Length;
            if (count == 0) return 0;

            // 不足 period 时返回平均值
            if (count < period)
            {
                return closes.Average();
            }

            // 足够时计算 EMA
            double multiplier = 2.0 / (period + 1);
            double ema = closes.Take(period).Average();  // 初始值：前 period 个的 SMA

            for (int i = period; i < count; i++)
            {
                ema = (closes[i] - ema) * multiplier + ema;
            }

            return ema;
        }
    }
}