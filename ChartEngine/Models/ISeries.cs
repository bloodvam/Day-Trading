using System.Collections.Generic;

namespace ChartEngine.Models
{
    /// <summary>
    /// 表示一组连续的 K 线序列。
    /// CandleLayer、VolumeLayer 通过它渲染图形。
    /// </summary>
    public interface ISeries
    {
        IReadOnlyList<IBar> Bars { get; }
        int Count { get; }
    }
}
