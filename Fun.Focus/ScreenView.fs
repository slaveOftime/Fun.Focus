[<AutoOpen>]
module Fun.Focus.ScreenView

open System.Windows.Controls
open System.Drawing
open System.Drawing.Imaging
open SkiaSharp
open SkiaSharp.Views.WPF
open SkiaSharp.Views.Desktop
open Microsoft.Extensions.DependencyInjection
open Fun.SunUI
open Fun.Focus


let screenView = {
    RenderMode = RenderMode.CreateOnce
    CreateOrUpdate =
        fun (sp, ctx) ->
            match ctx with
            | ValueSome x -> x
            | ValueNone ->
                let focusService = sp.GetService<FocusService>()
                let canvas = SKElement()

                Grid.SetRowSpan(canvas, 2)

                canvas.PaintSurface.Add(fun e ->
                    let canvas = e.Surface.Canvas
                    let settings = focusService.Settings.GetValue()

                    if focusService.IsActive.Value then
                        canvas.Clear SKColors.Transparent
                    else
                        use bitmap =
                            new Bitmap(
                                (int) (settings.Width * focusService.DpiX),
                                (int) (settings.Height * focusService.DpiY),
                                PixelFormat.Format32bppArgb
                            )
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
                    while true do
                        let mutable delay = focusService.Settings.GetFieldValue(fun x -> x.FrameDelay)
                        if delay < 10 || delay > 1_000 then delay <- 100

                        do! Async.Sleep delay

                        focusService.Dispatcher canvas.InvalidateVisual
                }
                |> Async.Start

                new ElementBuildContext<SKElement>(canvas, sp, RenderMode.CreateOnce)
}
