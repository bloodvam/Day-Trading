// ChartEngine/Controls/ChartControls/ChartControl.Input.cs
using System;
using System.Drawing;
using System.Windows.Forms;
using ChartEngine.Interaction;

namespace ChartEngine.Controls.ChartControls
{
    /// <summary>
    /// ChartControl 的输入处理部分（partial）
    /// 只负责接收 WinForms 事件并转发给 InputHandler
    /// 不包含任何业务逻辑
    /// </summary>
    public partial class ChartControl
    {
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

        // ========================================
        // WinForms 事件接收 - 纯转发
        // ========================================

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (EnableInteraction)
            {
                _inputHandler?.OnMouseDown(e);
            }

            // 获取焦点以接收键盘事件
            if (!Focused)
                Focus();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (EnableInteraction)
            {
                _inputHandler?.OnMouseMove(e);
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            if (EnableInteraction)
            {
                _inputHandler?.OnMouseUp(e);
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);

            if (EnableInteraction)
            {
                _inputHandler?.OnMouseWheel(e);
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);

            if (EnableInteraction)
            {
                _inputHandler?.OnMouseLeave();
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (EnableInteraction)
            {
                _inputHandler?.OnKeyDown(e);
            }
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);

            if (EnableInteraction)
            {
                _inputHandler?.OnKeyUp(e);
            }
        }
    }
}