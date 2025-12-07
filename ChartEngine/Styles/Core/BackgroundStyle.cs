namespace ChartEngine.Styles.Core
{
    public class BackgroundStyle
    {
        public Color PriceAreaBackColor { get; set; } = Color.FromArgb(30, 245, 245);
        public Color VolumeAreaBackColor { get; set; } = Color.FromArgb(235, 235, 235);

        // 可以以后扩展渐变、图片、纹理
    }
}
