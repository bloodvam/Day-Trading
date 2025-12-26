using System.Globalization;
using AlgoTrading.Config;
using AlgoTrading.Core.Models;

namespace AlgoTrading.DataFeed
{
    /// <summary>
    /// 本地 Trade 数据读取器
    /// </summary>
    public class TradeDataReader
    {
        private readonly string _basePath;

        public TradeDataReader()
        {
            _basePath = AppConfig.Instance.DataBasePath;
        }

        public TradeDataReader(string basePath)
        {
            _basePath = basePath;
        }

        /// <summary>
        /// 读取指定股票、日期的 Trades 数据
        /// </summary>
        /// <param name="symbol">股票代码</param>
        /// <param name="date">日期</param>
        /// <param name="startTime">开始时间（可选，格式 HH:mm）</param>
        /// <param name="endTime">结束时间（可选，格式 HH:mm）</param>
        public async Task<List<Trade>> ReadAsync(
            string symbol,
            DateTime date,
            string? startTime = null,
            string? endTime = null)
        {
            var directory = GetDirectory(date, symbol);
            var csvPath = Path.Combine(directory, "trades.csv");

            if (!File.Exists(csvPath))
            {
                throw new FileNotFoundException($"Trade data not found: {csvPath}");
            }

            var trades = await ReadCsvAsync(csvPath);

            // 如果指定了时间范围，进行过滤
            if (!string.IsNullOrEmpty(startTime) || !string.IsNullOrEmpty(endTime))
            {
                var start = string.IsNullOrEmpty(startTime)
                    ? TimeSpan.Zero
                    : TimeSpan.Parse(startTime);
                var end = string.IsNullOrEmpty(endTime)
                    ? new TimeSpan(23, 59, 59)
                    : TimeSpan.Parse(endTime);

                trades = trades
                    .Where(t =>
                    {
                        var time = t.ParticipantTimestampEt.TimeOfDay;
                        return time >= start && time <= end;
                    })
                    .ToList();
            }

            return trades;
        }

        /// <summary>
        /// 获取数据目录路径
        /// </summary>
        public string GetDirectory(DateTime date, string symbol)
        {
            return Path.Combine(
                _basePath,
                date.Year.ToString(),
                date.Month.ToString("D2"),
                date.Day.ToString("D2"),
                symbol.ToUpper()
            );
        }

        /// <summary>
        /// 检查数据是否存在
        /// </summary>
        public bool DataExists(string symbol, DateTime date)
        {
            var directory = GetDirectory(date, symbol);
            var csvPath = Path.Combine(directory, "trades.csv");
            return File.Exists(csvPath);
        }

        /// <summary>
        /// 读取 CSV 文件
        /// </summary>
        private async Task<List<Trade>> ReadCsvAsync(string path)
        {
            var trades = new List<Trade>();
            var lines = await File.ReadAllLinesAsync(path);

            // 跳过 header
            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;

                var trade = ParseCsvLine(line);
                if (trade != null)
                {
                    trades.Add(trade);
                }
            }

            return trades;
        }

        /// <summary>
        /// 解析 CSV 行
        /// </summary>
        private Trade? ParseCsvLine(string line)
        {
            try
            {
                // 处理带引号的字段（conditions 字段可能包含逗号）
                var parts = ParseCsvFields(line);
                if (parts.Length < 10) return null;

                return new Trade
                {
                    ParticipantTimestamp = long.Parse(parts[0]),
                    ParticipantTimestampEt = DateTime.Parse(parts[1], CultureInfo.InvariantCulture),
                    SipTimestamp = long.Parse(parts[2]),
                    SipTimestampEt = DateTime.Parse(parts[3], CultureInfo.InvariantCulture),
                    Price = double.Parse(parts[4], CultureInfo.InvariantCulture),
                    Size = int.Parse(parts[5]),
                    Conditions = ParseConditions(parts[6]),
                    Correction = int.Parse(parts[7]),
                    Exchange = int.Parse(parts[8]),
                    TrfId = int.Parse(parts[9])
                };
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 解析 CSV 字段（处理引号）
        /// </summary>
        private string[] ParseCsvFields(string line)
        {
            var fields = new List<string>();
            var current = "";
            var inQuotes = false;

            foreach (var c in line)
            {
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    fields.Add(current);
                    current = "";
                }
                else
                {
                    current += c;
                }
            }
            fields.Add(current);

            return fields.ToArray();
        }

        /// <summary>
        /// 解析 conditions 字段
        /// </summary>
        private int[] ParseConditions(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return Array.Empty<int>();

            return value
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => int.TryParse(s.Trim(), out var n) ? n : 0)
                .Where(n => n > 0)
                .ToArray();
        }
    }
}