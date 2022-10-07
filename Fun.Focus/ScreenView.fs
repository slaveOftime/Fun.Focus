[<AutoOpen>]
module Fun.Focus.ScreenView

open System.Threading
open System.Windows.Controls
open System.Drawing
open System.Drawing.Imaging
open SkiaSharp
open SkiaSharp.Views.WPF
open SkiaSharp.Views.Desktop
open Microsoft.Extensions.DependencyInjection
open Fun.SunUI
open Fun.SunUI.WPF
open Fun.Focus


let screenView =
    UI.native<SKElement, WPF> (fun ctx ->
        let cts = new CancellationTokenSource()
        let focusService = ctx.ServiceProvider.GetService<FocusService>()
        let canvas = SKElement()

        ctx.AddDispose cts

        Grid.SetRowSpan(canvas, 2)

        canvas.PaintSurface.Add(fun e ->
            let canvas = e.Surface.Canvas
            let settings = focusService.Settings.GetValue()

            if focusService.IsActive.Value then
                canvas.Clear SKColors.Transparent
            else
                use bitmap =
                    new Bitmap((int) (settings.Width * focusService.DpiX), (int) (settings.Height * focusService.DpiY), PixelFormat.Format32bppArgb)
                use graphics = Graphics.FromImage(bitmap)

                graphics.CopyFromScreen(
                    (int) (settings.Left * focusService.DpiX),
                    (int) (settings.Top * focusService.DpiY),
                    0,
                    0,
                    bitmap.Size,
                    CopyPixelOperation.SourceCopy
                )

                let skBitmap = bitmap.ToSKBitmap()

                canvas.Clear SKColors.Transparent
                canvas.DrawBitmap(skBitmap, SKPoint(float32 0, float32 0))
        )

        async {
            while not cts.IsCancellationRequested do
                let mutable delay = focusService.Settings.GetFieldValue(fun x -> x.FrameDelay)
                if delay < 10 || delay > 1_000 then delay <- 100

                do! Async.Sleep delay

                focusService.Dispatcher canvas.InvalidateVisual
        }
        |> Async.Start

        canvas
    )
