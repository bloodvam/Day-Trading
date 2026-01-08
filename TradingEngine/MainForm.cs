using TradingEngine.UI;
using TradingEngine.Core;
using TradingEngine.Managers;

namespace TradingEngine
{
    public class MainForm : Form
    {
        private TradingController _controller = null!;

        // Panels
        private ConnectionPanel _connectionPanel = null!;
        private SubscriptionPanel _subscriptionPanel = null!;
        private SymbolBar _symbolBar = null!;
        private QuotePanel _quotePanel = null!;
        private AccountPanel _accountPanel = null!;
        private PositionsPanel _positionsPanel = null!;
        private OrdersPanel _ordersPanel = null!;
        private WorkingZonePanel _workingZonePanel = null!;

        // Log Panels (3 个分类)
        private LogPanel _orderLogPanel = null!;
        private LogPanel _strategyLogPanel = null!;
        private LogPanel _agentLogPanel = null!;

        public MainForm()
        {
            InitializeComponent();
            InitializeApp();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(900, 960);
            this.Name = "MainForm";
            this.Text = "Trading Engine";
            this.ResumeLayout(false);
        }

        private void InitializeApp()
        {
            this.FormClosing += MainForm_FormClosing;

            // 初始化Controller
            _controller = new TradingController();

            // 创建Panels
            CreatePanels();

            // 布局
            LayoutPanels();

            // 设置热键并记录结果
            var (allSuccess, failedKey) = _controller.SetupHotkeys(this);
            _orderLogPanel.LogHotkeyResult(allSuccess, failedKey);

            // 添加热键提示
            AddHotkeyHint();
        }

        private void CreatePanels()
        {
            _connectionPanel = new ConnectionPanel(_controller);
            _subscriptionPanel = new SubscriptionPanel(_controller);
            _symbolBar = new SymbolBar(_controller);
            _quotePanel = new QuotePanel(_controller);
            _accountPanel = new AccountPanel(_controller);
            _positionsPanel = new PositionsPanel(_controller);
            _ordersPanel = new OrdersPanel(_controller);
            _workingZonePanel = new WorkingZonePanel(_controller);

            // 3 个分类 LogPanel
            _orderLogPanel = new LogPanel(_controller, LogPanelType.Order);
            _strategyLogPanel = new LogPanel(_controller, LogPanelType.Strategy);
            _agentLogPanel = new LogPanel(_controller, LogPanelType.Agent);
        }

        private void LayoutPanels()
        {
            // 顶部区域 - Global 配置
            var topContainer = new Panel
            {
                Dock = DockStyle.Top,
                Height = 575  // 50+40+40+135+40+70+200
            };

            _connectionPanel.Dock = DockStyle.Top;
            _subscriptionPanel.Dock = DockStyle.Top;
            _symbolBar.Dock = DockStyle.Top;
            _quotePanel.Dock = DockStyle.Top;
            _accountPanel.Dock = DockStyle.Top;

            // 中间区域 - Positions 和 Orders 并排
            var middleContainer = new Panel
            {
                Dock = DockStyle.Top,
                Height = 135
            };

            _positionsPanel.Location = new Point(5, 0);
            _positionsPanel.Size = new Size(430, 130);

            _ordersPanel.Location = new Point(445, 0);
            _ordersPanel.Size = new Size(430, 130);

            middleContainer.Controls.Add(_positionsPanel);
            middleContainer.Controls.Add(_ordersPanel);

            // 注意添加顺序（后添加的在上面）
            topContainer.Controls.Add(_workingZonePanel);
            topContainer.Controls.Add(_quotePanel);
            topContainer.Controls.Add(_symbolBar);
            topContainer.Controls.Add(middleContainer);
            topContainer.Controls.Add(_accountPanel);
            topContainer.Controls.Add(_subscriptionPanel);
            topContainer.Controls.Add(_connectionPanel);

            // 底部区域 - 3 个 Log 面板
            // 布局：上面 Strategy + Agent 并排 (40%)，下面 Order (60%)
            var logContainer = new Panel
            {
                Dock = DockStyle.Fill
            };

            // 上面一行：Strategy + Agent 并排
            var topLogRow = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60  // 会在 Resize 时调整
            };

            _strategyLogPanel.Dock = DockStyle.Left;
            _agentLogPanel.Dock = DockStyle.Fill;

            topLogRow.Controls.Add(_agentLogPanel);
            topLogRow.Controls.Add(_strategyLogPanel);

            // 下面一行：Order 填充剩余
            _orderLogPanel.Dock = DockStyle.Fill;

            logContainer.Controls.Add(_orderLogPanel);
            logContainer.Controls.Add(topLogRow);

            // 窗口大小变化时调整比例
            logContainer.Resize += (s, e) =>
            {
                int totalHeight = logContainer.Height;
                topLogRow.Height = (int)(totalHeight * 0.4);  // 40% 给 Strategy + Agent
                _strategyLogPanel.Width = topLogRow.Width / 2;
            };

            // 添加到窗体
            this.Controls.Add(logContainer);
            this.Controls.Add(topContainer);
        }

        private void AddHotkeyHint()
        {
            var lblHotkeys = new Label
            {
                Text = "Hotkeys: [Shift+1] Buy | [Alt+1] Sell All | [Alt+2] Sell 50% | [Alt+3] Sell 70% | [Space] Stop→BE | [Shift+Q] Add(BE) | [Shift+W] Add(50%)",
                Dock = DockStyle.Bottom,
                Height = 25,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.Gray,
                Padding = new Padding(10, 0, 0, 0)
            };
            this.Controls.Add(lblHotkeys);
        }

        private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            _controller.Dispose();
        }
    }
}