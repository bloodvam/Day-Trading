using AlgoTrading.Config;

namespace AlgoTrading.Backtest
{
    /// <summary>
    /// 数据验证结果
    /// </summary>
    public class DataValidationResult
    {
        public bool IsValid => MissingData.Count == 0;
        public List<BacktestTask> MissingData { get; set; } = new();
        public List<BacktestTask> AvailableData { get; set; } = new();
    }

    /// <summary>
    /// 数据验证器 - 检查回测所需数据是否存在
    /// </summary>
    public class DataValidator
    {
        private readonly string _dataBasePath;

        public DataValidator()
        {
            _dataBasePath = AppConfig.Instance.DataBasePath;
        }

        public DataValidator(string dataBasePath)
        {
            _dataBasePath = dataBasePath;
        }

        /// <summary>
        /// 验证所有回测任务的数据是否存在
        /// </summary>
        public DataValidationResult Validate(IEnumerable<BacktestTask> tasks)
        {
            var result = new DataValidationResult();

            foreach (var task in tasks)
            {
                var dataPath = GetDataPath(task.Symbol, task.Date);
                var csvPath = Path.Combine(dataPath, "trades.csv");

                if (File.Exists(csvPath))
                {
                    result.AvailableData.Add(task);
                }
                else
                {
                    result.MissingData.Add(task);
                }
            }

            return result;
        }

        /// <summary>
        /// 获取数据存储路径
        /// </summary>
        public string GetDataPath(string symbol, DateTime date)
        {
            return Path.Combine(
                _dataBasePath,
                date.Year.ToString(),
                date.Month.ToString("D2"),
                date.Day.ToString("D2"),
                symbol.ToUpper()
            );
        }

        /// <summary>
        /// 打印验证结果
        /// </summary>
        public static void PrintValidationResult(DataValidationResult result, Action<string>? log = null)
        {
            var output = log ?? Console.WriteLine;

            output("\n========== Data Validation ==========");
            output($"Available: {result.AvailableData.Count} task(s)");
            output($"Missing: {result.MissingData.Count} task(s)");

            if (result.MissingData.Count > 0)
            {
                output("\nMissing data for:");
                foreach (var task in result.MissingData)
                {
                    output($"  - {task}");
                }
            }

            output("======================================\n");
        }
    }
}