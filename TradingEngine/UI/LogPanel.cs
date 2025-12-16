using TradingEngine.Core;

namespace TradingEngine.UI
{
    public class LogPanel : BasePanel
    {
        private ListBox _lstLog;
        private Button _btnClear;

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
            _btnClear.Click += (s, e) => _lstLog.Items.Clear();

            topPanel.Controls.Add(lblTitle);
            topPanel.Controls.Add(_btnClear);

            _lstLog = new ListBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 9)
            };

            this.Controls.Add(_lstLog);
            this.Controls.Add(topPanel);
        }

        private void BindEvents()
        {
            Controller.Log += (msg) => InvokeUI(() => AddLog(msg));
            Controller.RawMessage += (msg) => InvokeUI(() =>
            {
                // 打印初始化数据和订单相关消息
                if (msg.StartsWith("%POS") || msg.StartsWith("#POS") ||
                    msg.StartsWith("%ORDER") || msg.StartsWith("#Order") ||
                    msg.StartsWith("%TRADE") || msg.StartsWith("#Trade") ||
                    msg.StartsWith("%OrderAct") ||
                    msg.StartsWith("$AccountInfo") ||
                    msg.StartsWith("BP "))
                {
                    AddLog($"[RAW] {msg}");
                }
            });
        }

        public void AddLog(string message)
        {
            string line = $"[{DateTime.Now:HH:mm:ss}] {message}";
            _lstLog.Items.Add(line);
            _lstLog.TopIndex = _lstLog.Items.Count - 1;

            // 限制日志数量
            while (_lstLog.Items.Count > 500)
            {
                _lstLog.Items.RemoveAt(0);
            }
        }
    }
}