open System
open System.Windows
open System.Windows.Shell
open System.Windows.Controls
open Microsoft.Extensions.DependencyInjection
open Fun.SunUI
open Fun.SunUI.WPF
open Fun.SunUI.WPF.Controls
open Fun.Focus


let normalizeWindow (settings: AdaptiveForm<SettingsData, _>) (windowRef: Window) dpiX dpiY =
    windowRef.Left <- float (int (windowRef.Left * dpiX)) / dpiX
    windowRef.Top <- float (int (windowRef.Top * dpiY)) / dpiY
    windowRef.Width <- float (int (windowRef.Width * dpiX)) / dpiX
    windowRef.Height <- float (int (windowRef.Height * dpiY)) / dpiY

    settings.UseFieldSetter (fun x -> x.Width) windowRef.Width
    settings.UseFieldSetter (fun x -> x.Height) windowRef.Height
    settings.UseFieldSetter (fun x -> x.Top) windowRef.Top
    settings.UseFieldSetter (fun x -> x.Left) windowRef.Left


let window =
    UI.inject (fun ctx ->
        let focusService = ctx.ServiceProvider.GetService<FocusService>()
        let settings = focusService.Settings

        Window'() {
            With(fun this ->
                focusService.Dispatcher <- this.Dispatcher.Invoke
                focusService.StartDrag <- this.DragMove
                focusService.CloseWindow <- this.Close
                focusService.SetAsTopMost <- fun x -> this.Dispatcher.Invoke(fun _ -> this.Topmost <- x)

                let windowChrome = WindowChrome()
                windowChrome.ResizeBorderThickness <- new Thickness(4)
                windowChrome.CaptionHeight <- 0
                WindowChrome.SetWindowChrome(this, windowChrome)
            )
            Title(settings.UseFieldValue(fun x -> x.Title))
            Width(settings.UseFieldValue(fun x -> x.Width))
            Height(settings.UseFieldValue(fun x -> x.Height))
            Left(settings.UseFieldValue(fun x -> x.Left))
            Top(settings.UseFieldValue(fun x -> x.Top))
            WindowStyle WindowStyle.None
            WindowState WindowState.Normal
            AllowsTransparency true
            Background Media.Brushes.Transparent
            Activated(fun _ -> focusService.IsActive.Publish true)
            Deactivated(fun _ -> focusService.IsActive.Publish false)
            LocationChanged(fun (this, _) -> normalizeWindow settings this focusService.DpiX focusService.DpiY)
            SizeChanged(fun (x, _) -> normalizeWindow settings x focusService.DpiX focusService.DpiY)
            Closed(fun _ ->
                Settings.Default.Value <- settings.GetValue()
                Settings.Default.Save()
            )
            Grid'() {
                RowDefinitions(fun row ->
                    row.Add(RowDefinition(Height = GridLength(22.)))
                    row.Add(RowDefinition(Height = GridLength(1, GridUnitType.Star)))
                )
                StaticChildren [
                    screenView
                    Border'() {
                        GridRow 1
                        BorderThickness(Thickness(6, 0, 6, 6))
                        BorderBrush focusService.FocusBrush

                        Border'() {
                            BorderThickness(Thickness 1)
                            BorderBrush Media.Brushes.LightGray
                        }
                    }
                    toolbar
                    Border'() {
                        GridRowSpan 2
                        BorderBrush Media.Brushes.LightGray
                        BorderThickness(Thickness 1)
                    }
                ]
            }
        }
    )


[<EntryPoint; STAThread>]
let main (_: string[]) =

    let services = ServiceCollection()
    services.AddSingleton<FocusService>() |> ignore

    let sp = services.BuildServiceProvider()
    let nativeWindow = window.Build<Window>(sp)

    Application() |> ignore
    Application.Current.Run nativeWindow
