using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Pawshot_Windows
{
    /// <summary>
    /// Windows APIを使用してグローバルホットキー（アプリが背面にあっても反応するキー）を管理するクラスです。
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
        private readonly int _id;

        public event Action? HotkeyPressed;

        public HotkeyManager(int id = 9000)
        {
            _id = id;
        }

        public void Register(Window window, uint modifiers, uint key)
        {
            _hWnd = new WindowInteropHelper(window).Handle;
            _source = HwndSource.FromHwnd(_hWnd);
            _source.AddHook(HwndHook);

            if (!RegisterHotKey(_hWnd, _id, modifiers, key))
            {
                // すでに他のアプリに使われている場合など
                System.Windows.MessageBox.Show("ショートカットキーの登録に失敗しました。他のアプリと競合している可能性があります。");
            }
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY && wParam.ToInt32() == _id)
            {
                HotkeyPressed?.Invoke();
                handled = true;
            }
            return IntPtr.Zero;
        }

        public void Dispose()
        {
            _source?.RemoveHook(HwndHook);
            if (_hWnd != IntPtr.Zero)
            {
                UnregisterHotKey(_hWnd, _id);
            }
            _source?.Dispose();
        }
    }
}
