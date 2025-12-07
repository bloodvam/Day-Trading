using ChartEngine.Styles.Core;

namespace ChartEngine.Styles.Themes
{
    /// <summary>
    /// 图表主题接口
    /// </summary>
    public interface IChartTheme
    {
        string Name { get; }
        BackgroundStyle BackgroundStyle { get; }
        CandleStyle CandleStyle { get; }
        VolumeStyle VolumeStyle { get; }
    }
}