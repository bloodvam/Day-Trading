using AlgoTrading.Core.Models;

namespace AlgoTrading.Core.Strategies
{
    /// <summary>
    /// 策略接口
    /// </summary>
    public interface IStrategy
    {
        /// <summary>
        /// 策略名称
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 初始化（每个股票/日期开始前调用）
        /// </summary>
        /// <param name="symbol">股票代码</param>
        /// <param name="date">日期</param>
        void Initialize(string symbol, DateTime date);

        /// <summary>
        /// 每笔 tick 数据触发
        /// </summary>
        /// <param name="tick">tick 数据</param>
        void OnTick(Trade tick);

        /// <summary>
        /// 获取待执行的交易信号
        /// </summary>
        /// <returns>交易信号，无信号返回 null</returns>
        TradeSignal? GetSignal();

        /// <summary>
        /// 信号已被执行的回调
        /// </summary>
        /// <param name="signal">已执行的信号</param>
        /// <param name="executedPrice">实际执行价格</param>
        /// <param name="executedShares">实际执行股数</param>
        void OnSignalExecuted(TradeSignal signal, double executedPrice, int executedShares);

        /// <summary>
        /// 日结（每个股票/日期结束后调用）
        /// </summary>
        void OnSessionEnd();

        /// <summary>
        /// 获取当前持仓
        /// </summary>
        Position GetPosition();
    }
}