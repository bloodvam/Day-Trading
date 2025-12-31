using TradingEngine.Core;

namespace TradingEngine.UI
{
    /// <summary>
    /// 股票选择栏 - 显示所有订阅的股票按钮
    /// 左键选中，右键取消订阅
    /// </summary>
    public class SymbolBar : BasePanel
    {
        private readonly FlowLayoutPanel _buttonPanel;
        private readonly Dictionary<string, Button> _buttons = new();

        public SymbolBar(TradingController controller) : base(controller)
        {
            this.Height = 40;
            this.Dock = DockStyle.Top;

            _buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(5, 5, 5, 5)
            };

            this.Controls.Add(_buttonPanel);
            BindEvents();
        }

        private void BindEvents()
        {
            Controller.SymbolSubscribed += (symbol) => InvokeUI(() => AddButton(symbol));
            Controller.SymbolUnsubscribed += (symbol) => InvokeUI(() => RemoveButton(symbol));
            Controller.ActiveSymbolChanged += (symbol) => InvokeUI(() => UpdateSelection(symbol));
        }

        private void AddButton(string symbol)
        {
            if (_buttons.ContainsKey(symbol)) return;

            var btn = new Button
            {
                Text = symbol,
                Size = new Size(70, 28),
                Margin = new Padding(3),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };

            // 左键选中
            btn.Click += (s, e) =>
            {
                Controller.SetActiveSymbol(symbol);
            };

            // 右键取消订阅
            btn.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Right)
                {
                    _ = Controller.UnsubscribeAsync(symbol);
                }
            };

            _buttons[symbol] = btn;
            _buttonPanel.Controls.Add(btn);

            // 新添加的自动选中，更新样式
            UpdateSelection(Controller.ActiveSymbol);
        }

        private void RemoveButton(string symbol)
        {
            if (_buttons.TryGetValue(symbol, out var btn))
            {
                _buttonPanel.Controls.Remove(btn);
                btn.Dispose();
                _buttons.Remove(symbol);
            }
        }

        private void UpdateSelection(string? activeSymbol)
        {
            foreach (var kvp in _buttons)
            {
                bool isActive = kvp.Key == activeSymbol;
                var btn = kvp.Value;

                if (isActive)
                {
                    btn.BackColor = Color.DodgerBlue;
                    btn.ForeColor = Color.White;
                    btn.FlatAppearance.BorderColor = Color.DodgerBlue;
                }
                else
                {
                    btn.BackColor = SystemColors.Control;
                    btn.ForeColor = Color.Black;
                    btn.FlatAppearance.BorderColor = Color.Gray;
                }
            }
        }
    }
}
