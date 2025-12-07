using System;
using System.Threading;
using System.Threading.Tasks;
using ChartEngine.Models;

namespace ChartEngine.DataLoading
{
    /// <summary>
    /// 数据加载进度报告
    /// </summary>
    public class DataLoadProgress
    {
        public int CurrentIndex { get; set; }
        public int TotalCount { get; set; }
        public double PercentComplete => TotalCount > 0 ? (CurrentIndex * 100.0 / TotalCount) : 0;
        public string StatusMessage { get; set; }
        public DataLoadStage Stage { get; set; }
    }

    /// <summary>
    /// 数据加载阶段
    /// </summary>
    public enum DataLoadStage
    {
        Initializing,
        LoadingData,
        CalculatingRanges,
        CalculatingIndicators,
        BuildingCache,
        Finalizing,
        Completed
    }

    /// <summary>
    /// 异步数据加载器接口
    /// </summary>
    public interface IAsyncDataLoader
    {
        /// <summary>
        /// 异步加载数据
        /// </summary>
        Task<ISeries> LoadAsync(
            string symbol,
            TimeFrame timeFrame,
            DateTime? startDate = null,
            DateTime? endDate = null,
            IProgress<DataLoadProgress> progress = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 取消当前加载
        /// </summary>
        void CancelLoad();
    }
}