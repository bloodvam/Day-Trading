using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using ChartEngine.Interfaces;
using ChartEngine.Rendering;
using ChartEngine.Styles;

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
        public int ZOrder { get; set; } = 1; // 在背景之上,K线之下

        private GridStyle _style;

        public GridLayer(GridStyle style = null)
        {
            _style = style ?? GridStyle.GetDarkThemeDefault();
        }

        /// <summary>
        /// 更新网格样式
        /// </summary>
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

            // 计算完整绘图区域 (主图 + 成交量)
            int top = Math.Min(priceArea.Top, volumeArea.Top);
            int bottom = Math.Max(priceArea.Bottom, volumeArea.Bottom);
            int left = priceArea.Left;
            int right = priceArea.Right;

            Rectangle fullArea = new Rectangle(left, top, right - left, bottom - top);

            // 绘制横向网格线 (价格)
            if (_style.ShowHorizontalLines)
            {
                DrawHorizontalGridLines(g, ctx, fullArea);
            }

            // 绘制纵向网格线 (时间)
            if (_style.ShowVerticalLines)
            {
                DrawVerticalGridLines(g, ctx, fullArea);
            }
        }

        /// <summary>
        /// 绘制横向网格线 (价格网格)
        /// </summary>
        private void DrawHorizontalGridLines(Graphics g, ChartRenderContext ctx, Rectangle area)
        {
            var priceRange = ctx.PriceRange;
            if (!priceRange.IsValid)
                return;

            double minPrice = priceRange.MinPrice;
            double maxPrice = priceRange.MaxPrice;
            double range = maxPrice - minPrice;

            // 计算主网格间距
            double majorInterval = CalculateSmartInterval(range);

            // 次网格间距 (主网格的一半)
            double minorInterval = majorInterval / 2.0;

            // 计算起始价格 (向下取整到主网格)
            double startPrice = Math.Floor(minPrice / majorInterval) * majorInterval;

            // 绘制主网格线
            using (var majorPen = new Pen(_style.MajorLineColor, _style.MajorLineWidth))
            {
                majorPen.DashStyle = _style.LineStyle;

                for (double price = startPrice; price <= maxPrice; price += majorInterval)
                {
                    if (price < minPrice)
                        continue;

                    float y = ctx.Transform.PriceToY(price, ctx.PriceArea);

                    // 确保在绘图区域内
                    if (y >= area.Top && y <= area.Bottom)
                    {
                        g.DrawLine(majorPen, area.Left, y, area.Right, y);
                    }
                }
            }

            // 绘制次网格线
            if (_style.ShowMinorLines)
            {
                using (var minorPen = new Pen(_style.MinorLineColor, _style.MinorLineWidth))
                {
                    minorPen.DashStyle = _style.LineStyle;

                    for (double price = startPrice; price <= maxPrice; price += minorInterval)
                    {
                        // 跳过主网格线位置
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
        }

        /// <summary>
        /// 绘制纵向网格线 (时间网格)
        /// </summary>
        private void DrawVerticalGridLines(Graphics g, ChartRenderContext ctx, Rectangle area)
        {
            var visibleRange = ctx.VisibleRange;
            if (visibleRange.Count <= 0)
                return;

            // 计算合适的时间间隔 (K线索引间隔)
            int barInterval = CalculateBarInterval(visibleRange.Count);

            // 计算起始索引 (对齐到间隔的倍数)
            int startIndex = (visibleRange.StartIndex / barInterval) * barInterval;
            if (startIndex < visibleRange.StartIndex)
                startIndex += barInterval;

            using (var pen = new Pen(_style.MajorLineColor, _style.MajorLineWidth))
            {
                pen.DashStyle = _style.LineStyle;

                for (int index = startIndex; index <= visibleRange.EndIndex; index += barInterval)
                {
                    float x = ctx.Transform.IndexToX(index, ctx.PriceArea);

                    // 确保在绘图区域内
                    if (x >= area.Left && x <= area.Right)
                    {
                        g.DrawLine(pen, x, area.Top, x, area.Bottom);
                    }
                }
            }
        }

        /// <summary>
        /// 智能计算网格间距
        /// 根据价格范围自动选择合适的间距,确保网格线数量适中
        /// </summary>
        private double CalculateSmartInterval(double range)
        {
            if (range <= 0)
                return 1;

            // 1. 计算数量级 (10的幂次)
            double magnitude = Math.Pow(10, Math.Floor(Math.Log10(range)));

            // 2. 归一化到 1-10 之间
            double normalized = range / magnitude;

            // 3. 根据归一化值选择合适的间距倍数
            double multiplier;
            if (normalized <= 1.5)
                multiplier = 0.2;   // 间距 = 0.2, 0.02, 0.002...
            else if (normalized <= 3)
                multiplier = 0.5;   // 间距 = 0.5, 0.05, 0.005...
            else if (normalized <= 7)
                multiplier = 1;     // 间距 = 1, 0.1, 0.01...
            else
                multiplier = 2;     // 间距 = 2, 0.2, 0.02...

            double interval = multiplier * magnitude;

            // 4. 确保间距合理 (不会产生过多或过少的线)
            int estimatedLines = (int)(range / interval);

            // 如果线条太少,减小间距
            if (estimatedLines < 4)
                interval /= 2;

            // 如果线条太多,增大间距
            if (estimatedLines > 15)
                interval *= 2;

            return interval;
        }

        /// <summary>
        /// 计算纵向网格线的 K 线间隔
        /// </summary>
        private int CalculateBarInterval(int visibleCount)
        {
            if (visibleCount <= 0)
                return 1;

            // 目标: 显示 6-10 条纵线
            int targetLines = 8;

            // 计算基础间隔
            int baseInterval = Math.Max(1, visibleCount / targetLines);

            // 调整到合适的数字 (5, 10, 20, 50, 100...)
            int[] niceNumbers = { 1, 2, 5, 10, 20, 50, 100, 200, 500, 1000 };

            foreach (int num in niceNumbers)
            {
                if (num >= baseInterval)
                    return num;
            }

            // 如果都不满足,返回基础间隔
            return baseInterval;
        }

        /// <summary>
        /// 获取当前网格样式
        /// </summary>
        public GridStyle GetStyle()
        {
            return _style;
        }
    }
}