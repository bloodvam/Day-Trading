using System.Globalization;
using TradingEngine.Models;

namespace TradingEngine.Utils
{
    public static class MessageParser
    {
        /// <summary>
        /// 解析 $Quote 消息
        /// </summary>
        public static void ParseQuote(string line, Quote quote)
        {
            // $Quote MSFT A:26.33 Asz:2 B:26.32 Bsz:3 V:89765 L:26.35 Hi:0 Lo:0 op:0 ycl:25.96 tcl:0 PE:Q
            if (!line.StartsWith("$Quote ")) return;

            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) return;

            quote.Symbol = parts[1];
            quote.UpdateTime = DateTime.Now;

            for (int i = 2; i < parts.Length; i++)
            {
                var kv = parts[i].Split(':');
                if (kv.Length != 2) continue;

                string key = kv[0];
                string value = kv[1];

                switch (key)
                {
                    case "A":
                        quote.Ask = ParseDouble(value);
                        break;
                    case "Asz":
                        quote.AskSize = ParseInt(value);
                        break;
                    case "B":
                        quote.Bid = ParseDouble(value);
                        break;
                    case "Bsz":
                        quote.BidSize = ParseInt(value);
                        break;
                    case "V":
                        quote.Volume = ParseLong(value);
                        break;
                    case "L":
                        quote.Last = ParseDouble(value);
                        break;
                    case "Hi":
                        quote.High = ParseDouble(value);
                        break;
                    case "Lo":
                        quote.Low = ParseDouble(value);
                        break;
                    case "op":
                        quote.Open = ParseDouble(value);
                        break;
                    case "ycl":
                        quote.PrevClose = ParseDouble(value);
                        break;
                    case "tcl":
                        quote.TodayClose = ParseDouble(value);
                        break;
                    case "PE":
                        quote.PrimaryExchange = value;
                        break;
                    case "VWAP":
                        quote.VWAP = ParseDouble(value);
                        break;
                    case "T":
                        // 格式 HHMMSS，如 "093015" = 09:30:15
                        if (value.Length >= 6)
                        {
                            int hh = int.Parse(value.Substring(0, 2));
                            int mm = int.Parse(value.Substring(2, 2));
                            int ss = int.Parse(value.Substring(4, 2));
                            var today = DateTime.Today;
                            quote.UpdateTime = new DateTime(today.Year, today.Month, today.Day, hh, mm, ss);
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// 解析 $T&S 消息
        /// </summary>
        public static Tick? ParseTick(string line)
        {
            // $T&S AAPL 284.25 100 T 09:30:01 Q B 96
            if (!line.StartsWith("$T&S ")) return null;

            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 9) return null;

            try
            {
                string symbol = parts[1];
                double price = ParseDouble(parts[2]);
                int volume = ParseInt(parts[3]);
                // parts[4] 是 flag
                string timeStr = parts[5];
                string exchange = parts[6];
                char side = parts[7][0];
                int condition = ParseInt(parts[8]);

                var today = DateTime.Today;
                var tParts = timeStr.Split(':');
                int hh = int.Parse(tParts[0]);
                int mm = int.Parse(tParts[1]);
                int ss = int.Parse(tParts[2]);

                return new Tick
                {
                    Symbol = symbol,
                    Price = price,
                    Volume = volume,
                    Side = side,
                    Time = new DateTime(today.Year, today.Month, today.Day, hh, mm, ss, DateTimeKind.Local),
                    Condition = condition,
                    Exchange = exchange
                };
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 解析 %ORDER 消息
        /// </summary>
        public static Order? ParseOrder(string line)
        {
            // %ORDER id token symb b/s type qty lvqty cxlqty price route status time origoid account trader orderSrc
            if (!line.StartsWith("%ORDER ")) return null;

            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 13) return null;

            try
            {
                // 解析 Type 字段，可能是 "L", "M", "SL:160", "STOPLMTP:160" 等
                string typeField = parts[5];
                OrderType orderType;
                double stopPrice = 0;

                if (typeField.StartsWith("SL:"))
                {
                    orderType = OrderType.StopLimit;
                    stopPrice = ParseDouble(typeField.Substring(3));
                }
                else if (typeField.StartsWith("STOPLMTP:"))
                {
                    orderType = OrderType.StopLimitPost;
                    stopPrice = ParseDouble(typeField.Substring(9));
                }
                else if (typeField.StartsWith("STOPMKT:"))
                {
                    orderType = OrderType.StopMarket;
                    stopPrice = ParseDouble(typeField.Substring(8));
                }
                else
                {
                    orderType = ParseOrderType(typeField);
                }

                var order = new Order
                {
                    OrderId = ParseInt(parts[1]),
                    Token = ParseInt(parts[2]),
                    Symbol = parts[3],
                    Side = ParseSide(parts[4]),
                    Type = orderType,
                    Quantity = ParseInt(parts[6]),
                    LeftQuantity = ParseInt(parts[7]),
                    CanceledQuantity = ParseInt(parts[8]),
                    Price = ParseDouble(parts[9]),
                    StopPrice = stopPrice,
                    Route = parts[10],
                    Status = ParseOrderStatus(parts[11]),
                    Time = ParseTime(parts[12])
                };

                if (parts.Length > 14) order.Account = parts[14];

                order.FilledQuantity = order.Quantity - order.LeftQuantity - order.CanceledQuantity;

                return order;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 解析 %OrderAct 消息
        /// </summary>
        public static (int orderId, string action, int qty, double price, int token)? ParseOrderAction(string line)
        {
            // %OrderAct id ActionType B/S symbol qty price route time notes token
            if (!line.StartsWith("%OrderAct ")) return null;

            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 8) return null;

            try
            {
                int orderId = ParseInt(parts[1]);
                string action = parts[2];
                int qty = ParseInt(parts[5]);
                double price = ParseDouble(parts[6]);
                int token = parts.Length > 10 ? ParseInt(parts[10]) : 0;

                return (orderId, action, qty, price, token);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 解析 %TRADE 消息
        /// </summary>
        public static Trade? ParseTrade(string line)
        {
            // %TRADE id symb B/S qty price route time orderid Liq EcnFee PL
            if (!line.StartsWith("%TRADE ")) return null;

            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 9) return null;

            try
            {
                var trade = new Trade
                {
                    TradeId = ParseInt(parts[1]),
                    Symbol = parts[2],
                    Side = ParseSide(parts[3]),
                    Quantity = ParseInt(parts[4]),
                    Price = ParseDouble(parts[5]),
                    Route = parts[6],
                    Time = ParseTime(parts[7]),
                    OrderId = ParseInt(parts[8])
                };

                if (parts.Length > 9) trade.Liquidity = parts[9][0];
                if (parts.Length > 10) trade.ECNFee = ParseDouble(parts[10]);
                if (parts.Length > 11) trade.PL = ParseDouble(parts[11]);

                return trade;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 解析 %POS 消息
        /// </summary>
        public static Position? ParsePosition(string line)
        {
            // %POS Symbol Type Quantity AvgCost InitQuantity InitPrice Realized CreateTime Unrealized
            if (!line.StartsWith("%POS ") && !line.StartsWith("#POS ")) return null;
            if (line.StartsWith("#POSEND")) return null;

            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 7) return null;

            // #POS 是标题行
            if (parts[1] == "symb") return null;

            try
            {
                var pos = new Position
                {
                    Symbol = parts[1],
                    Type = (PositionType)ParseInt(parts[2]),
                    Quantity = ParseInt(parts[3]),
                    AvgCost = ParseDouble(parts[4]),
                    InitQuantity = ParseInt(parts[5]),
                    InitPrice = ParseDouble(parts[6])
                };

                if (parts.Length > 7) pos.RealizedPL = ParseDouble(parts[7]);
                if (parts.Length > 8) pos.CreateTime = ParseDateTime(parts[8]);
                if (parts.Length > 9) pos.UnrealizedPL = ParseDouble(parts[9]);

                return pos;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 解析 $AccountInfo 消息
        /// </summary>
        public static AccountInfo? ParseAccountInfo(string line)
        {
            // $AccountInfo OpenEQ CurrEQ RealizedPL UnrealizedPL NetPL HTBCost SecFee FINRAFee ECNFee Commission
            if (!line.StartsWith("$AccountInfo ")) return null;

            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 11) return null;

            try
            {
                return new AccountInfo
                {
                    OpenEquity = ParseDouble(parts[1]),
                    CurrentEquity = ParseDouble(parts[2]),
                    RealizedPL = ParseDouble(parts[3]),
                    UnrealizedPL = ParseDouble(parts[4]),
                    NetPL = ParseDouble(parts[5]),
                    HTBCost = ParseDouble(parts[6]),
                    SecFee = ParseDouble(parts[7]),
                    FINRAFee = ParseDouble(parts[8]),
                    ECNFee = ParseDouble(parts[9]),
                    Commission = ParseDouble(parts[10]),
                    UpdateTime = DateTime.Now
                };
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 解析 BP 消息
        /// </summary>
        public static (double bp, double overnightBp)? ParseBuyingPower(string line)
        {
            // BP 94339 100000
            if (!line.StartsWith("BP ")) return null;

            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3) return null;

            try
            {
                return (ParseDouble(parts[1]), ParseDouble(parts[2]));
            }
            catch
            {
                return null;
            }
        }

        #region Helper Methods

        private static double ParseDouble(string s)
        {
            return double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 0;
        }

        private static int ParseInt(string s)
        {
            return int.TryParse(s, out var v) ? v : 0;
        }

        private static long ParseLong(string s)
        {
            return long.TryParse(s, out var v) ? v : 0;
        }

        private static OrderSide ParseSide(string s)
        {
            return s.ToUpper() switch
            {
                "B" => OrderSide.Buy,
                "S" => OrderSide.Sell,
                "SS" => OrderSide.Short,
                "BUY" => OrderSide.Buy,
                "SELL" => OrderSide.Sell,
                "SHRT" => OrderSide.Short,
                _ => OrderSide.Buy
            };
        }

        private static OrderType ParseOrderType(string s)
        {
            return s.ToUpper() switch
            {
                "L" => OrderType.Limit,
                "M" => OrderType.Market,
                "MKT" => OrderType.Market,
                "STOPMKT" => OrderType.StopMarket,
                "STOPLMT" => OrderType.StopLimit,
                "STOPLMTP" => OrderType.StopLimitPost,
                _ => OrderType.Limit
            };
        }

        private static OrderStatus ParseOrderStatus(string s)
        {
            return s switch
            {
                "Hold" => OrderStatus.Hold,
                "Sending" => OrderStatus.Sending,
                "Accepted" => OrderStatus.Accepted,
                "Partial" => OrderStatus.Partial,
                "Executed" => OrderStatus.Executed,
                "Canceled" => OrderStatus.Canceled,
                "Rejected" => OrderStatus.Rejected,
                "Closed" => OrderStatus.Closed,
                "Triggered" => OrderStatus.Triggered,
                _ => OrderStatus.Hold
            };
        }

        private static DateTime ParseTime(string s)
        {
            // HH:MM:SS 或 HH:MM:SS.fff
            try
            {
                var parts = s.Split(':');
                if (parts.Length >= 3)
                {
                    int h = int.Parse(parts[0]);
                    int m = int.Parse(parts[1]);
                    int sec = int.Parse(parts[2].Split('.')[0]);
                    var today = DateTime.Today;
                    return new DateTime(today.Year, today.Month, today.Day, h, m, sec);
                }
            }
            catch { }
            return DateTime.Now;
        }

        private static DateTime ParseDateTime(string s)
        {
            // YYYY/MM/DD-HH:MM:SS
            try
            {
                if (DateTime.TryParseExact(s, "yyyy/MM/dd-HH:mm:ss", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var dt))
                {
                    return dt;
                }
            }
            catch { }
            return DateTime.Now;
        }

        #endregion
    }
}