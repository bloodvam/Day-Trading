using TradingEngine.Core;
using TradingEngine.Managers;

namespace TradingEngine.UI
{
    public class LogPanel : BasePanel
    {
        private RichTextBox _txtLog;
        private Button _btnClear;
        private readonly LogManager _logManager;

        public LogManager LogManager => _logManager;

        public LogPanel(TradingController controller) : base(controller)
        {
            this.Dock = DockStyle.Fill;
            _logManager = new LogManager();
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
            _btnClear.Click += (s, e) => _txtLog.Clear();

            topPanel.Controls.Add(lblTitle);
            topPanel.Controls.Add(_btnClear);

            _txtLog = new RichTextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 9),
                ReadOnly = true,
                BackColor = Color.White
            };

            this.Controls.Add(_txtLog);
            this.Controls.Add(topPanel);
        }

        private void BindEvents()
        {
            // 初始化 LogManager
            _logManager.Initialize(Controller);

            // 监听 LogManager 的日志事件
            _logManager.LogReceived += (msg, level) => InvokeUI(() => AddLog(msg, level));
        }

        public void AddLog(string message, LogLevel level = LogLevel.Normal)
        {
            string line = $"[{DateTime.Now:HH:mm:ss}] {message}\n";

            Color color = level switch
            {
                LogLevel.Success => Color.DarkGreen,
                LogLevel.Error => Color.Red,
                _ => Color.Black
            };

            _txtLog.SelectionStart = _txtLog.TextLength;
            _txtLog.SelectionLength = 0;
            _txtLog.SelectionColor = color;
            _txtLog.AppendText(line);
            _txtLog.SelectionColor = Color.Black;

            // 滚动到底部
            _txtLog.SelectionStart = _txtLog.TextLength;
            _txtLog.ScrollToCaret();

            // 限制日志行数
            if (_txtLog.Lines.Length > 500)
            {
                _txtLog.SelectionStart = 0;
                _txtLog.SelectionLength = _txtLog.GetFirstCharIndexFromLine(100);
                _txtLog.SelectedText = "";
            }
        }
    }
}