using System.Globalization;
using System.Text;
using Parquet;
using Parquet.Data;
using Parquet.Schema;
using AlgoTrading.Config;
using AlgoTrading.Core.Models;

namespace AlgoTrading.DataFeed
{
    /// <summary>
    /// Trade 数据存储（CSV + Parquet）
    /// </summary>
    public class TradeDataStorage
    {
        private readonly string _basePath;

        public event Action<string>? Log;

        public TradeDataStorage()
        {
            _basePath = AppConfig.Instance.DataBasePath;
        }

        public TradeDataStorage(string basePath)
        {
            _basePath = basePath;
        }

        /// <summary>
        /// 保存 Trades 到指定日期和股票的目录
        /// </summary>
        public async Task SaveAsync(string symbol, DateTime date, List<Trade> trades)
        {
            var directory = GetDirectory(date, symbol);
            Directory.CreateDirectory(directory);

            // 保存 CSV
            var csvPath = Path.Combine(directory, "trades.csv");
            await SaveCsvAsync(csvPath, trades);
            Log?.Invoke($"Saved CSV: {csvPath} ({trades.Count} records)");

            // 保存 Parquet
            var parquetPath = Path.Combine(directory, "trades.parquet");
            await SaveParquetAsync(parquetPath, trades);
            Log?.Invoke($"Saved Parquet: {parquetPath}");
        }

        /// <summary>
        /// 获取存储目录路径
        /// </summary>
        public string GetDirectory(DateTime date, string symbol)
        {
            // 格式: D:\day trading\TradingData\2025\12\15\RADX\
            return Path.Combine(
                _basePath,
                date.Year.ToString(),
                date.Month.ToString("D2"),
                date.Day.ToString("D2"),
                symbol.ToUpper()
            );
        }

        /// <summary>
        /// 检查数据是否已存在
        /// </summary>
        public bool DataExists(DateTime date, string symbol)
        {
            var directory = GetDirectory(date, symbol);
            var csvPath = Path.Combine(directory, "trades.csv");
            return File.Exists(csvPath);
        }

        #region CSV

        private async Task SaveCsvAsync(string path, List<Trade> trades)
        {
            var sb = new StringBuilder();

            // Header
            sb.AppendLine("participant_timestamp,participant_timestamp_et,sip_timestamp,sip_timestamp_et,price,size,conditions,correction,exchange,trf_id");

            // Data rows
            foreach (var trade in trades)
            {
                sb.AppendLine(string.Join(",",
                    trade.ParticipantTimestamp,
                    trade.ParticipantTimestampEt.ToString("yyyy-MM-dd HH:mm:ss.ffffff", CultureInfo.InvariantCulture),
                    trade.SipTimestamp,
                    trade.SipTimestampEt.ToString("yyyy-MM-dd HH:mm:ss.ffffff", CultureInfo.InvariantCulture),
                    trade.Price.ToString(CultureInfo.InvariantCulture),
                    trade.Size,
                    $"\"{trade.ConditionsString}\"",  // 用引号包裹，因为里面有逗号
                    trade.Correction,
                    trade.Exchange,
                    trade.TrfId
                ));
            }

            await File.WriteAllTextAsync(path, sb.ToString());
        }

        #endregion

        #region Parquet

        private async Task SaveParquetAsync(string path, List<Trade> trades)
        {
            if (trades.Count == 0)
            {
                // 创建空文件
                await File.WriteAllBytesAsync(path, Array.Empty<byte>());
                return;
            }

            // 定义 Schema
            var schema = new ParquetSchema(
                new DataField<long>("participant_timestamp"),
                new DataField<string>("participant_timestamp_et"),
                new DataField<long>("sip_timestamp"),
                new DataField<string>("sip_timestamp_et"),
                new DataField<double>("price"),
                new DataField<int>("size"),
                new DataField<string>("conditions"),
                new DataField<int>("correction"),
                new DataField<int>("exchange"),
                new DataField<int>("trf_id")
            );

            // 准备数据列
            var participantTimestamps = trades.Select(t => t.ParticipantTimestamp).ToArray();
            var participantTimestampsEt = trades.Select(t => t.ParticipantTimestampEt.ToString("yyyy-MM-dd HH:mm:ss.ffffff", CultureInfo.InvariantCulture)).ToArray();
            var sipTimestamps = trades.Select(t => t.SipTimestamp).ToArray();
            var sipTimestampsEt = trades.Select(t => t.SipTimestampEt.ToString("yyyy-MM-dd HH:mm:ss.ffffff", CultureInfo.InvariantCulture)).ToArray();
            var prices = trades.Select(t => t.Price).ToArray();
            var sizes = trades.Select(t => t.Size).ToArray();
            var conditions = trades.Select(t => t.ConditionsString).ToArray();
            var corrections = trades.Select(t => t.Correction).ToArray();
            var exchanges = trades.Select(t => t.Exchange).ToArray();
            var trfIds = trades.Select(t => t.TrfId).ToArray();

            // 写入文件
            using var stream = File.Create(path);
            using var writer = await ParquetWriter.CreateAsync(schema, stream);

            using var groupWriter = writer.CreateRowGroup();

            await groupWriter.WriteColumnAsync(new DataColumn(schema.DataFields[0], participantTimestamps));
            await groupWriter.WriteColumnAsync(new DataColumn(schema.DataFields[1], participantTimestampsEt));
            await groupWriter.WriteColumnAsync(new DataColumn(schema.DataFields[2], sipTimestamps));
            await groupWriter.WriteColumnAsync(new DataColumn(schema.DataFields[3], sipTimestampsEt));
            await groupWriter.WriteColumnAsync(new DataColumn(schema.DataFields[4], prices));
            await groupWriter.WriteColumnAsync(new DataColumn(schema.DataFields[5], sizes));
            await groupWriter.WriteColumnAsync(new DataColumn(schema.DataFields[6], conditions));
            await groupWriter.WriteColumnAsync(new DataColumn(schema.DataFields[7], corrections));
            await groupWriter.WriteColumnAsync(new DataColumn(schema.DataFields[8], exchanges));
            await groupWriter.WriteColumnAsync(new DataColumn(schema.DataFields[9], trfIds));
        }

        #endregion
    }
}