namespace TradingEngine.Models
{
    /// <summary>
    /// 账户信息
    /// </summary>
    public class AccountInfo
    {
        public double OpenEquity { get; set; }
        public double CurrentEquity { get; set; }
        public double RealizedPL { get; set; }
        public double UnrealizedPL { get; set; }
        public double NetPL { get; set; }
        public double HTBCost { get; set; }         // Hard to Borrow (Locate) Cost
        public double SecFee { get; set; }
        public double FINRAFee { get; set; }
        public double ECNFee { get; set; }
        public double Commission { get; set; }

        public double BuyingPower { get; set; }
        public double OvernightBP { get; set; }

        public DateTime UpdateTime { get; set; }

        public override string ToString()
        {
            return $"Equity:{CurrentEquity:F2} BP:{BuyingPower:F2} PL:{NetPL:F2}";
        }
    }
}