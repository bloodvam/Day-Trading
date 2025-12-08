using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ChartEngine.Data.Models;
using ChartEngine.Data.Loading;
using ChartEngine.Rendering;

namespace ChartEngine.Controls.Specialized
{
    /// <summary>
    /// 集成异步加载和脏区域渲染的优化 ChartControl
    /// </summary>
    public partial class OptimizedChartControl : Control
    {
        // ========== 异步数据加载 ==========
        private readonly IAsyncDataLoader _dataLoader;
        private CancellationTokenSource _loadCancellationTokenSource;
        private bool _isLoading = false;



        // ========== 原有组件 ==========
        private readonly RenderResourcePool _resourcePool;
        private ISeries _series;
        private bool _isDataReady = false;

        // ========== 加载进度 ==========
        public event EventHandler<DataLoadProgress> LoadProgressChanged;
        public event EventHandler<ISeries> DataLoadCompleted;
        public event EventHandler<Exception> DataLoadFailed;

        public OptimizedChartControl()
        {
            DoubleBuffered = true; // 仍然启用双缓冲作为后备

            // 初始化组件
            _resourcePool = new RenderResourcePool();
            _dataLoader = new AsyncDataLoader();

            // 其他初始化...
        }



        /// <summary>
        /// 是否正在加载数据
        /// </summary>
        public bool IsLoading => _isLoading;

        /// <summary>
        /// 异步加载数据
        /// </summary>
        public async Task LoadDataAsync(
            string symbol,
            TimeFrame timeFrame,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            // 取消之前的加载
            if (_isLoading)
            {
                CancelDataLoad();
            }

            _isLoading = true;
            _isDataReady = false;

            try
            {
                _loadCancellationTokenSource = new CancellationTokenSource();

                // 创建进度报告器
                var progress = new Progress<DataLoadProgress>(p =>
                {
                    // 在UI线程触发事件
                    if (InvokeRequired)
                    {
                        BeginInvoke(new Action(() =>
                        {
                            LoadProgressChanged?.Invoke(this, p);
                            Invalidate(); // 触发重绘进度条
                        }));
                    }
                    else
                    {
                        LoadProgressChanged?.Invoke(this, p);
                        Invalidate();
                    }
                });

                // 异步加载数据
                var series = await _dataLoader.LoadAsync(
                    symbol,
                    timeFrame,
                    startDate,
                    endDate,
                    progress,
                    _loadCancellationTokenSource.Token);

                // 在UI线程设置数据
                if (InvokeRequired)
                {
                    Invoke(new Action(() =>
                    {
                        SetLoadedData(series);
                    }));
                }
                else
                {
                    SetLoadedData(series);
                }
            }
            catch (OperationCanceledException)
            {
                // 加载被取消
                _isLoading = false;
            }
            catch (Exception ex)
            {
                _isLoading = false;

                if (InvokeRequired)
                {
                    BeginInvoke(new Action(() =>
                    {
                        DataLoadFailed?.Invoke(this, ex);
                    }));
                }
                else
                {
                    DataLoadFailed?.Invoke(this, ex);
                }
            }
            finally
            {
                _loadCancellationTokenSource?.Dispose();
                _loadCancellationTokenSource = null;
            }
        }

        /// <summary>
        /// 设置加载完成的数据
        /// </summary>
        private void SetLoadedData(ISeries series)
        {
            _series = series;
            _isDataReady = true;
            _isLoading = false;


            Invalidate();

            // 触发完成事件
            DataLoadCompleted?.Invoke(this, series);
        }

        /// <summary>
        /// 取消数据加载
        /// </summary>
        public void CancelDataLoad()
        {
            _loadCancellationTokenSource?.Cancel();
            _dataLoader?.CancelLoad();
        }





        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (_isLoading)
            {
                DrawLoadingScreen(e.Graphics);
                return;
            }

            if (!_isDataReady || _series == null || _series.Count == 0)
            {
                DrawNoDataMessage(e.Graphics);
                return;
            }


            else
            {
                RenderFull(e.Graphics);
            }
        }

        /// <summary>
        /// 全屏渲染
        /// </summary>
        private void RenderFull(Graphics g)
        {
            RenderAllLayers(g);
        }

        /// <summary>
        /// 渲染所有图层（实际渲染逻辑）
        /// </summary>
        private void RenderAllLayers(Graphics g)
        {
            // 这里应该调用原有的渲染逻辑
            // 例如：RenderAll(g);

            // 简化示例：绘制背景
            using (var brush = new SolidBrush(Color.FromArgb(20, 20, 20)))
            {
                g.FillRectangle(brush, ClientRectangle);
            }

            // 绘制K线、成交量等...
            // foreach (var layer in _layers)
            // {
            //     if (layer.IsVisible)
            //         layer.Render(ctx);
            // }
        }

        /// <summary>
        /// 绘制加载界面
        /// </summary>
        private void DrawLoadingScreen(Graphics g)
        {
            // 清除背景
            g.Clear(Color.FromArgb(20, 20, 20));

            // 绘制加载文字
            string message = "正在加载数据...";
            using (var font = new Font("Arial", 14))
            using (var brush = new SolidBrush(Color.White))
            {
                var size = g.MeasureString(message, font);
                float x = (Width - size.Width) / 2;
                float y = (Height - size.Height) / 2;
                g.DrawString(message, font, brush, x, y);
            }

            // 可以绘制进度条
            // DrawProgressBar(g, ...);
        }

        /// <summary>
        /// 绘制无数据提示
        /// </summary>
        private void DrawNoDataMessage(Graphics g)
        {
            g.Clear(Color.FromArgb(20, 20, 20));

            string message = "无数据";
            using (var font = new Font("Arial", 14))
            using (var brush = new SolidBrush(Color.Gray))
            {
                var size = g.MeasureString(message, font);
                float x = (Width - size.Width) / 2;
                float y = (Height - size.Height) / 2;
                g.DrawString(message, font, brush, x, y);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // 取消加载
                CancelDataLoad();
                _loadCancellationTokenSource?.Dispose();

            }

            base.Dispose(disposing);
        }


    }
}