using System.Drawing;
using ChartEngine.Interfaces;

namespace ChartEngine.Transforms
{
    /// <summary>
    /// 图表坐标转换（优化版本）
    /// </summary>
    public class ChartTransform : IChartTransform
    {
        private readonly HorizontalScale _horizontal = new HorizontalScale();
        private readonly VerticalScale _vertical = new VerticalScale();
        private readonly VolumeScale _volume = new VolumeScale();
        private Rectangle _priceArea;
        private Rectangle _volumeArea;

        public VisibleRange VisibleRange { get; } = new VisibleRange();
        public PriceRange PriceRange { get; } = new PriceRange();

        public TransformConfig Config { get; } = new TransformConfig();

        public ChartTransform()
        {
        }

        public void UpdateLayout(Rectangle priceArea, Rectangle volumeArea)
        {
            _priceArea = priceArea;
            _volumeArea = volumeArea;

            // 🔥 优化点：只在布局变化时更新一次
            _horizontal.Update(priceArea, VisibleRange);
        }

        public void SetVisibleRange(int startIndex, int endIndex)
        {
            VisibleRange.Set(startIndex, endIndex);

            // 🔥 优化点：只在可视范围变化时清除缓存
            _horizontal.InvalidateCache();

            // 如果已经有布局信息,立即更新
            if (_priceArea.Width > 0)
            {
                _horizontal.Update(_priceArea, VisibleRange);
            }
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
            // 🔥 优化点：Update 内部有缓存机制，不会重复计算
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

        /// <summary>
        /// 清除所有缓存（在数据重新加载时调用）
        /// </summary>
        public void InvalidateAllCaches()
        {
            _horizontal.InvalidateCache();
        }
    }
}