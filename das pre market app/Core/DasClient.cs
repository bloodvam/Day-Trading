using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PreMarketTrader.Core
{
    public class DasClient : IDisposable
    {
        private TcpClient _client;
        private NetworkStream _stream;

        private readonly string _host;
        private readonly int _port;
        private readonly CancellationTokenSource _cts = new();

        private readonly byte[] _buffer = new byte[8192];
        private readonly StringBuilder _lineBuffer = new();

        public bool IsConnected => _client?.Connected ?? false;

        // ========================================================================
        // ========================  Events (回调)  ==============================
        // ========================================================================

        public event Action Connected;
        public event Action Disconnected;

        public event Action<string> RawMessage;
        public event Action<string> QuoteReceived;     // $Quote
        public event Action<string> TsReceived;        // $T&S
        public event Action<string> Lv2Received;       // $Lv2
        public event Action<string> BarReceived;       // $Bar
        public event Action<string> OrderUpdate;       // %ORDER
        public event Action<string> OrderAction;       // %OrderAct
        public event Action<string> TradeUpdate;       // %TRADE
        public event Action<string> PosUpdate;         // %POS

        // ========================================================================
        // =========================== Constructor ================================
        // ========================================================================

        public DasClient(string host = "127.0.0.1", int port = 9090)
        {
            _host = host;
            _port = port;
        }

        // ========================================================================
        // =========================== Connect / Disconnect =======================
        // ========================================================================

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

        public void Disconnect()
        {
            try
            {
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
        }

        // ========================================================================
        // =========================== Receiving Logic ============================
        // ========================================================================

        private async Task ReceiveLoop(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    if (!_stream.CanRead)
                        break;

                    int read = await _stream.ReadAsync(_buffer, 0, _buffer.Length, token);

                    if (read == 0)
                        break;

                    string text = Encoding.ASCII.GetString(_buffer, 0, read);
                    _lineBuffer.Append(text);

                    // 逐行解析
                    while (true)
                    {
                        var full = _lineBuffer.ToString();
                        int idx = full.IndexOf("\r\n", StringComparison.Ordinal);

                        if (idx < 0)
                            break;

                        string line = full[..idx];
                        _lineBuffer.Remove(0, idx + 2);

                        DispatchLine(line);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ReceiveLoop stopped: " + ex.Message);
            }

            Disconnected?.Invoke();
        }

        // ========================================================================
        // =========================== Message Dispatcher ==========================
        // ========================================================================

        private void DispatchLine(string line)
        {
            RawMessage?.Invoke(line);

            if (line.StartsWith("$Quote"))
                QuoteReceived?.Invoke(line);

            else if (line.StartsWith("$T&S"))
                TsReceived?.Invoke(line);

            else if (line.StartsWith("$Lv2"))
                Lv2Received?.Invoke(line);

            else if (line.StartsWith("$Bar"))
                BarReceived?.Invoke(line);

            else if (line.StartsWith("%ORDER"))
                OrderUpdate?.Invoke(line);

            else if (line.StartsWith("%OrderAct"))
                OrderAction?.Invoke(line);

            else if (line.StartsWith("%TRADE"))
                TradeUpdate?.Invoke(line);

            else if (line.StartsWith("%POS"))
                PosUpdate?.Invoke(line);

            // Others: #POS, #Order, #Trade 也可以继续扩展
        }

        // ========================================================================
        // ============================= Send Commands ============================
        // ========================================================================

        public async Task SendAsync(string cmd)
        {
            if (!cmd.EndsWith("\r\n"))
                cmd += "\r\n";

            byte[] data = Encoding.ASCII.GetBytes(cmd);

            await _stream.WriteAsync(data, 0, data.Length);
            await _stream.FlushAsync();
        }

        // ===================== High-level command shortcuts =====================

        public Task Login(string user, string pass, string account)
        {
            return SendAsync($"LOGIN {user} {pass} {account}");
        }

        public Task SubscribeLv1(string symbol)
        {
            return SendAsync($"SB {symbol} Lv1");
        }

        public Task SubscribeTimeSales(string symbol)
        {
            return SendAsync($"SB {symbol} tms");
        }

        public Task SubscribeLv2(string symbol)
        {
            return SendAsync($"SB {symbol} Lv2");
        }

        public Task SubscribeMinChart(string symbol)
        {
            return SendAsync($"SB {symbol} MINCHART LATEST");
        }

        public Task UnsubscribeLv1(string symbol)
        {
            return SendAsync($"UNSB {symbol} Lv1");
        }

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

        public Task CancelOrder(int orderId)
        {
            return SendAsync($"CANCEL {orderId}");
        }

        public Task CancelAll()
        {
            return SendAsync("CANCEL ALL");
        }
    }
}
