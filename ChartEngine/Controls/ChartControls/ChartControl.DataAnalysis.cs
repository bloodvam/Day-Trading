using System;
using System.Collections.Generic;
using ChartEngine.Models;

namespace ChartEngine.ChartControls
{
    /// <summary>
    /// ChartControl 的数据分析部分（partial）
    /// 负责计算可视区间、价格区间、成交量统计等数据分析功能
    /// </summary>
    public partial class ChartControl
    {
        /// <summary>
        /// 自动更新可视区间 & 价格区间
        /// 在数据变化、缩放、平移后调用
        /// </summary>
        private void UpdateAutoRanges()
        {
            if (_series == null || _series.Count == 0)
                return;

            var bars = _series.Bars;
            var visible = _transform.VisibleRange;

            // 1. 确保 VisibleRange 已初始化
            if (visible.Count <= 0)
            {
                _transform.SetVisibleRange(0, _series.Count - 1);
                visible = _transform.VisibleRange;
            }

            // 2. 获取有效的索引范围
            int start = Math.Max(0, visible.StartIndex);
            int end = Math.Min(_series.Count - 1, visible.EndIndex);

            if (end < start)
                return;

            // 3. 计算价格区间
            var priceRange = CalculatePriceRange(bars, start, end);
            _transform.SetPriceRange(priceRange.Min, priceRange.Max);

            // 4. 计算成交量最大值
            double maxVol = CalculateMaxVolume(bars, start, end);
            _transform.SetMaxVolume(maxVol);
        }

        /// <summary>
        /// 计算指定区间的价格范围（带 padding）
        /// </summary>
        /// <param name="bars">K线数据</param>
        /// <param name="startIndex">起始索引</param>
        /// <param name="endIndex">结束索引</param>
        /// <returns>最小价格和最大价格（已加 padding）</returns>
        private (double Min, double Max) CalculatePriceRange(
            IReadOnlyList<IBar> bars,
            int startIndex,
            int endIndex)
        {
            if (bars == null || bars.Count == 0)
                return (0, 100);

            double minPrice = double.MaxValue;
            double maxPrice = double.MinValue;

            // 遍历可视区间找最高最低价
            for (int i = startIndex; i <= endIndex && i < bars.Count; i++)
            {
                var bar = bars[i];
                if (bar.Low < minPrice) minPrice = bar.Low;
                if (bar.High > maxPrice) maxPrice = bar.High;
            }

            // 如果没有有效数据
            if (minPrice >= double.MaxValue || maxPrice <= double.MinValue)
                return (0, 100);

            // 加上下 padding，避免 K 线贴边
            double range = maxPrice - minPrice;
            double padding = range * 0.05; // 上下各留 5%

            if (padding <= 0)
                padding = Math.Abs(maxPrice) * 0.05; // 如果价格区间为0，用价格的5%

            if (padding <= 0)
                padding = 1; // 最小 padding

            return (minPrice - padding, maxPrice + padding);
        }

        /// <summary>
        /// 计算指定区间的最大成交量
        /// </summary>
        /// <param name="bars">K线数据</param>
        /// <param name="startIndex">起始索引</param>
        /// <param name="endIndex">结束索引</param>
        /// <returns>最大成交量</returns>
        private double CalculateMaxVolume(
            IReadOnlyList<IBar> bars,
            int startIndex,
            int endIndex)
        {
            if (bars == null || bars.Count == 0)
                return 1;

            double maxVol = 0;

            for (int i = startIndex; i <= endIndex && i < bars.Count; i++)
            {
                if (bars[i].Volume > maxVol)
                    maxVol = bars[i].Volume;
            }

            return maxVol <= 0 ? 1 : maxVol;
        }

        /// <summary>
        /// 从 Transform 中读取当前可视区间的最大成交量
        /// （用于渲染时的快速访问）
        /// </summary>
        private double ComputeMaxVolumeInVisibleRange()
        {
            if (_series == null || _series.Count == 0)
                return 1;

            var visible = _transform.VisibleRange;
            int start = Math.Max(0, visible.StartIndex);
            int end = Math.Min(_series.Count - 1, visible.EndIndex);

            return CalculateMaxVolume(_series.Bars, start, end);
        }

        /// <summary>
        /// 计算指定区间的平均价格
        /// </summary>
        private double CalculateAveragePrice(
            IReadOnlyList<IBar> bars,
            int startIndex,
            int endIndex)
        {
            if (bars == null || bars.Count == 0)
                return 0;

            double sum = 0;
            int count = 0;

            for (int i = startIndex; i <= endIndex && i < bars.Count; i++)
            {
                sum += (bars[i].High + bars[i].Low + bars[i].Close) / 3.0;
                count++;
            }

            return count > 0 ? sum / count : 0;
        }

        /// <summary>
        /// 计算指定区间的总成交量
        /// </summary>
        private double CalculateTotalVolume(
            IReadOnlyList<IBar> bars,
            int startIndex,
            int endIndex)
        {
            if (bars == null || bars.Count == 0)
                return 0;

            double total = 0;

            for (int i = startIndex; i <= endIndex && i < bars.Count; i++)
            {
                total += bars[i].Volume;
            }

            return total;
        }

        /// <summary>
        /// 获取可视区间的统计信息
        /// </summary>
        public VisibleRangeStatistics GetVisibleRangeStatistics()
        {
            if (_series == null || _series.Count == 0)
                return new VisibleRangeStatistics();

            var visible = _transform.VisibleRange;
            int start = Math.Max(0, visible.StartIndex);
            int end = Math.Min(_series.Count - 1, visible.EndIndex);

            var priceRange = CalculatePriceRange(_series.Bars, start, end);

            return new VisibleRangeStatistics
            {
                StartIndex = start,
                EndIndex = end,
                Count = end - start + 1,
                MinPrice = priceRange.Min,
                MaxPrice = priceRange.Max,
                AveragePrice = CalculateAveragePrice(_series.Bars, start, end),
                MaxVolume = CalculateMaxVolume(_series.Bars, start, end),
                TotalVolume = CalculateTotalVolume(_series.Bars, start, end)
            };
        }
    }

    /// <summary>
    /// 可视区间统计信息
    /// </summary>
    public class VisibleRangeStatistics
    {
        public int StartIndex { get; set; }
        public int EndIndex { get; set; }
        public int Count { get; set; }
        public double MinPrice { get; set; }
        public double MaxPrice { get; set; }
        public double AveragePrice { get; set; }
        public double MaxVolume { get; set; }
        public double TotalVolume { get; set; }
    }
}