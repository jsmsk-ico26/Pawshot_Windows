using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Pawshot_Windows
{
    public partial class AnnotationWindow : Window
    {
        private BitmapSource _baseImage;
        private System.Windows.Point _startPoint;
        private Shape? _currentShape;
        private UIElement? _selectedElement;
        private bool _isDragging = false;
        private System.Windows.Point _dragOffset;
        private List<UIElement> _history = new List<UIElement>();
        private System.Windows.Media.Brush _currentBrush = System.Windows.Media.Brushes.Red;
        private double _currentThickness = 5.0; // デフォルト：普通
        
        public RenderTargetBitmap? ResultImage { get; private set; }

        public AnnotationWindow(BitmapSource image)
        {
            InitializeComponent();
            _baseImage = image;
            SourceImage.Source = _baseImage;
            
            // ウィンドウサイズをキャプチャサイズに合わせる (ツールバー分を追加)
            this.Width = _baseImage.PixelWidth + 40; // 左右マージン計40
            this.Height = _baseImage.PixelHeight + 85; // ツールバー(65) + 下マージン(20)
            
            DrawingCanvas.Width = _baseImage.PixelWidth;
            DrawingCanvas.Height = _baseImage.PixelHeight;

            this.PreviewKeyDown += AnnotationWindow_KeyDown;
            DrawingCanvas.MouseWheel += DrawingCanvas_MouseWheel;
        }

        private void AnnotationWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Z && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                Undo_Click(this, null!);
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                Cancel_Click(this, null!);
                 e.Handled = true;
            }
            else if (e.Key == Key.Delete || e.Key == Key.Back)
            {
                // 個別に消せる機能
                if (_selectedElement != null)
                {
                    DrawingCanvas.Children.Remove(_selectedElement);
                    _history.Remove(_selectedElement);
                    _selectedElement = null;
                    UpdateSelectionAdorner();
                    e.Handled = true;
                }
            }
        }

        private void DrawingCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _startPoint = e.GetPosition(DrawingCanvas);

            if (ToolSelect.IsChecked == true)
            {
                // 背景クリックで選択解除
                var hit = DrawingCanvas.InputHitTest(_startPoint) as UIElement;
                if (hit == DrawingCanvas)
                {
                    _selectedElement = null;
                    UpdateSelectionAdorner();
                }
                return;
            }

            if (ToolText.IsChecked == true)
            {
                // テキスト入力モード
                CreateTextAt(_startPoint);
                return;
            }

            if (ToolRect.IsChecked == true)
            {
                var rect = new System.Windows.Shapes.Rectangle
                {
                    Stroke = _currentBrush,
                    StrokeThickness = _currentThickness,
                    Fill = System.Windows.Media.Brushes.Transparent,
                    IsHitTestVisible = true
                };
                rect.PreviewMouseDown += Element_PreviewMouseDown;
                Canvas.SetLeft(rect, _startPoint.X);
                Canvas.SetTop(rect, _startPoint.Y);
                _currentShape = rect;
            }
            else if (ToolCircle.IsChecked == true)
            {
                var ellipse = new Ellipse
                {
                    Stroke = _currentBrush,
                    StrokeThickness = _currentThickness,
                    Fill = System.Windows.Media.Brushes.Transparent,
                    IsHitTestVisible = true
                };
                ellipse.PreviewMouseDown += Element_PreviewMouseDown;
                Canvas.SetLeft(ellipse, _startPoint.X);
                Canvas.SetTop(ellipse, _startPoint.Y);
                _currentShape = ellipse;
            }
            else if (ToolArrow.IsChecked == true)
            {
                var arrowPath = new Path
                {
                    Stroke = _currentBrush,
                    StrokeThickness = _currentThickness,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round,
                    IsHitTestVisible = true
                };
                arrowPath.PreviewMouseDown += Element_PreviewMouseDown;
                _currentShape = arrowPath;
            }
            else if (ToolBlur.IsChecked == true)
            {
                // ぼかし（Blur）の実装
                var blurRect = new System.Windows.Shapes.Rectangle
                {
                    Stroke = System.Windows.Media.Brushes.Transparent,
                    Fill = CreateBlurBrush(),
                    Effect = new System.Windows.Media.Effects.BlurEffect { Radius = 20, RenderingBias = System.Windows.Media.Effects.RenderingBias.Quality },
                    IsHitTestVisible = true
                };
                blurRect.PreviewMouseDown += Element_PreviewMouseDown;
                Canvas.SetLeft(blurRect, _startPoint.X);
                Canvas.SetTop(blurRect, _startPoint.Y);
                _currentShape = blurRect;
            }

            if (_currentShape != null)
            {
                DrawingCanvas.Children.Add(_currentShape);
            }
        }

        private void DrawingCanvas_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var currentPoint = e.GetPosition(DrawingCanvas);

            // 選択物のドラッグ移動
            if (_isDragging && _selectedElement != null)
            {
                Canvas.SetLeft(_selectedElement, currentPoint.X - _dragOffset.X);
                Canvas.SetTop(_selectedElement, currentPoint.Y - _dragOffset.Y);

                // ぼかし（Blur）の場合は背景ブラシの Viewbox も同期して動かす
                if (_selectedElement is System.Windows.Shapes.Rectangle selectedRect && selectedRect.Effect is System.Windows.Media.Effects.BlurEffect && selectedRect.Fill is VisualBrush brush)
                {
                    brush.Viewbox = new Rect(Canvas.GetLeft(selectedRect), Canvas.GetTop(selectedRect), selectedRect.Width, selectedRect.Height);
                }
                
                UpdateSelectionAdorner();
                return;
            }

            if (e.LeftButton != MouseButtonState.Pressed || _currentShape == null) return;

            if (_currentShape is System.Windows.Shapes.Rectangle newRect)
            {
                var left = Math.Min(_startPoint.X, currentPoint.X);
                var top = Math.Min(_startPoint.Y, currentPoint.Y);
                var width = Math.Max(1, Math.Abs(_startPoint.X - currentPoint.X));
                var height = Math.Max(1, Math.Abs(_startPoint.Y - currentPoint.Y));

                Canvas.SetLeft(newRect, left);
                Canvas.SetTop(newRect, top);
                newRect.Width = width;
                newRect.Height = height;

                if (ToolBlur.IsChecked == true && newRect.Fill is VisualBrush brush)
                {
                    brush.Viewbox = new Rect(left, top, width, height);
                }
            }
            else if (_currentShape is Ellipse ellipse)
            {
                var left = Math.Min(_startPoint.X, currentPoint.X);
                var top = Math.Min(_startPoint.Y, currentPoint.Y);
                var width = Math.Max(1, Math.Abs(_startPoint.X - currentPoint.X));
                var height = Math.Max(1, Math.Abs(_startPoint.Y - currentPoint.Y));

                Canvas.SetLeft(ellipse, left);
                Canvas.SetTop(ellipse, top);
                ellipse.Width = width;
                ellipse.Height = height;
            }
            else if (_currentShape is Path arrow)
            {
                arrow.Data = CreateArrowGeometry(_startPoint, currentPoint);
            }
        }

        private void DrawingCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
            // _selectedElement は保持する（ホイール操作やカラー変更のため）
            UpdateSelectionAdorner();

            if (_currentShape != null)
            {
                _history.Add(_currentShape);
                _currentShape = null;
            }
        }

        private void DrawingCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (_selectedElement == null) return;

            double scaleFactor = e.Delta > 0 ? 1.1 : 0.9;

            if (_selectedElement is System.Windows.Controls.TextBox tb)
            {
                tb.FontSize = Math.Max(8, tb.FontSize * scaleFactor);
            }
            else if (_selectedElement is FrameworkElement fe)
            {
                // すべての要素で幾何学的な拡大縮小を適用（太さだけでなく全体）
                if (!(fe.RenderTransform is ScaleTransform st))
                {
                    st = new ScaleTransform(1, 1);
                    fe.RenderTransform = st;
                    fe.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
                }
                
                st.ScaleX *= scaleFactor;
                st.ScaleY *= scaleFactor;

                UpdateSelectionAdorner();

                // ぼかし（Blur）の場合は背景ブラシの Viewbox も同期が必要（スケールがかかるとズレるため再計算）
                if (fe is System.Windows.Shapes.Rectangle r && r.Effect is System.Windows.Media.Effects.BlurEffect && r.Fill is VisualBrush b)
                {
                    // スケール後の見かけのサイズに合わせてViewboxを調整（簡易版）
                    // 実際にはScaleTransformで表示されているのでViewboxそのままでも概ね合うはずだが、
                    // 位置がズレる場合はここで補正。
                }
            }
        }

        private void CreateTextAt(System.Windows.Point point)
        {
            var textBox = new System.Windows.Controls.TextBox
            {
                // 透明だと反応が悪いため、わずかに不透明な色（#02...）を指定
                Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(2, 0, 0, 0)),
                Foreground = _currentBrush,
                BorderThickness = new Thickness(1),
                BorderBrush = System.Windows.Media.Brushes.LightGray,
                FontSize = 26, // 少し大きく
                FontWeight = FontWeights.Bold,
                MinWidth = 50,
                AcceptsReturn = true,
                IsHitTestVisible = true
            };

            Canvas.SetLeft(textBox, point.X);
            Canvas.SetTop(textBox, point.Y);
            DrawingCanvas.Children.Add(textBox);

            // 明示的にマウスイベントを拾ってドラッグを開始できるようにする
            textBox.PreviewMouseDown += Element_PreviewMouseDown;
            
            textBox.Loaded += (s, e) => textBox.Focus();
            textBox.LostFocus += (s, e) => FinalizeText(textBox);
            textBox.KeyDown += (s, e) => {
                if (e.Key == System.Windows.Input.Key.Enter && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                {
                    FinalizeText(textBox);
                }
            };
            
            _history.Add(textBox);
        }

        private void FinalizeText(System.Windows.Controls.TextBox textBox)
        {
            if (string.IsNullOrWhiteSpace(textBox.Text))
            {
                DrawingCanvas.Children.Remove(textBox);
                _history.Remove(textBox);
                textBox.PreviewMouseDown -= Element_PreviewMouseDown;
                UpdateSelectionAdorner();
                return;
            }

            textBox.BorderThickness = new Thickness(0);
            textBox.IsReadOnly = true;
            textBox.Focusable = false;
            // 未入力部分も掴めるように背景を維持
            UpdateSelectionAdorner();
        }

        private void Element_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (ToolSelect.IsChecked == true)
            {
                // ウィンドウにフォーカスを戻してキーイベントを受け取れるようにする
                this.Focus();

                // sender (=UI要素自体) をガッチリ掴む
                _selectedElement = sender as UIElement;
                if (_selectedElement != null)
                {
                    _isDragging = true;
                    _startPoint = e.GetPosition(DrawingCanvas);
                    var left = Canvas.GetLeft(_selectedElement);
                    var top = Canvas.GetTop(_selectedElement);
                    if (double.IsNaN(left)) left = 0;
                    if (double.IsNaN(top)) top = 0;
                    _dragOffset = new System.Windows.Point(_startPoint.X - left, _startPoint.Y - top);

                    // ダブルクリックでテキスト編集再開
                    if (e.ClickCount == 2 && _selectedElement is System.Windows.Controls.TextBox tb)
                    {
                        tb.IsReadOnly = false;
                        tb.Focusable = true;
                        tb.BorderThickness = new Thickness(1);
                        tb.Focus();
                    }

                    UpdateSelectionAdorner();
                    e.Handled = true; // イベントが貫通しないようにする
                }
            }
        }

        private void UpdateSelectionAdorner()
        {
            if (_selectedElement == null || _selectedElement == DrawingCanvas || _selectedElement == SelectionAdorner)
            {
                SelectionAdorner.Visibility = Visibility.Collapsed;
                return;
            }

            try
            {
                // 要素の実際のサイズと描画領域（スケール含む）を取得
                var bounds = VisualTreeHelper.GetDescendantBounds(_selectedElement);
                if (bounds.IsEmpty)
                {
                    // テキストが空の場合などの暫定処理
                    if (_selectedElement is FrameworkElement fe)
                    {
                        bounds = new Rect(0, 0, fe.ActualWidth, fe.ActualHeight);
                    }
                }
                
                var transform = _selectedElement.TransformToVisual(DrawingCanvas);
                var rect = transform.TransformBounds(bounds);

                // 枠線を少し広げる
                SelectionAdorner.Width = rect.Width + 10;
                SelectionAdorner.Height = rect.Height + 10;
                Canvas.SetLeft(SelectionAdorner, rect.Left - 5);
                Canvas.SetTop(SelectionAdorner, rect.Top - 5);
                SelectionAdorner.Visibility = Visibility.Visible;
            }
            catch
            {
                SelectionAdorner.Visibility = Visibility.Collapsed;
            }
        }

        private Geometry CreateArrowGeometry(System.Windows.Point start, System.Windows.Point end)
        {
            var geometry = new StreamGeometry();
            using (var ctx = geometry.Open())
            {
                ctx.BeginFigure(start, false, false);
                ctx.LineTo(end, true, true);

                // 矢印の頭（線の太さに比例させる）
                var vector = end - start;
                vector.Normalize();
                var angle = Math.Atan2(vector.Y, vector.X);
                var headLen = 10.0 + _currentThickness * 2; // 太さに応じて調整
                var headAngle = Math.PI / 6;

                ctx.BeginFigure(new System.Windows.Point(end.X - headLen * Math.Cos(angle - headAngle), end.Y - headLen * Math.Sin(angle - headAngle)), false, false);
                ctx.LineTo(end, true, true);
                ctx.LineTo(new System.Windows.Point(end.X - headLen * Math.Cos(angle + headAngle), end.Y - headLen * Math.Sin(angle + headAngle)), true, true);
            }
            geometry.Freeze();
            return geometry;
        }

        private VisualBrush CreateBlurBrush()
        {
            // 背後の画像を表示するブラシ
            var brush = new VisualBrush(SourceImage)
            {
                ViewboxUnits = BrushMappingMode.Absolute,
                Stretch = Stretch.Fill
            };
            return brush;
        }

        private void Thickness_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border)
            {
                if (border == ThickThin) _currentThickness = 2.0;
                else if (border == ThickMedium) _currentThickness = 5.0;
                else if (border == ThickHeavy) _currentThickness = 10.0;

                // 選択アイテムがあれば太さを変更
                if (_selectedElement != null)
                {
                    if (_selectedElement is Shape shape)
                    {
                        shape.StrokeThickness = _currentThickness;
                        if (_selectedElement is Path path)
                        {
                            // 矢印（Path）の場合は形状も再描画が必要（今は固定形状なので簡易的に）
                            // ※ _startPoint などの管理が複雑なので、ここでは太さのみ、
                            // 次回のホイール操作などでリフレッシュされる想定。
                        }
                    }
                }

                // UIの選択状態を更新
                ThickThin.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(136, 255, 255, 255));
                ThickMedium.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(136, 255, 255, 255));
                ThickHeavy.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(136, 255, 255, 255));
                border.BorderBrush = System.Windows.Media.Brushes.White;
            }
        }

        private void Color_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border)
            {
                _currentBrush = border.Background;

                // 選択中アイテムがあれば色を変える
                if (_selectedElement != null)
                {
                    if (_selectedElement is Shape shape) shape.Stroke = _currentBrush;
                    else if (_selectedElement is System.Windows.Controls.TextBox tb) tb.Foreground = _currentBrush;
                }

                // UIの選択状態を更新
                PaletteRed.BorderBrush = null; PaletteRed.BorderThickness = new Thickness(0);
                PaletteBlue.BorderBrush = null; PaletteBlue.BorderThickness = new Thickness(0);
                PaletteGreen.BorderBrush = null; PaletteGreen.BorderThickness = new Thickness(0);
                PaletteYellow.BorderBrush = null; PaletteYellow.BorderThickness = new Thickness(0);
                PaletteWhite.BorderBrush = null; PaletteWhite.BorderThickness = new Thickness(0);
                PaletteBlack.BorderBrush = null; PaletteBlack.BorderThickness = new Thickness(0);

                border.BorderBrush = System.Windows.Media.Brushes.White;
                border.BorderThickness = new Thickness(2);
            }
        }

        private void Undo_Click(object sender, RoutedEventArgs e)
        {
            if (_history.Count > 0)
            {
                var last = _history[_history.Count - 1];
                DrawingCanvas.Children.Remove(last);
                _history.RemoveAt(_history.Count - 1);
            }
        }

        private void Done_Click(object sender, RoutedEventArgs e)
        {
            // キャンバスの内容を画像としてレンダリング
            var rtb = new RenderTargetBitmap(
                (int)DrawingCanvas.Width, (int)DrawingCanvas.Height, 
                96, 96, PixelFormats.Pbgra32);
            
            // 背景（元画像）と描画内容を合成
            VisualBrush sourceBrush = new VisualBrush(CanvasGrid);
            DrawingVisual visual = new DrawingVisual();
            using (DrawingContext dc = visual.RenderOpen())
            {
                dc.DrawRectangle(sourceBrush, null, new Rect(0, 0, DrawingCanvas.Width, DrawingCanvas.Height));
            }
            rtb.Render(visual);
            
            ResultImage = rtb;
            this.DialogResult = true;
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
