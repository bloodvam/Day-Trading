using AlgoTrading.Core.Models;

namespace AlgoTrading.DataFeed
{
    /// <summary>
    /// Trade 数据下载器（协调 PolygonClient 和 TradeDataStorage）
    /// </summary>
    public class TradeDownloader : IDisposable
    {
        private readonly PolygonClient _client;
        private readonly TradeDataStorage _storage;

        public event Action<string>? Log;

        public TradeDownloader()
        {
            _client = new PolygonClient();
            _storage = new TradeDataStorage();

            _client.Log += msg => Log?.Invoke(msg);
            _storage.Log += msg => Log?.Invoke(msg);
        }

        public TradeDownloader(PolygonClient client, TradeDataStorage storage)
        {
            _client = client;
            _storage = storage;

            _client.Log += msg => Log?.Invoke(msg);
            _storage.Log += msg => Log?.Invoke(msg);
        }

        /// <summary>
        /// 下载单个请求
        /// </summary>
        public async Task<DownloadResult> DownloadAsync(
            DownloadRequest request,
            bool skipIfExists = true,
            CancellationToken cancellationToken = default)
        {
            var result = new DownloadResult
            {
                Symbol = request.Symbol,
                Date = request.Date
            };

            try
            {
                // 检查是否已存在
                if (skipIfExists && _storage.DataExists(request.Date, request.Symbol))
                {
                    Log?.Invoke($"[{request.Symbol}] Data already exists, skipping...");
                    result.Skipped = true;
                    result.Success = true;
                    return result;
                }

                Log?.Invoke($"[{request.Symbol}] Starting download: {request}");

                // 下载数据
                var startUtc = request.GetStartDateTimeUtc();
                var endUtc = request.GetEndDateTimeUtc();

                var trades = await _client.GetTradesAsync(
                    request.Symbol,
                    startUtc,
                    endUtc,
                    cancellationToken);

                result.TradeCount = trades.Count;

                if (trades.Count == 0)
                {
                    Log?.Invoke($"[{request.Symbol}] Warning: No trades found for {request.Date:yyyy-MM-dd}");
                }

                // 保存数据
                await _storage.SaveAsync(request.Symbol, request.Date, trades);

                result.Success = true;
                result.OutputDirectory = _storage.GetDirectory(request.Date, request.Symbol);

                Log?.Invoke($"[{request.Symbol}] Download complete: {trades.Count} trades saved to {result.OutputDirectory}");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error = ex.Message;
                Log?.Invoke($"[{request.Symbol}] Error: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// 批量下载多个请求
        /// </summary>
        public async Task<List<DownloadResult>> DownloadAsync(
            IEnumerable<DownloadRequest> requests,
            bool skipIfExists = true,
            CancellationToken cancellationToken = default)
        {
            var results = new List<DownloadResult>();

            foreach (var request in requests)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var result = await DownloadAsync(request, skipIfExists, cancellationToken);
                results.Add(result);

                // 避免 rate limit
                await Task.Delay(200, cancellationToken);
            }

            // 打印汇总
            var successful = results.Count(r => r.Success && !r.Skipped);
            var skipped = results.Count(r => r.Skipped);
            var failed = results.Count(r => !r.Success);
            var totalTrades = results.Sum(r => r.TradeCount);

            Log?.Invoke($"\n=== Download Summary ===");
            Log?.Invoke($"Total requests: {results.Count}");
            Log?.Invoke($"Successful: {successful}");
            Log?.Invoke($"Skipped (exists): {skipped}");
            Log?.Invoke($"Failed: {failed}");
            Log?.Invoke($"Total trades: {totalTrades:N0}");

            return results;
        }

        /// <summary>
        /// 快捷方法：下载单只股票某天全天数据
        /// </summary>
        public Task<DownloadResult> DownloadAsync(
            string symbol,
            DateTime date,
            CancellationToken cancellationToken = default)
        {
            return DownloadAsync(new DownloadRequest(symbol, date), cancellationToken: cancellationToken);
        }

        /// <summary>
        /// 快捷方法：下载单只股票某天指定时间段数据
        /// </summary>
        public Task<DownloadResult> DownloadAsync(
            string symbol,
            DateTime date,
            string startTime,
            string endTime,
            CancellationToken cancellationToken = default)
        {
            return DownloadAsync(new DownloadRequest(symbol, date, startTime, endTime), cancellationToken: cancellationToken);
        }

        public void Dispose()
        {
            _client.Dispose();
        }
    }

    /// <summary>
    /// 下载结果
    /// </summary>
    public class DownloadResult
    {
        public string Symbol { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public bool Success { get; set; }
        public bool Skipped { get; set; }
        public int TradeCount { get; set; }
        public string? OutputDirectory { get; set; }
        public string? Error { get; set; }
    }
}