using TradingEngine.Core;
using TradingEngine.Managers;

namespace TradingEngine.UI
{
    /// <summary>
    /// 工作区面板 - 运行时参数控制
    /// </summary>
    public class WorkingZonePanel : BasePanel
    {
        // 数据显示区
        private Label _lblATR = null!;
        private Label _lblEMA = null!;
        private Label _lblVWAP = null!;
        private Label _lblSessionHigh = null!;
        private Label _lblTrailHalf = null!;
        private Label _lblTrailAll = null!;

        // 控制区 - VWAP
        private TextBox _txtInitialVwap = null!;
        private Button _btnResetVwap = null!;

        // 控制区 - Strategy
        private TextBox _txtTriggerPrice = null!;
        private Button _btnOpen = null!;       // 没有持仓时显示
        private Button _btnAddAll = null!;     // 有持仓时显示
        private Button _btnAddHalf = null!;    // 有持仓时显示
        private Button _btnStopStrategy = null!;

        // 控制区 - Session High
        private TextBox _txtSessionHigh = null!;
        private Button _btnResetSessionHigh = null!;

        // Agent Mode
        private Label _lblAgentPrice = null!;
        private Button _btnAgentMode = null!;
        private Label _lblBreakedLevel = null!;
        private TextBox _txtBreakedLevel = null!;
        private Button _btnResetBreakedLevel = null!;

        // TrailingStop 手动设置
        private Label _lblTrailHalfSet = null!;
        private TextBox _txtTrailHalf = null!;
        private Button _btnSetTrailHalf = null!;
        private Button _btnStartTrailing = null!;  // Start/On 按钮
        private Label _lblTrailAllSet = null!;
        private TextBox _txtTrailAll = null!;
        private Button _btnSetTrailAll = null!;

        // AutoFill 快捷按钮
        private Button _btnAutoFill1 = null!;  // CeilLevel(tick)
        private Button _btnAutoFill2 = null!;  // CeilLevel(tick) + 0.5
        private Button _btnAutoFill3 = null!;  // CeilLevel(sessionHigh)
        private Button _btnAutoFill4 = null!;  // SessionHigh - 0.01

        // HighBreakout 策略
        private Label _lblHighBreakout = null!;
        private TextBox _txtHighBreakoutPrice = null!;
        private Button _btnHighBreakoutOpen = null!;
        private Button _btnHBAutoFill1 = null!;  // SessionHigh - 0.01
        private Button _btnHBAutoFill2 = null!;  // FloorLevel(SessionHigh)

        public WorkingZonePanel(TradingController controller) : base(controller)
        {
            this.Height = 220;
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

            // Trailing Stop 显示（第二行）
            _lblTrailHalf = new Label
            {
                Text = "Trail½: --",
                Location = new Point(150, 30),
                Font = new Font("Consolas", 9),
                AutoSize = true
            };

            _lblTrailAll = new Label
            {
                Text = "TrailAll: --",
                Location = new Point(280, 30),
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

            _btnResetVwap = new Button
            {
                Text = "Reset",
                Location = new Point(190, 71),
                Size = new Size(60, 25)
            };
            _btnResetVwap.Click += BtnResetVwap_Click;

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

            // Open 按钮（没有持仓时显示）
            _btnOpen = new Button
            {
                Text = "Open",
                Location = new Point(190, 106),
                Size = new Size(60, 25),
                Visible = true
            };
            _btnOpen.Click += BtnOpen_Click;

            // Add All 按钮（有持仓时显示）
            _btnAddAll = new Button
            {
                Text = "Add All",
                Location = new Point(190, 106),
                Size = new Size(60, 25),
                Visible = false
            };
            _btnAddAll.Click += BtnAddAll_Click;

            // Add 1/2 按钮（有持仓时显示）
            _btnAddHalf = new Button
            {
                Text = "Add 1/2",
                Location = new Point(260, 106),
                Size = new Size(60, 25),
                Visible = false
            };
            _btnAddHalf.Click += BtnAddHalf_Click;

            // Stop 按钮
            _btnStopStrategy = new Button
            {
                Text = "Stop",
                Location = new Point(260, 106),  // 初始位置，会动态调整
                Size = new Size(60, 25)
            };
            _btnStopStrategy.Click += BtnStopStrategy_Click;

            // AutoFill 快捷按钮（TriggerPrice 下方）
            _btnAutoFill1 = new Button
            {
                Text = "--",
                Location = new Point(100, 132),
                Size = new Size(45, 22),
                Font = new Font("Consolas", 8),
                FlatStyle = FlatStyle.Flat
            };
            _btnAutoFill1.Click += (s, e) => AutoFillTriggerPrice(_btnAutoFill1.Text);

            _btnAutoFill2 = new Button
            {
                Text = "--",
                Location = new Point(148, 132),
                Size = new Size(45, 22),
                Font = new Font("Consolas", 8),
                FlatStyle = FlatStyle.Flat
            };
            _btnAutoFill2.Click += (s, e) => AutoFillTriggerPrice(_btnAutoFill2.Text);

            _btnAutoFill3 = new Button
            {
                Text = "--",
                Location = new Point(196, 132),
                Size = new Size(45, 22),
                Font = new Font("Consolas", 8),
                FlatStyle = FlatStyle.Flat
            };
            _btnAutoFill3.Click += (s, e) => AutoFillTriggerPrice(_btnAutoFill3.Text);

            _btnAutoFill4 = new Button
            {
                Text = "--",
                Location = new Point(244, 132),
                Size = new Size(50, 22),
                Font = new Font("Consolas", 8),
                FlatStyle = FlatStyle.Flat
            };
            _btnAutoFill4.Click += (s, e) => AutoFillTriggerPrice(_btnAutoFill4.Text);

            // Session High 控制
            var lblSessionHighCtrl = new Label
            {
                Text = "Session High:",
                Location = new Point(10, 160),
                AutoSize = true
            };

            _txtSessionHigh = new TextBox
            {
                Location = new Point(100, 157),
                Size = new Size(80, 23)
            };
            _txtSessionHigh.KeyDown += TxtSessionHigh_KeyDown;

            _btnResetSessionHigh = new Button
            {
                Text = "Reset",
                Location = new Point(190, 156),
                Size = new Size(60, 25)
            };
            _btnResetSessionHigh.Click += BtnResetSessionHigh_Click;

            // Agent Mode（右侧区域）
            _lblAgentPrice = new Label
            {
                Text = "Agent: --",
                Location = new Point(450, 110),
                Font = new Font("Consolas", 9),
                AutoSize = true
            };

            _btnAgentMode = new Button
            {
                Text = "Agent",
                Location = new Point(550, 106),
                Size = new Size(60, 25)
            };
            _btnAgentMode.Click += BtnAgentMode_Click;

            // BreakedLevel 控件（Agent 下方）
            _lblBreakedLevel = new Label
            {
                Text = "BrkLv: --",
                Location = new Point(450, 135),
                Font = new Font("Consolas", 9),
                AutoSize = true
            };

            _txtBreakedLevel = new TextBox
            {
                Location = new Point(540, 132),
                Size = new Size(50, 22),
                Font = new Font("Consolas", 9)
            };
            _txtBreakedLevel.KeyDown += TxtBreakedLevel_KeyDown;

            _btnResetBreakedLevel = new Button
            {
                Text = "Set",
                Location = new Point(595, 130),
                Size = new Size(40, 25)
            };
            _btnResetBreakedLevel.Click += BtnResetBreakedLevel_Click;

            // TrailHalf 控件（BrkLv 下方）
            _lblTrailHalfSet = new Label
            {
                Text = "TrlHalf:",
                Location = new Point(450, 160),
                Font = new Font("Consolas", 9),
                AutoSize = true
            };

            _txtTrailHalf = new TextBox
            {
                Location = new Point(510, 157),
                Size = new Size(50, 22),
                Font = new Font("Consolas", 9)
            };
            _txtTrailHalf.KeyDown += TxtTrailHalf_KeyDown;

            _btnSetTrailHalf = new Button
            {
                Text = "Set",
                Location = new Point(565, 155),
                Size = new Size(40, 25)
            };
            _btnSetTrailHalf.Click += BtnSetTrailHalf_Click;

            _btnStartTrailing = new Button
            {
                Text = "Start",
                Location = new Point(610, 155),
                Size = new Size(45, 25),
                Enabled = false
            };
            _btnStartTrailing.Click += BtnStartTrailing_Click;

            // TrailAll 控件（TrailHalf 下方）
            _lblTrailAllSet = new Label
            {
                Text = "TrlAll:",
                Location = new Point(450, 185),
                Font = new Font("Consolas", 9),
                AutoSize = true
            };

            _txtTrailAll = new TextBox
            {
                Location = new Point(510, 182),
                Size = new Size(50, 22),
                Font = new Font("Consolas", 9)
            };
            _txtTrailAll.KeyDown += TxtTrailAll_KeyDown;

            _btnSetTrailAll = new Button
            {
                Text = "Set",
                Location = new Point(565, 180),
                Size = new Size(40, 25)
            };
            _btnSetTrailAll.Click += BtnSetTrailAll_Click;

            // HighBreakout 策略控件（SessionHigh 下方）
            _lblHighBreakout = new Label
            {
                Text = "HiBrk:",
                Location = new Point(10, 185),
                Font = new Font("Consolas", 9),
                AutoSize = true
            };

            _txtHighBreakoutPrice = new TextBox
            {
                Location = new Point(60, 182),
                Size = new Size(60, 22),
                Font = new Font("Consolas", 9)
            };
            _txtHighBreakoutPrice.KeyDown += TxtHighBreakoutPrice_KeyDown;

            _btnHighBreakoutOpen = new Button
            {
                Text = "Open",
                Location = new Point(125, 180),
                Size = new Size(50, 25)
            };
            _btnHighBreakoutOpen.Click += BtnHighBreakoutOpen_Click;

            _btnHBAutoFill1 = new Button
            {
                Text = "--",
                Location = new Point(180, 180),
                Size = new Size(55, 25),
                Font = new Font("Consolas", 8),
                FlatStyle = FlatStyle.Flat
            };
            _btnHBAutoFill1.Click += (s, e) => AutoFillHighBreakoutPrice(_btnHBAutoFill1.Text);

            _btnHBAutoFill2 = new Button
            {
                Text = "--",
                Location = new Point(238, 180),
                Size = new Size(45, 25),
                Font = new Font("Consolas", 8),
                FlatStyle = FlatStyle.Flat
            };
            _btnHBAutoFill2.Click += (s, e) => AutoFillHighBreakoutPrice(_btnHBAutoFill2.Text);

            // 添加控件
            this.Controls.Add(lblTitle);
            this.Controls.Add(_lblATR);
            this.Controls.Add(_lblEMA);
            this.Controls.Add(_lblVWAP);
            this.Controls.Add(_lblSessionHigh);
            this.Controls.Add(_lblTrailHalf);
            this.Controls.Add(_lblTrailAll);
            this.Controls.Add(lblControlTitle);
            this.Controls.Add(lblInitVwap);
            this.Controls.Add(_txtInitialVwap);
            this.Controls.Add(_btnResetVwap);
            this.Controls.Add(lblTriggerPrice);
            this.Controls.Add(_txtTriggerPrice);
            this.Controls.Add(_btnOpen);
            this.Controls.Add(_btnAddAll);
            this.Controls.Add(_btnAddHalf);
            this.Controls.Add(_btnStopStrategy);
            this.Controls.Add(_btnAutoFill1);
            this.Controls.Add(_btnAutoFill2);
            this.Controls.Add(_btnAutoFill3);
            this.Controls.Add(_btnAutoFill4);
            this.Controls.Add(_lblHighBreakout);
            this.Controls.Add(_txtHighBreakoutPrice);
            this.Controls.Add(_btnHighBreakoutOpen);
            this.Controls.Add(_btnHBAutoFill1);
            this.Controls.Add(_btnHBAutoFill2);
            this.Controls.Add(lblSessionHighCtrl);
            this.Controls.Add(_txtSessionHigh);
            this.Controls.Add(_btnResetSessionHigh);
            this.Controls.Add(_lblAgentPrice);
            this.Controls.Add(_btnAgentMode);
            this.Controls.Add(_lblBreakedLevel);
            this.Controls.Add(_txtBreakedLevel);
            this.Controls.Add(_btnResetBreakedLevel);
            this.Controls.Add(_lblTrailHalfSet);
            this.Controls.Add(_txtTrailHalf);
            this.Controls.Add(_btnSetTrailHalf);
            this.Controls.Add(_btnStartTrailing);
            this.Controls.Add(_lblTrailAllSet);
            this.Controls.Add(_txtTrailAll);
            this.Controls.Add(_btnSetTrailAll);
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
                UpdateAutoFillButtons();
                UpdateHighBreakoutButtons();
            });

            // EMA20 穿越 - 更新颜色
            Controller.EMA20Crossed += (symbol, isAbove) => InvokeUI(() =>
            {
                _lblEMA.ForeColor = isAbove ? Color.Blue : Color.Red;
            });

            // VWAP 穿越 - 更新颜色
            Controller.VWAPCrossed += (symbol, isAbove) => InvokeUI(() =>
            {
                _lblVWAP.ForeColor = isAbove ? Color.Green : Color.Red;
            });

            // 价格跨越关键位 - 更新 AutoFill 按钮和 BreakedLevel 显示
            Controller.LevelCrossed += (symbol, ceilLevel) => InvokeUI(() =>
            {
                UpdateAutoFillButtons();
                UpdateBreakedLevelDisplay();
            });

            // Trailing Stop 更新
            Controller.TrailingStopUpdated += (symbol, trailHalf, trailAll) => InvokeUI(() =>
            {
                _lblTrailHalf.Text = trailHalf > 0 ? $"Trail½: {trailHalf:F3}" : "Trail½: --";
                _lblTrailAll.Text = trailAll > 0 ? $"TrailAll: {trailAll:F3}" : "TrailAll: --";
                _txtTrailHalf.Text = trailHalf > 0 ? trailHalf.ToString("F3") : "";
                _txtTrailAll.Text = trailAll > 0 ? trailAll.ToString("F3") : "";
                UpdateTrailingButton();
            });

            // Agent TriggerPrice 更新
            Controller.AgentTriggerPriceUpdated += (symbol, price) => InvokeUI(() =>
            {
                _lblAgentPrice.Text = price > 0 ? $"Agent: {price:F2}" : "Agent: --";
            });

            // Agent 状态变化 - 更新按钮
            Controller.AgentStateChanged += (symbol, isEnabled) => InvokeUI(() =>
            {
                UpdateAgentButton();
            });

            // Position 变化 - 更新按钮显示和 Agent 状态
            Controller.PositionChanged += (pos) => InvokeUI(() =>
            {
                UpdateStrategyButtons();
                UpdateAgentButton();
                UpdateTrailingButton();
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
                    _lblTrailHalf.Text = "Trail½: --";
                    _lblTrailAll.Text = "TrailAll: --";
                    _lblAgentPrice.Text = "Agent: --";
                    _lblBreakedLevel.Text = "BrkLv: --";
                    _txtBreakedLevel.Text = "";
                    _txtTrailHalf.Text = "";
                    _txtTrailAll.Text = "";
                    _lblEMA.ForeColor = SystemColors.ControlText;
                    _lblVWAP.ForeColor = SystemColors.ControlText;
                }
                else
                {
                    // 切换 symbol 时，显示已缓存的值
                    var state = Controller.GetSymbolState(symbol);
                    if (state != null)
                    {
                        _lblATR.Text = state.ATR14 > 0 ? $"ATR14: {state.ATR14:F4}" : "ATR14: --";
                        _lblEMA.Text = state.EMA20 > 0 ? $"EMA20: {state.EMA20:F2}" : "EMA20: --";
                        _lblEMA.ForeColor = state.EMA20 > 0
                            ? (state.IsAboveEMA20 ? Color.Blue : Color.Red)
                            : SystemColors.ControlText;
                        _lblVWAP.Text = state.VWAP > 0 ? $"VWAP: {state.VWAP:F3}" : "VWAP: --";
                        _lblVWAP.ForeColor = state.VWAP > 0
                            ? (state.IsAboveVWAP ? Color.Green : Color.Red)
                            : SystemColors.ControlText;
                        _lblSessionHigh.Text = state.SessionHigh > 0 ? $"SHigh: {state.SessionHigh:F3}" : "SHigh: --";
                        _lblTrailHalf.Text = state.TrailHalf > 0 ? $"Trail½: {state.TrailHalf:F3}" : "Trail½: --";
                        _lblTrailAll.Text = state.TrailAll > 0 ? $"TrailAll: {state.TrailAll:F3}" : "TrailAll: --";
                        _txtTrailHalf.Text = state.TrailHalf > 0 ? state.TrailHalf.ToString("F3") : "";
                        _txtTrailAll.Text = state.TrailAll > 0 ? state.TrailAll.ToString("F3") : "";
                        _lblAgentPrice.Text = state.AgentTriggerPrice > 0 ? $"Agent: {state.AgentTriggerPrice:F2}" : "Agent: --";
                        _lblBreakedLevel.Text = state.AgentBreakedLevel > 0 ? $"BrkLv: {state.AgentBreakedLevel:F2}" : "BrkLv: --";
                        _txtBreakedLevel.Text = state.AgentBreakedLevel > 0 ? state.AgentBreakedLevel.ToString("F2") : "";
                    }
                    else
                    {
                        _lblATR.Text = "ATR14: --";
                        _lblEMA.Text = "EMA20: --";
                        _lblEMA.ForeColor = SystemColors.ControlText;
                        _lblVWAP.Text = "VWAP: --";
                        _lblVWAP.ForeColor = SystemColors.ControlText;
                        _lblSessionHigh.Text = "SHigh: --";
                        _lblTrailHalf.Text = "Trail½: --";
                        _lblTrailAll.Text = "TrailAll: --";
                        _txtTrailHalf.Text = "";
                        _txtTrailAll.Text = "";
                        _lblAgentPrice.Text = "Agent: --";
                        _lblBreakedLevel.Text = "BrkLv: --";
                        _txtBreakedLevel.Text = "";
                    }
                }

                // 更新按钮显示
                UpdateStrategyButtons();
                UpdateAgentButton();
                UpdateAutoFillButtons();
                UpdateHighBreakoutButtons();
                UpdateTrailingButton();
            });
        }

        /// <summary>
        /// 根据当前持仓更新策略按钮显示
        /// </summary>
        private void UpdateStrategyButtons()
        {
            string? symbol = Controller.ActiveSymbol;
            bool hasPosition = false;

            if (!string.IsNullOrEmpty(symbol))
            {
                var positions = Controller.GetAllPositions();
                var pos = positions.Find(p => p.Symbol == symbol);
                hasPosition = pos != null && pos.Quantity > 0;
            }

            if (hasPosition)
            {
                // 有持仓：显示 [Add All] [Add 1/2] [Stop]
                _btnOpen.Visible = false;
                _btnAddAll.Visible = true;
                _btnAddHalf.Visible = true;
                _btnStopStrategy.Location = new Point(330, 106);
            }
            else
            {
                // 没有持仓：显示 [Open] [Stop]
                _btnOpen.Visible = true;
                _btnAddAll.Visible = false;
                _btnAddHalf.Visible = false;
                _btnStopStrategy.Location = new Point(260, 106);
            }
        }

        /// <summary>
        /// 更新 Agent 按钮显示
        /// </summary>
        private void UpdateAgentButton()
        {
            string? symbol = Controller.ActiveSymbol;
            if (string.IsNullOrEmpty(symbol))
            {
                _btnAgentMode.Text = "Agent";
                _btnAgentMode.BackColor = SystemColors.Control;
                return;
            }

            bool isEnabled = Controller.IsAgentEnabled(symbol);
            if (isEnabled)
            {
                _btnAgentMode.Text = "Agent ON";
                _btnAgentMode.BackColor = Color.LightGreen;
            }
            else
            {
                _btnAgentMode.Text = "Agent";
                _btnAgentMode.BackColor = SystemColors.Control;
            }
        }

        private void BtnAgentMode_Click(object? sender, EventArgs e)
        {
            string? symbol = Controller.ActiveSymbol;
            if (string.IsNullOrEmpty(symbol))
            {
                Controller.LogMessage("No symbol selected");
                return;
            }

            bool isEnabled = Controller.IsAgentEnabled(symbol);
            if (isEnabled)
            {
                Controller.DisableAgent(symbol);
            }
            else
            {
                Controller.EnableAgent(symbol);
            }

            UpdateAgentButton();
        }

        private void TxtBreakedLevel_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                ResetBreakedLevel();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void BtnResetBreakedLevel_Click(object? sender, EventArgs e)
        {
            ResetBreakedLevel();
        }

        private void ResetBreakedLevel()
        {
            string? symbol = Controller.ActiveSymbol;
            if (string.IsNullOrEmpty(symbol))
            {
                Controller.LogMessage("No symbol selected");
                return;
            }

            if (!double.TryParse(_txtBreakedLevel.Text, out double newLevel))
            {
                Controller.LogMessage("Invalid BreakedLevel value");
                return;
            }

            Controller.SetAgentBreakedLevel(symbol, newLevel);
            _lblBreakedLevel.Text = $"BrkLv: {newLevel:F2}";
            Controller.LogMessage($"BreakedLevel set to {newLevel:F2}");
        }

        #region TrailingStop Manual Set

        private void TxtTrailHalf_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                SetTrailHalf();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void BtnSetTrailHalf_Click(object? sender, EventArgs e)
        {
            SetTrailHalf();
        }

        private void SetTrailHalf()
        {
            string? symbol = Controller.ActiveSymbol;
            if (string.IsNullOrEmpty(symbol))
            {
                Controller.LogMessage("No symbol selected");
                return;
            }

            if (!double.TryParse(_txtTrailHalf.Text, out double newValue) || newValue <= 0)
            {
                Controller.LogMessage("Invalid TrailHalf value");
                return;
            }

            Controller.SetTrailHalf(symbol, newValue);
            _lblTrailHalf.Text = $"Trail½: {newValue:F3}";

            // SetTrailHalf 会自动计算 TrailAll，更新显示
            var state = Controller.GetSymbolState(symbol);
            if (state != null)
            {
                _lblTrailAll.Text = $"TrailAll: {state.TrailAll:F3}";
                _txtTrailAll.Text = state.TrailAll.ToString("F3");
            }

            Controller.LogMessage($"TrailHalf set to {newValue:F3}");
        }

        private void TxtTrailAll_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                SetTrailAll();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void BtnSetTrailAll_Click(object? sender, EventArgs e)
        {
            SetTrailAll();
        }

        private void SetTrailAll()
        {
            string? symbol = Controller.ActiveSymbol;
            if (string.IsNullOrEmpty(symbol))
            {
                Controller.LogMessage("No symbol selected");
                return;
            }

            if (!double.TryParse(_txtTrailAll.Text, out double newValue) || newValue <= 0)
            {
                Controller.LogMessage("Invalid TrailAll value");
                return;
            }

            Controller.SetTrailAll(symbol, newValue);
            _lblTrailAll.Text = $"TrailAll: {newValue:F3}";
            Controller.LogMessage($"TrailAll set to {newValue:F3}");
        }

        private void BtnStartTrailing_Click(object? sender, EventArgs e)
        {
            string? symbol = Controller.ActiveSymbol;
            if (string.IsNullOrEmpty(symbol)) return;

            Controller.ToggleTrailingStop(symbol);
            UpdateTrailingButton();
        }

        private void UpdateTrailingButton()
        {
            string? symbol = Controller.ActiveSymbol;
            if (string.IsNullOrEmpty(symbol))
            {
                _btnStartTrailing.Text = "Start";
                _btnStartTrailing.Enabled = false;
                return;
            }

            var state = Controller.GetSymbolState(symbol);
            var position = Controller.GetPosition(symbol);
            bool hasPosition = position != null && position.Quantity > 0;

            if (!hasPosition)
            {
                // 无持仓
                _btnStartTrailing.Text = "Start";
                _btnStartTrailing.Enabled = false;
            }
            else if (state == null || state.TrailHalf <= 0)
            {
                // 有持仓但还没计算 TrailHalf（等待第一根 bar）
                _btnStartTrailing.Text = "Start";
                _btnStartTrailing.Enabled = false;
            }
            else if (state.TrailingStopActive)
            {
                // 已激活 → 可以点击停止
                _btnStartTrailing.Text = "Stop";
                _btnStartTrailing.Enabled = true;
            }
            else
            {
                // 有持仓 + 有数据 + 未激活 → 可以点击启动
                _btnStartTrailing.Text = "Start";
                _btnStartTrailing.Enabled = true;
            }
        }

        #endregion

        #region AutoFill

        private void UpdateAutoFillButtons()
        {
            string symbol = Controller.ActiveSymbol ?? "";
            var (ceil1, ceil2, ceilSessionHigh) = Controller.GetAutoFillPrices(symbol);

            _btnAutoFill1.Text = ceil1 > 0 ? ceil1.ToString("F1") : "--";
            _btnAutoFill2.Text = ceil2 > 0 ? ceil2.ToString("F1") : "--";
            _btnAutoFill3.Text = ceilSessionHigh > 0 ? ceilSessionHigh.ToString("F1") : "--";

            // SessionHigh - 0.01
            var state = Controller.GetSymbolState(symbol);
            double sessionHighMinus = state?.SessionHigh > 0 ? state.SessionHigh - 0.01 : 0;
            _btnAutoFill4.Text = sessionHighMinus > 0 ? sessionHighMinus.ToString("F2") : "--";
        }

        private void AutoFillTriggerPrice(string priceText)
        {
            if (priceText == "--") return;
            if (!double.TryParse(priceText, out double price)) return;

            string? symbol = Controller.ActiveSymbol;
            if (string.IsNullOrEmpty(symbol)) return;

            _txtTriggerPrice.Text = price.ToString("F2");
            Controller.AutoFillOpen(symbol, price);
        }

        #endregion

        #region HighBreakout

        private void UpdateHighBreakoutButtons()
        {
            string symbol = Controller.ActiveSymbol ?? "";
            var state = Controller.GetSymbolState(symbol);

            // SessionHigh - 0.01
            double sessionHighMinus = state?.SessionHigh > 0 ? state.SessionHigh - 0.01 : 0;
            _btnHBAutoFill1.Text = sessionHighMinus > 0 ? sessionHighMinus.ToString("F2") : "--";

            // FloorLevel(SessionHigh)
            double floorSessionHigh = state?.SessionHigh > 0 ? AgentStrategy.FloorLevel(state.SessionHigh) : 0;
            _btnHBAutoFill2.Text = floorSessionHigh > 0 ? floorSessionHigh.ToString("F1") : "--";
        }

        private void AutoFillHighBreakoutPrice(string priceText)
        {
            if (priceText == "--") return;
            if (!double.TryParse(priceText, out double price)) return;

            string? symbol = Controller.ActiveSymbol;
            if (string.IsNullOrEmpty(symbol)) return;

            _txtHighBreakoutPrice.Text = price.ToString("F2");
            Controller.StartHighBreakout(symbol, price);
        }

        private void TxtHighBreakoutPrice_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                StartHighBreakout();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void BtnHighBreakoutOpen_Click(object? sender, EventArgs e)
        {
            StartHighBreakout();
        }

        private void StartHighBreakout()
        {
            string? symbol = Controller.ActiveSymbol;
            if (string.IsNullOrEmpty(symbol))
            {
                Controller.LogMessage("No symbol selected");
                return;
            }

            if (!double.TryParse(_txtHighBreakoutPrice.Text.Trim(), out double triggerPrice) || triggerPrice <= 0)
            {
                Controller.LogMessage("Invalid HighBreakout trigger price");
                return;
            }

            Controller.StartHighBreakout(symbol, triggerPrice);
        }

        #endregion

        #region BreakedLevel

        private void UpdateBreakedLevelDisplay()
        {
            var state = Controller.GetSymbolState(Controller.ActiveSymbol ?? "");
            if (state != null && state.AgentBreakedLevel > 0)
            {
                _lblBreakedLevel.Text = $"BrkLv: {state.AgentBreakedLevel:F2}";
            }
            else
            {
                _lblBreakedLevel.Text = "BrkLv: --";
            }
        }

        #endregion

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
                // Enter 默认执行当前可见的按钮操作
                if (_btnOpen.Visible)
                    StartOpen();
                else
                    StartAddAll();

                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void BtnOpen_Click(object? sender, EventArgs e)
        {
            StartOpen();
        }

        private void BtnAddAll_Click(object? sender, EventArgs e)
        {
            StartAddAll();
        }

        private void BtnAddHalf_Click(object? sender, EventArgs e)
        {
            StartAddHalf();
        }

        private void BtnStopStrategy_Click(object? sender, EventArgs e)
        {
            StopStrategy();
        }

        private double? GetTriggerPrice()
        {
            if (!double.TryParse(_txtTriggerPrice.Text.Trim(), out double triggerPrice) || triggerPrice <= 0)
            {
                Controller.LogMessage("Invalid trigger price");
                return null;
            }
            return triggerPrice;
        }

        private void StartOpen()
        {
            string? symbol = Controller.ActiveSymbol;
            if (string.IsNullOrEmpty(symbol))
            {
                Controller.LogMessage("No symbol selected");
                return;
            }

            var triggerPrice = GetTriggerPrice();
            if (!triggerPrice.HasValue) return;

            Controller.StartOpen(symbol, triggerPrice.Value);
        }

        private void StartAddAll()
        {
            string? symbol = Controller.ActiveSymbol;
            if (string.IsNullOrEmpty(symbol))
            {
                Controller.LogMessage("No symbol selected");
                return;
            }

            var triggerPrice = GetTriggerPrice();
            if (!triggerPrice.HasValue) return;

            Controller.StartAddAll(symbol, triggerPrice.Value);
        }

        private void StartAddHalf()
        {
            string? symbol = Controller.ActiveSymbol;
            if (string.IsNullOrEmpty(symbol))
            {
                Controller.LogMessage("No symbol selected");
                return;
            }

            var triggerPrice = GetTriggerPrice();
            if (!triggerPrice.HasValue) return;

            Controller.StartAddHalf(symbol, triggerPrice.Value);
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