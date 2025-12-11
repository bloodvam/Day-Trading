using TradingEngine.Core;

namespace TradingEngine.UI
{
    public class SubscriptionPanel : BasePanel
    {
        private TextBox _txtSymbol;
        private Button _btnSubscribe;
        private Label _lblCurrentSymbol;
        private Button _btnUnsubscribe;

        public SubscriptionPanel(TradingController controller) : base(controller)
        {
            this.Height = 50;
            BuildUI();
            BindEvents();
        }

        private void BuildUI()
        {
            var lblSymbol = new Label
            {
                Text = "Symbol:",
                Location = new Point(10, 16),
                Size = new Size(50, 20)
            };

            _txtSymbol = new TextBox
            {
                Location = new Point(65, 12),
                Size = new Size(80, 23),
                CharacterCasing = CharacterCasing.Upper
            };
            _txtSymbol.KeyDown += TxtSymbol_KeyDown;

            _btnSubscribe = new Button
            {
                Text = "Subscribe",
                Location = new Point(155, 11),
                Size = new Size(75, 25)
            };
            _btnSubscribe.Click += BtnSubscribe_Click;

            _lblCurrentSymbol = new Label
            {
                Text = "Current: None",
                Location = new Point(250, 16),
                Size = new Size(150, 20),
                Font = new Font(this.Font, FontStyle.Bold)
            };

            _btnUnsubscribe = new Button
            {
                Text = "X",
                Location = new Point(400, 11),
                Size = new Size(30, 25),
                ForeColor = Color.Red
            };
            _btnUnsubscribe.Click += BtnUnsubscribe_Click;

            this.Controls.Add(lblSymbol);
            this.Controls.Add(_txtSymbol);
            this.Controls.Add(_btnSubscribe);
            this.Controls.Add(_lblCurrentSymbol);
            this.Controls.Add(_btnUnsubscribe);
        }

        private void BindEvents()
        {
            Controller.SymbolSubscribed += (symbol) => InvokeUI(() =>
            {
                _lblCurrentSymbol.Text = $"Current: {symbol}";
            });

            Controller.SymbolUnsubscribed += (symbol) => InvokeUI(() =>
            {
                _lblCurrentSymbol.Text = "Current: None";
            });
        }

        private void TxtSymbol_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                BtnSubscribe_Click(sender, e);
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private async void BtnSubscribe_Click(object? sender, EventArgs e)
        {
            string symbol = _txtSymbol.Text.Trim();
            if (string.IsNullOrEmpty(symbol)) return;

            await Controller.SubscribeAsync(symbol);
            _txtSymbol.Clear();
        }

        private async void BtnUnsubscribe_Click(object? sender, EventArgs e)
        {
            await Controller.UnsubscribeAsync();
        }
    }
}
