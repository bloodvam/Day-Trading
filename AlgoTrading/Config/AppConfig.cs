using System.Text.Json;

namespace AlgoTrading.Config
{
    public class AppConfig
    {
        public string PolygonApiKey { get; set; } = string.Empty;
        public string DataBasePath { get; set; } = string.Empty;

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
            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");

            if (!File.Exists(configPath))
            {
                throw new FileNotFoundException($"Configuration file not found: {configPath}");
            }

            var json = File.ReadAllText(configPath);
            var config = JsonSerializer.Deserialize<AppConfig>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return config ?? throw new InvalidOperationException("Failed to deserialize config.json");
        }

        /// <summary>
        /// 重新加载配置（用于测试或热更新）
        /// </summary>
        public static void Reload()
        {
            lock (_lock)
            {
                _instance = Load();
            }
        }
    }
}