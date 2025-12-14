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
                Text = "Pending Orders:",
                Location = new Point(5, 5),
                Size = new Size(120, 20)
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
            Controller.OrderActionReceived += () => InvokeUI(() => RefreshOrders());
            Controller.LoginSuccess += () => InvokeUI(() => RefreshOrders());
        }

        private void RefreshOrders()
        {
            _lstOrders.Items.Clear();

            foreach (var order in Controller.GetPendingOrders().OrderByDescending(o => o.Time))
            {
                string typeStr = order.Type switch
                {
                    Models.OrderType.StopLimit => "STOP",
                    Models.OrderType.StopLimitPost => "STOP",
                    Models.OrderType.StopMarket => "STOP",
                    _ => "LMT"
                };

                string priceStr = order.StopPrice > 0
                    ? $"Stop@{order.StopPrice:F2}"
                    : $"@{order.Price:F2}";

                _lstOrders.Items.Add($"[{order.OrderId}] {order.Symbol} {order.Side} {order.LeftQuantity}/{order.Quantity} {typeStr} {priceStr} {order.Status}");
            }
        }
    }
}