using TradingEngine.Core;
using TradingEngine.Models;

namespace TradingEngine.UI
{
    public class QuotePanel : BasePanel
    {
        private Label _lblQuote;
        private Label _lblBar;

        public QuotePanel(TradingController controller) : base(controller)
        {
            this.Height = 70;
            BuildUI();
            BindEvents();
        }

        private void BuildUI()
        {
            _lblQuote = new Label
            {
                Text = "Quote: --",
                Location = new Point(10, 10),
                Size = new Size(850, 20),
                Font = new Font("Consolas", 10)
            };

            _lblBar = new Label
            {
                Text = "Bar: --",
                Location = new Point(10, 35),
                Size = new Size(850, 20),
                Font = new Font("Consolas", 10)
            };

            this.Controls.Add(_lblQuote);
            this.Controls.Add(_lblBar);
        }

        private void BindEvents()
        {
            Controller.QuoteUpdated += (quote) => InvokeUI(() => UpdateQuote(quote));
            Controller.BarUpdated += (bar) => InvokeUI(() => UpdateBar(bar));
            Controller.ActiveSymbolChanged += (s) => InvokeUI(() =>
            {
                if (string.IsNullOrEmpty(s))
                {
                    Clear();
                }
            });
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
    }
}
