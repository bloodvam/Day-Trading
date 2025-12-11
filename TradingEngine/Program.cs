namespace TradingEngine
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());

            // 测试版（取消注释切换）
            // Application.Run(new TestForm());
        }
    }
}