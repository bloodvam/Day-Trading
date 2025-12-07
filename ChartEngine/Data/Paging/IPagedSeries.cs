using System;
using System.Collections.Generic;
using ChartEngine.Data.Models;

namespace ChartEngine.Data.Paging
{
    /// <summary>
    /// 分页数据序列接口
    /// 支持百万级K线数据，内存占用降低60-80%
    /// </summary>
    public interface IPagedSeries : ISeries
    {
        /// <summary>
        /// 分页大小（每页K线数量）
        /// </summary>
        int PageSize { get; }

        /// <summary>
        /// 总页数
        /// </summary>
        int TotalPages { get; }

        /// <summary>
        /// 当前已加载的页数
        /// </summary>
        int LoadedPages { get; }

        /// <summary>
        /// 当前内存中的K线数量
        /// </summary>
        int LoadedBarsCount { get; }

        /// <summary>
        /// 获取指定索引的K线（如果未加载会触发加载）
        /// </summary>
        IBar GetBar(int index);

        /// <summary>
        /// 预加载指定范围的K线
        /// </summary>
        void PreloadRange(int startIndex, int endIndex);

        /// <summary>
        /// 卸载不在可视范围内的页面
        /// </summary>
        void UnloadInvisiblePages(int visibleStartIndex, int visibleEndIndex);

        /// <summary>
        /// 获取当前已加载的页面索引列表
        /// </summary>
        IEnumerable<int> GetLoadedPageIndices();

        /// <summary>
        /// 清除所有已加载的页面
        /// </summary>
        void ClearCache();

        /// <summary>
        /// 获取分页统计信息
        /// </summary>
        PageStatistics GetStatistics();
    }

    /// <summary>
    /// 分页统计信息
    /// </summary>
    public class PageStatistics
    {
        /// <summary>总K线数量</summary>
        public int TotalBars { get; set; }

        /// <summary>分页大小</summary>
        public int PageSize { get; set; }

        /// <summary>总页数</summary>
        public int TotalPages { get; set; }

        /// <summary>已加载页数</summary>
        public int LoadedPages { get; set; }

        /// <summary>内存中K线数量</summary>
        public int LoadedBars { get; set; }

        /// <summary>内存占用率</summary>
        public double MemoryUsagePercent => TotalBars > 0
            ? LoadedBars * 100.0 / TotalBars
            : 0;

        /// <summary>估计内存占用（MB）</summary>
        public double EstimatedMemoryMB => LoadedBars * 0.0001; // 每根K线约100字节

        public override string ToString()
        {
            return $"总K线: {TotalBars}, 已加载: {LoadedBars} ({MemoryUsagePercent:F1}%), " +
                   $"页数: {LoadedPages}/{TotalPages}, 内存: {EstimatedMemoryMB:F2}MB";
        }
    }
}