using System.Text.Json;
using AlgoTrading.Config;
using AlgoTrading.Core.Models;

namespace AlgoTrading.DataFeed
{
    /// <summary>
    /// Polygon.io API 客户端
    /// </summary>
    public class PolygonClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private const string BaseUrl = "https://api.polygon.io";
        private static readonly TimeZoneInfo EasternZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");

        public event Action<string>? Log;

        public PolygonClient()
        {
            _apiKey = AppConfig.Instance.PolygonApiKey;
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(5)
            };
        }

        public PolygonClient(string apiKey)
        {
            _apiKey = apiKey;
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(5)
            };
        }

        /// <summary>
        /// 获取指定股票、指定时间范围的所有 Trades
        /// </summary>
        public async Task<List<Trade>> GetTradesAsync(
            string symbol,
            DateTime startUtc,
            DateTime endUtc,
            CancellationToken cancellationToken = default)
        {
            var allTrades = new List<Trade>();
            var startNs = ToNanoseconds(startUtc);
            var endNs = ToNanoseconds(endUtc);

            var url = $"{BaseUrl}/v3/trades/{symbol.ToUpper()}?" +
                      $"timestamp.gte={startNs}&timestamp.lt={endNs}" +
                      $"&order=asc&limit=50000&apiKey={_apiKey}";

            int pageCount = 0;

            while (!string.IsNullOrEmpty(url))
            {
                cancellationToken.ThrowIfCancellationRequested();

                pageCount++;
                Log?.Invoke($"[{symbol}] Fetching page {pageCount}...");

                var response = await _httpClient.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<PolygonTradesResponse>(json, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                });

                if (result?.Results != null)
                {
                    foreach (var raw in result.Results)
                    {
                        var trade = ConvertToTrade(raw);
                        allTrades.Add(trade);
                    }
                    Log?.Invoke($"[{symbol}] Page {pageCount}: {result.Results.Count} trades (total: {allTrades.Count})");
                }

                // 检查是否有下一页
                url = result?.NextUrl;
                if (!string.IsNullOrEmpty(url) && !url.Contains("apiKey="))
                {
                    url += $"&apiKey={_apiKey}";
                }

                // 避免 rate limit
                if (!string.IsNullOrEmpty(url))
                {
                    await Task.Delay(100, cancellationToken);
                }
            }

            Log?.Invoke($"[{symbol}] Download complete: {allTrades.Count} trades total");
            return allTrades;
        }

        /// <summary>
        /// 将 Polygon API 返回的原始数据转换为 Trade 对象
        /// </summary>
        private Trade ConvertToTrade(PolygonTradeRaw raw)
        {
            return new Trade
            {
                ParticipantTimestamp = raw.ParticipantTimestamp,
                ParticipantTimestampEt = NanosecondsToEasternTime(raw.ParticipantTimestamp),
                SipTimestamp = raw.SipTimestamp,
                SipTimestampEt = NanosecondsToEasternTime(raw.SipTimestamp),
                Price = raw.Price,
                Size = raw.Size,
                Conditions = raw.Conditions ?? Array.Empty<int>(),
                Correction = raw.Correction,
                Exchange = raw.Exchange,
                TrfId = raw.TrfId
            };
        }

        /// <summary>
        /// 将 DateTime 转换为纳秒时间戳
        /// </summary>
        private static long ToNanoseconds(DateTime utcTime)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return (utcTime - epoch).Ticks * 100; // 1 tick = 100 nanoseconds
        }

        /// <summary>
        /// 将纳秒时间戳转换为美东时间
        /// </summary>
        private static DateTime NanosecondsToEasternTime(long nanoseconds)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var ticks = nanoseconds / 100; // 100 nanoseconds = 1 tick
            var utcTime = epoch.AddTicks(ticks);
            return TimeZoneInfo.ConvertTimeFromUtc(utcTime, EasternZone);
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }

        #region Polygon API Response Models

        private class PolygonTradesResponse
        {
            public List<PolygonTradeRaw>? Results { get; set; }
            public string? NextUrl { get; set; }
            public string? Status { get; set; }
            public string? RequestId { get; set; }
        }

        private class PolygonTradeRaw
        {
            public long ParticipantTimestamp { get; set; }
            public long SipTimestamp { get; set; }
            public double Price { get; set; }
            public int Size { get; set; }
            public int[]? Conditions { get; set; }
            public int Correction { get; set; }
            public int Exchange { get; set; }
            public int TrfId { get; set; }
            public string? Id { get; set; }
            public long SequenceNumber { get; set; }
            public int Tape { get; set; }
            public long TrfTimestamp { get; set; }
        }

        #endregion
    }
}