using TradingEngine.Core;

namespace TradingEngine.UI
{
    public class ConnectionPanel : BasePanel
    {
        private Button _btnConnect;
        private Button _btnToggleHotkeys;
        private Label _lblStatus;

        public ConnectionPanel(TradingController controller) : base(controller)
        {
            this.Height = 50;
            BuildUI();
            BindEvents();
        }

        private void BuildUI()
        {
            _btnConnect = new Button
            {
                Text = "Connect",
                Location = new Point(10, 12),
                Size = new Size(80, 25)
            };
            _btnConnect.Click += BtnConnect_Click;

            _lblStatus = new Label
            {
                Text = "Disconnected",
                Location = new Point(100, 16),
                Size = new Size(150, 20),
                ForeColor = Color.Red
            };

            _btnToggleHotkeys = new Button
            {
                Text = "Disable Hotkeys",
                Location = new Point(260, 12),
                Size = new Size(110, 25),
                FlatStyle = FlatStyle.Flat
            };
            _btnToggleHotkeys.Click += BtnToggleHotkeys_Click;

            this.Controls.Add(_btnConnect);
            this.Controls.Add(_lblStatus);
            this.Controls.Add(_btnToggleHotkeys);
        }

        private void BindEvents()
        {
            Controller.Connected += () => InvokeUI(() =>
            {
                _lblStatus.Text = "Connected";
                _lblStatus.ForeColor = Color.Orange;
            });

            Controller.LoginSuccess += () => InvokeUI(() =>
            {
                _lblStatus.Text = "Logged In";
                _lblStatus.ForeColor = Color.Green;
                _btnConnect.Text = "Disconnect";
            });

            Controller.LoginFailed += (msg) => InvokeUI(() =>
            {
                _lblStatus.Text = "Login Failed";
                _lblStatus.ForeColor = Color.Red;
            });

            Controller.Disconnected += () => InvokeUI(() =>
            {
                _lblStatus.Text = "Disconnected";
                _lblStatus.ForeColor = Color.Red;
                _btnConnect.Text = "Connect";
            });
        }

        private async void BtnConnect_Click(object? sender, EventArgs e)
        {
            if (Controller.IsConnected)
            {
                Controller.Disconnect();
            }
            else
            {
                try
                {
                    _btnConnect.Enabled = false;
                    _lblStatus.Text = "Connecting...";
                    _lblStatus.ForeColor = Color.Orange;
                    await Controller.ConnectAsync();
                }
                catch (Exception ex)
                {
                    _lblStatus.Text = "Connection Failed";
                    _lblStatus.ForeColor = Color.Red;
                    Controller.LogMessage($"Connection error: {ex.Message}");
                }
                finally
                {
                    _btnConnect.Enabled = true;
                }
            }
        }

        private void BtnToggleHotkeys_Click(object? sender, EventArgs e)
        {
            bool currentlyEnabled = Controller.IsHotkeysEnabled;
            Controller.EnableHotkeys(!currentlyEnabled);

            bool newState = Controller.IsHotkeysEnabled;
            _btnToggleHotkeys.Text = newState ? "Disable Hotkeys" : "Enable Hotkeys";
            _btnToggleHotkeys.BackColor = newState ? SystemColors.Control : Color.LightCoral;
        }
    }
}