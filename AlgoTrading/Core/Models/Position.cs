namespace AlgoTrading.Core.Models
{
    /// <summary>
    /// 持仓信息
    /// </summary>
    public class Position
    {
        /// <summary>
        /// 股票代码
        /// </summary>
        public string Symbol { get; set; } = string.Empty;

        /// <summary>
        /// 持仓股数
        /// </summary>
        public int Shares { get; set; }

        /// <summary>
        /// 平均成本价
        /// </summary>
        public double AvgCost { get; set; }

        /// <summary>
        /// 总成本
        /// </summary>
        public double TotalCost => Shares * AvgCost;

        /// <summary>
        /// 是否有持仓
        /// </summary>
        public bool HasPosition => Shares > 0;

        /// <summary>
        /// 计算当前盈亏
        /// </summary>
        public double GetUnrealizedPnL(double currentPrice)
        {
            return Shares * (currentPrice - AvgCost);
        }

        /// <summary>
        /// 买入/加仓
        /// </summary>
        public void Buy(int shares, double price)
        {
            if (shares <= 0) return;

            var newTotalCost = TotalCost + (shares * price);
            var newShares = Shares + shares;
            AvgCost = newTotalCost / newShares;
            Shares = newShares;
        }

        /// <summary>
        /// 卖出
        /// </summary>
        /// <returns>实现盈亏</returns>
        public double Sell(int shares, double price)
        {
            if (shares <= 0 || shares > Shares) return 0;

            var pnl = shares * (price - AvgCost);
            Shares -= shares;

            if (Shares == 0)
            {
                AvgCost = 0;
            }

            return pnl;
        }

        /// <summary>
        /// 卖出一半
        /// </summary>
        public double SellHalf(double price)
        {
            var sharesToSell = Shares / 2;
            return Sell(sharesToSell, price);
        }

        /// <summary>
        /// 全部卖出
        /// </summary>
        public double SellAll(double price)
        {
            return Sell(Shares, price);
        }

        /// <summary>
        /// 重置
        /// </summary>
        public void Reset()
        {
            Shares = 0;
            AvgCost = 0;
        }

        public override string ToString()
        {
            return HasPosition
                ? $"{Symbol}: {Shares} shares @ {AvgCost:F2}"
                : $"{Symbol}: No position";
        }
    }
}