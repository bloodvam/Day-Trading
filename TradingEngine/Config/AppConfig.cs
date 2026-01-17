using System.Text.Json;

namespace TradingEngine.Config
{
    public class DasApiConfig
    {
        public string Host { get; set; } = "127.0.0.1";
        public int Port { get; set; } = 9090;
        public string User { get; set; } = "";
        public string Password { get; set; } = "";
        public string Account { get; set; } = "";
    }

    public class TradingConfig
    {
        public double RiskAmount { get; set; } = 10.0;
        public double SpreadPercent { get; set; } = 0.03;
        public double Leverage { get; set; } = 6.0;
        public double SingleStockBPMultiplier { get; set; } = 3.0;  // 单股票 BP = Equity × 此值
        public int MaxSharesPerSymbol { get; set; } = 10000;        // 单股票最大持股数
        public double MinRiskPerShare { get; set; } = 0.05;          // 最小每股风险（Ask - Stop）
        public double MinTrailAll { get; set; } = 0.2;               // TrailAll 最小值
        public int MinStopVolume { get; set; } = 50;                 // 触发止损前最小累计成交量
        public string BuyRoute { get; set; } = "VLCTL";
        public string SellRoute { get; set; } = "VLCTL";
        public string StopRoute { get; set; } = "STOP";
        public int DefaultBarInterval { get; set; } = 5;
    }

    public class AppConfig
    {
        public DasApiConfig DasApi { get; set; } = new();
        public TradingConfig Trading { get; set; } = new();

        private static AppConfig? _instance;
        private static readonly object _lock = new();

        public static AppConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= Load();
                    }
                }
                return _instance;
            }
        }

        private static AppConfig Load()
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    return JsonSerializer.Deserialize<AppConfig>(json, options) ?? new AppConfig();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load config: {ex.Message}");
            }
            return new AppConfig();
        }

        public void Save()
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(this, options);
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to save config: {ex.Message}");
            }
        }
    }
}