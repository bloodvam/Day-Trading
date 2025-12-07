using System;
using System.Threading;
using System.Threading.Tasks;
using ChartEngine.Models;

namespace ChartEngine.DataLoading
{
    /// <summary>
    /// 异步数据加载器实现
    /// </summary>
    public class AsyncDataLoader : IAsyncDataLoader
    {
        private readonly DataPreProcessor _preProcessor;
        private CancellationTokenSource _cancellationTokenSource;

        public AsyncDataLoader()
        {
            _preProcessor = new DataPreProcessor();
        }

        /// <summary>
        /// 异步加载数据
        /// </summary>
        public async Task<ISeries> LoadAsync(
            string symbol,
            TimeFrame timeFrame,
            DateTime? startDate = null,
            DateTime? endDate = null,
            IProgress<DataLoadProgress> progress = null,
            CancellationToken cancellationToken = default)
        {
            // 创建内部取消令牌，可以被外部取消或手动取消
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            try
            {
                // 阶段1: 初始化
                ReportProgress(progress, 0, 100, DataLoadStage.Initializing, "初始化数据加载...");
                await Task.Delay(100, _cancellationTokenSource.Token); // 模拟初始化

                // 阶段2: 加载原始数据
                ReportProgress(progress, 10, 100, DataLoadStage.LoadingData, $"加载 {symbol} 数据...");

                var series = await LoadRawDataAsync(
                    symbol,
                    timeFrame,
                    startDate,
                    endDate,
                    _cancellationTokenSource.Token);

                ReportProgress(progress, 30, 100, DataLoadStage.LoadingData, $"已加载 {series.Count} 根K线");

                // 阶段3: 预处理数据
                var preprocessProgress = new Progress<DataLoadProgress>(p =>
                {
                    // 将预处理进度映射到30-100范围
                    int overallProgress = 30 + (int)(p.PercentComplete * 0.7);
                    progress?.Report(new DataLoadProgress
                    {
                        CurrentIndex = overallProgress,
                        TotalCount = 100,
                        Stage = p.Stage,
                        StatusMessage = p.StatusMessage
                    });
                });

                var preprocessedData = await _preProcessor.PreProcessAsync(
                    series,
                    preprocessProgress,
                    _cancellationTokenSource.Token);

                // 阶段4: 完成
                ReportProgress(progress, 100, 100, DataLoadStage.Completed, "数据加载完成");

                return preprocessedData.Series;
            }
            catch (OperationCanceledException)
            {
                ReportProgress(progress, 0, 100, DataLoadStage.Initializing, "加载已取消");
                throw;
            }
            finally
            {
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        /// <summary>
        /// 取消当前加载
        /// </summary>
        public void CancelLoad()
        {
            _cancellationTokenSource?.Cancel();
        }

        /// <summary>
        /// 加载原始数据（实际项目中应该从数据源加载）
        /// </summary>
        private async Task<ISeries> LoadRawDataAsync(
            string symbol,
            TimeFrame timeFrame,
            DateTime? startDate,
            DateTime? endDate,
            CancellationToken cancellationToken)
        {
            // 模拟数据加载延迟
            await Task.Run(async () =>
            {
                await Task.Delay(500, cancellationToken); // 模拟网络延迟
            }, cancellationToken);

            // 实际项目中，这里应该：
            // 1. 从数据库读取
            // 2. 从API获取
            // 3. 从文件加载
            // 等等...

            // 这里生成测试数据
            return GenerateTestData(symbol, timeFrame, startDate, endDate);
        }

        /// <summary>
        /// 生成测试数据
        /// </summary>
        private ISeries GenerateTestData(
            string symbol,
            TimeFrame timeFrame,
            DateTime? startDate,
            DateTime? endDate)
        {
            var series = new SimpleSeries();
            var rnd = new Random();

            DateTime start = startDate ?? DateTime.Now.AddDays(-30);
            DateTime end = endDate ?? DateTime.Now;

            double price = 100;
            DateTime currentTime = start;

            while (currentTime <= end)
            {
                double open = price;
                double close = open + rnd.Next(-3, 4);
                double high = Math.Max(open, close) + rnd.Next(1, 5);
                double low = Math.Min(open, close) - rnd.Next(1, 5);
                double vol = rnd.Next(500, 5000);

                series.AddBar(open, high, low, close, vol, currentTime);

                price = close;
                currentTime = GetNextBarTime(currentTime, timeFrame);
            }

            return series;
        }

        /// <summary>
        /// 获取下一根K线时间
        /// </summary>
        private DateTime GetNextBarTime(DateTime current, TimeFrame timeFrame)
        {
            return timeFrame switch
            {
                TimeFrame.Minute1 => current.AddMinutes(1),
                TimeFrame.Minute5 => current.AddMinutes(5),
                TimeFrame.Minute15 => current.AddMinutes(15),
                TimeFrame.Minute30 => current.AddMinutes(30),
                TimeFrame.Hour1 => current.AddHours(1),
                TimeFrame.Hour4 => current.AddHours(4),
                TimeFrame.Day => current.AddDays(1),
                _ => current.AddMinutes(1)
            };
        }

        /// <summary>
        /// 报告进度
        /// </summary>
        private void ReportProgress(
            IProgress<DataLoadProgress> progress,
            int current,
            int total,
            DataLoadStage stage,
            string message)
        {
            progress?.Report(new DataLoadProgress
            {
                CurrentIndex = current,
                TotalCount = total,
                Stage = stage,
                StatusMessage = message
            });
        }
    }
}