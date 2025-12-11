using TradingEngine.Core;

namespace TradingEngine.UI
{
    public class PositionsPanel : BasePanel
    {
        private ListBox _lstPositions;

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
        }

        private void RefreshPositions()
        {
            _lstPositions.Items.Clear();
            foreach (var pos in Controller.GetAllPositions())
            {
                _lstPositions.Items.Add($"{pos.Symbol}: {pos.Quantity}@{pos.AvgCost:F2} P&L:{pos.RealizedPL:F2}");
            }
        }
    }
}
