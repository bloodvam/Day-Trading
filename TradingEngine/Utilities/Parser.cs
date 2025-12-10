using System;
using System.Globalization;
using TradingEngine.Models;

namespace PreMarketTrader.Core
{
    public static class DasTsParser
    {
        /// <summary>
        /// 解析一行 $T&S 文本为 Tick 对象
        /// 示例: 
        /// $T&S AAPL 284.25 100 T 09:30:01 Q B 96
        /// </summary>
        public static Tick? ParseTimeSales(string line)
        {
            if (!line.StartsWith("$T&S "))
                return null;

            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 9)
                return null;

            try
            {
                string symbol = parts[1];
                double price = double.Parse(parts[2], CultureInfo.InvariantCulture);
                int volume = int.Parse(parts[3], CultureInfo.InvariantCulture);
                // parts[4] 是 flag, 目前忽略
                string timeStr = parts[5];          // HH:MM:SS
                // parts[6] exchange, 忽略
                char side = parts[7][0];           // B/S/I
                int condition = int.Parse(parts[8], CultureInfo.InvariantCulture);

                // 组合今天的日期 + 成交时间
                var today = DateTime.Today;
                var tParts = timeStr.Split(':');
                int hh = int.Parse(tParts[0]);
                int mm = int.Parse(tParts[1]);
                int ss = int.Parse(tParts[2]);

                var tsTime = new DateTime(today.Year, today.Month, today.Day, hh, mm, ss,
                    DateTimeKind.Local);

                return new Tick
                {
                    Symbol = symbol,
                    Price = price,
                    Volume = volume,
                    Side = side,
                    Time = tsTime,
                    Condition = condition
                };
            }
            catch
            {
                // 解析失败就丢掉
                return null;
            }
        }
    }
}
