using TradingEngine.UI;
using TradingEngine.Core;

namespace TradingEngine
{
    public class MainForm : Form
    {
        private TradingController _controller = null!;

        // Panels
        private ConnectionPanel _connectionPanel = null!;
        private SubscriptionPanel _subscriptionPanel = null!;
        private QuotePanel _quotePanel = null!;
        private AccountPanel _accountPanel = null!;
        private PositionsPanel _positionsPanel = null!;
        private OrdersPanel _ordersPanel = null!;
        private LogPanel _logPanel = null!;

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
            this.ClientSize = new Size(900, 700);
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

            // 设置热键
            _controller.SetupHotkeys(this);

            // 添加热键提示
            AddHotkeyHint();
        }

        private void CreatePanels()
        {
            _connectionPanel = new ConnectionPanel(_controller);
            _subscriptionPanel = new SubscriptionPanel(_controller);
            _quotePanel = new QuotePanel(_controller);
            _accountPanel = new AccountPanel(_controller);
            _positionsPanel = new PositionsPanel(_controller);
            _ordersPanel = new OrdersPanel(_controller);
            _logPanel = new LogPanel(_controller);
        }

        private void LayoutPanels()
        {
            // 顶部区域 - 使用 Dock
            var topContainer = new Panel
            {
                Dock = DockStyle.Top,
                Height = 220
            };

            _connectionPanel.Dock = DockStyle.Top;
            _subscriptionPanel.Dock = DockStyle.Top;
            _quotePanel.Dock = DockStyle.Top;
            _accountPanel.Dock = DockStyle.Top;

            // 注意添加顺序（后添加的在上面）
            topContainer.Controls.Add(_accountPanel);
            topContainer.Controls.Add(_quotePanel);
            topContainer.Controls.Add(_subscriptionPanel);
            topContainer.Controls.Add(_connectionPanel);

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

            // 底部区域 - Log 填充剩余空间
            _logPanel.Dock = DockStyle.Fill;

            // 添加到窗体（顺序：先Fill，再Top）
            this.Controls.Add(_logPanel);
            this.Controls.Add(middleContainer);
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