using System.Runtime.InteropServices;

namespace TradingEngine.Managers
{
    /// <summary>
    /// 全局热键管理（任何时候都能响应）
    /// </summary>
    public class HotkeyManager : IDisposable
    {
        // Windows API
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        // 修饰键常量
        private const uint MOD_ALT = 0x0001;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        private const uint MOD_NOREPEAT = 0x4000;

        private const int WM_HOTKEY = 0x0312;

        private readonly Form _form;
        private readonly Dictionary<int, Action> _hotkeyActions = new();
        private readonly Dictionary<int, Keys> _hotkeyKeys = new();
        private int _hotkeyIdCounter = 0;
        private bool _isRegistered = true;

        public bool IsRegistered => _isRegistered;

        public event Action<string>? Log;

        public HotkeyManager(Form form)
        {
            _form = form;

            // 拦截 Windows 消息
            Application.AddMessageFilter(new HotkeyMessageFilter(this));
        }

        /// <summary>
        /// 注册全局热键
        /// </summary>
        public bool RegisterHotkey(Keys keys, Action action)
        {
            int id = ++_hotkeyIdCounter;

            // 分离修饰键和主键
            uint modifiers = MOD_NOREPEAT;
            uint vk = (uint)(keys & Keys.KeyCode);

            if ((keys & Keys.Shift) == Keys.Shift) modifiers |= MOD_SHIFT;
            if ((keys & Keys.Alt) == Keys.Alt) modifiers |= MOD_ALT;
            if ((keys & Keys.Control) == Keys.Control) modifiers |= MOD_CONTROL;

            if (RegisterHotKey(_form.Handle, id, modifiers, vk))
            {
                _hotkeyActions[id] = action;
                _hotkeyKeys[id] = keys;
                Log?.Invoke($"Registered global hotkey: {keys}");
                return true;
            }
            else
            {
                Log?.Invoke($"Failed to register hotkey: {keys}");
                return false;
            }
        }

        /// <summary>
        /// 注销所有热键（释放给其他程序使用）
        /// </summary>
        public void UnregisterAll()
        {
            if (!_isRegistered) return;

            foreach (var id in _hotkeyActions.Keys)
            {
                UnregisterHotKey(_form.Handle, id);
            }
            _isRegistered = false;
            Log?.Invoke("All hotkeys unregistered");
        }

        /// <summary>
        /// 重新注册所有热键
        /// </summary>
        public void ReregisterAll()
        {
            if (_isRegistered) return;

            foreach (var kvp in _hotkeyKeys)
            {
                int id = kvp.Key;
                Keys keys = kvp.Value;

                uint modifiers = MOD_NOREPEAT;
                uint vk = (uint)(keys & Keys.KeyCode);

                if ((keys & Keys.Shift) == Keys.Shift) modifiers |= MOD_SHIFT;
                if ((keys & Keys.Alt) == Keys.Alt) modifiers |= MOD_ALT;
                if ((keys & Keys.Control) == Keys.Control) modifiers |= MOD_CONTROL;

                RegisterHotKey(_form.Handle, id, modifiers, vk);
            }
            _isRegistered = true;
            Log?.Invoke("All hotkeys re-registered");
        }

        /// <summary>
        /// 处理热键消息
        /// </summary>
        internal void HandleHotkey(int id)
        {
            if (!_isRegistered) return;

            if (_hotkeyActions.TryGetValue(id, out var action))
            {
                if (_hotkeyKeys.TryGetValue(id, out var keys))
                {
                    Log?.Invoke($"Hotkey triggered: {keys}");
                }

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
            // 注销所有热键
            foreach (var id in _hotkeyActions.Keys)
            {
                UnregisterHotKey(_form.Handle, id);
            }
            _hotkeyActions.Clear();
            _hotkeyKeys.Clear();
        }

        /// <summary>
        /// 消息过滤器，拦截 WM_HOTKEY 消息
        /// </summary>
        private class HotkeyMessageFilter : IMessageFilter
        {
            private readonly HotkeyManager _manager;

            public HotkeyMessageFilter(HotkeyManager manager)
            {
                _manager = manager;
            }

            public bool PreFilterMessage(ref Message m)
            {
                if (m.Msg == WM_HOTKEY)
                {
                    int id = m.WParam.ToInt32();
                    _manager.HandleHotkey(id);
                    return true;
                }
                return false;
            }
        }
    }
}