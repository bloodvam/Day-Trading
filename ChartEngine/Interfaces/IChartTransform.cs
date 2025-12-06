using System.Drawing;
using ChartEngine.Transforms;

namespace ChartEngine.Interfaces
{
    /// <summary>
    /// 图表坐标变换接口：负责 数据空间 ↔ 像素空间 的转换。
    /// 所有 Layer 只依赖这个接口。
    /// </summary>
    public interface IChartTransform
    {
        /// <summary>当前可见的 K 线索引区间</summary>
        VisibleRange VisibleRange { get; }

        /// <summary>当前主图的价格显示区间</summary>
        PriceRange PriceRange { get; }

        /// <summary>
        /// 数据索引 → 像素 X 坐标（主图/volume 共用横轴）
        /// </summary>
        float IndexToX(int index, Rectangle plotArea);

        /// <summary>
        /// 像素 X → 数据索引（用于鼠标 hit test）
        /// </summary>
        int XToIndex(float x, Rectangle plotArea);

        /// <summary>
        /// 价格 → 像素 Y（主图区域）
        /// </summary>
        float PriceToY(double price, Rectangle plotArea);

        /// <summary>
        /// 像素 Y → 价格（主图区域）
        /// </summary>
        double YToPrice(float y, Rectangle plotArea);

        /// <summary>
        /// 成交量 → 像素 Y（volume 区域）
        /// </summary>
        float VolumeToY(double volume, Rectangle volumeArea);

        /// <summary>
        /// 设置当前可见的 K 线索引范围
        /// </summary>
        void SetVisibleRange(int startIndex, int endIndex);

        /// <summary>
        /// 设置主图价格范围
        /// </summary>
        void SetPriceRange(double minPrice, double maxPrice);

        /// <summary>
        /// 设置当前可见区间内的最大成交量（用于 VolumeToY 缩放）
        /// </summary>
        void SetMaxVolume(double maxVolume);
    }
}
