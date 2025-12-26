using AlgoTrading.Backtest;
using AlgoTrading.Core.Models;

namespace AlgoTrading.Core.Strategies
{
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

        #endregion

        #region 状态变量

        private string _symbol = string.Empty;
        private DateTime _date;
        private Position _position = new();
        private TradeSignal? _pendingSignal;

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

        #endregion

        public BreakoutStrategy()
        {
            _barAggregator = new BarAggregator(5);
            _barAggregator.BarCompleted += OnBarCompleted;
            _barAggregator.BarStarted += OnBarStarted;
        }

        /// <summary>
        /// Bar 完成时更新 SessionHigh
        /// </summary>
        private void OnBarCompleted(Bar bar)
        {
            if (bar.High > _sessionHigh)
            {
                _sessionHigh = bar.High;
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
            _pendingSignal = null;
            _previousPrice = 0;
            _sessionHigh = 0;
            _currentBarHigh = 0;
            _breakedLevel = 0;
            _barAggregator.Reset();
        }

        public void OnTick(Trade tick)
        {
            double price = tick.Price;

            // 更新 5s Bar（事件会更新 sessionHigh 和 currentBarHigh）
            _barAggregator.ProcessTrade(tick);



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


                if (condition2 || condition3)
                {
                    // 计算止损价
                    double stopLoss = CalculateStopLoss(entryLevel);

                    // 计算仓位
                    int shares = CalculateShares(price, stopLoss);

                    if (shares > 0)
                    {
                        // 生成买入信号
                        _pendingSignal = new TradeSignal
                        {
                            Type = SignalType.Buy,
                            Price = price,
                            Time = tick.SipTimestampEt,
                            Shares = shares,
                            Reason = $"突破 ${entryLevel:F2}, 止损 ${stopLoss:F2}, SessionHigh ${_sessionHigh:F2}, BreakedLevel ${_breakedLevel:F2}"
                        };
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
            var signal = _pendingSignal;
            _pendingSignal = null;
            return signal;
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
                    _position.SellAll(executedPrice);
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