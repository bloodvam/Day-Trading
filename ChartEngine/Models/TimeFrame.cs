namespace ChartEngine.Models
{
    /// <summary>
    /// K线时间周期
    /// </summary>
    public enum TimeFrame
    {
        /// <summary>5秒</summary>
        Second5,

        /// <summary>10秒</summary>
        Second10,

        /// <summary>30秒</summary>
        Second30,

        /// <summary>1分钟</summary>
        Minute1,

        /// <summary>5分钟</summary>
        Minute5,

        /// <summary>15分钟</summary>
        Minute15,

        /// <summary>30分钟</summary>
        Minute30,

        /// <summary>1小时</summary>
        Hour1,

        /// <summary>4小时</summary>
        Hour4,

        /// <summary>1天</summary>
        Day,

        /// <summary>1周</summary>
        Week,

        /// <summary>1月</summary>
        Month
    }
}