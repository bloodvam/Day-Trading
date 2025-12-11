using TradingEngine.Core;

namespace TradingEngine.UI
{
    /// <summary>
    /// Panel 基类
    /// </summary>
    public class BasePanel : UserControl
    {
        protected TradingController Controller { get; }

        public BasePanel(TradingController controller)
        {
            Controller = controller;
            this.Dock = DockStyle.Top;
            this.Padding = new Padding(5);
        }

        protected void InvokeUI(Action action)
        {
            if (InvokeRequired)
                BeginInvoke(action);
            else
                action();
        }
    }
}
