using TradingEngine.Core;

namespace TradingEngine.UI
{
    public class SubscriptionPanel : BasePanel
    {
        private TextBox _txtSymbol;
        private Button _btnSubscribe;

        public SubscriptionPanel(TradingController controller) : base(controller)
        {
            this.Height = 40;
            BuildUI();
        }

        private void BuildUI()
        {
            var lblSymbol = new Label
            {
                Text = "Symbol:",
                Location = new Point(10, 12),
                Size = new Size(50, 20)
            };

            _txtSymbol = new TextBox
            {
                Location = new Point(65, 8),
                Size = new Size(80, 23),
                CharacterCasing = CharacterCasing.Upper
            };
            _txtSymbol.KeyDown += TxtSymbol_KeyDown;

            _btnSubscribe = new Button
            {
                Text = "Subscribe",
                Location = new Point(155, 7),
                Size = new Size(75, 25)
            };
            _btnSubscribe.Click += BtnSubscribe_Click;

            this.Controls.Add(lblSymbol);
            this.Controls.Add(_txtSymbol);
            this.Controls.Add(_btnSubscribe);
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
            try
            {
                string symbol = _txtSymbol.Text.Trim();
                if (string.IsNullOrEmpty(symbol)) return;

                await Controller.SubscribeAsync(symbol);
                _txtSymbol.Clear();
            }
            catch (Exception ex)
            {
                Controller.LogMessage($"Subscribe error: {ex.Message}");
            }
        }
    }
}