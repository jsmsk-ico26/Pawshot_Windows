using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using Application = System.Windows.Application;

namespace Pawshot_Windows;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private NotifyIcon? _notifyIcon;
    private HotkeyManager? _hotkeyManager;
    private Window? _dummyWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 0. 設定の読み込み
        ConfigManager.Load();

        // 1. タスクトレイアイコンの設定
        _notifyIcon = new NotifyIcon();
        // 標準アイコンを使用 (System.Drawing.Commonが必要)
        _notifyIcon.Icon = System.Drawing.SystemIcons.Application; 
        _notifyIcon.Visible = true;
        _notifyIcon.Text = "WinShot (Ctrl+Shift+6)";

        // コンテキストメニュー（右クリックメニュー）
        var menu = new ContextMenuStrip();
        menu.Items.Add("キャプチャ開始 (&C)", null, (s, ev) => ShowCaptureWindow());
        menu.Items.Add("設定 (&S)", null, (s, ev) => ShowSettings());
        menu.Items.Add("-"); // セパレーター
        menu.Items.Add("終了 (&X)", null, (s, ev) => ShutdownApp());
        _notifyIcon.ContextMenuStrip = menu;

        // ダブルクリックでも起動
        _notifyIcon.DoubleClick += (s, ev) => ShowCaptureWindow();

        // 2. ホットキーの登録
        // RegisterHotKeyにはウィンドウハンドルが必要なため、非表示のダミーウィンドウを作成
        _dummyWindow = new Window { 
            WindowStyle = WindowStyle.None, 
            ShowInTaskbar = false, 
            Width = 0, 
            Height = 0, 
            Left = -100, 
            Top = -100,
            Opacity = 0 
        };
        _dummyWindow.Show();
        _dummyWindow.Hide();

        _hotkeyManager = new HotkeyManager();
        // Ctrl(2) + Shift(4) = 6, Key '6' = 0x36
        _hotkeyManager.Register(_dummyWindow, HotkeyManager.MOD_CONTROL | HotkeyManager.MOD_SHIFT, 0x36);
        _hotkeyManager.HotkeyPressed += () => ShowCaptureWindow();
    }

    private void ShowCaptureWindow()
    {
        // 既存のウィンドウがある場合は前面に出すだけにするなどの工夫も可能だが、
        // 現状のMainWindowはClose()で自身を閉じる設計なので、都度生成する
        var captureWindow = new MainWindow();
        captureWindow.Show();
        captureWindow.Activate();
    }

    private void ShowSettings()
    {
        var settingsWindow = new SettingsWindow();
        settingsWindow.ShowDialog();
    }

    private void ShutdownApp()
    {
        _hotkeyManager?.Dispose();
        _notifyIcon?.Dispose();
        _dummyWindow?.Close();
        Shutdown();
    }
}

