using System.Collections.Generic;
using System.Linq;
using ChartEngine.Interfaces;
using ChartEngine.Rendering.Layers;

namespace ChartEngine.Controls.ChartControls
{
    /// <summary>
    /// ChartControl 的图层管理部分（partial）。
    /// 负责图层的增删查改、排序、显示控制。
    /// </summary>
    public partial class ChartControl
    {
        /// <summary>
        /// 图层列表，按 ZOrder 排序渲染。
        /// </summary>
        private readonly List<IChartLayer> _layers = new();

        /// <summary>
        /// 初始化默认图层：背景、网格、成交量、K线、坐标轴、十字光标。
        /// </summary>
        private void InitializeLayers()
        {
            _layers.Clear();

            // 添加图层 (按 ZOrder 自动排序)
            // GridStyle、AxisStyle、CrosshairStyle 从 Styles 模块获取
            _layers.Add(new BackgroundLayer(BackgroundStyle) { ZOrder = 0 });
            _layers.Add(new GridLayer(GridStyle) { ZOrder = 1 });
            _layers.Add(new VolumeLayer() { ZOrder = 10 });
            _layers.Add(new CandleLayer() { ZOrder = 20 });
            _layers.Add(new AxisLayer(AxisStyle) { ZOrder = 30 });
            _layers.Add(new CrosshairLayer(CrosshairStyle) { ZOrder = 100 });

            // 按 ZOrder 排序
            SortLayersByZOrder();
        }

        /// <summary>
        /// 向图表添加一个新的图层。
        /// </summary>
        public void AddLayer(IChartLayer layer)
        {
            if (layer == null) return;

            _layers.Add(layer);
            SortLayersByZOrder();

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
            return _layers.OfType<TLayer>().FirstOrDefault();
        }

        /// <summary>
        /// 按类型获取所有图层。
        /// </summary>
        public IEnumerable<TLayer> GetLayers<TLayer>() where TLayer : class, IChartLayer
        {
            return _layers.OfType<TLayer>();
        }

        /// <summary>
        /// 按 ZOrder 排序图层。
        /// </summary>
        private void SortLayersByZOrder()
        {
            _layers.Sort((a, b) => a.ZOrder.CompareTo(b.ZOrder));
        }

        /// <summary>
        /// 切换网格线的显示/隐藏。
        /// </summary>
        public void ToggleGrid()
        {
            var gridLayer = GetLayer<GridLayer>();
            if (gridLayer != null)
            {
                gridLayer.IsVisible = !gridLayer.IsVisible;
                Invalidate();
            }
        }

        /// <summary>
        /// 切换成交量的显示/隐藏。
        /// </summary>
        public void ToggleVolume()
        {
            var volumeLayer = GetLayer<VolumeLayer>();
            if (volumeLayer != null)
            {
                volumeLayer.IsVisible = !volumeLayer.IsVisible;
                Invalidate();
            }
        }

        /// <summary>
        /// 切换K线的显示/隐藏。
        /// </summary>
        public void ToggleCandles()
        {
            var candleLayer = GetLayer<CandleLayer>();
            if (candleLayer != null)
            {
                candleLayer.IsVisible = !candleLayer.IsVisible;
                Invalidate();
            }
        }

        /// <summary>
        /// 切换坐标轴的显示/隐藏。
        /// </summary>
        public void ToggleAxis()
        {
            var axisLayer = GetLayer<AxisLayer>();
            if (axisLayer != null)
            {
                axisLayer.IsVisible = !axisLayer.IsVisible;
                Invalidate();
            }
        }

        /// <summary>
        /// 切换十字光标的显示/隐藏。
        /// </summary>
        public void ToggleCrosshair()
        {
            var crosshairLayer = GetLayer<CrosshairLayer>();
            if (crosshairLayer != null)
            {
                crosshairLayer.IsVisible = !crosshairLayer.IsVisible;
                Invalidate();
            }
        }
    }
}