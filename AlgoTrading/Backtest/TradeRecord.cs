using System.Globalization;
using System.Text;
using AlgoTrading.Core.Models;

namespace AlgoTrading.Backtest
{
    /// <summary>
    /// 单笔交易记录
    /// </summary>
    public class TradeRecord
    {
        public string Symbol { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public SignalType Type { get; set; }
        public double Price { get; set; }
        public DateTime Time { get; set; }
        public int Shares { get; set; }
        public double? PnL { get; set; }
        public string Reason { get; set; } = string.Empty;

        public override string ToString()
        {
            var pnlStr = PnL.HasValue ? $"PnL: {PnL:F2}" : "";
            return $"[{Time:HH:mm:ss}] {Symbol} {Type} {Shares} @ {Price:F2} {pnlStr} - {Reason}";
        }
    }

    /// <summary>
    /// 交易记录管理器
    /// </summary>
    public class TradeRecordManager
    {
        private readonly List<TradeRecord> _records = new();
        private readonly string _outputPath;

        public TradeRecordManager(string outputPath)
        {
            _outputPath = outputPath;
        }

        /// <summary>
        /// 添加交易记录
        /// </summary>
        public void Add(TradeRecord record)
        {
            _records.Add(record);
        }

        /// <summary>
        /// 获取所有记录
        /// </summary>
        public IReadOnlyList<TradeRecord> Records => _records;

        /// <summary>
        /// 计算总 PnL
        /// </summary>
        public double TotalPnL => _records.Where(r => r.PnL.HasValue).Sum(r => r.PnL!.Value);

        /// <summary>
        /// 交易次数
        /// </summary>
        public int TradeCount => _records.Count;

        /// <summary>
        /// 买入次数
        /// </summary>
        public int BuyCount => _records.Count(r => r.Type == SignalType.Buy || r.Type == SignalType.AddPosition);

        /// <summary>
        /// 卖出次数
        /// </summary>
        public int SellCount => _records.Count(r => r.Type == SignalType.SellHalf || r.Type == SignalType.SellAll);

        /// <summary>
        /// 保存到 CSV
        /// </summary>
        public async Task SaveToCsvAsync(string? filename = null)
        {
            Directory.CreateDirectory(_outputPath);

            var fileName = filename ?? $"trades_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            var filePath = Path.Combine(_outputPath, fileName);

            var sb = new StringBuilder();

            // Header
            sb.AppendLine("symbol,date,type,price,time,shares,pnl,reason");

            // Data rows
            foreach (var record in _records)
            {
                sb.AppendLine(string.Join(",",
                    record.Symbol,
                    record.Date.ToString("yyyy-MM-dd"),
                    record.Type,
                    record.Price.ToString(CultureInfo.InvariantCulture),
                    record.Time.ToString("HH:mm:ss.fff"),
                    record.Shares,
                    record.PnL?.ToString(CultureInfo.InvariantCulture) ?? "",
                    $"\"{record.Reason}\""
                ));
            }

            await File.WriteAllTextAsync(filePath, sb.ToString());
        }

        /// <summary>
        /// 打印汇总
        /// </summary>
        public void PrintSummary(Action<string>? log = null)
        {
            var output = log ?? Console.WriteLine;

            output("\n========== Backtest Summary ==========");
            output($"Total Trades: {TradeCount}");
            output($"  - Buy/AddPosition: {BuyCount}");
            output($"  - Sell: {SellCount}");
            output($"Total PnL: ${TotalPnL:F2}");
            output("=======================================\n");
        }

        /// <summary>
        /// 清空记录
        /// </summary>
        public void Clear()
        {
            _records.Clear();
        }
    }
}