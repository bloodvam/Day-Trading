using System;
using System.Collections.Generic;
using ChartEngine.Data.Models;

namespace ChartEngine.Utils
{
    /// <summary>
    /// 边界检查和验证工具
    /// 优化要点：防御性编程，避免越界和空引用异常
    /// </summary>
    public static class BoundsChecker
    {
        /// <summary>
        /// 安全地获取K线索引范围（确保不越界）
        /// </summary>
        public static (int safeStart, int safeEnd) GetSafeIndexRange(
            int requestedStart,
            int requestedEnd,
            int dataCount)
        {
            if (dataCount <= 0)
                return (0, -1); // 无数据

            int safeStart = Math.Max(0, Math.Min(requestedStart, dataCount - 1));
            int safeEnd = Math.Max(0, Math.Min(requestedEnd, dataCount - 1));

            // 确保 start <= end
            if (safeStart > safeEnd)
                safeStart = safeEnd;

            return (safeStart, safeEnd);
        }

        /// <summary>
        /// 验证价格值是否有效
        /// </summary>
        public static bool IsValidPrice(double price)
        {
            return !double.IsNaN(price) &&
                   !double.IsInfinity(price) &&
                   price >= 0;
        }

        /// <summary>
        /// 验证成交量是否有效
        /// </summary>
        public static bool IsValidVolume(double volume)
        {
            return !double.IsNaN(volume) &&
                   !double.IsInfinity(volume) &&
                   volume >= 0;
        }

        /// <summary>
        /// 安全地验证K线数据
        /// </summary>
        public static bool IsValidBar(IBar bar)
        {
            if (bar == null)
                return false;

            return IsValidPrice(bar.Open) &&
                   IsValidPrice(bar.High) &&
                   IsValidPrice(bar.Low) &&
                   IsValidPrice(bar.Close) &&
                   IsValidVolume(bar.Volume) &&
                   bar.High >= bar.Low; // 最高价应该 >= 最低价
        }

        /// <summary>
        /// 限制值在指定范围内
        /// </summary>
        public static T Clamp<T>(T value, T min, T max) where T : IComparable<T>
        {
            if (value.CompareTo(min) < 0) return min;
            if (value.CompareTo(max) > 0) return max;
            return value;
        }

        /// <summary>
        /// 安全地从列表获取元素
        /// </summary>
        public static T SafeGet<T>(IReadOnlyList<T> list, int index, T defaultValue = default(T))
        {
            if (list == null || index < 0 || index >= list.Count)
                return defaultValue;

            return list[index];
        }
    }
}