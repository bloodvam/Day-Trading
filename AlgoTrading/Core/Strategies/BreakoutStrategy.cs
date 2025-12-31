using AlgoTrading.Backtest;
using AlgoTrading.Core.Models;

namespace AlgoTrading.Core.Strategies
{
    /// <summary>
    /// 独立仓位单元（每次买入创建一个）
    /// </summary>
    public class PositionUnit
    {
        public int Id { get; set; }
        public double EntryPrice { get; set; }
        public int Shares { get; set; }
        public double StopLoss { get; set; }
        public double HighSinceEntry { get; set; }
        public DateTime EntryTime { get; set; }
        public int BarsSinceEntry { get; set; }
        public bool ShouldExitOnTimeout { get; set; }
        public double TimeoutExitPrice { get; set; }
        public double EntryLevel { get; set; }
    }

    /// <summary>
    /// 整数位/半数位突破策略
    /// 
    /// 买入条件（三个都要满足）：
    /// 1. 发生上穿事件：previousPrice < level + 0.05 且 price >= level + 0.05
    /// 2. 入场点 > _breakedLevel：floor(price * 2) / 2 > _breakedLevel
    /// 3. 不太近：price > sessionHigh（创新高）或 sessionHigh - price >= 0.5
    /// 
    /// _breakedLevel 更新规则：
    /// - 初始化：ceil(price * 2) / 2
    /// - 上穿：floor(price * 2) / 2
    /// - 下破：ceil(price * 2) / 2
    /// </summary>
    public class BreakoutStrategy : IStrategy
    {
        public string Name => "Breakout Strategy";

        #region 配置参数

        /// <summary>
        /// 风险金额（R）
        /// </summary>
        public double RiskAmount { get; set; } = 100;

        /// <summary>
        /// 突破确认偏移量
        /// </summary>
        public double BreakoutConfirmOffset { get; set; } = 0.05;

        /// <summary>
        /// 距离最高点的最小距离
        /// </summary>
        public double MinDistanceFromHigh { get; set; } = 0.50;

        /// <summary>
        /// 是否启用 Bar 超时卖出（买入后下一根 Bar 收盘价 < 买入价则卖出）
        /// </summary>
        public bool EnableBarTimeoutExit { get; set; } = false;

        /// <summary>
        /// 超时后盈利时，是否将止损移到成本价（需要 EnableBarTimeoutExit 开启）
        /// </summary>
        public bool EnableMoveStopToCost { get; set; } = false;

        /// <summary>
        /// 最小累计成交量阈值（达到后才开始交易，0 表示不限制）
        /// </summary>
        public long MinVolumeThreshold { get; set; } = 0;

        /// <summary>
        /// ATR 周期（用于 Trailing Stop）
        /// </summary>
        public int AtrPeriod { get; set; } = 14;

        /// <summary>
        /// 是否允许多仓位（每个信号都买入，独立止损止盈）
        /// </summary>
        public bool EnableMultiplePositions { get; set; } = false;

        #endregion

        #region 状态变量

        private string _symbol = string.Empty;
        private DateTime _date;
        private Position _position = new();
        private List<TradeSignal> _pendingSignals = new();

        // Session 最高价（已完成 bar 的最高）
        private double _sessionHigh;

        // 当前 bar 内的最高价
        private double _currentBarHigh;

        // 最近一次穿越的关键位
        private double _breakedLevel;

        // 上一笔 tick 的价格
        private double _previousPrice;

        // 5s Bar 聚合器
        private readonly BarAggregator _barAggregator;

        // 累计成交量
        private long _cumulativeVolume;

        // 仓位管理（统一用 List，单仓位模式下限制只能有一个）
        private List<PositionUnit> _positionUnits = new();
        private int _nextUnitId = 1;

        #endregion

        public BreakoutStrategy()
        {
            _barAggregator = new BarAggregator(5);
            _barAggregator.BarCompleted += OnBarCompleted;
            _barAggregator.BarStarted += OnBarStarted;
        }

        /// <summary>
        /// Bar 完成时更新 SessionHigh，检查 Bar Timeout Exit
        /// </summary>
        private void OnBarCompleted(Bar bar)
        {
            if (bar.High > _sessionHigh)
            {
                _sessionHigh = bar.High;
            }

            // Bar Timeout Exit 检查（统一逻辑）
            if (EnableBarTimeoutExit)
            {
                foreach (var unit in _positionUnits)
                {
                    unit.BarsSinceEntry++;
                    if (unit.BarsSinceEntry == 2)
                    {
                        if (bar.Close < unit.EntryPrice)
                        {
                            // 亏损 → 标记超时卖出
                            unit.ShouldExitOnTimeout = true;
                            unit.TimeoutExitPrice = bar.Close;
                        }
                        else if (EnableMoveStopToCost)
                        {
                            // 盈利 → 止损移到成本价
                            unit.StopLoss = unit.EntryPrice;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 新 Bar 开始时重置 CurrentBarHigh
        /// </summary>
        private void OnBarStarted(Bar bar)
        {
            _currentBarHigh = bar.High;
        }

        public void Initialize(string symbol, DateTime date)
        {
            _symbol = symbol;
            _date = date;
            _position = new Position { Symbol = symbol };
            _pendingSignals.Clear();
            _previousPrice = 0;
            _sessionHigh = 0;
            _currentBarHigh = 0;
            _breakedLevel = 0;
            _cumulativeVolume = 0;

            // 仓位管理重置
            _positionUnits.Clear();
            _nextUnitId = 1;

            // 设置 ATR 周期并重置
            _barAggregator.AtrPeriod = AtrPeriod;
            _barAggregator.Reset();
        }

        public void OnTick(Trade tick)
        {
            double price = tick.Price;

            // 更新累计成交量
            _cumulativeVolume += tick.Size;

            // 更新 5s Bar（事件会更新 sessionHigh 和 currentBarHigh）
            _barAggregator.ProcessTrade(tick);

            // 检查卖出信号（统一逻辑）
            if (_positionUnits.Count > 0)
            {
                CheckSellSignals(price, tick.SipTimestampEt);
            }

            // 初始化（第一笔 tick）
            if (_previousPrice == 0)
            {
                // 视为刚下破，_breakedLevel = ceil(price * 2) / 2
                _breakedLevel = CeilLevel(price);
                _previousPrice = price;
                return;
            }

            // 计算当前价格所在区间
            double currentLevel = FloorLevel(price);
            double previousLevel = FloorLevel(_previousPrice);

            // 检测穿越事件
            bool crossedUp = false;
            bool crossedDown = false;

            if (price > _previousPrice)
            {
                // 检查是否上穿当前关键位
                double confirmPrice = Math.Round(currentLevel + BreakoutConfirmOffset, 2);
                if (_previousPrice < confirmPrice && price >= confirmPrice)
                {
                    crossedUp = true;
                }
            }
            else if (price < _previousPrice)
            {
                // 检查是否下破：当前关键位 < 之前关键位
                if (price <= previousLevel)
                {
                    crossedDown = true;
                }
            }

            // 检查买入信号
            if (crossedUp)
            {
                double entryLevel = FloorLevel(price - BreakoutConfirmOffset);
                double sessionHighLevel = FloorLevel(_sessionHigh);
                double currentHighLevel = FloorLevel(_currentBarHigh - BreakoutConfirmOffset);

                // 条件 2：入场点 > _breakedLevel 且距离够远
                // 条件 3：创新高（entryLevel > sessionHighLevel）
                double distance = Math.Round(_sessionHigh - entryLevel, 2);
                bool condition1 = entryLevel > _breakedLevel;
                bool condition2 = entryLevel > _breakedLevel && distance >= MinDistanceFromHigh && entryLevel > currentHighLevel;
                bool condition3 = entryLevel > sessionHighLevel && entryLevel > _breakedLevel && entryLevel > currentHighLevel;

                // 检查成交量是否达到阈值
                bool volumeReached = MinVolumeThreshold == 0 || _cumulativeVolume >= MinVolumeThreshold;

                // 买入条件：多仓位模式始终允许，单仓位模式需要无持仓
                bool canBuy = EnableMultiplePositions || _positionUnits.Count == 0;

                if (canBuy && volumeReached && (condition2 || condition3))
                {
                    // 计算止损价
                    double stopLoss = CalculateStopLoss(entryLevel);

                    // 计算仓位
                    int shares = CalculateShares(price, stopLoss);

                    if (shares > 0)
                    {
                        // 创建新的仓位单元
                        var unit = new PositionUnit
                        {
                            Id = _nextUnitId++,
                            EntryPrice = price,
                            Shares = shares,
                            StopLoss = stopLoss,
                            HighSinceEntry = price,
                            EntryTime = tick.SipTimestampEt,
                            BarsSinceEntry = 0,
                            ShouldExitOnTimeout = false,
                            TimeoutExitPrice = 0,
                            EntryLevel = entryLevel
                        };
                        _positionUnits.Add(unit);

                        // 生成买入信号
                        _pendingSignals.Add(new TradeSignal
                        {
                            Type = SignalType.Buy,
                            Price = price,
                            Time = tick.SipTimestampEt,
                            Shares = shares,
                            Reason = $"[#{unit.Id}] 突破 ${entryLevel:F2}, 止损 ${stopLoss:F2}"
                        });
                    }
                }
                if (condition1)
                {
                    _breakedLevel = FloorLevel(price);
                }
            }
            else if (crossedDown)
            {
                // 下破：只有新关键位 < 当前 _breakedLevel 才更新
                double newLevel = CeilLevel(price);
                if (newLevel < _breakedLevel)
                {
                    _breakedLevel = newLevel;
                }
            }
            // 更新当前 bar 内最高价
            if (price > _currentBarHigh)
            {
                _currentBarHigh = price;
            }
            _previousPrice = price;
        }

        /// <summary>
        /// 检查所有仓位单元的卖出信号（统一逻辑）
        /// </summary>
        private void CheckSellSignals(double price, DateTime time)
        {
            var unitsToRemove = new List<PositionUnit>();
            double atr = _barAggregator.CurrentATR;
            if (atr <= 0) atr = 0.5;

            foreach (var unit in _positionUnits)
            {
                TradeSignal? sellSignal = null;

                // 策略0：Bar Timeout Exit
                if (unit.ShouldExitOnTimeout)
                {
                    unit.ShouldExitOnTimeout = false;
                    sellSignal = new TradeSignal
                    {
                        Type = SignalType.SellAll,
                        Price = price,
                        Time = time,
                        Shares = unit.Shares,
                        EntryPrice = unit.EntryPrice,
                        Reason = $"[#{unit.Id}] Bar超时: Bar1收盘 ${unit.TimeoutExitPrice:F2} < 买入 ${unit.EntryPrice:F2}"
                    };
                }
                // 策略1：硬止损
                else if (price <= Math.Round(unit.StopLoss - 0.01, 2))
                {
                    sellSignal = new TradeSignal
                    {
                        Type = SignalType.SellAll,
                        Price = price,
                        Time = time,
                        Shares = unit.Shares,
                        EntryPrice = unit.EntryPrice,
                        Reason = $"[#{unit.Id}] 硬止损: ${price:F2} <= ${unit.StopLoss:F2}-0.01"
                    };
                }
                else
                {
                    // 更新最高价
                    if (price > unit.HighSinceEntry)
                    {
                        unit.HighSinceEntry = price;
                    }

                    // 策略2：Trailing Stop
                    double trailingTrigger = Math.Round(unit.HighSinceEntry - 4, 2);
                    if (price <= trailingTrigger)
                    {
                        sellSignal = new TradeSignal
                        {
                            Type = SignalType.SellAll,
                            Price = price,
                            Time = time,
                            Shares = unit.Shares,
                            EntryPrice = unit.EntryPrice,
                            Reason = $"[#{unit.Id}] Trailing: ${price:F2} <= 最高${unit.HighSinceEntry:F2} - ATR${atr:F2}"
                        };
                    }
                }

                if (sellSignal != null)
                {
                    _pendingSignals.Add(sellSignal);
                    unitsToRemove.Add(unit);
                }
            }

            // 移除已卖出的仓位单元
            foreach (var unit in unitsToRemove)
            {
                _positionUnits.Remove(unit);
            }
        }

        /// <summary>
        /// 计算止损价
        /// 止损 = max(当前 5s bar 低点, 入场关键位 - 0.51)
        /// </summary>
        private double CalculateStopLoss(double entryLevel)
        {
            double maxStopLoss = Math.Round(entryLevel - 0.51, 2);

            var currentBar = _barAggregator.CurrentBar;
            if (currentBar != null)
            {
                return Math.Max(currentBar.Low, maxStopLoss);
            }

            return maxStopLoss;
        }

        /// <summary>
        /// 计算仓位（股数）
        /// shares = R / (入场价 - 止损价)
        /// </summary>
        private int CalculateShares(double entryPrice, double stopLoss)
        {
            double risk = Math.Round(entryPrice - stopLoss, 2);
            if (risk <= 0) return 0;

            return (int)(RiskAmount / risk);
        }

        public TradeSignal? GetSignal()
        {
            if (_pendingSignals.Count > 0)
            {
                var signal = _pendingSignals[0];
                _pendingSignals.RemoveAt(0);
                return signal;
            }
            return null;
        }

        public void OnSignalExecuted(TradeSignal signal, double executedPrice, int executedShares)
        {
            switch (signal.Type)
            {
                case SignalType.Buy:
                case SignalType.AddPosition:
                    _position.Buy(executedShares, executedPrice);
                    break;

                case SignalType.SellHalf:
                    _position.SellHalf(executedPrice);
                    break;

                case SignalType.SellAll:
                    _position.Sell(executedShares, executedPrice);
                    break;
            }
        }

        public void OnSessionEnd()
        {
            _barAggregator.FlushCurrentBar();
        }

        public Position GetPosition()
        {
            return _position;
        }

        #region 辅助方法

        /// <summary>
        /// 获取价格所在的关键位（向下取整到 0.5）
        /// </summary>
        private static double FloorLevel(double price)
        {
            return Math.Floor(Math.Round(price * 2, 2)) / 2;
        }

        /// <summary>
        /// 获取价格所在的关键位（向上取整到 0.5）
        /// </summary>
        private static double CeilLevel(double price)
        {
            return Math.Ceiling(Math.Round(price * 2, 2)) / 2;
        }

        #endregion
    }
}