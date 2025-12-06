namespace ChartEngine.Config
{
    /// <summary>
    /// 图表布局配置
    /// 控制主图区域、成交量区域、边距等布局参数
    /// </summary>
    public class ChartLayoutConfig
    {
        /// <summary>
        /// 左边距（像素）
        /// 可以预留给价格坐标轴或其他左侧控件
        /// </summary>
        public int LeftMargin { get; set; } = 10;

        /// <summary>
        /// 右边距（像素）
        /// 通常预留给价格坐标轴显示
        /// </summary>
        public int RightMargin { get; set; } = 70;

        /// <summary>
        /// 上边距（像素）
        /// </summary>
        public int TopMargin { get; set; } = 10;

        /// <summary>
        /// 底边距（像素）
        /// 通常预留给时间坐标轴显示
        /// </summary>
        public int BottomMargin { get; set; } = 30;

        /// <summary>
        /// 成交量区域占总可用高度的比例（0-1）
        /// 例如：0.2 表示成交量占 20%，主图占 80%
        /// </summary>
        public double VolumeHeightRatio { get; set; } = 0.2;

        /// <summary>
        /// 主图和成交量区域之间的间隔（像素）
        /// </summary>
        public int Spacing { get; set; } = 10;

        /// <summary>
        /// 是否显示成交量区域
        /// </summary>
        public bool ShowVolumeArea { get; set; } = true;

        /// <summary>
        /// 验证配置的有效性
        /// </summary>
        public bool IsValid()
        {
            return LeftMargin >= 0
                && RightMargin >= 0
                && TopMargin >= 0
                && BottomMargin >= 0
                && VolumeHeightRatio >= 0
                && VolumeHeightRatio <= 1
                && Spacing >= 0;
        }

        /// <summary>
        /// 获取默认的深色主题布局配置
        /// </summary>
        public static ChartLayoutConfig GetDarkThemeDefault()
        {
            return new ChartLayoutConfig
            {
                LeftMargin = 10,
                RightMargin = 80,
                TopMargin = 15,
                BottomMargin = 35,
                VolumeHeightRatio = 0.25,
                Spacing = 12
            };
        }

        /// <summary>
        /// 获取紧凑型布局配置（适合小窗口）
        /// </summary>
        public static ChartLayoutConfig GetCompactLayout()
        {
            return new ChartLayoutConfig
            {
                LeftMargin = 5,
                RightMargin = 50,
                TopMargin = 5,
                BottomMargin = 20,
                VolumeHeightRatio = 0.15,
                Spacing = 5
            };
        }
    }
}