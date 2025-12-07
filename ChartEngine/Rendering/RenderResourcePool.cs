using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace ChartEngine.Rendering
{
    /// <summary>
    /// 渲染资源对象池
    /// 优化要点：避免在渲染循环中频繁创建和销毁 GDI+ 对象
    /// </summary>
    public class RenderResourcePool : IDisposable
    {
        // ========== 画刷缓存 ==========
        private readonly Dictionary<Color, SolidBrush> _brushCache = new Dictionary<Color, SolidBrush>();

        // ========== 画笔缓存 ==========
        private readonly Dictionary<(Color color, float width), Pen> _penCache =
            new Dictionary<(Color, float), Pen>();

        // ========== 带样式的画笔缓存 ==========
        private readonly Dictionary<(Color color, float width, DashStyle style), Pen> _styledPenCache =
            new Dictionary<(Color, float, DashStyle), Pen>();

        private bool _disposed = false;

        /// <summary>
        /// 获取实心画刷（带缓存）
        /// </summary>
        public SolidBrush GetBrush(Color color)
        {
            if (!_brushCache.TryGetValue(color, out var brush))
            {
                brush = new SolidBrush(color);
                _brushCache[color] = brush;
            }
            return brush;
        }

        /// <summary>
        /// 获取画笔（带缓存）
        /// </summary>
        public Pen GetPen(Color color, float width = 1f)
        {
            var key = (color, width);
            if (!_penCache.TryGetValue(key, out var pen))
            {
                pen = new Pen(color, width);
                _penCache[key] = pen;
            }
            return pen;
        }

        /// <summary>
        /// 获取带样式的画笔（带缓存）
        /// </summary>
        public Pen GetStyledPen(Color color, float width, DashStyle style)
        {
            var key = (color, width, style);
            if (!_styledPenCache.TryGetValue(key, out var pen))
            {
                pen = new Pen(color, width) { DashStyle = style };
                _styledPenCache[key] = pen;
            }
            return pen;
        }

        /// <summary>
        /// 清理所有缓存的资源
        /// </summary>
        public void Clear()
        {
            // 释放所有画刷
            foreach (var brush in _brushCache.Values)
            {
                brush?.Dispose();
            }
            _brushCache.Clear();

            // 释放所有画笔
            foreach (var pen in _penCache.Values)
            {
                pen?.Dispose();
            }
            _penCache.Clear();

            // 释放所有样式画笔
            foreach (var pen in _styledPenCache.Values)
            {
                pen?.Dispose();
            }
            _styledPenCache.Clear();
        }

        /// <summary>
        /// 获取缓存统计信息
        /// </summary>
        public string GetCacheStats()
        {
            return $"Brushes: {_brushCache.Count}, Pens: {_penCache.Count}, StyledPens: {_styledPenCache.Count}";
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Clear();
                _disposed = true;
            }
        }
    }
}