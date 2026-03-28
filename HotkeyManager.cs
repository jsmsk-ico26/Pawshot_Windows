using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Pawshot_Windows
{
    /// <summary>
    /// Windows APIを使用してグローバルホットキー（アプリが背面にあっても反応するキー）を管理するクラスです。
    /// 複数のホットキーを登録でき、どのIDが押されたかをイベントで通知します。
    /// </summary>
    public class HotkeyManager : IDisposable
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        public const uint MOD_ALT = 0x0001;
        public const uint MOD_CONTROL = 0x0002;
        public const uint MOD_SHIFT = 0x0004;
        public const uint MOD_WIN = 0x0008;

        private const int WM_HOTKEY = 0x0312;
        private IntPtr _hWnd;
        private HwndSource? _source;
        private readonly List<int> _registeredIds = new();

        /// <summary>ホットキーが押されたときに発火。引数はホットキーID。</summary>
        public event Action<int>? HotkeyPressed;

        public void Register(Window window, int id, uint modifiers, uint key)
        {
            // 初回登録時のみウィンドウに紐付ける
            if (_source == null)
            {
                _hWnd = new WindowInteropHelper(window).Handle;
                _source = HwndSource.FromHwnd(_hWnd);
                _source.AddHook(HwndHook);
            }

            if (!RegisterHotKey(_hWnd, id, modifiers, key))
            {
                System.Windows.MessageBox.Show(
                    $"ショートカットキー (ID:{id}) の登録に失敗しました。他のアプリと競合している可能性があります。");
            }
            else
            {
                _registeredIds.Add(id);
            }
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY)
            {
                int id = wParam.ToInt32();
                if (_registeredIds.Contains(id))
                {
                    HotkeyPressed?.Invoke(id);
                    handled = true;
                }
            }
            return IntPtr.Zero;
        }

        public void Dispose()
        {
            _source?.RemoveHook(HwndHook);
            foreach (var id in _registeredIds)
            {
                UnregisterHotKey(_hWnd, id);
            }
            _registeredIds.Clear();
            _source?.Dispose();
        }
    }
}
