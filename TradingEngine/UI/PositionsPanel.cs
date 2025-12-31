using TradingEngine.Core;
using TradingEngine.Models;

namespace TradingEngine.UI
{
    public class PositionsPanel : BasePanel
    {
        private ListBox _lstPositions;
        private string? _activeSymbol;

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
                Font = new Font("Consolas", 9),
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = 16
            };
            _lstPositions.DrawItem += LstPositions_DrawItem;

            this.Controls.Add(lblTitle);
            this.Controls.Add(_lstPositions);
        }

        private void BindEvents()
        {
            Controller.PositionChanged += (pos) => InvokeUI(() => RefreshPositions());
            Controller.LoginSuccess += () => InvokeUI(() => RefreshPositions());

            // 监听所有 Quote 更新（用于计算盈亏）
            Controller.AnyQuoteUpdated += (quote) => InvokeUI(() => RefreshPositions());

            // 监听 ActiveSymbol 变化
            Controller.ActiveSymbolChanged += (symbol) => InvokeUI(() =>
            {
                _activeSymbol = symbol;
                RefreshPositions();
            });
        }

        private void LstPositions_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;

            e.DrawBackground();

            var item = _lstPositions.Items[e.Index] as PositionItem;
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

        private void RefreshPositions()
        {
            _lstPositions.Items.Clear();

            foreach (var pos in Controller.GetAllPositions())
            {
                // 从 Controller 获取该 symbol 的 quote
                var quote = Controller.GetQuote(pos.Symbol);
                double bid = quote?.Bid ?? 0;

                double unrealizedPL = 0;
                if (bid > 0 && pos.Quantity != 0)
                {
                    unrealizedPL = (bid - pos.AvgCost) * pos.Quantity;
                }

                string plSign = unrealizedPL >= 0 ? "+" : "";
                string displayText = $"{pos.Symbol}: {pos.Quantity}@{pos.AvgCost:F2} | Unrealized:{plSign}{unrealizedPL:F2} | Realized:{pos.RealizedPL:F2}";

                _lstPositions.Items.Add(new PositionItem
                {
                    Symbol = pos.Symbol,
                    DisplayText = displayText
                });
            }
        }

        private class PositionItem
        {
            public string Symbol { get; set; } = "";
            public string DisplayText { get; set; } = "";
            public override string ToString() => DisplayText;
        }
    }
}
