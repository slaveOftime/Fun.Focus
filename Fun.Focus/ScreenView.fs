[<AutoOpen>]
module Fun.Focus.ScreenView

open System.IO
open System.Drawing
open System.Drawing.Imaging
open System.Threading
open System.Windows.Media
open System.Windows.Media.Imaging
open System.Windows.Controls
open FSharp.Data.Adaptive
open Microsoft.Extensions.DependencyInjection
open Fun.SunUI
open Fun.SunUI.WPF.Controls
open Fun.Focus


let screenView =
    UI.inject (fun ctx ->
        let cts = new CancellationTokenSource()
        let focusService = ctx.ServiceProvider.GetService<FocusService>()
        let imageStore: ImageSource cval = cval null

        ctx.AddDispose cts


        let refreshImage () =
            let settings = focusService.Settings.GetValue()

            if focusService.IsActive.Value then
                imageStore.Publish null
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

                let stream = new MemoryStream()
                let image = new BitmapImage()

                bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Bmp)

                image.BeginInit()
                stream.Seek(0, SeekOrigin.Begin) |> ignore
                image.StreamSource <- stream
                image.EndInit()

                imageStore.Publish image


        async {
            while not cts.IsCancellationRequested do
                let delay =
                    let delay = focusService.Settings.GetFieldValue(fun x -> x.FrameDelay)
                    if delay < 10 || delay > 1_000 then 100 else delay

                do! Async.Sleep delay
                focusService.Dispatcher refreshImage
        }
        |> Async.Start


        Image'() {
            With(fun this -> Grid.SetRowSpan(this, 2))
            Source imageStore
        }
    )
