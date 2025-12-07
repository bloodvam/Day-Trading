using System;
using System.Collections.Generic;
using System.Diagnostics;
using ChartEngine.Data.Models;

namespace ChartEngine.Data.Paging
{
    /// <summary>
    /// 分页管理器
    /// 负责协调分页加载、缓存策略和性能监控
    /// </summary>
    public class PageManager
    {
        private readonly IPagedSeries _pagedSeries;
        private int _lastVisibleStart = -1;
        private int _lastVisibleEnd = -1;
        private readonly Stopwatch _performanceTimer = new Stopwatch();

        // 性能统计
        private int _totalPageLoads = 0;
        private int _totalPageUnloads = 0;
        private long _totalLoadTimeMs = 0;

        public PageManager(IPagedSeries pagedSeries)
        {
            _pagedSeries = pagedSeries ?? throw new ArgumentNullException(nameof(pagedSeries));
        }

        /// <summary>
        /// 更新可视区域（自动管理页面加载/卸载）
        /// </summary>
        /// <param name="visibleStartIndex">可视起始索引</param>
        /// <param name="visibleEndIndex">可视结束索引</param>
        public void UpdateVisibleRange(int visibleStartIndex, int visibleEndIndex)
        {
            // 检查是否需要更新
            if (visibleStartIndex == _lastVisibleStart && visibleEndIndex == _lastVisibleEnd)
                return;

            _performanceTimer.Restart();

            // 预加载可视范围
            _pagedSeries.PreloadRange(visibleStartIndex, visibleEndIndex);

            // 卸载不可见的页面
            _pagedSeries.UnloadInvisiblePages(visibleStartIndex, visibleEndIndex);

            _performanceTimer.Stop();
            _totalLoadTimeMs += _performanceTimer.ElapsedMilliseconds;

            _lastVisibleStart = visibleStartIndex;
            _lastVisibleEnd = visibleEndIndex;
        }

        /// <summary>
        /// 预测性加载（根据滚动方向预加载）
        /// </summary>
        /// <param name="currentStart">当前起始索引</param>
        /// <param name="currentEnd">当前结束索引</param>
        /// <param name="previousStart">之前起始索引</param>
        /// <param name="previousEnd">之前结束索引</param>
        public void PredictiveLoad(
            int currentStart,
            int currentEnd,
            int previousStart,
            int previousEnd)
        {
            // 检测滚动方向
            bool scrollingRight = currentStart > previousStart;
            bool scrollingLeft = currentStart < previousStart;

            if (scrollingRight)
            {
                // 向右滚动，预加载右侧更多数据
                int extraLoad = (currentEnd - currentStart) / 2; // 预加载当前范围的50%
                _pagedSeries.PreloadRange(currentStart, currentEnd + extraLoad);
            }
            else if (scrollingLeft)
            {
                // 向左滚动，预加载左侧更多数据
                int extraLoad = (currentEnd - currentStart) / 2;
                _pagedSeries.PreloadRange(currentStart - extraLoad, currentEnd);
            }
        }

        /// <summary>
        /// 获取指定范围内的K线（智能加载）
        /// </summary>
        public IEnumerable<IBar> GetBarsInRange(int startIndex, int endIndex)
        {
            // 确保范围有效
            startIndex = Math.Max(0, startIndex);
            endIndex = Math.Min(_pagedSeries.Count - 1, endIndex);

            if (startIndex > endIndex)
                yield break;

            // 预加载整个范围
            _pagedSeries.PreloadRange(startIndex, endIndex);

            // 返回K线
            for (int i = startIndex; i <= endIndex; i++)
            {
                yield return _pagedSeries.GetBar(i);
            }
        }

        /// <summary>
        /// 强制清除所有缓存
        /// </summary>
        public void ClearAllCache()
        {
            _pagedSeries.ClearCache();
            _lastVisibleStart = -1;
            _lastVisibleEnd = -1;
        }

        /// <summary>
        /// 获取性能报告
        /// </summary>
        public PagePerformanceReport GetPerformanceReport()
        {
            var stats = _pagedSeries.GetStatistics();

            return new PagePerformanceReport
            {
                Statistics = stats,
                TotalPageLoads = _totalPageLoads,
                TotalPageUnloads = _totalPageUnloads,
                TotalLoadTimeMs = _totalLoadTimeMs,
                AverageLoadTimeMs = _totalPageLoads > 0
                    ? _totalLoadTimeMs / (double)_totalPageLoads
                    : 0,
                LoadedPageIndices = new List<int>(_pagedSeries.GetLoadedPageIndices())
            };
        }

        /// <summary>
        /// 重置性能统计
        /// </summary>
        public void ResetPerformanceCounters()
        {
            _totalPageLoads = 0;
            _totalPageUnloads = 0;
            _totalLoadTimeMs = 0;
        }
    }

    /// <summary>
    /// 分页性能报告
    /// </summary>
    public class PagePerformanceReport
    {
        public PageStatistics Statistics { get; set; }
        public int TotalPageLoads { get; set; }
        public int TotalPageUnloads { get; set; }
        public long TotalLoadTimeMs { get; set; }
        public double AverageLoadTimeMs { get; set; }
        public List<int> LoadedPageIndices { get; set; }

        public override string ToString()
        {
            return $"{Statistics}\n" +
                   $"页面操作: 加载 {TotalPageLoads} 次, 卸载 {TotalPageUnloads} 次\n" +
                   $"加载耗时: 总计 {TotalLoadTimeMs}ms, 平均 {AverageLoadTimeMs:F2}ms/页\n" +
                   $"已加载页: [{string.Join(", ", LoadedPageIndices)}]";
        }
    }
}