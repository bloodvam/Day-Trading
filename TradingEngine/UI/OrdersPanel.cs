using TradingEngine.Core;

namespace TradingEngine.UI
{
    public class OrdersPanel : BasePanel
    {
        private ListBox _lstOrders;

        public OrdersPanel(TradingController controller) : base(controller)
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
                Text = "Orders:",
                Location = new Point(5, 5),
                Size = new Size(100, 20)
            };

            _lstOrders = new ListBox
            {
                Location = new Point(5, 25),
                Size = new Size(420, 100),
                Font = new Font("Consolas", 9)
            };

            this.Controls.Add(lblTitle);
            this.Controls.Add(_lstOrders);
        }

        private void BindEvents()
        {
            Controller.OrderChanged += (order) => InvokeUI(() => RefreshOrders());
            Controller.LoginSuccess += () => InvokeUI(() => RefreshOrders());
        }

        private void RefreshOrders()
        {
            _lstOrders.Items.Clear();
            foreach (var order in Controller.GetAllOrders().OrderByDescending(o => o.Time).Take(20))
            {
                _lstOrders.Items.Add($"[{order.OrderId}] {order.Symbol} {order.Side} {order.Quantity}@{order.Price:F2} {order.Status}");
            }
        }
    }
}
