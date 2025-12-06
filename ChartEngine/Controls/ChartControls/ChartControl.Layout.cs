using System;
using System.Drawing;
using ChartEngine.Config;

namespace ChartEngine.ChartControls
{
    /// <summary>
    /// ChartControl 的布局计算部分（partial）
    /// 负责计算主图区域、成交量区域等的位置和尺寸
    /// </summary>
    public partial class ChartControl
    {
        /// <summary>
        /// 布局配置（可外部修改）
        /// </summary>
        public ChartLayoutConfig LayoutConfig { get; set; } = new ChartLayoutConfig();

        /// <summary>
        /// 主图区域（缓存）
        /// </summary>
        private Rectangle _cachedPriceArea;

        /// <summary>
        /// 成交量区域（缓存）
        /// </summary>
        private Rectangle _cachedVolumeArea;

        /// <summary>
        /// 上一次计算布局时的控件尺寸
        /// </summary>
        private Size _lastLayoutSize;

        /// <summary>
        /// 布局是否有效（尺寸未变化）
        /// </summary>
        private bool IsLayoutValid => _lastLayoutSize == this.Size && _cachedPriceArea.Width > 0;

        /// <summary>
        /// 计算主图区域和成交量区域的布局
        /// 使用缓存机制提高性能
        /// </summary>
        private void CalculateLayout(out Rectangle priceArea, out Rectangle volumeArea)
        {
            // 如果尺寸没变且缓存有效，直接返回缓存
            if (IsLayoutValid)
            {
                priceArea = _cachedPriceArea;
                volumeArea = _cachedVolumeArea;
                return;
            }

            // 验证配置
            if (!LayoutConfig.IsValid())
            {
                throw new InvalidOperationException("ChartLayoutConfig 配置无效");
            }

            // 计算可用空间
            int left = LayoutConfig.LeftMargin;
            int right = LayoutConfig.RightMargin;
            int top = LayoutConfig.TopMargin;
            int bottom = LayoutConfig.BottomMargin;

            // 计算宽度和总高度
            int width = Math.Max(10, this.Width - left - right);
            int totalHeight = Math.Max(50, this.Height - top - bottom);

            if (LayoutConfig.ShowVolumeArea)
            {
                // 有成交量区域
                int volHeight = (int)(totalHeight * LayoutConfig.VolumeHeightRatio);
                volHeight = Math.Max(20, volHeight); // 最小高度 20

                int priceHeight = totalHeight - volHeight - LayoutConfig.Spacing;
                priceHeight = Math.Max(30, priceHeight); // 最小高度 30

                priceArea = new Rectangle(left, top, width, priceHeight);
                volumeArea = new Rectangle(
                    left,
                    top + priceHeight + LayoutConfig.Spacing,
                    width,
                    volHeight
                );
            }
            else
            {
                // 没有成交量区域，主图占全部
                priceArea = new Rectangle(left, top, width, totalHeight);
                volumeArea = Rectangle.Empty;
            }

            // 缓存结果
            _cachedPriceArea = priceArea;
            _cachedVolumeArea = volumeArea;
            _lastLayoutSize = this.Size;
        }

        /// <summary>
        /// 强制重新计算布局
        /// 当 LayoutConfig 被修改后，需要调用此方法
        /// </summary>
        public void InvalidateLayout()
        {
            _lastLayoutSize = Size.Empty; // 清除缓存
            Invalidate(); // 触发重绘
        }

        /// <summary>
        /// 获取当前主图区域（只读）
        /// </summary>
        public Rectangle GetPriceArea()
        {
            if (!IsLayoutValid)
            {
                CalculateLayout(out _, out _);
            }
            return _cachedPriceArea;
        }

        /// <summary>
        /// 获取当前成交量区域（只读）
        /// </summary>
        public Rectangle GetVolumeArea()
        {
            if (!IsLayoutValid)
            {
                CalculateLayout(out _, out _);
            }
            return _cachedVolumeArea;
        }

        /// <summary>
        /// 应用预设的布局配置
        /// </summary>
        public void ApplyLayoutPreset(ChartLayoutConfig preset)
        {
            if (preset == null)
                throw new ArgumentNullException(nameof(preset));

            if (!preset.IsValid())
                throw new ArgumentException("无效的布局配置", nameof(preset));

            LayoutConfig = preset;
            InvalidateLayout();
        }

        /// <summary>
        /// 切换成交量区域的显示/隐藏
        /// </summary>
        public void ToggleVolumeArea()
        {
            LayoutConfig.ShowVolumeArea = !LayoutConfig.ShowVolumeArea;
            InvalidateLayout();
        }
    }
}