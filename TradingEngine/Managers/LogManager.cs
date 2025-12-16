using TradingEngine.Core;

namespace TradingEngine.Managers
{
    public enum LogLevel
    {
        Normal,
        Success,
        Error
    }

    /// <summary>
    /// 日志管理器 - 处理日志过滤和格式化
    /// </summary>
    public class LogManager
    {
        public event Action<string, LogLevel>? LogReceived;

        public void Initialize(TradingController controller)
        {
            // [SEND] 命令
            controller.CommandSent += (msg) => Log(msg);

            // 连接状态
            controller.Connected += () => Log("Connected to server", LogLevel.Success);
            controller.Disconnected += () => Log("Disconnected", LogLevel.Error);
            controller.LoginSuccess += () => Log("Login successfully", LogLevel.Success);
            controller.LoginFailed += (msg) => Log($"Login failed: {msg}", LogLevel.Error);

            // 订阅状态
            controller.SymbolSubscribed += (symbol) => Log($"Subscribe {symbol} successfully", LogLevel.Success);
            controller.SymbolUnsubscribed += (symbol) => Log($"Unsubscribed {symbol}");

            // %OrderAct 消息
            controller.RawMessage += OnRawMessage;
        }

        /// <summary>
        /// Hotkey 注册结果
        /// </summary>
        public void LogHotkeyResult(bool allSuccess, string? failedKey = null)
        {
            if (allSuccess)
            {
                Log("Hotkey register successfully", LogLevel.Success);
            }
            else
            {
                Log($"Hotkey register failed: {failedKey}", LogLevel.Error);
            }
        }

        private void OnRawMessage(string msg)
        {
            // 只处理 %OrderAct
            if (!msg.StartsWith("%OrderAct")) return;

            // 解析 action type
            // %OrderAct id ActionType B/S symbol qty price route time notes token
            var parts = msg.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3) return;

            string actionType = parts[2];

            LogLevel level = actionType switch
            {
                "Send_Rej" => LogLevel.Error,
                "Execute" => LogLevel.Success,
                "Rejected" => LogLevel.Error,
                "CancelRej" => LogLevel.Error,
                "ReplaceRej" => LogLevel.Error,
                _ => LogLevel.Normal
            };

            Log($"[OrderAct] {msg}", level);
        }

        private void Log(string message, LogLevel level = LogLevel.Normal)
        {
            LogReceived?.Invoke(message, level);
        }
    }
}