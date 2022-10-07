open System
open System.Threading.Tasks
open System.Windows
open System.Windows.Shell
open System.Windows.Controls
open System.Windows.Interop
open System.Drawing
open System.Drawing.Imaging
open SkiaSharp
open SkiaSharp.Views.WPF
open SkiaSharp.Views.Desktop
open FSharp.Data.Adaptive
open Microsoft.Extensions.DependencyInjection
open Fun.SunUI
open Fun.SunUI.WPF
open Fun.SunUI.WPF.Controls
open Fun.SunUI.WPF.Shapes
open Fun.Focus


let dpiX, dpiY =
    use source = new HwndSource(new HwndSourceParameters())
    source.CompositionTarget.TransformToDevice.M11, source.CompositionTarget.TransformToDevice.M22

let settings = new AdaptiveForm<_, string>(Settings.Default.Value)

let isActive = cval true

let focusBrush = adaptive {
    match! isActive with
    | true -> return Media.SolidColorBrush Media.Colors.White :> Media.Brush
    | false -> return Media.BrushConverter().ConvertFrom(settings.GetFieldValue(fun x -> x.FocusColor)) :?> Media.Brush
}

let mutable windowRef: Window = null


let normalizeWindow (windowRef: Window) =
    windowRef.Left <- windowRef.Left * dpiX / dpiX
    windowRef.Top <- windowRef.Top * dpiY / dpiY
    windowRef.Width <- windowRef.Width * dpiX / dpiX
    windowRef.Height <- windowRef.Height * dpiY / dpiY


let loopUpdateCanvas (canvas: SkiaSharp.Views.WPF.SKElement) = async {
    while true do
        let mutable delay = settings.GetFieldValue(fun x -> x.FrameDelay)
        if delay < 10 || delay > 1_000 then delay <- 100

        do! Async.Sleep delay

        windowRef.Dispatcher.Invoke canvas.InvalidateVisual
}


let toolbar =
    Grid'() {
        ColumnDefinitions(fun col ->
            col.Add(ColumnDefinition(Width = GridLength(0, GridUnitType.Auto)))
            col.Add(ColumnDefinition(Width = GridLength(1., GridUnitType.Star)))
            col.Add(ColumnDefinition(Width = GridLength(0, GridUnitType.Auto)))
        )
        Background focusBrush
        StaticChildren [
            TextBox'() {
                Text(settings.GetFieldValue(fun x -> x.Title))
                TextChanged(fun (this, _) -> settings.UseFieldSetter (fun x -> x.Title) (this.Text))
                BorderThickness(Thickness(0))
                Background(Media.SolidColorBrush Media.Colors.Transparent)
                Margin(Thickness(10, 0, 0, 0))
                Padding(Thickness(0, 3, 0, 0))
            }
            Rectangle'() {
                With(fun this -> Grid.SetColumn(this, 1))
                Fill(Media.SolidColorBrush Media.Colors.Transparent)
                MouseDown(fun _ ->
                    windowRef.DragMove()
                    normalizeWindow windowRef
                )
            }
            StackPanel'() {
                With(fun this -> Grid.SetColumn(this, 2))
                Orientation Orientation.Horizontal
                VerticalAlignment VerticalAlignment.Center
                StaticChildren [
                    TextBlock'() { Text "Frame delay (ms, 10~1000):" }
                    TextBox'() {
                        Text(settings.GetFieldValue(fun x -> x.FrameDelay) |> string)
                        TextChanged(fun (this, _) ->
                            match Int32.TryParse this.Text with
                            | true, x -> settings.UseFieldSetter (fun x -> x.FrameDelay) (x)
                            | _ -> ()
                        )
                        BorderThickness(Thickness(0))
                        Background(Media.SolidColorBrush Media.Colors.Transparent)
                    }
                    TextBlock'() {
                        Text "ocus color:"
                        Margin(Thickness(15, 0, 0, 0))
                    }
                    TextBox'() {
                        Text(settings.GetFieldValue(fun x -> x.FocusColor))
                        TextChanged(fun (this, _) -> settings.UseFieldSetter (fun x -> x.FocusColor) (this.Text))
                        BorderThickness(Thickness(0))
                        Background(Media.SolidColorBrush Media.Colors.Transparent)
                    }
                    Button'() {
                        Content' "Close"
                        Click(fun _ -> windowRef.Close())
                        Margin(Thickness(5, 0, 0, 0))
                        Padding(Thickness(10, 0, 10, 2))
                        BorderThickness(Thickness(0))
                        Background(Media.SolidColorBrush Media.Colors.Transparent)
                    }
                ]
            }
        ]
    }


let screenView = {
    RenderMode = RenderMode.CreateOnce
    CreateOrUpdate =
        fun (sp, _) ->
            let canvas = SKElement()
            Grid.SetRowSpan(canvas, 2)
            canvas.PaintSurface.Add(fun e ->
                let canvas = e.Surface.Canvas

                if isActive.Value then
                    canvas.Clear SKColors.Transparent
                else
                    use bitmap = new Bitmap((int) (windowRef.Width * dpiX), (int) (windowRef.Height * dpiY), PixelFormat.Format32bppArgb)
                    use graphics = Graphics.FromImage(bitmap)

                    graphics.CopyFromScreen(
                        (int) (windowRef.Left * dpiX),
                        (int) (windowRef.Top * dpiY),
                        0,
                        0,
                        bitmap.Size,
                        CopyPixelOperation.SourceCopy
                    )

                    let skBitmap = bitmap.ToSKBitmap()

                    canvas.Clear SKColors.Transparent
                    canvas.DrawBitmap(skBitmap, SKPoint(float32 0, float32 0))
            )
            loopUpdateCanvas canvas |> Async.Start
            new ElementBuildContext<SKElement>(canvas, sp, RenderMode.CreateOnce)
}


let window =
    Window'() {
        With(fun this ->
            windowRef <- this

            let windowChrome = WindowChrome()
            windowChrome.ResizeBorderThickness <- new Thickness(3)
            windowChrome.CaptionHeight <- 0
            WindowChrome.SetWindowChrome(this, windowChrome)
        )
        Title(settings.GetFieldValue(fun x -> x.Title))
        Width(settings.GetFieldValue(fun x -> x.Width))
        Height(settings.GetFieldValue(fun x -> x.Height))
        Activated(fun _ -> isActive.Publish true)
        Deactivated(fun _ -> isActive.Publish false)
        AllowsTransparency true
        Background(Media.SolidColorBrush Media.Colors.Transparent)
        WindowState WindowState.Normal
        WindowStyle WindowStyle.None
        Closed(fun _ ->
            Settings.Default.Value <- settings.GetValue()
            Settings.Default.Save()
        )
        SizeChanged(fun (x, _) -> normalizeWindow x)
        Grid'() {
            RowDefinitions(fun row ->
                row.Add(RowDefinition(Height = GridLength(22.)))
                row.Add(RowDefinition(Height = GridLength(1., GridUnitType.Star)))
            )
            StaticChildren [
                screenView
                Border'() {
                    With(fun this -> Grid.SetRow(this, 1))
                    BorderThickness(Thickness(2))
                    BorderBrush focusBrush
                }
                toolbar
            ]
        }
    }


[<EntryPoint; STAThread>]
let main (_: string[]) =

    let services = ServiceCollection()
    let sp = services.BuildServiceProvider()
    let nativeWindow = window.Build<Window>(sp)

    Application() |> ignore
    Application.Current.Run nativeWindow
