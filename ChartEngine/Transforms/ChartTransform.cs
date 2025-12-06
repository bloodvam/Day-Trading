using System.Drawing;
using ChartEngine.Interfaces;

namespace ChartEngine.Transforms
{
    /// <summary>
    /// ChartTransform 实现：
    /// 组合 HorizontalScale / VerticalScale / VolumeScale / VisibleRange / PriceRange
    /// 对外通过 IChartTransform 暴露简单接口。
    /// </summary>
    public class ChartTransform : IChartTransform
    {
        private readonly HorizontalScale _horizontal = new HorizontalScale();
        private readonly VerticalScale _vertical = new VerticalScale();
        private readonly VolumeScale _volume = new VolumeScale();

        public VisibleRange VisibleRange { get; } = new VisibleRange();
        public PriceRange PriceRange { get; } = new PriceRange();

        public TransformConfig Config { get; } = new TransformConfig();

        public ChartTransform()
        {
        }

        public void SetVisibleRange(int startIndex, int endIndex)
        {
            VisibleRange.Set(startIndex, endIndex);
        }

        public void SetPriceRange(double minPrice, double maxPrice)
        {
            PriceRange.Set(minPrice, maxPrice);
            _vertical.SetRange(PriceRange.MinPrice, PriceRange.MaxPrice);
        }

        public void SetMaxVolume(double maxVolume)
        {
            _volume.SetMaxVolume(maxVolume);
        }

        public float IndexToX(int index, Rectangle plotArea)
        {
            _horizontal.Update(plotArea, VisibleRange);
            return _horizontal.IndexToX(index, plotArea, VisibleRange);
        }

        public int XToIndex(float x, Rectangle plotArea)
        {
            _horizontal.Update(plotArea, VisibleRange);
            return _horizontal.XToIndex(x, plotArea, VisibleRange);
        }

        public float PriceToY(double price, Rectangle plotArea)
        {
            // 垂直方向不依赖 plotArea 宽度，只用高度
            return _vertical.PriceToY(price, plotArea);
        }

        public double YToPrice(float y, Rectangle plotArea)
        {
            return _vertical.YToPrice(y, plotArea);
        }

        public float VolumeToY(double volume, Rectangle volumeArea)
        {
            return _volume.VolumeToY(volume, volumeArea);
        }
    }
}
