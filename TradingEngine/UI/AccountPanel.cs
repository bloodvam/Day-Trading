using TradingEngine.Core;
using TradingEngine.Models;

namespace TradingEngine.UI
{
    public class AccountPanel : BasePanel
    {
        private Label _lblAccount;

        public AccountPanel(TradingController controller) : base(controller)
        {
            this.Height = 40;
            BuildUI();
            BindEvents();
        }

        private void BuildUI()
        {
            _lblAccount = new Label
            {
                Text = "Account: --",
                Location = new Point(10, 10),
                Size = new Size(850, 20),
                Font = new Font("Consolas", 10)
            };

            this.Controls.Add(_lblAccount);
        }

        private void BindEvents()
        {
            Controller.AccountInfoChanged += (info) => InvokeUI(() => UpdateAccount(info));
        }

        private void UpdateAccount(AccountInfo info)
        {
            _lblAccount.Text = $"Account: Equity:{info.CurrentEquity:N2}  BP:{info.BuyingPower:N2}  P&L:{info.NetPL:N2}";
        }
    }
}
