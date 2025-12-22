namespace AlgoTrading.Core.Models
{
    /// <summary>
    /// Polygon API 返回的原始 Trade 数据
    /// </summary>
    public class Trade
    {
        /// <summary>
        /// 交易所撮合成交的时间（纳秒）
        /// </summary>
        public long ParticipantTimestamp { get; set; }

        /// <summary>
        /// 交易所时间（美东时间，可读格式）
        /// </summary>
        public DateTime ParticipantTimestampEt { get; set; }

        /// <summary>
        /// SIP 收到数据的时间（纳秒）
        /// </summary>
        public long SipTimestamp { get; set; }

        /// <summary>
        /// SIP 时间（美东时间，可读格式）
        /// </summary>
        public DateTime SipTimestampEt { get; set; }

        /// <summary>
        /// 成交价格
        /// </summary>
        public double Price { get; set; }

        /// <summary>
        /// 成交数量
        /// </summary>
        public int Size { get; set; }

        /// <summary>
        /// 成交条件代码（如 12=盘前, 37=碎股）
        /// </summary>
        public int[] Conditions { get; set; } = Array.Empty<int>();

        /// <summary>
        /// 更正标志（0=正常, 1=取消, 2=更正后）
        /// </summary>
        public int Correction { get; set; }

        /// <summary>
        /// 交易所 ID
        /// </summary>
        public int Exchange { get; set; }

        /// <summary>
        /// Trade Reporting Facility ID（0=交易所, 其他=暗池）
        /// </summary>
        public int TrfId { get; set; }

        /// <summary>
        /// 将 Conditions 数组转为逗号分隔字符串
        /// </summary>
        public string ConditionsString => string.Join(",", Conditions);
    }
}