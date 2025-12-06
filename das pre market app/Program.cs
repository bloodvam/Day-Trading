using System;
using System.Threading.Tasks;
using PreMarketTrader.Core;
using PreMarketTrader.Models;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Connecting to DAS...");

        var das = new DasClient("127.0.0.1", 9090);
 
        var barEngine = new BarEngine5s();

        // bar 收盘时打印出来（简单可视化）
        barEngine.BarClosed += bar =>
        {
            Console.WriteLine("[BAR] " + bar);
        };

        // 调试：打印所有原始消息
        // das.RawMessage += line => Console.WriteLine("[RAW] " + line);

        // 只关注 T&S
        das.TsReceived += line =>
        {
            var tick = DasTsParser.ParseTimeSales(line);
            if (tick != null)
            {
                barEngine.OnTick(tick);
            }
        };

        das.Connected += () => Console.WriteLine("Connected to DAS API!");
        das.Disconnected += () => Console.WriteLine("Disconnected from DAS API!");

        try
        {
            await das.ConnectAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Connect failed: " + ex.Message);
            return;
        }

        // 替换成你自己的登录信息
        await das.Login("19201", "Dqy32119322", "TR19201");
        Console.WriteLine("LOGIN sent.");

        // 订阅你要构建 5s k 线的股票，示例 AAPL
        await das.SubscribeTimeSales("AAPL");
        Console.WriteLine("SB AAPL tms sent.");

        Console.WriteLine("Building 5s bars... Press ENTER to quit.");
        Console.ReadLine();

        das.Disconnect();
    }
}
