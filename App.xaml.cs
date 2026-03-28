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

    // ホットキーID の定数
    private const int HotkeyId_Annotation = 9001; // Ctrl+Shift+6 → アノテーション
    private const int HotkeyId_Ocr        = 9002; // Ctrl+Shift+7 → OCR専用

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 0. 設定の読み込み
        ConfigManager.Load();

        // 1. タスクトレイアイコンの設定
        _notifyIcon = new NotifyIcon();
        _notifyIcon.Icon = System.Drawing.SystemIcons.Application;
        _notifyIcon.Visible = true;
        _notifyIcon.Text = "Pawshot\n6: アノテーション\n7: OCR";

        // コンテキストメニュー（右クリックメニュー）
        var menu = new ContextMenuStrip();
        menu.Items.Add("📸 キャプチャ + アノテーション (Ctrl+Shift+6)", null, (s, ev) => ShowCaptureWindow(CaptureMode.Annotation));
        menu.Items.Add("🔤 OCR テキスト抽出 (Ctrl+Shift+7)", null, (s, ev) => ShowCaptureWindow(CaptureMode.Ocr));
        menu.Items.Add("設定 (&S)", null, (s, ev) => ShowSettings());
        menu.Items.Add("-"); // セパレーター
        menu.Items.Add("終了 (&X)", null, (s, ev) => ShutdownApp());
        _notifyIcon.ContextMenuStrip = menu;

        // ダブルクリックでアノテーションモード起動
        _notifyIcon.DoubleClick += (s, ev) => ShowCaptureWindow(CaptureMode.Annotation);

        // 2. ホットキーの登録
        _dummyWindow = new Window {
            WindowStyle = WindowStyle.None,
            ShowInTaskbar = false,
            Width = 0, Height = 0,
            Left = -100, Top = -100,
            Opacity = 0
        };
        _dummyWindow.Show();
        _dummyWindow.Hide();

        _hotkeyManager = new HotkeyManager();
        // Ctrl+Shift+6 → アノテーションモード (Key '6' = 0x36)
        _hotkeyManager.Register(_dummyWindow, HotkeyId_Annotation, HotkeyManager.MOD_CONTROL | HotkeyManager.MOD_SHIFT, 0x36);
        // Ctrl+Shift+7 → OCR専用モード (Key '7' = 0x37)
        _hotkeyManager.Register(_dummyWindow, HotkeyId_Ocr,        HotkeyManager.MOD_CONTROL | HotkeyManager.MOD_SHIFT, 0x37);

        _hotkeyManager.HotkeyPressed += (id) =>
        {
            if (id == HotkeyId_Annotation)
                ShowCaptureWindow(CaptureMode.Annotation);
            else if (id == HotkeyId_Ocr)
                ShowCaptureWindow(CaptureMode.Ocr);
        };
    }

    private void ShowCaptureWindow(CaptureMode mode)
    {
        var captureWindow = new MainWindow(mode);
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

