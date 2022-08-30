using SkiaSharp;
using SkiaSharp.Views.Desktop;
using System.Drawing;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using PixelFormat = System.Drawing.Imaging.PixelFormat;

namespace Fun.Focus;

public partial class MainWindow : Window
{
    private readonly double dpiX;
    private readonly double dpiY;
    private bool isExtraPropertiesSetted = false;


    public MainWindow()
    {
        InitializeComponent();

        SourceInitialized += MainWindow_SourceInitialized;
        Closed += MainWindow_Closed;
        SizeChanged += MainWindow_SizeChanged;

        windowChrome.ResizeBorderThickness = new Thickness(3);
        windowChrome.CaptionHeight = 0;

        using var source = new HwndSource(new HwndSourceParameters());
        dpiX = source.CompositionTarget.TransformToDevice.M11;
        dpiY = source.CompositionTarget.TransformToDevice.M22;

        LoopUpdateCanvas();
    }


    private async void LoopUpdateCanvas()
    {
        while (true)
        {
            int.TryParse(FrameDelay.Text, out var delay);
            if (delay < 10 || delay > 1_000)
            {
                delay = 100;
            }

            await Task.Delay(delay);

            Canvas.InvalidateVisual();

            if (IsActive)
            {
                isExtraPropertiesSetted = false;
                Header.Background = System.Windows.Media.Brushes.White;
                Border.BorderBrush = System.Windows.Media.Brushes.White;
            }
            else
            {
                if (!isExtraPropertiesSetted)
                {
                    try
                    {
                        var brush = (SolidColorBrush)new BrushConverter().ConvertFrom(FocusColor.Text);

                        FrameDelay.Text = delay.ToString();
                        Header.Background = brush;
                        Border.BorderBrush = brush;
                        isExtraPropertiesSetted = true;
                    }
                    catch (Exception) { }
                }
            }
        }
    }


    private void CloseApp(object sender, RoutedEventArgs e) => Application.Current.Shutdown();

    private void StartDrag(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        DragMove();
        NormalizeWindow();
    }

    private void MainWindow_SourceInitialized(object? sender, EventArgs e)
    {
        TitleText.Text = Properties.Settings.Default.Title;
        Height = Properties.Settings.Default.Height;
        Width = Properties.Settings.Default.Width;
        if (Properties.Settings.Default.Top != 0 && Properties.Settings.Default.Left != 0)
        {
            Top = Properties.Settings.Default.Top;
            Left = Properties.Settings.Default.Left;
        }
        FocusColor.Text = Properties.Settings.Default.FocusColor;
        FrameDelay.Text = Properties.Settings.Default.FrameDelay.ToString();
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        Properties.Settings.Default.Title = TitleText.Text;
        Properties.Settings.Default.Top = Top;
        Properties.Settings.Default.Left = Left;
        Properties.Settings.Default.Height = Height;
        Properties.Settings.Default.Width = Width;
        Properties.Settings.Default.FocusColor = FocusColor.Text;
        if (int.TryParse(FrameDelay.Text, out var delay))
        {
            Properties.Settings.Default.FrameDelay = delay;
        }
        Properties.Settings.Default.Save();
    }

    private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        NormalizeWindow();
    }

    private void Canvas_PaintSurface(object sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;

        if (!IsActive)
        {
            using var bitmap = new Bitmap((int)(Width * dpiX), (int)(Height * dpiY), PixelFormat.Format32bppArgb);
            using var graphics = Graphics.FromImage(bitmap);

            graphics.CopyFromScreen((int)(Left * dpiX), (int)(Top * dpiY), 0, 0, bitmap.Size, CopyPixelOperation.SourceCopy);

            using var skBitmap = bitmap.ToSKBitmap();

            canvas.Clear(SKColors.Transparent);
            canvas.DrawBitmap(skBitmap, new SKPoint(0, 0));
        }
        else
        {
            canvas.Clear(SKColors.Transparent);
        }
    }


    private void NormalizeWindow()
    {
        Left = (int)(Left * dpiX) / dpiX;
        Top = (int)(Top * dpiY) / dpiY;

        Width = (int)(Width * dpiX) / dpiX;
        Height = (int)(Height * dpiY) / dpiY;
    }
}
