using TradingEngine.Core;
using TradingEngine.Models;
using TradingEngine.Config;

namespace TradingEngine.UI
{
    public class QuotePanel : BasePanel
    {
        private Label _lblQuote;
        private Label _lblBar;
        private Label _lblCountdown;
        private System.Windows.Forms.Timer _countdownTimer;
        private int _barIntervalSeconds;

        public QuotePanel(TradingController controller) : base(controller)
        {
            this.Height = 70;
            _barIntervalSeconds = AppConfig.Instance.Trading.DefaultBarInterval;
            BuildUI();
            BindEvents();
            StartCountdownTimer();
        }

        private void BuildUI()
        {
            _lblQuote = new Label
            {
                Text = "Quote: --",
                Location = new Point(10, 10),
                Size = new Size(750, 20),
                Font = new Font("Consolas", 10)
            };

            _lblBar = new Label
            {
                Text = "Bar: --",
                Location = new Point(10, 35),
                Size = new Size(750, 20),
                Font = new Font("Consolas", 10)
            };

            _lblCountdown = new Label
            {
                Text = "0.0",
                Location = new Point(770, 10),
                Size = new Size(100, 45),
                Font = new Font("Consolas", 24, FontStyle.Bold),
                ForeColor = Color.DarkBlue,
                TextAlign = ContentAlignment.MiddleCenter
            };

            this.Controls.Add(_lblQuote);
            this.Controls.Add(_lblBar);
            this.Controls.Add(_lblCountdown);
        }

        private void BindEvents()
        {
            Controller.QuoteUpdated += (quote) => InvokeUI(() => UpdateQuote(quote));
            Controller.BarUpdated += (bar) => InvokeUI(() => UpdateBar(bar));
            Controller.SymbolUnsubscribed += (s) => InvokeUI(() => Clear());
        }

        private void StartCountdownTimer()
        {
            _countdownTimer = new System.Windows.Forms.Timer();
            _countdownTimer.Interval = 100; // 100ms 更新一次
            _countdownTimer.Tick += CountdownTimer_Tick;
            _countdownTimer.Start();
        }

        private void CountdownTimer_Tick(object? sender, EventArgs e)
        {
            // 计算当前 bar 剩余时间
            double totalSeconds = DateTime.Now.TimeOfDay.TotalSeconds;
            double elapsed = totalSeconds % _barIntervalSeconds;
            double remaining = _barIntervalSeconds - elapsed;

            // 更新显示
            _lblCountdown.Text = remaining.ToString("F1");

            // 根据剩余时间改变颜色
            if (remaining <= 1.0)
            {
                _lblCountdown.ForeColor = Color.Red;
            }
            else if (remaining <= 2.0)
            {
                _lblCountdown.ForeColor = Color.Orange;
            }
            else
            {
                _lblCountdown.ForeColor = Color.DarkBlue;
            }
        }

        private void UpdateQuote(Quote quote)
        {
            _lblQuote.Text = $"Quote: {quote.Symbol}  Bid:{quote.Bid:F2}x{quote.BidSize}  Ask:{quote.Ask:F2}x{quote.AskSize}  Last:{quote.Last:F2}  Vol:{quote.Volume:N0}";
        }

        private void UpdateBar(Bar bar)
        {
            _lblBar.Text = $"Bar [{bar.IntervalSeconds}s]: {bar.Time:HH:mm:ss}  O:{bar.Open:F2} H:{bar.High:F2} L:{bar.Low:F2} C:{bar.Close:F2}  Vol:{bar.Volume:N0}";
        }

        private void Clear()
        {
            _lblQuote.Text = "Quote: --";
            _lblBar.Text = "Bar: --";
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _countdownTimer?.Stop();
                _countdownTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}