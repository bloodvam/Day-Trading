using System;
using System.Drawing;
using ChartEngine.Transforms.DataModels;
using ChartEngine.Config;
namespace ChartEngine.Transforms.Scales
{
    /// <summary>
    /// index ↔ 像素 X 的映射（带缓存优化）
    /// 优化要点：避免重复计算相同参数的坐标转换
    /// 
    /// 修改记录:
    /// - 2024/12/09: 修正 XToIndex 吸附逻辑，去掉 -0.5f，使用纯 Floor 计算
    ///   使吸附行为更符合直觉：鼠标在哪根 bar 上就吸附哪根 bar
    /// </summary>
    public class HorizontalScale
    {
        /// <summary>每根 K 线占多少像素（在当前 plotArea 下计算）</summary>
        public float PixelsPerBar { get; private set; }

        // ========== 缓存字段 ==========
        private int _cachedWidth = -1;
        private int _cachedCount = -1;
        private int _cachedStartIndex = -1;
        private int _cachedEndIndex = -1;

        /// <summary>
        /// 更新缩放参数（带缓存优化）
        /// </summary>
        public void Update(Rectangle plotArea, VisibleRange range)
        {
            // 🔥 优化点1：检查是否可以使用缓存
            if (_cachedWidth == plotArea.Width &&
                _cachedCount == range.Count &&
                _cachedStartIndex == range.StartIndex &&
                _cachedEndIndex == range.EndIndex)
            {
                return; // 使用缓存，跳过重新计算
            }

            // 更新缓存值
            _cachedWidth = plotArea.Width;
            _cachedCount = range.Count;
            _cachedStartIndex = range.StartIndex;
            _cachedEndIndex = range.EndIndex;

            // 重新计算
            if (range.Count <= 0 || plotArea.Width <= 0)
            {
                PixelsPerBar = 0;
                return;
            }

            PixelsPerBar = (float)plotArea.Width / range.Count;
        }

        public float IndexToX(int index, Rectangle plotArea, VisibleRange range)
        {
            if (range.Count <= 0)
                return plotArea.Left;

            float barWidth = (float)plotArea.Width / range.Count;
            float localIndex = index - range.StartIndex;

            return plotArea.Left + (localIndex + 0.5f) * barWidth;
        }

        /// <summary>
        /// 将像素 X 坐标转换为 K 线索引
        /// 
        /// 吸附逻辑：
        /// - 鼠标在 [barN_left, barN_right) 范围内都吸附到 barN
        /// - 使用 Floor 确保在整根 bar 范围内都返回相同索引
        /// - 与 IndexToX 的 +0.5f 逻辑对称一致
        /// </summary>
        public int XToIndex(float x, Rectangle plotArea, VisibleRange range)
        {
            if (range.Count <= 0 || plotArea.Width <= 0)
                return range.StartIndex;

            float barWidth = (float)plotArea.Width / range.Count;

            // 计算相对位置 (0-based)
            float relativeX = x - plotArea.Left;
            float localIndexFloat = relativeX / barWidth;

            // 直接 Floor，鼠标在 [barN, barN+1) 范围内都吸附到 barN
            // 例如：localIndexFloat = 1.3 -> localIndex = 1
            //       localIndexFloat = 1.9 -> localIndex = 1
            //       localIndexFloat = 2.0 -> localIndex = 2
            int localIndex = (int)Math.Floor(localIndexFloat);

            int index = range.StartIndex + localIndex;

            // 边界检查
            if (index < range.StartIndex) index = range.StartIndex;
            if (index > range.EndIndex) index = range.EndIndex;

            return index;
        }

        /// <summary>
        /// 清除缓存（在布局变化或数据重新加载时调用）
        /// </summary>
        public void InvalidateCache()
        {
            _cachedWidth = -1;
            _cachedCount = -1;
            _cachedStartIndex = -1;
            _cachedEndIndex = -1;
        }
    }
}