using TradingEngine.Core;

namespace TradingEngine.UI
{
    public class OrdersPanel : BasePanel
    {
        private ListBox _lstOrders;
        private string? _activeSymbol;

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
                Font = new Font("Consolas", 9),
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = 16
            };
            _lstOrders.DrawItem += LstOrders_DrawItem;

            // 右键菜单
            var contextMenu = new ContextMenuStrip();
            var cancelItem = new ToolStripMenuItem("Cancel Order");
            cancelItem.Click += async (s, e) =>
            {
                if (_lstOrders.SelectedItem is OrderItem item)
                {
                    await Controller.CancelOrder(item.OrderId);
                }
            };
            contextMenu.Items.Add(cancelItem);
            _lstOrders.ContextMenuStrip = contextMenu;

            // 右键选中
            _lstOrders.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Right)
                {
                    int index = _lstOrders.IndexFromPoint(e.Location);
                    if (index >= 0)
                    {
                        _lstOrders.SelectedIndex = index;
                    }
                }
            };

            this.Controls.Add(lblTitle);
            this.Controls.Add(_lstOrders);
        }

        private void BindEvents()
        {
            Controller.OrderAdded += (order) => InvokeUI(() => RefreshOrders());
            Controller.OrderRemoved += (order) => InvokeUI(() => RefreshOrders());
            Controller.LoginSuccess += () => InvokeUI(() => RefreshOrders());

            // 监听 ActiveSymbol 变化
            Controller.ActiveSymbolChanged += (symbol) => InvokeUI(() =>
            {
                _activeSymbol = symbol;
                _lstOrders.Invalidate();  // 重绘以更新高亮
            });
        }

        private void LstOrders_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;

            e.DrawBackground();

            var item = _lstOrders.Items[e.Index] as OrderItem;
            if (item == null) return;

            // 判断是否是当前选中的 symbol
            bool isActive = item.Symbol == _activeSymbol;

            // 背景色
            Color backColor = isActive ? Color.LightSkyBlue : e.BackColor;
            using (var brush = new SolidBrush(backColor))
            {
                e.Graphics.FillRectangle(brush, e.Bounds);
            }

            // 文字
            using (var brush = new SolidBrush(e.ForeColor))
            {
                e.Graphics.DrawString(item.DisplayText, e.Font!, brush, e.Bounds);
            }

            e.DrawFocusRectangle();
        }

        private void RefreshOrders()
        {
            _lstOrders.Items.Clear();

            foreach (var order in Controller.GetAllOrders().OrderByDescending(o => o.Time))
            {
                string typeStr = order.Type switch
                {
                    Models.OrderType.StopLimit => "STOP",
                    Models.OrderType.StopLimitPost => "STOP",
                    Models.OrderType.StopMarket => "STOP",
                    Models.OrderType.Market => "MKT",
                    _ => "LMT"
                };

                string priceStr = order.StopPrice > 0
                    ? $"Trigger@{order.StopPrice:F2} Limit@{order.Price:F2}"
                    : $"@{order.Price:F2}";

                string displayText = $"[{order.OrderId}] {order.Symbol} {order.Side} {order.LeftQuantity}/{order.Quantity} {typeStr} {priceStr}";

                _lstOrders.Items.Add(new OrderItem
                {
                    OrderId = order.OrderId,
                    Symbol = order.Symbol,
                    DisplayText = displayText
                });
            }
        }

        private class OrderItem
        {
            public int OrderId { get; set; }
            public string Symbol { get; set; } = "";
            public string DisplayText { get; set; } = "";
            public override string ToString() => DisplayText;
        }
    }
}