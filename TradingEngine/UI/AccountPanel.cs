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
            Controller.LoginSuccess += () => InvokeUI(() => UpdateAccount(Controller.AccountInfo));
        }

        private void UpdateAccount(AccountInfo info)
        {
            if (info.CurrentEquity > 0)
            {
                double leverage = TradingEngine.Config.AppConfig.Instance.Trading.Leverage;
                _lblAccount.Text = $"Account: Equity:{info.CurrentEquity:N2}  BP:{info.BuyingPower:N2}  (Leverage:{leverage}x)";
            }
            else
            {
                _lblAccount.Text = "Account: (waiting for data...)";
            }
        }
    }
}