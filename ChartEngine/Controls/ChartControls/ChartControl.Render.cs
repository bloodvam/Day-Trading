using System;
using System.Drawing;
using ChartEngine.Rendering;
using ChartEngine.Transforms;
using ChartEngine.Models;
using ChartEngine.Interfaces;

namespace ChartEngine.ChartControls
{
    /// <summary>
    /// ChartControl 的渲染相关逻辑（partial）。
    /// </summary>
    public partial class ChartControl
    {
        /// <summary>
        /// 负责 index/price ↔ 像素坐标的变换。
        /// </summary>
        private IChartTransform _transform;

        /// <summary>
        /// 对外只读的 Transform（如果你需要在外部查看当前可视范围）。
        /// </summary>
        public IChartTransform Transform => _transform;

        /// <summary>
        /// 初始化 Transform。
        /// </summary>
        private void InitializeTransform()
        {
            _transform = new ChartTransform();
        }

        /// <summary>
        /// 计算布局并渲染所有图层。
        /// </summary>
        internal void RenderAll(Graphics g)
        {
            Console.WriteLine($"VisibleRange: {Transform.VisibleRange.StartIndex} - {Transform.VisibleRange.EndIndex}");
            System.Diagnostics.Debug.WriteLine($"=== RenderAll 开始 ===");
            // 1. 布局：简单版，后续可以拆到 ChartControl.Layout.cs 里
            CalculateLayout(out Rectangle priceArea, out Rectangle volumeArea);
            System.Diagnostics.Debug.WriteLine($"PriceArea: X={priceArea.X}, Y={priceArea.Y}, W={priceArea.Width}, H={priceArea.Height}");
            System.Diagnostics.Debug.WriteLine($"VolumeArea: X={volumeArea.X}, Y={volumeArea.Y}, W={volumeArea.Width}, H={volumeArea.Height}");
            _transform.UpdateLayout(priceArea, volumeArea);
            System.Diagnostics.Debug.WriteLine($"UpdateLayout 前 - Visible Range: {_transform.VisibleRange.StartIndex} - {_transform.VisibleRange.EndIndex}, Count={_transform.VisibleRange.Count}");
            // 2. 自动更新可视区/价格区/Volume 最大值
            UpdateAutoRanges();
            System.Diagnostics.Debug.WriteLine($"UpdateLayout 后 - Visible Range: {_transform.VisibleRange.StartIndex} - {_transform.VisibleRange.EndIndex}, Count={_transform.VisibleRange.Count}");
            System.Diagnostics.Debug.WriteLine($"Price Range: {_transform.PriceRange.MinPrice} - {_transform.PriceRange.MaxPrice}");

            double maxVolume = ComputeMaxVolumeInVisibleRange();

            // 3. 构造渲染上下文
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

            // 4. 依次渲染每一个 Layer
            foreach (var layer in _layers)
            {
                if (layer.IsVisible)
                {
                    layer.Render(ctx);
                }
            }
        }

        /// <summary>
        /// 简单计算主图区域和成交量区域的布局。
        /// 后续你可以把它迁移到 ChartControl.Layout.cs 做更复杂布局。
        /// </summary>
        private void CalculateLayout(out Rectangle priceArea, out Rectangle volumeArea)
        {
            int left = 50;
            int rightPadding = 20;
            int top = 10;
            int volHeight = 70;
            int spacing = 10;

            int width = Math.Max(10, this.Width - left - rightPadding);
            int height = Math.Max(10, this.Height - top - volHeight - spacing - 10);

            priceArea = new Rectangle(left, top, width, height);
            volumeArea = new Rectangle(left, top + height + spacing, width, volHeight);
        }

        /// <summary>
        /// 自动更新可视区间 & 价格区间。
        /// </summary>
        private void UpdateAutoRanges()
        {
            if (_series == null || _series.Count == 0)
                return;

            var bars = _series.Bars;

            // 1. 如果还没设置 VisibleRange，就默认显示全部
            var visible = _transform.VisibleRange;
            if (visible.Count <= 0)
            {
                _transform.SetVisibleRange(0, _series.Count - 1);
                visible = _transform.VisibleRange;
                System.Diagnostics.Debug.WriteLine($"UpdateAutoRanges: 设置 VisibleRange 为 0 - {_series.Count - 1}");
            }
            System.Diagnostics.Debug.WriteLine($"UpdateAutoRanges: Visible Range = {visible.StartIndex} - {visible.EndIndex}, Count = {visible.Count}");


            int start = Math.Max(0, visible.StartIndex);
            int end = Math.Min(_series.Count - 1, visible.EndIndex);

            if (end < start) return;

            // 2. 自动计算当前可视区间内的价格高低
            double minPrice = double.MaxValue;
            double maxPrice = double.MinValue;

            for (int i = start; i <= end; i++)
            {
                var bar = bars[i];
                if (bar.Low < minPrice) minPrice = bar.Low;
                if (bar.High > maxPrice) maxPrice = bar.High;
            }

            if (minPrice < double.MaxValue && maxPrice > double.MinValue)
            {
                // 加一点 padding，让图形不贴边
                double padding = (maxPrice - minPrice) * 0.05;
                if (padding <= 0) padding = 1;

                _transform.SetPriceRange(minPrice - padding, maxPrice + padding);
                System.Diagnostics.Debug.WriteLine($"UpdateAutoRanges: Price Range = {minPrice - padding} - {maxPrice + padding}");
            }

            // 3. 自动计算 volume 最大值（放到 MaxVolume）
            double maxVol = 0;
            for (int i = start; i <= end; i++)
            {
                var bar = bars[i];
                if (bar.Volume > maxVol) maxVol = bar.Volume;
            }

            _transform.SetMaxVolume(maxVol <= 0 ? 1 : maxVol);
        }

        /// <summary>
        /// 从 Transform 中读取目前可视区间的最大成交量（如果 Transform 没存，就按 Series 再算一遍）。
        /// </summary>
        private double ComputeMaxVolumeInVisibleRange()
        {
            if (_series == null || _series.Count == 0)
                return 1;

            var bars = _series.Bars;
            var visible = _transform.VisibleRange;

            int start = Math.Max(0, visible.StartIndex);
            int end = Math.Min(_series.Count - 1, visible.EndIndex);

            double maxVol = 0;
            for (int i = start; i <= end; i++)
            {
                var bar = bars[i];
                if (bar.Volume > maxVol) maxVol = bar.Volume;
            }

            return maxVol <= 0 ? 1 : maxVol;
        }
    }
}
