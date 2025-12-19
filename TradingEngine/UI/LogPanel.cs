using TradingEngine.Core;

namespace TradingEngine.UI
{
    /// <summary>
    /// 日志面板
    /// </summary>
    public class LogPanel : BasePanel
    {
        private RichTextBox _logBox;

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

            var lblTitle = new Label
            {
                Text = "Log:",
                Location = new Point(0, 5),
                Size = new Size(40, 20)
            };

            topPanel.Controls.Add(lblTitle);

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
            // 连接状态
            Controller.LoginSuccess += () => InvokeUI(() => AppendLog("connect successfully", Color.DarkGreen));
            Controller.LoginFailed += (msg) => InvokeUI(() => AppendLog("connection error", Color.Red));
            Controller.Disconnected += () => InvokeUI(() => AppendLog("disconnected", Color.Red));

            // 订阅状态
            Controller.SymbolSubscribed += (symbol) => InvokeUI(() => AppendLog($"subscribe {symbol} successfully", Color.DarkGreen));

            // OrderAct (来自 DAS 原始消息)
            Controller.RawMessage += (msg) => InvokeUI(() => OnRawMessage(msg));

            // OrderManager 的 Log 消息 (买入失败原因等)
            Controller.Log += (msg) => InvokeUI(() => OnLogMessage(msg));

            // 新操作开始（清空）
            Controller.NewOperationStarted += () => InvokeUI(() => _logBox.Clear());
        }

        /// <summary>
        /// 由 MainForm 调用，记录热键注册结果
        /// </summary>
        public void LogHotkeyResult(bool allSuccess, string? failedKey = null)
        {
            if (allSuccess)
            {
                AppendLog("hotkey register successfully", Color.DarkGreen);
            }
            else
            {
                AppendLog($"hotkey register failed: {failedKey}", Color.Red);
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
            AppendLog(msg, color);
        }

        private void OnRawMessage(string msg)
        {
            if (!msg.StartsWith("%OrderAct")) return;

            var parts = msg.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3) return;

            string actionType = parts[2];
            Color color = GetOrderActColor(actionType);
            AppendLog(msg, color);
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

        private void AppendLog(string message, Color color)
        {
            _logBox.SelectionStart = _logBox.TextLength;
            _logBox.SelectionLength = 0;
            _logBox.SelectionColor = color;
            _logBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
            _logBox.SelectionColor = Color.Black;
            _logBox.SelectionStart = _logBox.TextLength;
            _logBox.ScrollToCaret();
        }
    }
}