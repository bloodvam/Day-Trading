using System.Windows.Forms;
using ChartEngine.Interaction;

namespace ChartEngine.ChartControls
{
    /// <summary>
    /// ChartControl 的输入处理部分（partial）
    /// 负责连接鼠标和键盘事件到 ChartInputHandler
    /// </summary>
    public partial class ChartControl
    {
        /// <summary>
        /// 输入处理器
        /// </summary>
        private ChartInputHandler _inputHandler;

        /// <summary>
        /// 是否启用交互功能
        /// </summary>
        public bool EnableInteraction { get; set; } = true;

        /// <summary>
        /// 初始化输入处理器
        /// </summary>
        private void InitializeInputHandler()
        {
            _inputHandler = new ChartInputHandler(this);
        }

        /// <summary>
        /// 重写 OnMouseDown
        /// </summary>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (EnableInteraction && _inputHandler != null)
            {
                _inputHandler.OnMouseDown(e);
            }

            // 获得焦点以接收键盘事件
            this.Focus();
        }

        /// <summary>
        /// 重写 OnMouseMove
        /// </summary>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (EnableInteraction && _inputHandler != null)
            {
                _inputHandler.OnMouseMove(e);
            }
        }

        /// <summary>
        /// 重写 OnMouseUp
        /// </summary>
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            if (EnableInteraction && _inputHandler != null)
            {
                _inputHandler.OnMouseUp(e);
            }
        }

        /// <summary>
        /// 重写 OnMouseWheel
        /// </summary>
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);

            if (EnableInteraction && _inputHandler != null)
            {
                _inputHandler.OnMouseWheel(e);
            }
        }

        /// <summary>
        /// 重写 OnKeyDown
        /// </summary>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (EnableInteraction && _inputHandler != null)
            {
                _inputHandler.OnKeyDown(e);
            }
        }

        /// <summary>
        /// 重写 OnKeyUp
        /// </summary>
        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);

            if (EnableInteraction && _inputHandler != null)
            {
                _inputHandler.OnKeyUp(e);
            }
        }

        /// <summary>
        /// 重写 OnMouseEnter
        /// </summary>
        protected override void OnMouseEnter(System.EventArgs e)
        {
            base.OnMouseEnter(e);
            // 鼠标进入时可以显示十字光标
        }

        /// <summary>
        /// 重写 OnMouseLeave
        /// </summary>
        protected override void OnMouseLeave(System.EventArgs e)
        {
            base.OnMouseLeave(e);
            // 鼠标离开时可以隐藏十字光标
        }
    }
}