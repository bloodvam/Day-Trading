using System.Net.Sockets;
using System.Text;

namespace TradingEngine
{
    public class TestForm : Form
    {
        private TextBox txtHost = null!;
        private TextBox txtPort = null!;
        private TextBox txtUser = null!;
        private TextBox txtPassword = null!;
        private TextBox txtAccount = null!;
        private TextBox txtSymbol = null!;
        private TextBox txtShares = null!;
        private TextBox txtPrice = null!;
        private TextBox txtCommand = null!;
        private ListBox lstLog = null!;

        private TcpClient? _client;
        private NetworkStream? _stream;
        private CancellationTokenSource? _cts;

        public TestForm()
        {
            BuildUI();
        }

        private void BuildUI()
        {
            this.Text = "DAS API Test";
            this.ClientSize = new Size(700, 600);

            int y = 10;

            // Connection
            AddLabel("Host:", 10, y);
            txtHost = AddTextBox("127.0.0.1", 100, y, 100);
            AddLabel("Port:", 220, y);
            txtPort = AddTextBox("9910", 270, y, 60);
            y += 30;

            AddLabel("User:", 10, y);
            txtUser = AddTextBox("", 100, y, 100);
            AddLabel("Password:", 220, y);
            txtPassword = AddTextBox("", 290, y, 100);
            txtPassword.UseSystemPasswordChar = true;
            AddLabel("Account:", 410, y);
            txtAccount = AddTextBox("", 480, y, 100);
            y += 30;

            var btnConnect = new Button { Text = "Connect", Location = new Point(100, y), Size = new Size(80, 25) };
            btnConnect.Click += BtnConnect_Click;
            this.Controls.Add(btnConnect);

            var btnLogin = new Button { Text = "Login", Location = new Point(190, y), Size = new Size(80, 25) };
            btnLogin.Click += BtnLogin_Click;
            this.Controls.Add(btnLogin);

            var btnDisconnect = new Button { Text = "Disconnect", Location = new Point(280, y), Size = new Size(80, 25) };
            btnDisconnect.Click += BtnDisconnect_Click;
            this.Controls.Add(btnDisconnect);
            y += 40;

            // Separator
            var sep1 = new Label { Text = "── Order Test ──", Location = new Point(10, y), Size = new Size(300, 20) };
            this.Controls.Add(sep1);
            y += 25;

            AddLabel("Symbol:", 10, y);
            txtSymbol = AddTextBox("AAPL", 70, y, 60);
            AddLabel("Shares:", 150, y);
            txtShares = AddTextBox("1", 210, y, 50);
            AddLabel("Price:", 280, y);
            txtPrice = AddTextBox("150.00", 330, y, 70);
            y += 30;

            var btnBuy = new Button { Text = "Buy Limit", Location = new Point(70, y), Size = new Size(80, 25) };
            btnBuy.Click += BtnBuy_Click;
            this.Controls.Add(btnBuy);

            var btnSell = new Button { Text = "Sell Limit", Location = new Point(160, y), Size = new Size(80, 25) };
            btnSell.Click += BtnSell_Click;
            this.Controls.Add(btnSell);

            var btnCancelAll = new Button { Text = "Cancel All", Location = new Point(250, y), Size = new Size(80, 25) };
            btnCancelAll.Click += BtnCancelAll_Click;
            this.Controls.Add(btnCancelAll);
            y += 40;

            // Separator
            var sep2 = new Label { Text = "── Raw Command ──", Location = new Point(10, y), Size = new Size(300, 20) };
            this.Controls.Add(sep2);
            y += 25;

            txtCommand = AddTextBox("", 10, y, 500);
            var btnSend = new Button { Text = "Send", Location = new Point(520, y - 2), Size = new Size(60, 25) };
            btnSend.Click += BtnSend_Click;
            this.Controls.Add(btnSend);
            y += 35;

            // Quick buttons
            var btnGetBP = new Button { Text = "GET BP", Location = new Point(10, y), Size = new Size(70, 25) };
            btnGetBP.Click += (s, e) => SendCommand("GET BP");
            this.Controls.Add(btnGetBP);

            var btnGetPos = new Button { Text = "GET POS", Location = new Point(90, y), Size = new Size(70, 25) };
            btnGetPos.Click += (s, e) => SendCommand("GET POSITIONS");
            this.Controls.Add(btnGetPos);

            var btnGetOrders = new Button { Text = "GET ORDERS", Location = new Point(170, y), Size = new Size(85, 25) };
            btnGetOrders.Click += (s, e) => SendCommand("GET ORDERS");
            this.Controls.Add(btnGetOrders);

            var btnGetAcct = new Button { Text = "GET AcctInfo", Location = new Point(265, y), Size = new Size(90, 25) };
            btnGetAcct.Click += (s, e) => SendCommand("GET AccountInfo");
            this.Controls.Add(btnGetAcct);

            var btnEcho = new Button { Text = "ECHO", Location = new Point(365, y), Size = new Size(60, 25) };
            btnEcho.Click += (s, e) => SendCommand("ECHO");
            this.Controls.Add(btnEcho);
            y += 40;

            // Log
            AddLabel("Log:", 10, y);
            var btnClear = new Button { Text = "Clear", Location = new Point(50, y - 3), Size = new Size(50, 22) };
            btnClear.Click += (s, e) => lstLog.Items.Clear();
            this.Controls.Add(btnClear);
            y += 20;

            lstLog = new ListBox
            {
                Location = new Point(10, y),
                Size = new Size(670, 280),
                Font = new Font("Consolas", 9)
            };
            this.Controls.Add(lstLog);

            this.FormClosing += (s, e) => Disconnect();
        }

        private Label AddLabel(string text, int x, int y)
        {
            var lbl = new Label { Text = text, Location = new Point(x, y + 3), AutoSize = true };
            this.Controls.Add(lbl);
            return lbl;
        }

        private TextBox AddTextBox(string text, int x, int y, int width)
        {
            var txt = new TextBox { Text = text, Location = new Point(x, y), Size = new Size(width, 23) };
            this.Controls.Add(txt);
            return txt;
        }

        private void Log(string msg)
        {
            if (InvokeRequired)
            {
                BeginInvoke(() => Log(msg));
                return;
            }

            string line = $"[{DateTime.Now:HH:mm:ss.fff}] {msg}";
            lstLog.Items.Add(line);
            lstLog.TopIndex = lstLog.Items.Count - 1;
        }

        #region Connection

        private async void BtnConnect_Click(object? sender, EventArgs e)
        {
            try
            {
                string host = txtHost.Text.Trim();
                int port = int.Parse(txtPort.Text.Trim());

                Log($"Connecting to {host}:{port}...");

                _client = new TcpClient();
                await _client.ConnectAsync(host, port);
                _stream = _client.GetStream();

                Log("Connected!");

                _cts = new CancellationTokenSource();
                _ = Task.Run(() => ReceiveLoop(_cts.Token));
            }
            catch (Exception ex)
            {
                Log($"Connect failed: {ex.Message}");
            }
        }

        private async void BtnLogin_Click(object? sender, EventArgs e)
        {
            string user = txtUser.Text.Trim();
            string pass = txtPassword.Text;
            string account = txtAccount.Text.Trim();

            string cmd = $"LOGIN {user} {pass} {account}";
            Log($"[SEND] LOGIN {user} *** {account}");

            await SendRawAsync(cmd);
        }

        private void BtnDisconnect_Click(object? sender, EventArgs e)
        {
            Disconnect();
        }

        private void Disconnect()
        {
            try
            {
                _cts?.Cancel();
                _stream?.Close();
                _client?.Close();
                Log("Disconnected");
            }
            catch { }
        }

        #endregion

        #region Orders

        private async void BtnBuy_Click(object? sender, EventArgs e)
        {
            string symbol = txtSymbol.Text.Trim().ToUpper();
            string shares = txtShares.Text.Trim();
            string price = txtPrice.Text.Trim();

            string cmd = $"NEWORDER 1 B {symbol} VLCTL {shares} {price} TIF=DAY+";
            await SendCommand(cmd);
        }

        private async void BtnSell_Click(object? sender, EventArgs e)
        {
            string symbol = txtSymbol.Text.Trim().ToUpper();
            string shares = txtShares.Text.Trim();
            string price = txtPrice.Text.Trim();

            string cmd = $"NEWORDER 2 S {symbol} VLCTL {shares} {price} TIF=DAY+";
            await SendCommand(cmd);
        }

        private async void BtnCancelAll_Click(object? sender, EventArgs e)
        {
            await SendCommand("CANCEL ALL");
        }

        private async void BtnSend_Click(object? sender, EventArgs e)
        {
            string cmd = txtCommand.Text.Trim();
            if (!string.IsNullOrEmpty(cmd))
            {
                await SendCommand(cmd);
            }
        }

        #endregion

        #region Communication

        private async Task SendCommand(string cmd)
        {
            Log($"[SEND] {cmd}");
            await SendRawAsync(cmd);
        }

        private async Task SendRawAsync(string cmd)
        {
            if (_stream == null)
            {
                Log("Not connected!");
                return;
            }

            try
            {
                if (!cmd.EndsWith("\r\n"))
                    cmd += "\r\n";

                byte[] data = Encoding.ASCII.GetBytes(cmd);
                await _stream.WriteAsync(data);
                await _stream.FlushAsync();
            }
            catch (Exception ex)
            {
                Log($"Send error: {ex.Message}");
            }
        }

        private async Task ReceiveLoop(CancellationToken token)
        {
            byte[] buffer = new byte[4096];
            StringBuilder lineBuffer = new();

            try
            {
                while (!token.IsCancellationRequested && _stream != null)
                {
                    int read = await _stream.ReadAsync(buffer, token);
                    if (read == 0) break;

                    string text = Encoding.ASCII.GetString(buffer, 0, read);
                    lineBuffer.Append(text);

                    while (true)
                    {
                        string full = lineBuffer.ToString();
                        int idx = full.IndexOf("\r\n");
                        if (idx < 0) break;

                        string line = full[..idx];
                        lineBuffer.Remove(0, idx + 2);

                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            // 过滤掉 Quote 和 T&S 消息，避免刷屏
                            if (!line.StartsWith("$Quote") && !line.StartsWith("$T&S"))
                            {
                                Log($"[RECV] {line}");
                            }
                        }
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Log($"Receive error: {ex.Message}");
            }

            Log("Connection closed");
        }

        #endregion
    }
}