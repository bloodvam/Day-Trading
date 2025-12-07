using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ChartEngine.Data.Models;

namespace ChartEngine.Data.Loading
{
    /// <summary>
    /// 数据预处理结果
    /// </summary>
    public class PreProcessedData
    {
        public ISeries Series { get; set; }
        public double MinPrice { get; set; }
        public double MaxPrice { get; set; }
        public double MaxVolume { get; set; }
        public double TotalVolume { get; set; }
        public double AvgPrice { get; set; }
        public Dictionary<int, (double Min, double Max)> RangeCache { get; set; }
        public bool IsReady { get; set; }
    }

    /// <summary>
    /// 数据预处理器 - 后台计算价格区间、成交量等
    /// </summary>
    public class DataPreProcessor
    {
        /// <summary>
        /// 预处理数据（在后台线程执行）
        /// </summary>
        public async Task<PreProcessedData> PreProcessAsync(
            ISeries series,
            IProgress<DataLoadProgress> progress = null,
            CancellationToken cancellationToken = default)
        {
            if (series == null || series.Count == 0)
                throw new ArgumentException("Series is null or empty");

            var result = new PreProcessedData
            {
                Series = series,
                RangeCache = new Dictionary<int, (double, double)>()
            };

            await Task.Run(() =>
            {
                // 阶段1: 计算全局价格区间
                ReportProgress(progress, 0, 4, DataLoadStage.CalculatingRanges, "计算价格区间...");
                cancellationToken.ThrowIfCancellationRequested();

                CalculateGlobalRange(series, result);

                // 阶段2: 计算成交量统计
                ReportProgress(progress, 1, 4, DataLoadStage.CalculatingRanges, "计算成交量统计...");
                cancellationToken.ThrowIfCancellationRequested();

                CalculateVolumeStats(series, result);

                // 阶段3: 构建区间缓存（分段预计算）
                ReportProgress(progress, 2, 4, DataLoadStage.BuildingCache, "构建区间缓存...");
                cancellationToken.ThrowIfCancellationRequested();

                BuildRangeCache(series, result, progress, cancellationToken);

                // 阶段4: 完成
                ReportProgress(progress, 4, 4, DataLoadStage.Completed, "预处理完成");
                result.IsReady = true;

            }, cancellationToken);

            return result;
        }

        /// <summary>
        /// 计算全局价格区间
        /// </summary>
        private void CalculateGlobalRange(ISeries series, PreProcessedData result)
        {
            double minPrice = double.MaxValue;
            double maxPrice = double.MinValue;
            double sumPrice = 0;

            foreach (var bar in series.Bars)
            {
                if (bar.Low < minPrice) minPrice = bar.Low;
                if (bar.High > maxPrice) maxPrice = bar.High;
                sumPrice += (bar.High + bar.Low + bar.Close) / 3.0;
            }

            result.MinPrice = minPrice;
            result.MaxPrice = maxPrice;
            result.AvgPrice = sumPrice / series.Count;
        }

        /// <summary>
        /// 计算成交量统计
        /// </summary>
        private void CalculateVolumeStats(ISeries series, PreProcessedData result)
        {
            double maxVolume = 0;
            double totalVolume = 0;

            foreach (var bar in series.Bars)
            {
                if (bar.Volume > maxVolume)
                    maxVolume = bar.Volume;
                totalVolume += bar.Volume;
            }

            result.MaxVolume = maxVolume;
            result.TotalVolume = totalVolume;
        }

        /// <summary>
        /// 构建区间缓存（分段预计算常见区间）
        /// </summary>
        private void BuildRangeCache(
            ISeries series,
            PreProcessedData result,
            IProgress<DataLoadProgress> progress,
            CancellationToken cancellationToken)
        {
            int count = series.Count;
            int[] commonRangeSizes = { 50, 100, 200, 500, 1000 };

            int totalSteps = 0;
            foreach (int size in commonRangeSizes)
            {
                if (size < count)
                    totalSteps += (count - size) / (size / 2); // 50%重叠
            }

            int currentStep = 0;

            // 为常见区间大小预计算价格范围
            foreach (int rangeSize in commonRangeSizes)
            {
                if (rangeSize >= count)
                    continue;

                int step = Math.Max(1, rangeSize / 2); // 50% 重叠

                for (int start = 0; start < count - rangeSize; start += step)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    int end = start + rangeSize - 1;
                    var (min, max) = CalculateRangeMinMax(series.Bars, start, end);

                    int cacheKey = HashRangeKey(start, end);
                    result.RangeCache[cacheKey] = (min, max);

                    currentStep++;
                    if (currentStep % 10 == 0)
                    {
                        ReportProgress(
                            progress,
                            currentStep,
                            totalSteps,
                            DataLoadStage.BuildingCache,
                            $"构建缓存 {currentStep}/{totalSteps}...");
                    }
                }
            }
        }

        /// <summary>
        /// 计算指定区间的最小最大价格
        /// </summary>
        private (double min, double max) CalculateRangeMinMax(
            IReadOnlyList<IBar> bars,
            int start,
            int end)
        {
            double min = double.MaxValue;
            double max = double.MinValue;

            for (int i = start; i <= end && i < bars.Count; i++)
            {
                if (bars[i].Low < min) min = bars[i].Low;
                if (bars[i].High > max) max = bars[i].High;
            }

            return (min, max);
        }

        /// <summary>
        /// 生成区间缓存键
        /// </summary>
        private int HashRangeKey(int start, int end)
        {
            // 简单的哈希函数
            return start << 16 | end & 0xFFFF;
        }

        /// <summary>
        /// 报告进度
        /// </summary>
        private void ReportProgress(
            IProgress<DataLoadProgress> progress,
            int current,
            int total,
            DataLoadStage stage,
            string message)
        {
            progress?.Report(new DataLoadProgress
            {
                CurrentIndex = current,
                TotalCount = total,
                Stage = stage,
                StatusMessage = message
            });
        }
    }
}