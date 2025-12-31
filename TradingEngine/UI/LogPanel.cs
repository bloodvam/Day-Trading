using TradingEngine.Core;
using TradingEngine.Managers;

namespace TradingEngine.UI
{
    /// <summary>
    /// 日志面板 - 每个 Symbol 独立的日志
    /// </summary>
    public class LogPanel : BasePanel
    {
        private RichTextBox _logBox;
        private Label _lblTitle;

        // Global 日志（连接状态、热键等）
        private readonly List<LogEntry> _globalLogs = new();

        public LogPanel(TradingController controller) : base(controller)
        {
            this.Dock = DockStyle.Fill;
            BuildUI();
            BindEvents();
        }

        private void BuildUI()
        {
            var topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 25
            };

            _lblTitle = new Label
            {
                Text = "Log:",
                Location = new Point(0, 5),
                Size = new Size(200, 20)
            };

            topPanel.Controls.Add(_lblTitle);

            _logBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 9),
                ReadOnly = true,
                BackColor = Color.White
            };

            this.Controls.Add(_logBox);
            this.Controls.Add(topPanel);
        }

        private void BindEvents()
        {
            // Global 日志：连接状态
            Controller.LoginSuccess += () => InvokeUI(() => AppendGlobalLog("connect successfully", Color.DarkGreen));
            Controller.LoginFailed += (msg) => InvokeUI(() => AppendGlobalLog("connection error", Color.Red));
            Controller.Disconnected += () => InvokeUI(() => AppendGlobalLog("disconnected", Color.Red));

            // Global 日志：订阅状态
            Controller.SymbolSubscribed += (symbol) => InvokeUI(() => AppendGlobalLog($"subscribe {symbol} successfully", Color.DarkGreen));
            Controller.SymbolUnsubscribed += (symbol) => InvokeUI(() => AppendGlobalLog($"unsubscribe {symbol}", Color.Gray));

            // Symbol 切换
            Controller.ActiveSymbolChanged += (symbol) => InvokeUI(() => OnActiveSymbolChanged(symbol));

            // OrderAct (来自 DAS 原始消息)
            Controller.RawMessage += (msg) => InvokeUI(() => OnRawMessage(msg));

            // OrderManager 的 Log 消息
            Controller.Log += (msg) => InvokeUI(() => OnLogMessage(msg));

            // 新操作开始（清空当前 symbol 的日志）
            Controller.NewOperationStarted += () => InvokeUI(() => OnNewOperationStarted());
        }

        public void LogHotkeyResult(bool allSuccess, string? failedKey = null)
        {
            if (allSuccess)
            {
                AppendGlobalLog("hotkey register successfully", Color.DarkGreen);
            }
            else
            {
                AppendGlobalLog($"hotkey register failed: {failedKey}", Color.Red);
            }
        }

        private void OnActiveSymbolChanged(string? symbol)
        {
            _lblTitle.Text = string.IsNullOrEmpty(symbol) ? "Log:" : $"Log: [{symbol}]";
            RefreshLogDisplay();
        }

        private void OnNewOperationStarted()
        {
            var symbol = Controller.ActiveSymbol;
            if (!string.IsNullOrEmpty(symbol))
            {
                var state = Controller.GetSymbolState(symbol);
                state?.ClearLogs();
                RefreshLogDisplay();
            }
        }

        private void OnLogMessage(string msg)
        {
            Color color = Color.Black;
            if (msg.Contains("error") || msg.Contains("Invalid") || msg.Contains("Cannot") ||
                msg.Contains("No ") || msg.Contains("too high"))
            {
                color = Color.Red;
            }
            AppendSymbolLog(msg, color);
        }

        private void OnRawMessage(string msg)
        {
            if (!msg.StartsWith("%OrderAct")) return;

            var parts = msg.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3) return;

            string actionType = parts[2];
            Color color = GetOrderActColor(actionType);
            AppendSymbolLog(msg, color);
        }

        private Color GetOrderActColor(string actionType)
        {
            return actionType switch
            {
                "Send_Rej" => Color.Red,
                "Execute" => Color.Blue,
                "Canceled" => Color.Orange,
                _ => Color.Black
            };
        }

        private void AppendGlobalLog(string message, Color color)
        {
            string timestamped = $"[{DateTime.Now:HH:mm:ss}] {message}";
            _globalLogs.Add(new LogEntry
            {
                Message = timestamped,
                ColorArgb = color.ToArgb(),
                Time = DateTime.Now
            });

            if (string.IsNullOrEmpty(Controller.ActiveSymbol))
            {
                AppendToLogBox(timestamped, color);
            }
        }

        private void AppendSymbolLog(string message, Color color)
        {
            var symbol = Controller.ActiveSymbol;
            if (string.IsNullOrEmpty(symbol)) return;

            var state = Controller.GetSymbolState(symbol);
            if (state == null) return;

            string timestamped = $"[{DateTime.Now:HH:mm:ss}] {message}";
            state.AddLog(timestamped, color.ToArgb());

            AppendToLogBox(timestamped, color);
        }

        private void RefreshLogDisplay()
        {
            _logBox.Clear();

            var symbol = Controller.ActiveSymbol;
            if (string.IsNullOrEmpty(symbol))
            {
                foreach (var entry in _globalLogs)
                {
                    AppendToLogBox(entry.Message, Color.FromArgb(entry.ColorArgb));
                }
            }
            else
            {
                var state = Controller.GetSymbolState(symbol);
                if (state != null)
                {
                    foreach (var entry in state.Logs)
                    {
                        AppendToLogBox(entry.Message, Color.FromArgb(entry.ColorArgb));
                    }
                }
            }
        }

        private void AppendToLogBox(string message, Color color)
        {
            _logBox.SelectionStart = _logBox.TextLength;
            _logBox.SelectionLength = 0;
            _logBox.SelectionColor = color;
            _logBox.AppendText(message + "\n");
            _logBox.SelectionColor = Color.Black;
            _logBox.SelectionStart = _logBox.TextLength;
            _logBox.ScrollToCaret();
        }
    }
}