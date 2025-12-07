using System.Collections.Generic;
using System.Threading.Tasks;
using ChartEngine.Models;

namespace ChartEngine.DataPaging
{
    /// <summary>
    /// 分页数据提供器接口
    /// 负责从数据源加载指定页的K线数据
    /// </summary>
    public interface IPageDataProvider
    {
        /// <summary>
        /// 获取总K线数量
        /// </summary>
        int GetTotalCount();

        /// <summary>
        /// 同步加载指定页的K线数据
        /// </summary>
        /// <param name="pageIndex">页索引（从0开始）</param>
        /// <param name="pageSize">页大小</param>
        /// <returns>该页的K线列表</returns>
        IReadOnlyList<IBar> LoadPage(int pageIndex, int pageSize);

        /// <summary>
        /// 异步加载指定页的K线数据
        /// </summary>
        /// <param name="pageIndex">页索引（从0开始）</param>
        /// <param name="pageSize">页大小</param>
        /// <returns>该页的K线列表</returns>
        Task<IReadOnlyList<IBar>> LoadPageAsync(int pageIndex, int pageSize);

        /// <summary>
        /// 批量加载多个页的数据
        /// </summary>
        /// <param name="pageIndices">页索引列表</param>
        /// <param name="pageSize">页大小</param>
        /// <returns>页索引到K线列表的映射</returns>
        Task<Dictionary<int, IReadOnlyList<IBar>>> LoadPagesAsync(
            IEnumerable<int> pageIndices,
            int pageSize);
    }

    /// <summary>
    /// 内存数据提供器（从现有数据创建分页）
    /// </summary>
    public class MemoryPageDataProvider : IPageDataProvider
    {
        private readonly IReadOnlyList<IBar> _allBars;

        public MemoryPageDataProvider(IReadOnlyList<IBar> allBars)
        {
            _allBars = allBars ?? throw new System.ArgumentNullException(nameof(allBars));
        }

        public int GetTotalCount()
        {
            return _allBars.Count;
        }

        public IReadOnlyList<IBar> LoadPage(int pageIndex, int pageSize)
        {
            int startIndex = pageIndex * pageSize;
            int count = System.Math.Min(pageSize, _allBars.Count - startIndex);

            if (startIndex >= _allBars.Count || count <= 0)
                return new List<IBar>();

            var page = new List<IBar>(count);
            for (int i = 0; i < count; i++)
            {
                page.Add(_allBars[startIndex + i]);
            }

            return page;
        }

        public Task<IReadOnlyList<IBar>> LoadPageAsync(int pageIndex, int pageSize)
        {
            return Task.FromResult(LoadPage(pageIndex, pageSize));
        }

        public async Task<Dictionary<int, IReadOnlyList<IBar>>> LoadPagesAsync(
            IEnumerable<int> pageIndices,
            int pageSize)
        {
            var result = new Dictionary<int, IReadOnlyList<IBar>>();

            await Task.Run(() =>
            {
                foreach (var pageIndex in pageIndices)
                {
                    result[pageIndex] = LoadPage(pageIndex, pageSize);
                }
            });

            return result;
        }
    }
}