using TradingEngine.Core;

namespace TradingEngine.UI
{
    /// <summary>
    /// 日志面板 - 包含 History 和 Status 两个视图
    /// </summary>
    public class LogPanel : BasePanel
    {
        private RichTextBox _historyLog;
        private RichTextBox _statusLog;
        private Button _btnClear;
        private Button _btnToggle;

        private bool _showStatus = true;  // 默认显示 Status

        // Status Panel 用的 Timer（10秒后清除成功消息）
        private System.Windows.Forms.Timer? _connectTimer;
        private System.Windows.Forms.Timer? _subscribeTimer;
        private System.Windows.Forms.Timer? _hotkeyTimer;

        // OrderAct 优先级过滤（两个 Panel 各自维护）
        private readonly Dictionary<int, (string msg, int priority)> _historyOrderActs = new();
        private readonly Dictionary<int, (string msg, int priority)> _statusOrderActs = new();

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

            _btnClear = new Button
            {
                Text = "Clear",
                Location = new Point(45, 0),
                Size = new Size(50, 23)
            };
            _btnClear.Click += BtnClear_Click;

            _btnToggle = new Button
            {
                Text = "History",  // 当前显示 Status，按钮显示切换目标
                Location = new Point(100, 0),
                Size = new Size(60, 23)
            };
            _btnToggle.Click += BtnToggle_Click;

            topPanel.Controls.Add(lblTitle);
            topPanel.Controls.Add(_btnClear);
            topPanel.Controls.Add(_btnToggle);

            // 内容容器
            var contentPanel = new Panel
            {
                Dock = DockStyle.Fill
            };

            // History Log
            _historyLog = new RichTextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 9),
                ReadOnly = true,
                BackColor = Color.White,
                Visible = false
            };

            // Status Log (默认显示)
            _statusLog = new RichTextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 9),
                ReadOnly = true,
                BackColor = Color.White,
                Visible = true
            };

            contentPanel.Controls.Add(_historyLog);
            contentPanel.Controls.Add(_statusLog);

            this.Controls.Add(contentPanel);
            this.Controls.Add(topPanel);
        }

        private void BindEvents()
        {
            // 连接状态
            Controller.LoginSuccess += () => InvokeUI(() => OnLoginSuccess());
            Controller.LoginFailed += (msg) => InvokeUI(() => OnLoginFailed(msg));
            Controller.Disconnected += () => InvokeUI(() => OnDisconnected());

            // 订阅状态
            Controller.SymbolSubscribed += (symbol) => InvokeUI(() => OnSymbolSubscribed(symbol));

            // OrderAct
            Controller.RawMessage += (msg) => InvokeUI(() => OnRawMessage(msg));

            // 新操作开始（清除 Status Panel 的 OrderAct）
            Controller.NewOperationStarted += () => InvokeUI(() => OnNewOperationStarted());
        }

        #region Connection Events

        private void OnLoginSuccess()
        {
            AppendHistory("connect successfully", Color.DarkGreen);
            AppendStatus("connect successfully", Color.DarkGreen);

            // Status: 10秒后清除
            StartTimer(ref _connectTimer, () =>
            {
                RemoveStatusLineContaining("connect successfully");
            });
        }

        private void OnLoginFailed(string msg)
        {
            AppendHistory("connection error", Color.Red);
            AppendStatus("connection error", Color.Red);
            // 失败不清除
        }

        private void OnDisconnected()
        {
            AppendHistory("disconnected", Color.Red);
            AppendStatus("disconnected", Color.Red);
        }

        #endregion

        #region Subscribe Events

        private void OnSymbolSubscribed(string symbol)
        {
            string successMsg = $"subscribe {symbol} successfully";
            AppendHistory(successMsg, Color.DarkGreen);
            AppendStatus(successMsg, Color.DarkGreen);

            // Status: 10秒后清除
            StartTimer(ref _subscribeTimer, () =>
            {
                RemoveStatusLineContaining(successMsg);
            });
        }

        #endregion

        #region Hotkey

        /// <summary>
        /// 由 MainForm 调用，记录热键注册结果
        /// </summary>
        public void LogHotkeyResult(bool allSuccess, string? failedKey = null)
        {
            if (allSuccess)
            {
                AppendHistory("hotkey register successfully", Color.DarkGreen);
                AppendStatus("hotkey register successfully", Color.DarkGreen);

                // Status: 10秒后清除
                StartTimer(ref _hotkeyTimer, () =>
                {
                    RemoveStatusLineContaining("hotkey register successfully");
                });
            }
            else
            {
                string msg = $"hotkey register failed: {failedKey}";
                AppendHistory(msg, Color.Red);
                AppendStatus(msg, Color.Red);
                // 失败不清除
            }
        }

        #endregion

        #region OrderAct

        private void OnNewOperationStarted()
        {
            // 清除 Status Panel 的 OrderAct
            _statusOrderActs.Clear();
            RemoveStatusOrderActLines();
        }

        private void OnRawMessage(string msg)
        {
            if (!msg.StartsWith("%OrderAct")) return;

            var parts = msg.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 9) return;

            string actionType = parts[2];
            int priority = GetPriority(actionType);
            if (priority == 0) return;

            // Token 在最后
            if (!int.TryParse(parts[^1], out int token) || token == 0) return;

            // History Panel: 更新
            bool historyUpdated = UpdateOrderActMap(_historyOrderActs, token, msg, priority);
            if (historyUpdated)
            {
                RefreshOrderActDisplay(_historyLog, _historyOrderActs);
            }

            // Status Panel: 更新
            bool statusUpdated = UpdateOrderActMap(_statusOrderActs, token, msg, priority);
            if (statusUpdated)
            {
                RefreshOrderActDisplay(_statusLog, _statusOrderActs);
            }
        }

        private bool UpdateOrderActMap(Dictionary<int, (string msg, int priority)> map, int token, string msg, int priority)
        {
            if (map.TryGetValue(token, out var existing))
            {
                if (priority > existing.priority)
                {
                    map[token] = (msg, priority);
                    return true;
                }
                return false;
            }
            else
            {
                map[token] = (msg, priority);
                return true;
            }
        }

        private void RefreshOrderActDisplay(RichTextBox log, Dictionary<int, (string msg, int priority)> orderActs)
        {
            // 保存非 OrderAct 的行
            var lines = log.Text.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Where(l => !l.StartsWith("%OrderAct"))
                .ToList();

            // 清空并重建
            log.Clear();

            // 先添加普通日志
            foreach (var line in lines)
            {
                Color color = Color.Black;
                if (line.Contains("successfully")) color = Color.DarkGreen;
                else if (line.Contains("error") || line.Contains("failed") || line.Contains("disconnected")) color = Color.Red;

                log.SelectionColor = color;
                log.AppendText(line + "\n");
            }

            // 再添加 OrderAct
            foreach (var kvp in orderActs.Values)
            {
                Color color = GetOrderActColor(kvp.msg);
                log.SelectionColor = color;
                log.AppendText(kvp.msg + "\n");
            }

            log.SelectionColor = Color.Black;
            log.SelectionStart = log.TextLength;
            log.ScrollToCaret();
        }

        private Color GetOrderActColor(string msg)
        {
            var parts = msg.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 3)
            {
                string actionType = parts[2];
                if (actionType == "Send_Rej") return Color.Red;
                if (actionType == "Execute") return Color.DarkGreen;
            }
            return Color.Black;
        }

        private int GetPriority(string action)
        {
            return action switch
            {
                "Sending" => 1,
                "Accept" => 2,
                "Canceled" => 5,
                "Execute" => 5,
                "Send_Rej" => 5,
                _ => 0
            };
        }

        private void RemoveStatusOrderActLines()
        {
            var lines = _statusLog.Text.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Where(l => !l.StartsWith("%OrderAct"))
                .ToList();

            _statusLog.Clear();
            foreach (var line in lines)
            {
                Color color = Color.Black;
                if (line.Contains("successfully")) color = Color.DarkGreen;
                else if (line.Contains("error") || line.Contains("failed") || line.Contains("disconnected")) color = Color.Red;

                _statusLog.SelectionColor = color;
                _statusLog.AppendText(line + "\n");
            }
            _statusLog.SelectionColor = Color.Black;
        }

        #endregion

        #region UI Helpers

        private void AppendHistory(string message, Color color)
        {
            AppendToLog(_historyLog, $"[{DateTime.Now:HH:mm:ss}] {message}", color);
        }

        private void AppendStatus(string message, Color color)
        {
            AppendToLog(_statusLog, $"[{DateTime.Now:HH:mm:ss}] {message}", color);
        }

        private void AppendToLog(RichTextBox log, string message, Color color)
        {
            log.SelectionStart = log.TextLength;
            log.SelectionLength = 0;
            log.SelectionColor = color;
            log.AppendText(message + "\n");
            log.SelectionColor = Color.Black;
            log.SelectionStart = log.TextLength;
            log.ScrollToCaret();
        }

        private void RemoveStatusLineContaining(string text)
        {
            var lines = _statusLog.Text.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Where(l => !l.Contains(text))
                .ToList();

            _statusLog.Clear();
            foreach (var line in lines)
            {
                Color color = Color.Black;
                if (line.Contains("successfully")) color = Color.DarkGreen;
                else if (line.Contains("error") || line.Contains("failed") || line.Contains("disconnected")) color = Color.Red;
                else if (line.StartsWith("%OrderAct")) color = GetOrderActColor(line);

                _statusLog.SelectionColor = color;
                _statusLog.AppendText(line + "\n");
            }
            _statusLog.SelectionColor = Color.Black;
        }

        private void StartTimer(ref System.Windows.Forms.Timer? timer, Action callback)
        {
            timer?.Stop();
            timer?.Dispose();

            var newTimer = new System.Windows.Forms.Timer();
            newTimer.Interval = 10000;  // 10秒
            newTimer.Tick += (s, e) =>
            {
                newTimer.Stop();
                callback();
            };
            newTimer.Start();
            timer = newTimer;
        }

        #endregion

        #region Button Events

        private void BtnClear_Click(object? sender, EventArgs e)
        {
            if (_showStatus)
            {
                _statusLog.Clear();
                _statusOrderActs.Clear();
            }
            else
            {
                _historyLog.Clear();
                _historyOrderActs.Clear();
            }
        }

        private void BtnToggle_Click(object? sender, EventArgs e)
        {
            _showStatus = !_showStatus;

            if (_showStatus)
            {
                _statusLog.Visible = true;
                _statusLog.BringToFront();
                _historyLog.Visible = false;
                _btnToggle.Text = "History";
            }
            else
            {
                _statusLog.Visible = false;
                _historyLog.Visible = true;
                _historyLog.BringToFront();
                _btnToggle.Text = "Status";
            }
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _connectTimer?.Dispose();
                _subscribeTimer?.Dispose();
                _hotkeyTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}