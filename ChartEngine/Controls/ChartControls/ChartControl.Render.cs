using System.Drawing;
using ChartEngine.Rendering;
using ChartEngine.Transforms;
using ChartEngine.Interfaces;

namespace ChartEngine.ChartControls
{
    /// <summary>
    /// ChartControl 的渲染相关逻辑（partial）
    /// 负责协调整个渲染流程
    /// </summary>
    public partial class ChartControl
    {
        /// <summary>
        /// 坐标转换引擎
        /// </summary>
        private IChartTransform _transform;

        /// <summary>
        /// 对外只读的 Transform（用于外部查看当前可视范围等信息）
        /// </summary>
        public IChartTransform Transform => _transform;

        /// <summary>
        /// 初始化 Transform
        /// </summary>
        private void InitializeTransform()
        {
            _transform = new ChartTransform();
        }

        /// <summary>
        /// 渲染所有图层
        /// 这是整个渲染流程的总控制方法
        /// </summary>
        /// <param name="g">GDI+ Graphics 对象</param>
        internal void RenderAll(Graphics g)
        {
            // 1. 计算布局（使用缓存机制，性能优化）
            CalculateLayout(out Rectangle priceArea, out Rectangle volumeArea);

            // 2. 更新 Transform 的布局信息
            _transform.UpdateLayout(priceArea, volumeArea);

            // 3. 自动更新可视区间和价格区间
            UpdateAutoRanges();

            // 4. 计算成交量最大值
            double maxVolume = ComputeMaxVolumeInVisibleRange();

            // 5. 构造渲染上下文（传递给所有 Layer）
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

            // 6. 依次渲染每一个 Layer（按 ZOrder 排序）
            foreach (var layer in _layers)
            {
                if (layer.IsVisible)
                {
                    layer.Render(ctx);
                }
            }
        }

        /// <summary>
        /// 使用脏区域优化的渲染（未来可以实现）
        /// </summary>
        /// <param name="g">GDI+ Graphics 对象</param>
        /// <param name="clipRect">需要重绘的区域</param>
        internal void RenderAll(Graphics g, Rectangle clipRect)
        {
            // TODO: 未来可以根据 clipRect 只渲染需要更新的部分
            // 当前先调用完整渲染
            RenderAll(g);
        }

        /// <summary>
        /// 设置 Graphics 的渲染质量
        /// </summary>
        private void ConfigureGraphicsQuality(Graphics g, bool highQuality = true)
        {
            if (highQuality)
            {
                // 高质量模式（适合静态显示）
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            }
            else
            {
                // 高性能模式（适合实时刷新）
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighSpeed;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SystemDefault;
                g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Low;
            }
        }
    }
}