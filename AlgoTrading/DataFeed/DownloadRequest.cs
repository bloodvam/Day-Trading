namespace AlgoTrading.DataFeed
{
    /// <summary>
    /// 数据下载请求
    /// </summary>
    public class DownloadRequest
    {
        /// <summary>
        /// 股票代码
        /// </summary>
        public string Symbol { get; set; }

        /// <summary>
        /// 日期
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// 开始时间（美东时间，格式 "HH:mm"），默认 04:00
        /// </summary>
        public string StartTime { get; set; }

        /// <summary>
        /// 结束时间（美东时间，格式 "HH:mm"），默认 20:00
        /// </summary>
        public string EndTime { get; set; }

        /// <summary>
        /// 创建全天数据下载请求
        /// </summary>
        public DownloadRequest(string symbol, DateTime date)
        {
            Symbol = symbol.ToUpper();
            Date = date.Date;
            StartTime = "04:00";
            EndTime = "20:00";
        }

        /// <summary>
        /// 创建指定时间段的下载请求
        /// </summary>
        public DownloadRequest(string symbol, DateTime date, string startTime, string endTime)
        {
            Symbol = symbol.ToUpper();
            Date = date.Date;
            StartTime = startTime;
            EndTime = endTime;
        }

        /// <summary>
        /// 获取开始时间的 UTC DateTime
        /// </summary>
        public DateTime GetStartDateTimeUtc()
        {
            var et = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            var startLocal = Date.Add(TimeSpan.Parse(StartTime));
            return TimeZoneInfo.ConvertTimeToUtc(startLocal, et);
        }

        /// <summary>
        /// 获取结束时间的 UTC DateTime
        /// </summary>
        public DateTime GetEndDateTimeUtc()
        {
            var et = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            var endLocal = Date.Add(TimeSpan.Parse(EndTime));
            return TimeZoneInfo.ConvertTimeToUtc(endLocal, et);
        }

        public override string ToString()
        {
            return $"{Symbol} {Date:yyyy-MM-dd} {StartTime}-{EndTime}";
        }
    }
}