﻿open System
open System.Windows
open System.Windows.Shell
open System.Windows.Controls
open Microsoft.Extensions.DependencyInjection
open Fun.SunUI
open Fun.SunUI.WPF
open Fun.SunUI.WPF.Controls
open Fun.Focus


let normalizeWindow (settings: AdaptiveForm<SettingsData, _>) (windowRef: Window) dpiX dpiY =
    windowRef.Left <- windowRef.Left * dpiX / dpiX
    windowRef.Top <- windowRef.Top * dpiY / dpiY
    windowRef.Width <- windowRef.Width * dpiX / dpiX
    windowRef.Height <- windowRef.Height * dpiY / dpiY

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
            Background(Media.SolidColorBrush Media.Colors.Transparent)
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
                        With(fun this -> Grid.SetRow(this, 1))
                        BorderThickness(Thickness(2))
                        BorderBrush focusService.FocusBrush
                    }
                    toolbar
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