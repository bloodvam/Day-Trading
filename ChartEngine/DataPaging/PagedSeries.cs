using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChartEngine.Models;

namespace ChartEngine.DataPaging
{
    /// <summary>
    /// 分页数据序列实现
    /// 核心优化：只在内存中保留可视范围及周边的数据页
    /// </summary>
    public class PagedSeries : IPagedSeries
    {
        private readonly IPageDataProvider _dataProvider;
        private readonly Dictionary<int, IReadOnlyList<IBar>> _loadedPages;
        private readonly int _pageSize;
        private readonly int _totalCount;
        private readonly int _totalPages;
        private readonly object _lock = new object();

        // 缓存策略配置
        private int _cacheWindowSize = 3; // 保留可视区域前后各3页

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="dataProvider">数据提供器</param>
        /// <param name="pageSize">页大小（推荐500-2000）</param>
        public PagedSeries(IPageDataProvider dataProvider, int pageSize = 1000)
        {
            _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));

            if (pageSize <= 0)
                throw new ArgumentException("页大小必须大于0", nameof(pageSize));

            _pageSize = pageSize;
            _totalCount = _dataProvider.GetTotalCount();
            _totalPages = (_totalCount + _pageSize - 1) / _pageSize;
            _loadedPages = new Dictionary<int, IReadOnlyList<IBar>>();
        }

        /// <summary>
        /// 设置缓存窗口大小（可视区域前后保留的页数）
        /// </summary>
        public int CacheWindowSize
        {
            get => _cacheWindowSize;
            set => _cacheWindowSize = Math.Max(1, value);
        }

        #region IPagedSeries 实现

        public int PageSize => _pageSize;
        public int TotalPages => _totalPages;
        public int LoadedPages
        {
            get
            {
                lock (_lock)
                {
                    return _loadedPages.Count;
                }
            }
        }

        public int LoadedBarsCount
        {
            get
            {
                lock (_lock)
                {
                    return _loadedPages.Values.Sum(page => page.Count);
                }
            }
        }

        public IBar GetBar(int index)
        {
            if (index < 0 || index >= _totalCount)
                throw new ArgumentOutOfRangeException(nameof(index));

            int pageIndex = index / _pageSize;
            int indexInPage = index % _pageSize;

            var page = GetOrLoadPage(pageIndex);

            if (indexInPage < page.Count)
                return page[indexInPage];

            throw new InvalidOperationException($"K线索引 {index} 在页 {pageIndex} 中不存在");
        }

        public void PreloadRange(int startIndex, int endIndex)
        {
            if (startIndex < 0) startIndex = 0;
            if (endIndex >= _totalCount) endIndex = _totalCount - 1;
            if (startIndex > endIndex) return;

            int startPage = startIndex / _pageSize;
            int endPage = endIndex / _pageSize;

            // 计算需要加载的页面（包括前后缓存窗口）
            int cacheStartPage = Math.Max(0, startPage - _cacheWindowSize);
            int cacheEndPage = Math.Min(_totalPages - 1, endPage + _cacheWindowSize);

            var pagesToLoad = new List<int>();

            lock (_lock)
            {
                for (int page = cacheStartPage; page <= cacheEndPage; page++)
                {
                    if (!_loadedPages.ContainsKey(page))
                    {
                        pagesToLoad.Add(page);
                    }
                }
            }

            // 批量加载
            if (pagesToLoad.Count > 0)
            {
                LoadPagesInBackground(pagesToLoad);
            }
        }

        public void UnloadInvisiblePages(int visibleStartIndex, int visibleEndIndex)
        {
            if (visibleStartIndex < 0) visibleStartIndex = 0;
            if (visibleEndIndex >= _totalCount) visibleEndIndex = _totalCount - 1;

            int startPage = visibleStartIndex / _pageSize;
            int endPage = visibleEndIndex / _pageSize;

            // 计算保留范围（包括缓存窗口）
            int keepStartPage = Math.Max(0, startPage - _cacheWindowSize);
            int keepEndPage = Math.Min(_totalPages - 1, endPage + _cacheWindowSize);

            lock (_lock)
            {
                var pagesToRemove = _loadedPages.Keys
                    .Where(page => page < keepStartPage || page > keepEndPage)
                    .ToList();

                foreach (var page in pagesToRemove)
                {
                    _loadedPages.Remove(page);
                }
            }
        }

        public IEnumerable<int> GetLoadedPageIndices()
        {
            lock (_lock)
            {
                return _loadedPages.Keys.ToList();
            }
        }

        public void ClearCache()
        {
            lock (_lock)
            {
                _loadedPages.Clear();
            }
        }

        public PageStatistics GetStatistics()
        {
            lock (_lock)
            {
                return new PageStatistics
                {
                    TotalBars = _totalCount,
                    PageSize = _pageSize,
                    TotalPages = _totalPages,
                    LoadedPages = _loadedPages.Count,
                    LoadedBars = _loadedPages.Values.Sum(page => page.Count)
                };
            }
        }

        #endregion

        #region ISeries 实现

        public IReadOnlyList<IBar> Bars
        {
            get
            {
                // 警告：访问此属性会加载所有数据到内存！
                // 对于大数据集，应该使用 GetBar(index) 或 PreloadRange()
                EnsureAllPagesLoaded();

                lock (_lock)
                {
                    var allBars = new List<IBar>(_totalCount);
                    for (int page = 0; page < _totalPages; page++)
                    {
                        if (_loadedPages.TryGetValue(page, out var pageData))
                        {
                            allBars.AddRange(pageData);
                        }
                    }
                    return allBars;
                }
            }
        }

        public int Count => _totalCount;

        #endregion

        #region 私有方法

        /// <summary>
        /// 获取或加载指定页
        /// </summary>
        private IReadOnlyList<IBar> GetOrLoadPage(int pageIndex)
        {
            lock (_lock)
            {
                if (_loadedPages.TryGetValue(pageIndex, out var page))
                {
                    return page;
                }
            }

            // 未加载，同步加载
            var newPage = _dataProvider.LoadPage(pageIndex, _pageSize);

            lock (_lock)
            {
                // 双重检查，避免重复加载
                if (!_loadedPages.ContainsKey(pageIndex))
                {
                    _loadedPages[pageIndex] = newPage;
                }
                return _loadedPages[pageIndex];
            }
        }

        /// <summary>
        /// 后台批量加载页面
        /// </summary>
        private void LoadPagesInBackground(List<int> pageIndices)
        {
            Task.Run(async () =>
            {
                try
                {
                    var pages = await _dataProvider.LoadPagesAsync(pageIndices, _pageSize);

                    lock (_lock)
                    {
                        foreach (var kvp in pages)
                        {
                            if (!_loadedPages.ContainsKey(kvp.Key))
                            {
                                _loadedPages[kvp.Key] = kvp.Value;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // 记录错误，但不影响主流程
                    Console.WriteLine($"后台加载页面失败: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// 确保所有页面已加载（用于 Bars 属性）
        /// </summary>
        private void EnsureAllPagesLoaded()
        {
            var missingPages = new List<int>();

            lock (_lock)
            {
                for (int page = 0; page < _totalPages; page++)
                {
                    if (!_loadedPages.ContainsKey(page))
                    {
                        missingPages.Add(page);
                    }
                }
            }

            if (missingPages.Count > 0)
            {
                // 同步加载所有缺失的页
                foreach (var page in missingPages)
                {
                    GetOrLoadPage(page);
                }
            }
        }

        #endregion
    }
}