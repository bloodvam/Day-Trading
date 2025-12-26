using AlgoTrading.Backtest;
using AlgoTrading.Core.Strategies;
using AlgoTrading.DataFeed;

namespace AlgoTrading
{
    public partial class Form1 : Form
    {
        private TextBox _logTextBox = null!;
        private Button _downloadButton = null!;
        private Button _backtestButton = null!;
        private TextBox _symbolTextBox = null!;
        private DateTimePicker _datePicker = null!;
        private CancellationTokenSource? _cts;

        public Form1()
        {
            InitializeComponent();
            InitializeCustomControls();
        }

        private void InitializeCustomControls()
        {
            // Symbol 输入
            var symbolLabel = new Label
            {
                Text = "Symbol:",
                Location = new Point(12, 15),
                AutoSize = true
            };
            Controls.Add(symbolLabel);

            _symbolTextBox = new TextBox
            {
                Location = new Point(70, 12),
                Width = 80,
                Text = "RADX"
            };
            Controls.Add(_symbolTextBox);

            // Date 选择
            var dateLabel = new Label
            {
                Text = "Date:",
                Location = new Point(160, 15),
                AutoSize = true
            };
            Controls.Add(dateLabel);

            _datePicker = new DateTimePicker
            {
                Location = new Point(200, 12),
                Width = 120,
                Format = DateTimePickerFormat.Short,
                Value = new DateTime(2025, 12, 15)
            };
            Controls.Add(_datePicker);

            // Download 按钮
            _downloadButton = new Button
            {
                Text = "Download",
                Location = new Point(340, 10),
                Width = 100
            };
            _downloadButton.Click += DownloadButton_Click;
            Controls.Add(_downloadButton);

            // Backtest 按钮
            _backtestButton = new Button
            {
                Text = "Backtest",
                Location = new Point(450, 10),
                Width = 100
            };
            _backtestButton.Click += BacktestButton_Click;
            Controls.Add(_backtestButton);

            // Cancel 按钮
            var cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(560, 10),
                Width = 80
            };
            cancelButton.Click += (s, e) => _cts?.Cancel();
            Controls.Add(cancelButton);

            // Log 文本框
            _logTextBox = new TextBox
            {
                Location = new Point(12, 45),
                Width = ClientSize.Width - 24,
                Height = ClientSize.Height - 57,
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                ReadOnly = true,
                Font = new Font("Consolas", 9),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            Controls.Add(_logTextBox);

            // 窗口设置
            Text = "AlgoTrading - Data Downloader & Backtest";
            ClientSize = new Size(800, 500);
        }

        private async void DownloadButton_Click(object? sender, EventArgs e)
        {
            var symbol = _symbolTextBox.Text.Trim().ToUpper();
            var date = _datePicker.Value.Date;

            if (string.IsNullOrWhiteSpace(symbol))
            {
                MessageBox.Show("Please enter a symbol", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            _downloadButton.Enabled = false;
            _backtestButton.Enabled = false;
            _logTextBox.Clear();
            _cts = new CancellationTokenSource();

            try
            {
                Log($"Starting download: {symbol} {date:yyyy-MM-dd}");
                Log($"Time range: 04:00 - 20:00 ET (full day)");
                Log("");

                using var downloader = new TradeDownloader();
                downloader.Log += Log;

                var result = await downloader.DownloadAsync(symbol, date, _cts.Token);

                Log("");
                if (result.Success)
                {
                    Log($"✓ Download successful!");
                    Log($"  Trades: {result.TradeCount:N0}");
                    Log($"  Output: {result.OutputDirectory}");
                }
                else
                {
                    Log($"✗ Download failed: {result.Error}");
                }
            }
            catch (OperationCanceledException)
            {
                Log("\n⚠ Download cancelled by user");
            }
            catch (Exception ex)
            {
                Log($"\n✗ Error: {ex.Message}");
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _downloadButton.Enabled = true;
                _backtestButton.Enabled = true;
                _cts?.Dispose();
                _cts = null;
            }
        }

        private async void BacktestButton_Click(object? sender, EventArgs e)
        {
            _downloadButton.Enabled = false;
            _backtestButton.Enabled = false;
            _logTextBox.Clear();
            _cts = new CancellationTokenSource();

            try
            {
                Log("=== Loading Backtest Config ===");
                var config = BacktestConfig.LoadDefault();
                Log($"Mode: {config.Mode}");
                Log($"Output Path: {config.ResultOutputPath}");
                Log("");

                var engine = new BacktestEngine(config);
                engine.Log += Log;

                var strategy = new BreakoutStrategy
                {
                    RiskAmount = 100,
                    BreakoutConfirmOffset = 0.05,
                    MinDistanceFromHigh = 0.50
                };

                Log($"Strategy: {strategy.Name}");
                Log($"  R (Risk Amount): ${strategy.RiskAmount}");
                Log($"  Breakout Confirm Offset: ${strategy.BreakoutConfirmOffset}");
                Log($"  Min Distance From High: ${strategy.MinDistanceFromHigh}");
                Log("");

                var success = await Task.Run(() => engine.RunAsync(strategy, _cts.Token));

                if (success)
                {
                    Log("\n✓ Backtest completed successfully!");
                }
                else
                {
                    Log("\n✗ Backtest failed or was incomplete.");
                }
            }
            catch (OperationCanceledException)
            {
                Log("\n⚠ Backtest cancelled by user");
            }
            catch (Exception ex)
            {
                Log($"\n✗ Error: {ex.Message}");
                Log($"Stack: {ex.StackTrace}");
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _downloadButton.Enabled = true;
                _backtestButton.Enabled = true;
                _cts?.Dispose();
                _cts = null;
            }
        }

        private void Log(string message)
        {
            if (InvokeRequired)
            {
                Invoke(() => Log(message));
                return;
            }

            _logTextBox.AppendText(message + Environment.NewLine);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // 显示配置信息
            try
            {
                var config = Config.AppConfig.Instance;
                Log("=== Configuration ===");
                Log($"API Key: {(config.PolygonApiKey.Length > 8 ? config.PolygonApiKey[..8] + "..." : "NOT SET")}");
                Log($"Data Path: {config.DataBasePath}");
                Log("");
                Log("=== Instructions ===");
                Log("1. Download: Enter symbol and date, click Download");
                Log("2. Backtest: Edit backtest_config.json, click Backtest");
                Log("");
                Log("Ready.");
            }
            catch (Exception ex)
            {
                Log($"Error loading config: {ex.Message}");
                Log("Please check config.json file.");
            }
        }
    }
}