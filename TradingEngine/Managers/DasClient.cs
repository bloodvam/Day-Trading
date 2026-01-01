using System.Net.Sockets;
using System.Text;
using TradingEngine.Config;
namespace TradingEngine.Managers
{
    public class DasClient : IDisposable
    {
        private TcpClient? _client;
        private NetworkStream? _stream;
        private readonly CancellationTokenSource _cts = new();
        private readonly SemaphoreSlim _sendLock = new(1, 1);

        private readonly byte[] _buffer = new byte[8192];
        private readonly StringBuilder _lineBuffer = new();

        private readonly string _host;
        private readonly int _port;

        public bool IsConnected => _client?.Connected ?? false;
        public bool IsLoggedIn { get; private set; }

        #region Events

        public event Action? Connected;
        public event Action? Disconnected;
        public event Action? LoginSuccess;
        public event Action<string>? LoginFailed;

        public event Action<string>? RawMessage;
        public event Action<string>? QuoteReceived;     // $Quote
        public event Action<string>? TsReceived;        // $T&S
        public event Action<string>? Lv2Received;       // $Lv2
        public event Action<string>? BarReceived;       // $Bar
        public event Action<string>? OrderUpdate;       // %ORDER
        public event Action<string>? OrderAction;       // %OrderAct
        public event Action<string>? TradeUpdate;       // %TRADE
        public event Action<string>? PosUpdate;         // %POS / #POS
        public event Action<string>? AccountInfoUpdate; // $AccountInfo
        public event Action<string>? BPUpdate;          // BP
        public event Action<string>? ServerStatus;      // #OrderServer / #QuoteServer

        #endregion

        public DasClient() : this(AppConfig.Instance.DasApi.Host, AppConfig.Instance.DasApi.Port)
        {
        }

        public DasClient(string host, int port)
        {
            _host = host;
            _port = port;
        }

        #region Connect / Disconnect

        public async Task ConnectAsync()
        {
            try
            {
                _client = new TcpClient();
                await _client.ConnectAsync(_host, _port);
                _stream = _client.GetStream();

                Connected?.Invoke();

                _ = Task.Run(() => ReceiveLoop(_cts.Token));
            }
            catch (Exception ex)
            {
                throw new Exception($"DAS API connection failed: {ex.Message}", ex);
            }
        }

        public async Task LoginAsync()
        {

            var config = AppConfig.Instance.DasApi;
            await LoginAsync(config.User, config.Password, config.Account);
        }

        public async Task LoginAsync(string user, string password, string account)
        {
            await SendAsync($"LOGIN {user} {password} {account}");
        }

        public void Disconnect()
        {
            try
            {
                IsLoggedIn = false;
                _cts.Cancel();
                _stream?.Close();
                _client?.Close();
                Disconnected?.Invoke();
            }
            catch { }
        }

        public void Dispose()
        {
            Disconnect();
            _sendLock.Dispose();
            _cts.Dispose();
        }

        #endregion

        #region Receive Loop

        private async Task ReceiveLoop(CancellationToken token)
        {
            try
            {

                while (!token.IsCancellationRequested && _stream != null)
                {
                    if (!_stream.CanRead) break;

                    int read = await _stream.ReadAsync(_buffer, 0, _buffer.Length, token);
                    if (read == 0) break;

                    string text = Encoding.ASCII.GetString(_buffer, 0, read);
                    _lineBuffer.Append(text);
                    while (true)
                    {
                        var full = _lineBuffer.ToString();
                        int idx = full.IndexOf("\r\n", StringComparison.Ordinal);

                        if (idx < 0) break;

                        string line = full[..idx];
                        _lineBuffer.Remove(0, idx + 2);

                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            DispatchLine(line);
                        }
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Console.WriteLine($"ReceiveLoop error: {ex.Message}");
            }

            Disconnected?.Invoke();
        }

        private void DispatchLine(string line)
        {
            RawMessage?.Invoke(line);

            // 登录成功检测
            if (line.StartsWith("#OrderServer:Logon:Successful") || line.StartsWith("#LOGIN SUCCESSED"))
            {
                IsLoggedIn = true;
                LoginSuccess?.Invoke();
            }
            else if (line.StartsWith("#OrderServer:Logon:Failed") || line.StartsWith("#LOGIN FAILED"))
            {
                LoginFailed?.Invoke(line);
            }

            // 服务器状态
            if (line.StartsWith("#OrderServer") || line.StartsWith("#QuoteServer"))
            {
                ServerStatus?.Invoke(line);
            }

            // 行情数据
            else if (line.StartsWith("$Quote"))
                QuoteReceived?.Invoke(line);

            else if (line.StartsWith("$T&S"))
                TsReceived?.Invoke(line);

            else if (line.StartsWith("$Lv2"))
                Lv2Received?.Invoke(line);

            else if (line.StartsWith("$Bar"))
                BarReceived?.Invoke(line);

            // 订单/成交/持仓
            else if (line.StartsWith("%ORDER"))
                OrderUpdate?.Invoke(line);

            else if (line.StartsWith("%OrderAct"))
                OrderAction?.Invoke(line);

            else if (line.StartsWith("%TRADE"))
                TradeUpdate?.Invoke(line);

            else if (line.StartsWith("%POS") || line.StartsWith("#POS"))
                PosUpdate?.Invoke(line);

            // 账户信息
            else if (line.StartsWith("$AccountInfo"))
                AccountInfoUpdate?.Invoke(line);

            else if (line.StartsWith("BP "))
                BPUpdate?.Invoke(line);
        }

        #endregion

        #region Send Commands

        public event Action<string>? CommandSent;

        public async Task SendAsync(string cmd)
        {
            if (_stream == null) return;

            await _sendLock.WaitAsync();
            try
            {
                if (_stream == null) return;

                // 记录发送的命令（密码脱敏）
                string logCmd = cmd.StartsWith("LOGIN")
                    ? "LOGIN *** *** ***"
                    : cmd.TrimEnd('\r', '\n');
                CommandSent?.Invoke($"[SEND] {logCmd}");

                if (!cmd.EndsWith("\r\n"))
                    cmd += "\r\n";

                byte[] data = Encoding.ASCII.GetBytes(cmd);
                await _stream.WriteAsync(data, 0, data.Length);
                await _stream.FlushAsync();
            }
            finally
            {
                _sendLock.Release();
            }
        }

        #endregion

        #region Subscription Commands

        public Task SubscribeLv1(string symbol) => SendAsync($"SB {symbol} Lv1");
        public Task SubscribeTimeSales(string symbol) => SendAsync($"SB {symbol} tms");
        public Task SubscribeLv2(string symbol) => SendAsync($"SB {symbol} Lv2");
        public Task SubscribeMinChart(string symbol) => SendAsync($"SB {symbol} MINCHART LATEST");

        public Task UnsubscribeLv1(string symbol) => SendAsync($"UNSB {symbol} Lv1");
        public Task UnsubscribeTimeSales(string symbol) => SendAsync($"UNSB {symbol} tms");
        public Task UnsubscribeLv2(string symbol) => SendAsync($"UNSB {symbol} Lv2");
        public Task UnsubscribeMinChart(string symbol) => SendAsync($"UNSB {symbol} MINCHART");

        #endregion

        #region Account Commands

        public Task GetBuyingPower() => SendAsync("GET BP");
        public Task GetAccountInfo() => SendAsync("GET AccountInfo");
        public Task GetPositions() => SendAsync("GET POSITIONS");
        public Task GetOrders() => SendAsync("GET ORDERS");
        public Task GetTrades() => SendAsync("GET TRADES");
        public Task RefreshPositions() => SendAsync("POSREFRESH");

        #endregion

        #region Order Commands

        public Task PlaceOrder(
            int token,
            string side,
            string symbol,
            string route,
            int qty,
            string priceOrType,
            string tif = "DAY+")
        {
            return SendAsync($"NEWORDER {token} {side} {symbol} {route} {qty} {priceOrType} TIF={tif}");
        }

        public Task PlaceLimitOrder(int token, string side, string symbol, string route, int qty, double price, string tif = "DAY+")
        {
            return SendAsync($"NEWORDER {token} {side} {symbol} {route} {qty} {price:F2} TIF={tif}");
        }

        public Task PlaceStopLimitPostOrder(int token, string side, string symbol, string route, int qty, double stopPrice, double limitPrice, string tif = "DAY+")
        {
            // STOPLMTP 支持盘前盘后
            return SendAsync($"NEWORDER {token} {side} {symbol} {route} {qty} STOPLMTP {stopPrice:F2} {limitPrice:F2} TIF={tif}");
        }

        public Task CancelOrder(int orderId) => SendAsync($"CANCEL {orderId}");
        public Task CancelAll() => SendAsync("CANCEL ALL");

        #endregion
    }
}