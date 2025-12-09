// ChartEngine/Config/TradingSessionConfig.cs
using System;

namespace ChartEngine.Config
{
    /// <summary>
    /// 交易时段配置（美东时间）
    /// </summary>
    public class TradingSessionConfig
    {
        /// <summary>盘前开始时间 (04:00)</summary>
        public TimeSpan PreMarketStart { get; set; } = new TimeSpan(4, 0, 0);

        /// <summary>盘中开始时间 (09:30)</summary>
        public TimeSpan RegularStart { get; set; } = new TimeSpan(9, 30, 0);

        /// <summary>盘中结束时间 (16:00)</summary>
        public TimeSpan RegularEnd { get; set; } = new TimeSpan(16, 0, 0);

        /// <summary>盘后结束时间 (20:00)</summary>
        public TimeSpan AfterHoursEnd { get; set; } = new TimeSpan(20, 0, 0);

        /// <summary>获取美股默认配置</summary>
        public static TradingSessionConfig GetUSStockDefault()
        {
            return new TradingSessionConfig();
        }
    }
}