namespace ChartEngine.Transforms
{
    /// <summary>
    /// Transform 的一些可调参数（缩放速度、padding 等）
    /// 后面你需要可以继续往里加。
    /// </summary>
    public class TransformConfig
    {
        /// <summary>bar 最小像素宽度</summary>
        public float MinBarWidth { get; set; } = 3f;

        /// <summary>bar 最大像素宽度</summary>
        public float MaxBarWidth { get; set; } = 50f;

        /// <summary>主图上下留白比例，比如 0.05 = 上下各留 5%</summary>
        public double PricePaddingRatio { get; set; } = 0.05;

        /// <summary>成交量上方留白比例</summary>
        public double VolumePaddingRatio { get; set; } = 0.1;
    }
}
