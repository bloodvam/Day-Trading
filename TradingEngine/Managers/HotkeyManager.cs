using System.Runtime.InteropServices;

namespace TradingEngine.Managers
{
    /// <summary>
    /// 热键管理（窗口激活时响应）
    /// </summary>
    public class HotkeyManager : IDisposable
    {
        private readonly Form _form;
        private readonly Dictionary<Keys, Action> _hotkeyActions = new();
        private bool _isEnabled = true;

        public bool IsEnabled
        {
            get => _isEnabled;
            set => _isEnabled = value;
        }

        public event Action<string>? Log;

        public HotkeyManager(Form form)
        {
            _form = form;
            _form.KeyPreview = true;
            _form.KeyDown += OnKeyDown;
        }

        /// <summary>
        /// 注册热键
        /// </summary>
        public void RegisterHotkey(Keys keys, Action action)
        {
            _hotkeyActions[keys] = action;
        }

        /// <summary>
        /// 取消注册热键
        /// </summary>
        public void UnregisterHotkey(Keys keys)
        {
            _hotkeyActions.Remove(keys);
        }

        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            if (!_isEnabled) return;

            // 构建完整的按键组合
            Keys keyWithModifiers = e.KeyCode;

            if (e.Shift) keyWithModifiers |= Keys.Shift;
            if (e.Alt) keyWithModifiers |= Keys.Alt;
            if (e.Control) keyWithModifiers |= Keys.Control;

            if (_hotkeyActions.TryGetValue(keyWithModifiers, out var action))
            {
                e.Handled = true;
                e.SuppressKeyPress = true;

                Log?.Invoke($"Hotkey triggered: {keyWithModifiers}");

                try
                {
                    action.Invoke();
                }
                catch (Exception ex)
                {
                    Log?.Invoke($"Hotkey action error: {ex.Message}");
                }
            }
        }

        public void Dispose()
        {
            _form.KeyDown -= OnKeyDown;
            _hotkeyActions.Clear();
        }
    }
}