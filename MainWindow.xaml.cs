using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
// using System.Drawing; // Removed to avoid namespace conflicts with WPF (System.Windows)
using System.IO;
using Windows.Media.Ocr; // Windows OCR
using Windows.Graphics.Imaging; // Windows Imaging
using Windows.Storage.Streams; // Windows Streams
using System.Runtime.InteropServices.WindowsRuntime; // For AsRandomAccessStream
using System.Threading.Tasks;
using System.Linq; // For Linq
using Windows.Globalization; // For Language

namespace Pawshot_Windows;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private System.Windows.Point _startPoint;
    private bool _isDragging = false;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void CaptureCanvas_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            _isDragging = true;
            _startPoint = e.GetPosition(CaptureCanvas);
            
            SelectionRectangle.Visibility = Visibility.Visible;
            Canvas.SetLeft(SelectionRectangle, _startPoint.X);
            Canvas.SetTop(SelectionRectangle, _startPoint.Y);
            SelectionRectangle.Width = 0;
            SelectionRectangle.Height = 0;

            CaptureCanvas.CaptureMouse();
        }
    }

    private void CaptureCanvas_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (_isDragging)
        {
            var endPoint = e.GetPosition(CaptureCanvas);

            var x = Math.Min(endPoint.X, _startPoint.X);
            var y = Math.Min(endPoint.Y, _startPoint.Y);
            var width = Math.Max(endPoint.X, _startPoint.X) - x;
            var height = Math.Max(endPoint.Y, _startPoint.Y) - y;

            Canvas.SetLeft(SelectionRectangle, x);
            Canvas.SetTop(SelectionRectangle, y);
            SelectionRectangle.Width = width;
            SelectionRectangle.Height = height;
        }
    }

    private async void CaptureCanvas_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        _isDragging = false;
        if (SelectionRectangle.Visibility == Visibility.Visible)
        {
            double x = Canvas.GetLeft(SelectionRectangle);
            double y = Canvas.GetTop(SelectionRectangle);
            double width = SelectionRectangle.Width;
            double height = SelectionRectangle.Height;

            if (width > 0 && height > 0)
            {
                // Capture the screen region - Pass doubles for precision
                await CaptureScreenRegionAsync(x, y, width, height);
            }

            // Close the overlay after capture
            this.Close();
        }
    }

    private async Task CaptureScreenRegionAsync(double x, double y, double width, double height)
    {
        // 1. Get DPI scaling factor (WPF logical pixels vs Physical pixels)
        var dpi = VisualTreeHelper.GetDpi(this);
        
        // 2. Convert WPF logical points (relative to this window) to Physical Screen Pixels
        // PointToScreen already accounts for DPI for the position, but width/height need scaling.
        System.Windows.Point screenPoint = this.PointToScreen(new System.Windows.Point(x, y));
        
        int physicalX = (int)screenPoint.X;
        int physicalY = (int)screenPoint.Y;
        int physicalWidth = (int)(width * dpi.DpiScaleX);
        int physicalHeight = (int)(height * dpi.DpiScaleY);

        // Hide the window so it doesn't get captured
        this.Opacity = 0;

        // Give the UI enough time to fully disappear from the desktop buffer
        await Task.Delay(250); 

        try
        {
            System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(physicalWidth, physicalHeight);
            try
            {
                using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(bmp))
                {
                    // Copy the screen pixels using physical coordinates
                    g.CopyFromScreen(physicalX, physicalY, 0, 0, new System.Drawing.Size(physicalWidth, physicalHeight));
                }

                // Convert System.Drawing.Bitmap to WPF BitmapSource for UI/Clipboard
                BitmapSource bmpSource = ConvertBitmap(bmp);
                System.Windows.Clipboard.SetImage(bmpSource);

                // OCR Extraction
                string extractedText = await ExtractTextFromBitmapAsync(bmp);

                // 注釈編集ウィンドウを表示
                var annotationWin = new AnnotationWindow(bmpSource);
                if (annotationWin.ShowDialog() == true && annotationWin.ResultImage != null)
                {
                    // 編集後の画像を使用
                    bmpSource = annotationWin.ResultImage;
                    
                    // 保存用に System.Drawing.Bitmap を更新（元のを破棄して差し替え）
                    var editedBmp = BitmapFromSource(bmpSource);
                    bmp.Dispose();
                    bmp = editedBmp;
                }
                else
                {
                    // キャンセルされた場合は終了
                    this.Close();
                    return;
                }

                // 自動保存の実行 (編集後の画像が保存される)
                string savedPath = "";
                if (ConfigManager.Current.AutoSaveEnabled)
                {
                    savedPath = SaveBitmapToFile(bmp);
                }

                // フローティングプレビューを表示 (編集後の画像)
                var preview = new PreviewWindow(bmpSource, savedPath);
                preview.Show();

                // OCRテキストをクリップボードにコピー
                if (!string.IsNullOrEmpty(extractedText) && !extractedText.StartsWith("（"))
                {
                    System.Windows.Clipboard.SetText(extractedText);
                }
            }
            finally
            {
                if (bmp != null) bmp.Dispose();
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"キャプチャ中にエラーが発生しました: {ex.Message}", "Error");
        }
    }

    private async Task<string> ExtractTextFromBitmapAsync(System.Drawing.Bitmap bitmap)
    {
        try
        {
            // Scale up the bitmap (2x) for better OCR accuracy (WPF resolution vs OCR needs)
            int scaledWidth = bitmap.Width * 2;
            int scaledHeight = bitmap.Height * 2;
            
            using (System.Drawing.Bitmap scaledBmp = new System.Drawing.Bitmap(bitmap, new System.Drawing.Size(scaledWidth, scaledHeight)))
            {
                using (var stream = new MemoryStream())
                {
                    // Convert System.Drawing.Bitmap to Windows Runtime SoftwareBitmap
                    scaledBmp.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                    stream.Position = 0;

                    var decoder = await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(stream.AsRandomAccessStream());
                    var softwareBitmap = await decoder.GetSoftwareBitmapAsync();

                    // Check available languages for debugging
                    var langList = OcrEngine.AvailableRecognizerLanguages.Select(l => l.LanguageTag).ToList();
                    string languages = langList.Count > 0 ? string.Join(", ", langList) : "なし";

                    // Initialize OCR Engine (Default Language)
                    var engine = OcrEngine.TryCreateFromUserProfileLanguages();
                    if (engine != null)
                    {
                        var result = await engine.RecognizeAsync(softwareBitmap);
                        if (string.IsNullOrEmpty(result.Text))
                        {
                            return $"（テキスト未検出 / システム言語: {languages}）";
                        }
                        return result.Text;
                    }
                    else
                    {
                        return $"（エラー: OCRエンジンなし / システム言語: {languages}）";
                    }
                }
            }
        }
        catch (Exception ex)
        {
            return $"（解析エラー: {ex.Message}）";
        }
    }

    private string SaveBitmapToFile(System.Drawing.Bitmap bitmap)
    {
        try
        {
            string folder = ConfigManager.Current.SavePath;
            if (string.IsNullOrEmpty(folder))
            {
                // デフォルトはアプリ実行ファイルの場所にある Captures フォルダ
                folder = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Captures");
            }

            if (!System.IO.Directory.Exists(folder))
            {
                System.IO.Directory.CreateDirectory(folder);
            }

            string fileName = $"WinShot_{DateTime.Now:yyyyMMdd_HHmmss}.png";
            string filePath = System.IO.Path.Combine(folder, fileName);
            
            bitmap.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);
            return filePath;
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"ファイルの保存に失敗しました: {ex.Message}", "保存エラー");
            return "";
        }
    }

    private BitmapSource ConvertBitmap(System.Drawing.Bitmap source)
    {
        IntPtr hBitmap = source.GetHbitmap();
        BitmapSource result = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
            hBitmap,
            IntPtr.Zero,
            Int32Rect.Empty,
            BitmapSizeOptions.FromEmptyOptions());

        // Cleanup HBitmap
        DeleteObject(hBitmap);
        return result;
    }

    private System.Drawing.Bitmap BitmapFromSource(BitmapSource snippetsource)
    {
        using (MemoryStream outStream = new MemoryStream())
        {
            System.Windows.Media.Imaging.BitmapEncoder enc = new System.Windows.Media.Imaging.BmpBitmapEncoder();
            enc.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(snippetsource));
            enc.Save(outStream);
            // new Bitmap(Stream) はストリームが開いている必要がある場合があるため、
            // 一度作成したあとにクローン（new Bitmap(tempBmp)）して返すことで完全に独立させる。
            using (var tempBmp = new System.Drawing.Bitmap(outStream))
            {
                return new System.Drawing.Bitmap(tempBmp);
            }
        }
    }

    [System.Runtime.InteropServices.DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);
}