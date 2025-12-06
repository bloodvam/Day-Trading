using System.Collections.Generic;
using ChartEngine.Interfaces;
using ChartEngine.Rendering.Layers;
using ChartEngine.Styles;

namespace ChartEngine.ChartControls
{
    /// <summary>
    /// ChartControl 的图层管理部分（partial）。
    /// </summary>
    public partial class ChartControl
    {
        /// <summary>
        /// 图层列表，按顺序依次渲染。
        /// </summary>
        private readonly List<IChartLayer> _layers = new();

        /// <summary>
        /// 初始化默认图层：背景、蜡烛、成交量。
        /// </summary>
        private void InitializeLayers()
        {
            _layers.Clear();

            // 背景层（最底层）
            _layers.Add(new BackgroundLayer(BackgroundStyle));

            // 蜡烛层
            _layers.Add(new CandleLayer());

            // 成交量层
            _layers.Add(new VolumeLayer());
        }

        /// <summary>
        /// 向图表添加一个新的图层（添加到最上方）。
        /// </summary>
        public void AddLayer(IChartLayer layer)
        {
            if (layer == null) return;
            _layers.Add(layer);
            Invalidate();
        }

        /// <summary>
        /// 移除指定图层。
        /// </summary>
        public void RemoveLayer(IChartLayer layer)
        {
            if (layer == null) return;
            if (_layers.Remove(layer))
            {
                Invalidate();
            }
        }

        /// <summary>
        /// 按类型查找某个图层。
        /// </summary>
        public TLayer GetLayer<TLayer>() where TLayer : class, IChartLayer
        {
            foreach (var layer in _layers)
            {
                if (layer is TLayer t)
                    return t;
            }

            return null;
        }
    }
}
