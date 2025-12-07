using System.Drawing;
using ChartEngine.Interfaces;
using ChartEngine.Data.Models;
using ChartEngine.Transforms.DataModels;
using ChartEngine.Styles.Core;

namespace ChartEngine.Rendering
{
    /// <summary>
    /// 提供给每个 Layer 的渲染上下文数据包。
    /// 每一帧 OnPaint 时由 ChartControl 构建一次。
    /// Layers 不负责计算坐标范围、价格区、或可视范围，
    /// 它们只使用此 Context 提供的数据进行绘制。
    /// </summary>
    public class ChartRenderContext
    {
        /// <summary>坐标转换引擎（index ↔ X, price ↔ Y）</summary>
        public IChartTransform Transform { get; }

        /// <summary>K线序列（IBar 列表）</summary>
        public ISeries Series { get; }

        /// <summary>主图区域（蜡烛图 / 指标）</summary>
        public Rectangle PriceArea { get; }

        /// <summary>成交量区域</summary>
        public Rectangle VolumeArea { get; }

        /// <summary>当前可视区间（由 Transform 内部维护）</summary>
        public VisibleRange VisibleRange => Transform.VisibleRange;

        /// <summary>当前主图价格区间（由 Transform 内部维护）</summary>
        public PriceRange PriceRange => Transform.PriceRange;

        /// <summary>当前可视区间最大成交量（VolumeScale 使用）</summary>
        public double MaxVolume { get; }

        /// <summary>当前帧的 Graphics（由 ChartControl 所提供）</summary>
        public Graphics Graphics { get; }

        public CandleStyle CandleStyle { get; }
        public VolumeStyle VolumeStyle { get; }

        public ChartRenderContext(
            IChartTransform transform,
            ISeries series,
            Rectangle priceArea,
            Rectangle volumeArea,
            double maxVolume,
            Graphics graphics,
            CandleStyle candleStyle,
             VolumeStyle volumeStyle)
        {
            Transform = transform;
            Series = series;
            PriceArea = priceArea;
            VolumeArea = volumeArea;
            MaxVolume = maxVolume;
            Graphics = graphics;
            CandleStyle = candleStyle;
            VolumeStyle = volumeStyle;
        }
    }
}
