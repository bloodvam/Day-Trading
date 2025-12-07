using System;
using System.Drawing;
using System.Windows.Forms;
using ChartEngine.Interaction;
using ChartEngine.Rendering.Layers;
using ChartEngine.Rendering;

namespace ChartEngine.ChartControls
{
    /// <summary>
    /// ChartControl 的输入处理部分（partial）
    /// 连接 WinForms 事件到 InputHandler 和 CrosshairLayer
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

            // 通知 CrosshairLayer
            NotifyCrosshairMouseMove(e);
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

            // 通知 CrosshairLayer 鼠标离开
            NotifyCrosshairMouseLeave();
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

        /// <summary>
        /// 通知 CrosshairLayer 鼠标移动
        /// </summary>
        private void NotifyCrosshairMouseMove(MouseEventArgs e)
        {
            var crosshairLayer = GetLayer<CrosshairLayer>();
            if (crosshairLayer != null && crosshairLayer.IsVisible)
            {
                // 需要传递 ChartRenderContext
                // 由于这里没有 Graphics,我们创建一个临时的
                using (var g = CreateGraphics())
                {
                    CalculateLayout(out Rectangle priceArea, out Rectangle volumeArea);
                    _transform.UpdateLayout(priceArea, volumeArea);
                    UpdateAutoRanges();

                    double maxVolume = ComputeMaxVolumeInVisibleRange();

                    var ctx = new ChartRenderContext(
                        _transform,
                        _series,
                        priceArea,
                        volumeArea,
                        maxVolume,
                        g,
                        CandleStyle,
                        VolumeStyle
                    );

                    crosshairLayer.OnMouseMove(e.Location, ctx);
                }

                Invalidate();
            }
        }

        /// <summary>
        /// 通知 CrosshairLayer 鼠标离开
        /// </summary>
        private void NotifyCrosshairMouseLeave()
        {
            var crosshairLayer = GetLayer<CrosshairLayer>();
            if (crosshairLayer != null)
            {
                crosshairLayer.OnMouseLeave();
                Invalidate();
            }
        }
    }
}