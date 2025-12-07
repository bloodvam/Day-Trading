namespace ChartEngine.Transforms.DataModels
{
    /// <summary>
    /// 主图价格范围
    /// </summary>
    public class PriceRange
    {
        public double MinPrice { get; private set; }
        public double MaxPrice { get; private set; }

        public bool IsValid => MaxPrice > MinPrice;

        public PriceRange(double minPrice = 0, double maxPrice = 0)
        {
            Set(minPrice, maxPrice);
        }

        public void Set(double minPrice, double maxPrice)
        {
            if (maxPrice <= minPrice)
            {
                // 避免除零，给一个非常小的区间
                maxPrice = minPrice + 1e-6;
            }

            MinPrice = minPrice;
            MaxPrice = maxPrice;
        }
    }
}
