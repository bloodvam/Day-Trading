namespace TradingEngine.Models
{
    /// <summary>
    /// Level 1 报价数据
    /// </summary>
    public class Quote
    {
        public string Symbol { get; set; } = "";
        public double Ask { get; set; }
        public int AskSize { get; set; }
        public double Bid { get; set; }
        public int BidSize { get; set; }
        public double Last { get; set; }
        public double High { get; set; }
        public double Low { get; set; }
        public double Open { get; set; }
        public double PrevClose { get; set; }
        public double TodayClose { get; set; }
        public long Volume { get; set; }
        public double VWAP { get; set; }
        public string PrimaryExchange { get; set; } = "";
        public DateTime UpdateTime { get; set; }

        public override string ToString()
        {
            return $"{Symbol} Bid:{Bid}x{BidSize} Ask:{Ask}x{AskSize} Last:{Last} Vol:{Volume}";
        }
    }
}