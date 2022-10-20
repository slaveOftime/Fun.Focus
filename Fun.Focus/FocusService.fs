namespace Fun.Focus

open System
open System.Windows
open System.Windows.Interop
open FSharp.Data.Adaptive
open Fun.SunUI
open Fun.Focus


type FocusService() =

    let dpiX, dpiY =
        use source = new HwndSource(new HwndSourceParameters())
        source.CompositionTarget.TransformToDevice.M11, source.CompositionTarget.TransformToDevice.M22

    let settings = new AdaptiveForm<_, string>(Settings.Default.Value)

    let isActive = cval true


    member val StartDrag = fun () -> () with get, set
    member val CloseWindow = fun () -> () with get, set
    member val SetAsTopMost = fun (_: bool) -> () with get, set
    member val Dispatcher = fun (fn: unit -> unit) -> fn () with get, set

    member _.DpiX = dpiX
    member _.DpiY = dpiY

    member _.Settings = settings
    member _.IsActive = isActive

    member _.FocusBrush =
        adaptive {
            match! isActive with
            | true -> return Media.SolidColorBrush Media.Colors.White :> Media.Brush
            | false ->
                return
                    Media
                        .BrushConverter()
                        .ConvertFrom(settings.GetFieldValue(fun x -> x.FocusColor))
                    :?> Media.Brush
        }
