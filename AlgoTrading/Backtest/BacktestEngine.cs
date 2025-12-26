using AlgoTrading.Core.Models;
using AlgoTrading.Core.Strategies;
using AlgoTrading.DataFeed;

namespace AlgoTrading.Backtest
{
    /// <summary>
    /// 回测引擎
    /// </summary>
    public class BacktestEngine
    {
        private readonly TradeDataReader _dataReader;
        private readonly DataValidator _dataValidator;
        private readonly TradeRecordManager _recordManager;
        private readonly BacktestConfig _config;

        public event Action<string>? Log;

        public BacktestEngine(BacktestConfig config)
        {
            _config = config;
            _dataReader = new TradeDataReader();
            _dataValidator = new DataValidator();
            _recordManager = new TradeRecordManager(config.ResultOutputPath);
        }

        /// <summary>
        /// 运行回测
        /// </summary>
        /// <param name="strategy">策略实例</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>是否成功完成</returns>
        public async Task<bool> RunAsync(IStrategy strategy, CancellationToken cancellationToken = default)
        {
            Log?.Invoke($"Starting backtest with strategy: {strategy.Name}");
            Log?.Invoke($"Mode: {_config.Mode}");

            // 1. 获取所有回测任务
            var tasks = _config.GetTasks();
            Log?.Invoke($"Total tasks: {tasks.Count}");

            if (tasks.Count == 0)
            {
                Log?.Invoke("No tasks to run. Check your config.");
                return false;
            }

            // 2. 验证数据是否存在
            Log?.Invoke("\nValidating data...");
            var validationResult = _dataValidator.Validate(tasks);
            DataValidator.PrintValidationResult(validationResult, Log);

            if (!validationResult.IsValid)
            {
                Log?.Invoke("ERROR: Missing data detected. Please download the required data first.");
                Log?.Invoke("Backtest aborted.");
                return false;
            }

            // 3. 执行回测
            Log?.Invoke("\n========== Starting Backtest ==========\n");

            foreach (var task in tasks)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await RunTaskAsync(strategy, task, cancellationToken);
            }

            // 4. 输出结果
            _recordManager.PrintSummary(Log);

            // 5. 保存交易记录
            await _recordManager.SaveToCsvAsync();
            Log?.Invoke($"Trade records saved to: {_config.ResultOutputPath}");

            return true;
        }

        /// <summary>
        /// 执行单个回测任务
        /// </summary>
        private async Task RunTaskAsync(IStrategy strategy, BacktestTask task, CancellationToken cancellationToken)
        {
            Log?.Invoke($"Processing: {task}");

            try
            {
                // 读取数据
                var trades = await _dataReader.ReadAsync(
                    task.Symbol,
                    task.Date,
                    task.StartTime,
                    task.EndTime);

                Log?.Invoke($"  Loaded {trades.Count} ticks");

                if (trades.Count == 0)
                {
                    Log?.Invoke($"  Warning: No data in specified time range");
                    return;
                }

                // 初始化策略
                strategy.Initialize(task.Symbol, task.Date);

                // 逐笔处理
                int signalCount = 0;
                foreach (var tick in trades)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // 调用策略处理 tick
                    strategy.OnTick(tick);

                    // 检查是否有信号
                    var signal = strategy.GetSignal();
                    if (signal != null)
                    {
                        signalCount++;
                        ExecuteSignal(strategy, task, signal, tick);
                    }
                }

                // 日结
                strategy.OnSessionEnd();

                // 检查是否有未平仓
                var position = strategy.GetPosition();
                if (position.HasPosition)
                {
                    Log?.Invoke($"  Warning: Unclosed position at session end: {position}");
                }

                Log?.Invoke($"  Completed: {signalCount} signals generated");
            }
            catch (Exception ex)
            {
                Log?.Invoke($"  Error: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行交易信号
        /// </summary>
        private void ExecuteSignal(IStrategy strategy, BacktestTask task, TradeSignal signal, Trade currentTick)
        {
            var position = strategy.GetPosition();

            // 确定执行股数
            int shares = signal.Shares ?? 100; // 默认 100 股，之后可以改

            if (signal.Type == SignalType.SellHalf)
            {
                shares = position.Shares / 2;
            }
            else if (signal.Type == SignalType.SellAll)
            {
                shares = position.Shares;
            }

            // 计算 PnL（卖出时）
            double? pnl = null;
            if (signal.Type == SignalType.SellHalf || signal.Type == SignalType.SellAll)
            {
                pnl = shares * (signal.Price - position.AvgCost);
            }

            // 记录交易
            var record = new TradeRecord
            {
                Symbol = task.Symbol,
                Date = task.Date,
                Type = signal.Type,
                Price = signal.Price,
                Time = signal.Time,
                Shares = shares,
                PnL = pnl,
                Reason = signal.Reason
            };
            _recordManager.Add(record);

            // 通知策略信号已执行
            strategy.OnSignalExecuted(signal, signal.Price, shares);

            Log?.Invoke($"    {record}");
        }

        /// <summary>
        /// 获取交易记录管理器
        /// </summary>
        public TradeRecordManager GetRecordManager() => _recordManager;
    }
}