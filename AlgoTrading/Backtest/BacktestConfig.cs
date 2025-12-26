using System.Text.Json;
using System.Text.Json.Serialization;

namespace AlgoTrading.Backtest
{
    /// <summary>
    /// 回测模式
    /// </summary>
    public enum BacktestMode
    {
        DateRange,  // 日期跨度模式
        Specific    // 精确指定模式
    }

    /// <summary>
    /// 回测配置
    /// </summary>
    public class BacktestConfig
    {
        /// <summary>
        /// 回测模式
        /// </summary>
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public BacktestMode Mode { get; set; } = BacktestMode.DateRange;

        /// <summary>
        /// 结果输出路径
        /// </summary>
        public string ResultOutputPath { get; set; } = string.Empty;

        /// <summary>
        /// 日期跨度模式配置
        /// </summary>
        public DateRangeModeConfig? DateRangeMode { get; set; }

        /// <summary>
        /// 精确指定模式配置
        /// </summary>
        public List<SpecificModeItem>? SpecificMode { get; set; }

        /// <summary>
        /// 从文件加载配置
        /// </summary>
        public static BacktestConfig Load(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Backtest config file not found: {path}");
            }

            var json = File.ReadAllText(path);
            var config = JsonSerializer.Deserialize<BacktestConfig>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return config ?? throw new InvalidOperationException("Failed to deserialize backtest config");
        }

        /// <summary>
        /// 从默认路径加载配置
        /// </summary>
        public static BacktestConfig LoadDefault()
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "backtest_config.json");
            return Load(path);
        }

        /// <summary>
        /// 获取所有回测任务
        /// </summary>
        public List<BacktestTask> GetTasks()
        {
            var tasks = new List<BacktestTask>();

            if (Mode == BacktestMode.DateRange && DateRangeMode != null && DateRangeMode.Enabled)
            {
                var startDate = DateTime.Parse(DateRangeMode.StartDate);
                var endDate = DateTime.Parse(DateRangeMode.EndDate);

                for (var date = startDate; date <= endDate; date = date.AddDays(1))
                {
                    // 跳过周末
                    if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
                        continue;

                    foreach (var symbol in DateRangeMode.Symbols)
                    {
                        tasks.Add(new BacktestTask
                        {
                            Symbol = symbol,
                            Date = date,
                            StartTime = DateRangeMode.StartTime,
                            EndTime = DateRangeMode.EndTime
                        });
                    }
                }
            }
            else if (Mode == BacktestMode.Specific && SpecificMode != null)
            {
                foreach (var item in SpecificMode.Where(x => x.Enabled))
                {
                    tasks.Add(new BacktestTask
                    {
                        Symbol = item.Symbol,
                        Date = DateTime.Parse(item.Date),
                        StartTime = item.StartTime,
                        EndTime = item.EndTime
                    });
                }
            }

            return tasks;
        }
    }

    /// <summary>
    /// 日期跨度模式配置
    /// </summary>
    public class DateRangeModeConfig
    {
        public bool Enabled { get; set; } = true;
        public string StartDate { get; set; } = string.Empty;
        public string EndDate { get; set; } = string.Empty;
        public List<string> Symbols { get; set; } = new();
        public string StartTime { get; set; } = "04:00";
        public string EndTime { get; set; } = "09:30";
    }

    /// <summary>
    /// 精确指定模式的单个配置项
    /// </summary>
    public class SpecificModeItem
    {
        public bool Enabled { get; set; } = true;
        public string Symbol { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public string StartTime { get; set; } = string.Empty;
        public string EndTime { get; set; } = string.Empty;
    }

    /// <summary>
    /// 单个回测任务
    /// </summary>
    public class BacktestTask
    {
        public string Symbol { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string StartTime { get; set; } = string.Empty;
        public string EndTime { get; set; } = string.Empty;

        /// <summary>
        /// 获取开始时间
        /// </summary>
        public DateTime GetStartDateTime()
        {
            return Date.Date.Add(TimeSpan.Parse(StartTime));
        }

        /// <summary>
        /// 获取结束时间
        /// </summary>
        public DateTime GetEndDateTime()
        {
            return Date.Date.Add(TimeSpan.Parse(EndTime));
        }

        public override string ToString()
        {
            return $"{Symbol} {Date:yyyy-MM-dd} {StartTime}-{EndTime}";
        }
    }
}