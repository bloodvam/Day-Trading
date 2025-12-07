using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace ChartEngine.Rendering
{
    /// <summary>
    /// 脏区域类型
    /// </summary>
    public enum DirtyRegionType
    {
        /// <summary>全屏刷新</summary>
        FullScreen,

        /// <summary>K线区域</summary>
        CandleRegion,

        /// <summary>成交量区域</summary>
        VolumeRegion,

        /// <summary>价格轴区域</summary>
        PriceAxisRegion,

        /// <summary>时间轴区域</summary>
        TimeAxisRegion,

        /// <summary>十字光标</summary>
        Crosshair,

        /// <summary>指定K线索引范围</summary>
        BarRange
    }

    /// <summary>
    /// 脏区域信息
    /// </summary>
    public class DirtyRegion
    {
        public Rectangle Bounds { get; set; }
        public DirtyRegionType Type { get; set; }
        public int StartBarIndex { get; set; } = -1;
        public int EndBarIndex { get; set; } = -1;
        public DateTime Timestamp { get; set; }
        public string Reason { get; set; }

        public DirtyRegion(Rectangle bounds, DirtyRegionType type, string reason = "")
        {
            Bounds = bounds;
            Type = type;
            Timestamp = DateTime.Now;
            Reason = reason;
        }

        public DirtyRegion(
            Rectangle bounds,
            DirtyRegionType type,
            int startBarIndex,
            int endBarIndex,
            string reason = "")
        {
            Bounds = bounds;
            Type = type;
            StartBarIndex = startBarIndex;
            EndBarIndex = endBarIndex;
            Timestamp = DateTime.Now;
            Reason = reason;
        }
    }

    /// <summary>
    /// 脏区域管理器 - 跟踪需要重绘的区域
    /// 优化要点：只重绘变化的部分，而不是整个图表
    /// </summary>
    public class DirtyRegionManager
    {
        private readonly List<DirtyRegion> _dirtyRegions = new List<DirtyRegion>();
        private readonly object _lock = new object();
        private bool _isFullScreenDirty = false;
        private Rectangle _fullScreenBounds;

        /// <summary>
        /// 是否有脏区域需要重绘
        /// </summary>
        public bool HasDirtyRegions
        {
            get
            {
                lock (_lock)
                {
                    return _isFullScreenDirty || _dirtyRegions.Count > 0;
                }
            }
        }

        /// <summary>
        /// 是否需要全屏刷新
        /// </summary>
        public bool IsFullScreenDirty
        {
            get
            {
                lock (_lock)
                {
                    return _isFullScreenDirty;
                }
            }
        }

        /// <summary>
        /// 标记全屏脏区域
        /// </summary>
        public void MarkFullScreen(Rectangle bounds, string reason = "")
        {
            lock (_lock)
            {
                _isFullScreenDirty = true;
                _fullScreenBounds = bounds;
                _dirtyRegions.Clear(); // 清除局部脏区域，因为全屏刷新会覆盖

                _dirtyRegions.Add(new DirtyRegion(bounds, DirtyRegionType.FullScreen, reason));
            }
        }

        /// <summary>
        /// 标记K线范围脏区域
        /// </summary>
        public void MarkBarRange(
            Rectangle bounds,
            int startBarIndex,
            int endBarIndex,
            string reason = "")
        {
            lock (_lock)
            {
                if (_isFullScreenDirty)
                    return; // 已经是全屏刷新，无需添加

                _dirtyRegions.Add(new DirtyRegion(
                    bounds,
                    DirtyRegionType.BarRange,
                    startBarIndex,
                    endBarIndex,
                    reason));
            }
        }

        /// <summary>
        /// 标记十字光标区域
        /// </summary>
        public void MarkCrosshair(Rectangle bounds, string reason = "")
        {
            lock (_lock)
            {
                if (_isFullScreenDirty)
                    return;

                // 移除旧的十字光标脏区域
                _dirtyRegions.RemoveAll(r => r.Type == DirtyRegionType.Crosshair);

                _dirtyRegions.Add(new DirtyRegion(bounds, DirtyRegionType.Crosshair, reason));
            }
        }

        /// <summary>
        /// 标记价格轴脏区域
        /// </summary>
        public void MarkPriceAxis(Rectangle bounds, string reason = "")
        {
            lock (_lock)
            {
                if (_isFullScreenDirty)
                    return;

                _dirtyRegions.Add(new DirtyRegion(bounds, DirtyRegionType.PriceAxisRegion, reason));
            }
        }

        /// <summary>
        /// 标记时间轴脏区域
        /// </summary>
        public void MarkTimeAxis(Rectangle bounds, string reason = "")
        {
            lock (_lock)
            {
                if (_isFullScreenDirty)
                    return;

                _dirtyRegions.Add(new DirtyRegion(bounds, DirtyRegionType.TimeAxisRegion, reason));
            }
        }

        /// <summary>
        /// 标记K线区域脏
        /// </summary>
        public void MarkCandleRegion(Rectangle bounds, string reason = "")
        {
            lock (_lock)
            {
                if (_isFullScreenDirty)
                    return;

                _dirtyRegions.Add(new DirtyRegion(bounds, DirtyRegionType.CandleRegion, reason));
            }
        }

        /// <summary>
        /// 标记成交量区域脏
        /// </summary>
        public void MarkVolumeRegion(Rectangle bounds, string reason = "")
        {
            lock (_lock)
            {
                if (_isFullScreenDirty)
                    return;

                _dirtyRegions.Add(new DirtyRegion(bounds, DirtyRegionType.VolumeRegion, reason));
            }
        }

        /// <summary>
        /// 获取所有脏区域
        /// </summary>
        public List<DirtyRegion> GetDirtyRegions()
        {
            lock (_lock)
            {
                return new List<DirtyRegion>(_dirtyRegions);
            }
        }

        /// <summary>
        /// 获取合并后的脏区域（优化重叠区域）
        /// </summary>
        public List<Rectangle> GetOptimizedDirtyBounds()
        {
            lock (_lock)
            {
                if (_isFullScreenDirty)
                {
                    return new List<Rectangle> { _fullScreenBounds };
                }

                if (_dirtyRegions.Count == 0)
                {
                    return new List<Rectangle>();
                }

                // 简单策略：如果脏区域超过5个，直接全屏刷新
                if (_dirtyRegions.Count > 5)
                {
                    var union = _dirtyRegions[0].Bounds;
                    foreach (var region in _dirtyRegions.Skip(1))
                    {
                        union = Rectangle.Union(union, region.Bounds);
                    }
                    return new List<Rectangle> { union };
                }

                // 合并重叠区域
                var optimized = new List<Rectangle>();
                var remaining = new List<Rectangle>(_dirtyRegions.Select(r => r.Bounds));

                while (remaining.Count > 0)
                {
                    var current = remaining[0];
                    remaining.RemoveAt(0);

                    bool merged = false;
                    for (int i = 0; i < remaining.Count; i++)
                    {
                        if (current.IntersectsWith(remaining[i]))
                        {
                            current = Rectangle.Union(current, remaining[i]);
                            remaining.RemoveAt(i);
                            merged = true;
                            break;
                        }
                    }

                    if (merged)
                    {
                        remaining.Insert(0, current); // 重新检查
                    }
                    else
                    {
                        optimized.Add(current);
                    }
                }

                return optimized;
            }
        }

        /// <summary>
        /// 清除所有脏区域
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _dirtyRegions.Clear();
                _isFullScreenDirty = false;
                _fullScreenBounds = Rectangle.Empty;
            }
        }

        /// <summary>
        /// 获取脏区域统计信息
        /// </summary>
        public string GetStatistics()
        {
            lock (_lock)
            {
                if (_isFullScreenDirty)
                {
                    return "全屏刷新";
                }

                if (_dirtyRegions.Count == 0)
                {
                    return "无脏区域";
                }

                var typeGroups = _dirtyRegions.GroupBy(r => r.Type);
                var stats = string.Join(", ", typeGroups.Select(g => $"{g.Key}: {g.Count()}"));
                return $"脏区域: {_dirtyRegions.Count} ({stats})";
            }
        }

        /// <summary>
        /// 检查指定点是否在脏区域内
        /// </summary>
        public bool IsPointDirty(Point point)
        {
            lock (_lock)
            {
                if (_isFullScreenDirty)
                    return true;

                return _dirtyRegions.Any(r => r.Bounds.Contains(point));
            }
        }

        /// <summary>
        /// 检查指定矩形是否与脏区域相交
        /// </summary>
        public bool IntersectsWithDirtyRegion(Rectangle rect)
        {
            lock (_lock)
            {
                if (_isFullScreenDirty)
                    return true;

                return _dirtyRegions.Any(r => r.Bounds.IntersectsWith(rect));
            }
        }
    }
}