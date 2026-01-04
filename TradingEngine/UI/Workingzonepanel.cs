using TradingEngine.Core;

namespace TradingEngine.UI
{
    /// <summary>
    /// 工作区面板 - 运行时参数控制
    /// </summary>
    public class WorkingZonePanel : BasePanel
    {
        // 数据显示区
        private Label _lblATR;
        private Label _lblEMA;
        private Label _lblVWAP;
        private Label _lblSessionHigh;

        // 控制区 - VWAP
        private TextBox _txtInitialVwap;
        private Button _btnStartVwap;

        // 控制区 - Strategy
        private TextBox _txtTriggerPrice;
        private Button _btnStartStrategy;
        private Button _btnStopStrategy;

        // 控制区 - Session High
        private TextBox _txtSessionHigh;
        private Button _btnResetSessionHigh;

        public WorkingZonePanel(TradingController controller) : base(controller)
        {
            this.Height = 200;
            this.Dock = DockStyle.Top;
            BuildUI();
            BindEvents();
        }

        private void BuildUI()
        {
            // 标题
            var lblTitle = new Label
            {
                Text = "Working Zone",
                Location = new Point(10, 10),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                AutoSize = true
            };

            // === 数据显示区 ===
            _lblATR = new Label
            {
                Text = "ATR14: --",
                Location = new Point(150, 12),
                Font = new Font("Consolas", 9),
                AutoSize = true
            };

            _lblEMA = new Label
            {
                Text = "EMA20: --",
                Location = new Point(280, 12),
                Font = new Font("Consolas", 9),
                AutoSize = true
            };

            _lblVWAP = new Label
            {
                Text = "VWAP: --",
                Location = new Point(410, 12),
                Font = new Font("Consolas", 9),
                AutoSize = true
            };

            _lblSessionHigh = new Label
            {
                Text = "SHigh: --",
                Location = new Point(540, 12),
                Font = new Font("Consolas", 9),
                AutoSize = true
            };

            // === 控制区 ===
            var lblControlTitle = new Label
            {
                Text = "Control",
                Location = new Point(10, 45),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                AutoSize = true
            };

            // VWAP 控制
            var lblInitVwap = new Label
            {
                Text = "Reset VWAP:",
                Location = new Point(10, 75),
                AutoSize = true
            };

            _txtInitialVwap = new TextBox
            {
                Location = new Point(100, 72),
                Size = new Size(80, 23)
            };
            _txtInitialVwap.KeyDown += TxtInitialVwap_KeyDown;

            _btnStartVwap = new Button
            {
                Text = "Reset",
                Location = new Point(190, 71),
                Size = new Size(60, 25)
            };
            _btnStartVwap.Click += BtnResetVwap_Click;

            // Strategy 控制
            var lblTriggerPrice = new Label
            {
                Text = "Trigger Price:",
                Location = new Point(10, 110),
                AutoSize = true
            };

            _txtTriggerPrice = new TextBox
            {
                Location = new Point(100, 107),
                Size = new Size(80, 23)
            };
            _txtTriggerPrice.KeyDown += TxtTriggerPrice_KeyDown;

            _btnStartStrategy = new Button
            {
                Text = "Start",
                Location = new Point(190, 106),
                Size = new Size(60, 25)
            };
            _btnStartStrategy.Click += BtnStartStrategy_Click;

            _btnStopStrategy = new Button
            {
                Text = "Stop",
                Location = new Point(260, 106),
                Size = new Size(60, 25)
            };
            _btnStopStrategy.Click += BtnStopStrategy_Click;

            // Session High 控制
            var lblSessionHighCtrl = new Label
            {
                Text = "Session High:",
                Location = new Point(10, 145),
                AutoSize = true
            };

            _txtSessionHigh = new TextBox
            {
                Location = new Point(100, 142),
                Size = new Size(80, 23)
            };
            _txtSessionHigh.KeyDown += TxtSessionHigh_KeyDown;

            _btnResetSessionHigh = new Button
            {
                Text = "Reset",
                Location = new Point(190, 141),
                Size = new Size(60, 25)
            };
            _btnResetSessionHigh.Click += BtnResetSessionHigh_Click;

            // 添加控件
            this.Controls.Add(lblTitle);
            this.Controls.Add(_lblATR);
            this.Controls.Add(_lblEMA);
            this.Controls.Add(_lblVWAP);
            this.Controls.Add(_lblSessionHigh);
            this.Controls.Add(lblControlTitle);
            this.Controls.Add(lblInitVwap);
            this.Controls.Add(_txtInitialVwap);
            this.Controls.Add(_btnStartVwap);
            this.Controls.Add(lblTriggerPrice);
            this.Controls.Add(_txtTriggerPrice);
            this.Controls.Add(_btnStartStrategy);
            this.Controls.Add(_btnStopStrategy);
            this.Controls.Add(lblSessionHighCtrl);
            this.Controls.Add(_txtSessionHigh);
            this.Controls.Add(_btnResetSessionHigh);
        }

        private void BindEvents()
        {
            // ATR14, EMA20 更新
            Controller.IndicatorsUpdated += (symbol, atr, ema) => InvokeUI(() =>
            {
                _lblATR.Text = $"ATR14: {atr:F4}";
                _lblEMA.Text = $"EMA20: {ema:F2}";
            });

            // VWAP 更新
            Controller.VwapUpdated += (symbol, vwap) => InvokeUI(() =>
            {
                _lblVWAP.Text = vwap > 0 ? $"VWAP: {vwap:F3}" : "VWAP: --";
            });

            // Session High 更新
            Controller.SessionHighUpdated += (symbol, high) => InvokeUI(() =>
            {
                _lblSessionHigh.Text = high > 0 ? $"SHigh: {high:F3}" : "SHigh: --";
            });

            // ActiveSymbol 切换
            Controller.ActiveSymbolChanged += (symbol) => InvokeUI(() =>
            {
                if (string.IsNullOrEmpty(symbol))
                {
                    _lblATR.Text = "ATR14: --";
                    _lblEMA.Text = "EMA20: --";
                    _lblVWAP.Text = "VWAP: --";
                    _lblSessionHigh.Text = "SHigh: --";
                }
                else
                {
                    // 切换 symbol 时，显示已缓存的值
                    var state = Controller.GetSymbolState(symbol);
                    if (state != null)
                    {
                        _lblATR.Text = state.ATR14 > 0 ? $"ATR14: {state.ATR14:F4}" : "ATR14: --";
                        _lblEMA.Text = state.EMA20 > 0 ? $"EMA20: {state.EMA20:F2}" : "EMA20: --";
                        _lblVWAP.Text = state.VWAP > 0 ? $"VWAP: {state.VWAP:F3}" : "VWAP: --";
                        _lblSessionHigh.Text = state.SessionHigh > 0 ? $"SHigh: {state.SessionHigh:F3}" : "SHigh: --";
                    }
                    else
                    {
                        _lblATR.Text = "ATR14: --";
                        _lblEMA.Text = "EMA20: --";
                        _lblVWAP.Text = "VWAP: --";
                        _lblSessionHigh.Text = "SHigh: --";
                    }
                }
            });
        }

        #region VWAP

        private void TxtInitialVwap_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                ResetVwap();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void BtnResetVwap_Click(object? sender, EventArgs e)
        {
            ResetVwap();
        }

        private void ResetVwap()
        {
            string? symbol = Controller.ActiveSymbol;
            if (string.IsNullOrEmpty(symbol))
            {
                Controller.LogMessage("No symbol selected");
                return;
            }

            double? initialVwap = null;
            string text = _txtInitialVwap.Text.Trim();
            if (!string.IsNullOrEmpty(text))
            {
                if (double.TryParse(text, out double value) && value > 0)
                {
                    initialVwap = value;
                }
            }

            Controller.ResetVwap(symbol, initialVwap);
            _txtInitialVwap.Clear();
        }

        #endregion

        #region Strategy

        private void TxtTriggerPrice_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                StartStrategy();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void BtnStartStrategy_Click(object? sender, EventArgs e)
        {
            StartStrategy();
        }

        private void BtnStopStrategy_Click(object? sender, EventArgs e)
        {
            StopStrategy();
        }

        private void StartStrategy()
        {
            string? symbol = Controller.ActiveSymbol;
            if (string.IsNullOrEmpty(symbol))
            {
                Controller.LogMessage("No symbol selected");
                return;
            }

            if (!double.TryParse(_txtTriggerPrice.Text.Trim(), out double triggerPrice) || triggerPrice <= 0)
            {
                Controller.LogMessage("Invalid trigger price");
                return;
            }

            Controller.StartStrategy(symbol, triggerPrice);
        }

        private void StopStrategy()
        {
            string? symbol = Controller.ActiveSymbol;
            if (string.IsNullOrEmpty(symbol))
            {
                Controller.LogMessage("No symbol selected");
                return;
            }

            Controller.StopStrategy(symbol);
        }

        #endregion

        #region Session High

        private void TxtSessionHigh_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                ResetSessionHigh();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void BtnResetSessionHigh_Click(object? sender, EventArgs e)
        {
            ResetSessionHigh();
        }

        private void ResetSessionHigh()
        {
            string? symbol = Controller.ActiveSymbol;
            if (string.IsNullOrEmpty(symbol))
            {
                Controller.LogMessage("No symbol selected");
                return;
            }

            double? initialValue = null;
            string text = _txtSessionHigh.Text.Trim();
            if (!string.IsNullOrEmpty(text))
            {
                if (double.TryParse(text, out double value) && value > 0)
                {
                    initialValue = value;
                }
            }

            Controller.ResetSessionHigh(symbol, initialValue);
            _txtSessionHigh.Clear();
        }

        #endregion
    }
}