using TradingEngine.Core;
using TradingEngine.Models;

namespace TradingEngine.UI
{
    public class PositionsPanel : BasePanel
    {
        private ListBox _lstPositions;
        private double _currentBid;

        public PositionsPanel(TradingController controller) : base(controller)
        {
            this.Height = 130;
            this.Dock = DockStyle.None;
            BuildUI();
            BindEvents();
        }

        private void BuildUI()
        {
            var lblTitle = new Label
            {
                Text = "Positions:",
                Location = new Point(5, 5),
                Size = new Size(100, 20)
            };

            _lstPositions = new ListBox
            {
                Location = new Point(5, 25),
                Size = new Size(420, 100),
                Font = new Font("Consolas", 9)
            };

            this.Controls.Add(lblTitle);
            this.Controls.Add(_lstPositions);
        }

        private void BindEvents()
        {
            Controller.PositionChanged += (pos) => InvokeUI(() => RefreshPositions());
            Controller.LoginSuccess += () => InvokeUI(() => RefreshPositions());

            // 监听 Quote 更新，实时计算盈亏
            Controller.QuoteUpdated += (quote) => InvokeUI(() =>
            {
                _currentBid = quote.Bid;
                RefreshPositions();
            });
        }

        private void RefreshPositions()
        {
            _lstPositions.Items.Clear();

            foreach (var pos in Controller.GetActivePositions())
            {
                // 用 Bid 计算未实现盈亏
                double unrealizedPL = 0;
                if (_currentBid > 0 && pos.Quantity != 0)
                {
                    unrealizedPL = (_currentBid - pos.AvgCost) * pos.Quantity;
                }

                string plColor = unrealizedPL >= 0 ? "+" : "";
                _lstPositions.Items.Add($"{pos.Symbol}: {pos.Quantity}@{pos.AvgCost:F2} | Unrealized:{plColor}{unrealizedPL:F2} | Realized:{pos.RealizedPL:F2}");
            }
        }
    }
}