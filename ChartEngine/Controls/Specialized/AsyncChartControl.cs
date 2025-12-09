using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ChartEngine.Data.Models;
using ChartEngine.Data.Loading;

namespace ChartEngine.Controls.ChartControls
{
    /// <summary>
    /// 支持异步数据加载的 ChartControl
    /// 继承自 ChartControl，增加异步加载功能
    /// 
    /// 使用示例:
    /// var chart = new AsyncChartControl();
    /// chart.LoadProgressChanged += (s, p) => progressBar.Value = p.PercentComplete;
    /// await chart.LoadDataAsync("AAPL", TimeFrame.Minute1);
    /// </summary>
    public class AsyncChartControl : ChartControl
    {
        #region 字段定义

        private readonly IAsyncDataLoader _dataLoader;
        private CancellationTokenSource _loadCancellationTokenSource;
        private bool _isLoading = false;

        #endregion

        #region 事件定义

        /// <summary>
        /// 数据加载进度变化事件
        /// </summary>
        public event EventHandler<DataLoadProgress> LoadProgressChanged;

        /// <summary>
        /// 数据加载完成事件
        /// </summary>
        public event EventHandler<ISeries> DataLoadCompleted;

        /// <summary>
        /// 数据加载失败事件
        /// </summary>
        public event EventHandler<Exception> DataLoadFailed;

        #endregion

        #region 构造函数

        public AsyncChartControl() : base()
        {
            _dataLoader = new AsyncDataLoader();
        }

        #endregion

        #region 公共属性

        /// <summary>
        /// 是否正在加载数据
        /// </summary>
        public bool IsLoading => _isLoading;

        #endregion

        #region 异步加载方法

        /// <summary>
        /// 异步加载数据
        /// </summary>
        /// <param name="symbol">股票代码</param>
        /// <param name="timeFrame">时间周期</param>
        /// <param name="startDate">起始日期（可选）</param>
        /// <param name="endDate">结束日期（可选）</param>
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

            try
            {
                _loadCancellationTokenSource = new CancellationTokenSource();

                // 创建进度报告器
                var progress = new Progress<DataLoadProgress>(p =>
                {
                    // 确保在UI线程触发事件
                    if (InvokeRequired)
                    {
                        BeginInvoke(new Action(() =>
                        {
                            LoadProgressChanged?.Invoke(this, p);
                            Invalidate(); // 触发重绘以显示进度
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
                    Invoke(new Action(() => SetLoadedData(series)));
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
                Invalidate();
            }
            catch (Exception ex)
            {
                _isLoading = false;
                Invalidate();

                // 触发失败事件
                if (InvokeRequired)
                {
                    BeginInvoke(new Action(() => DataLoadFailed?.Invoke(this, ex)));
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
        /// 取消当前的数据加载
        /// </summary>
        public void CancelDataLoad()
        {
            _loadCancellationTokenSource?.Cancel();
            _dataLoader?.CancelLoad();
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 设置加载完成的数据
        /// </summary>
        private void SetLoadedData(ISeries series)
        {
            // 使用基类的 Series 属性
            this.Series = series;

            _isLoading = false;

            // 触发完成事件
            DataLoadCompleted?.Invoke(this, series);

            // 重绘
            Invalidate();
        }

        #endregion

        #region 重写 OnPaint

        /// <summary>
        /// 绘制控件
        /// 如果正在加载，显示加载界面
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            // 如果正在加载，显示加载界面
            if (_isLoading)
            {
                DrawLoadingScreen(e.Graphics);
                return;
            }

            // 否则调用基类的正常渲染
            base.OnPaint(e);
        }

        /// <summary>
        /// 绘制加载界面
        /// </summary>
        private void DrawLoadingScreen(Graphics g)
        {
            // 绘制加载文字
            string message = "正在加载数据...";
            using (var font = new Font("Microsoft YaHei", 14))
            using (var brush = new SolidBrush(Color.White))
            {
                var size = g.MeasureString(message, font);
                float x = (Width - size.Width) / 2;
                float y = (Height - size.Height) / 2;
                g.DrawString(message, font, brush, x, y);
            }

            // 可以绘制进度条（如果需要的话）
            // DrawProgressBar(g, ...);
        }

        #endregion

        #region 资源清理

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

        #endregion
    }
}