using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Pawshot_Windows
{
    public partial class PreviewWindow : Window
    {
        private string _filePath = "";
        private BitmapSource? _image;
        private DispatcherTimer _autoCloseTimer;
        private bool _isMouseOver = false;

        public PreviewWindow(BitmapSource image, string filePath, string ocrText = "")
        {
            InitializeComponent();
            _image = image;
            _filePath = filePath;

            if (!string.IsNullOrEmpty(ocrText))
            {
                // OCRモード: テキストを表示、画像サムネイルは非表示
                PreviewImage.Visibility = Visibility.Collapsed;
                OcrPanel.Visibility = Visibility.Visible;
                OcrHeader.Visibility = Visibility.Visible;
                OcrText.Text = ocrText;
            }
            else
            {
                // アノテーションモード: 画像サムネイルを表示
                PreviewImage.Source = _image;
            }

            // 画面の右下（タスクバーの上）に配置
            var desktopWorkingArea = SystemParameters.WorkArea;
            this.Left = desktopWorkingArea.Right - this.Width - 10;
            this.Top = desktopWorkingArea.Bottom - this.Height - 10;

            // アニメーション（スライドアップ＆フェードイン）
            var startTop = this.Top + 50;
            var endTop = this.Top;
            this.Top = startTop;
            this.Opacity = 0;

            var slideAnim = new DoubleAnimation(startTop, endTop, TimeSpan.FromSeconds(0.4)) { EasingFunction = new QuadraticEase() };
            var fadeAnim = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.4));

            this.BeginAnimation(TopProperty, slideAnim);
            this.BeginAnimation(OpacityProperty, fadeAnim);

            // 自動消去タイマー（5秒）
            _autoCloseTimer = new DispatcherTimer();
            _autoCloseTimer.Interval = TimeSpan.FromSeconds(5);
            _autoCloseTimer.Tick += (s, e) => {
                if (!_isMouseOver) CloseWithAnimation();
            };
            _autoCloseTimer.Start();
        }

        private void CloseWithAnimation()
        {
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.5));
            fadeOut.Completed += (s, e) => this.Close();
            this.BeginAnimation(OpacityProperty, fadeOut);
        }

        private void MainBorder_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            _isMouseOver = true;
            ActionsOverlay.Visibility = Visibility.Visible;
        }

        private void MainBorder_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            _isMouseOver = false;
            ActionsOverlay.Visibility = Visibility.Collapsed;
        }

        private void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_filePath) && System.IO.File.Exists(_filePath))
            {
                System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{_filePath}\"");
            }
            this.Close();
        }

        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            if (_image != null)
            {
                System.Windows.Clipboard.SetImage(_image);
            }
            this.Close();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
