// ChartEngine/Rendering/Layers/GridLayer.cs
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using ChartEngine.Interfaces;
using ChartEngine.Rendering;
using ChartEngine.Styles.Core;

namespace ChartEngine.Rendering.Layers
{
    /// <summary>
    /// 网格线图层
    /// 绘制横向和纵向网格线,提供价格和时间参考
    /// </summary>
    public class GridLayer : IChartLayer
    {
        public string Name => "Grid";
        public bool IsVisible { get; set; } = true;
        public int ZOrder { get; set; } = 1;

        private GridStyle _style;
        private readonly RenderResourcePool _resourcePool;  // 🔥 新增

        // 🔥 修改构造函数
        public GridLayer(GridStyle style, RenderResourcePool resourcePool)
        {
            _style = style ?? GridStyle.GetDarkThemeDefault();
            _resourcePool = resourcePool ?? new RenderResourcePool();
        }

        public void UpdateStyle(GridStyle style)
        {
            _style = style ?? throw new ArgumentNullException(nameof(style));
        }

        public void Render(ChartRenderContext ctx)
        {
            if (!IsVisible)
                return;

            var g = ctx.Graphics;
            var priceArea = ctx.PriceArea;
            var volumeArea = ctx.VolumeArea;

            int top = Math.Min(priceArea.Top, volumeArea.Top);
            int bottom = Math.Max(priceArea.Bottom, volumeArea.Bottom);
            int left = priceArea.Left;
            int right = priceArea.Right;

            Rectangle fullArea = new Rectangle(left, top, right - left, bottom - top);

            if (_style.ShowHorizontalLines)
            {
                DrawHorizontalGridLines(g, ctx, fullArea);
            }

            if (_style.ShowVerticalLines)
            {
                DrawVerticalGridLines(g, ctx, fullArea);
            }
        }

        private void DrawHorizontalGridLines(Graphics g, ChartRenderContext ctx, Rectangle area)
        {
            var priceRange = ctx.PriceRange;
            if (!priceRange.IsValid)
                return;

            double minPrice = priceRange.MinPrice;
            double maxPrice = priceRange.MaxPrice;
            double range = maxPrice - minPrice;

            double majorInterval = CalculateSmartInterval(range);
            double minorInterval = majorInterval / 2.0;

            double startPrice = Math.Floor(minPrice / majorInterval) * majorInterval;

            // 🔥 使用 ResourcePool 获取画笔
            var majorPen = _resourcePool.GetStyledPen(
                _style.MajorLineColor,
                _style.MajorLineWidth,
                _style.LineStyle);

            for (double price = startPrice; price <= maxPrice; price += majorInterval)
            {
                if (price < minPrice)
                    continue;

                float y = ctx.Transform.PriceToY(price, ctx.PriceArea);

                if (y >= area.Top && y <= area.Bottom)
                {
                    g.DrawLine(majorPen, area.Left, y, area.Right, y);
                }
            }

            if (_style.ShowMinorLines)
            {
                // 🔥 使用 ResourcePool 获取画笔
                var minorPen = _resourcePool.GetStyledPen(
                    _style.MinorLineColor,
                    _style.MinorLineWidth,
                    _style.LineStyle);

                for (double price = startPrice; price <= maxPrice; price += minorInterval)
                {
                    if (Math.Abs(price % majorInterval) < 1e-10)
                        continue;

                    if (price < minPrice)
                        continue;

                    float y = ctx.Transform.PriceToY(price, ctx.PriceArea);

                    if (y >= area.Top && y <= area.Bottom)
                    {
                        g.DrawLine(minorPen, area.Left, y, area.Right, y);
                    }
                }
            }
        }

        private void DrawVerticalGridLines(Graphics g, ChartRenderContext ctx, Rectangle area)
        {
            var visibleRange = ctx.VisibleRange;
            if (visibleRange.Count <= 0)
                return;

            int barInterval = CalculateBarInterval(visibleRange.Count);

            int startIndex = (visibleRange.StartIndex / barInterval) * barInterval;
            if (startIndex < visibleRange.StartIndex)
                startIndex += barInterval;

            // 🔥 使用 ResourcePool 获取画笔
            var pen = _resourcePool.GetStyledPen(
                _style.MajorLineColor,
                _style.MajorLineWidth,
                _style.LineStyle);

            for (int index = startIndex; index <= visibleRange.EndIndex; index += barInterval)
            {
                float x = ctx.Transform.IndexToX(index, ctx.PriceArea);

                if (x >= area.Left && x <= area.Right)
                {
                    g.DrawLine(pen, x, area.Top, x, area.Bottom);
                }
            }
        }

        private double CalculateSmartInterval(double range)
        {
            if (range <= 0)
                return 1;

            double magnitude = Math.Pow(10, Math.Floor(Math.Log10(range)));
            double normalized = range / magnitude;

            double multiplier;
            if (normalized <= 1.5)
                multiplier = 0.2;
            else if (normalized <= 3)
                multiplier = 0.5;
            else if (normalized <= 7)
                multiplier = 1;
            else
                multiplier = 2;

            double interval = multiplier * magnitude;

            int estimatedLines = (int)(range / interval);

            if (estimatedLines < 4)
                interval /= 2;

            if (estimatedLines > 15)
                interval *= 2;

            return interval;
        }

        private int CalculateBarInterval(int visibleCount)
        {
            if (visibleCount <= 0)
                return 1;

            int targetLines = 8;
            int baseInterval = Math.Max(1, visibleCount / targetLines);

            int[] niceNumbers = { 1, 2, 5, 10, 20, 50, 100, 200, 500, 1000 };

            foreach (int num in niceNumbers)
            {
                if (num >= baseInterval)
                    return num;
            }

            return baseInterval;
        }

        public GridStyle GetStyle()
        {
            return _style;
        }
    }
}