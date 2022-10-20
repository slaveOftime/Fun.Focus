[<AutoOpen>]
module Fun.Focus.Toolbar

open System
open System.Windows
open System.Windows.Controls
open FSharp.Data.Adaptive
open Microsoft.Extensions.DependencyInjection
open Fun.SunUI
open Fun.SunUI.WPF
open Fun.SunUI.WPF.Controls
open Fun.SunUI.WPF.Shapes
open Fun.Focus


let toolbar =
    UI.inject (fun ctx ->
        let focusService = ctx.ServiceProvider.GetService<FocusService>()
        let settings = focusService.Settings

        Grid'() {
            ColumnDefinitions(fun col ->
                col.Add(ColumnDefinition(Width = GridLength.Auto))
                col.Add(ColumnDefinition(Width = GridLength(1, GridUnitType.Star)))
                col.Add(ColumnDefinition(Width = GridLength.Auto))
            )
            Background focusService.FocusBrush
            StaticChildren [
                TextBox'() {
                    Text(settings.GetFieldValue(fun x -> x.Title))
                    TextChanged(fun (this, _) -> settings.UseFieldSetter (fun x -> x.Title) (this.Text))
                    BorderThickness(Thickness(0))
                    Background Media.Brushes.Transparent
                    Margin(Thickness(10, 0, 0, 0))
                    Padding(Thickness(0, 3, 0, 0))
                }
                Rectangle'() {
                    GridCol 1
                    Fill Media.Brushes.Transparent
                    MouseDown(fun _ -> focusService.StartDrag())
                }
                StackPanel'() {
                    GridCol 2
                    Orientation Orientation.Horizontal

                    Children(
                        alist {
                            let! isActive = focusService.IsActive

                            let! isTransparentMode, setIsTransparentMode = settings.UseField(fun x -> x.TransparentMode)

                            if isActive then
                                RadioButton'() {
                                    VerticalAlignment VerticalAlignment.Center
                                    IsChecked isTransparentMode
                                    Click(fun _ -> setIsTransparentMode (not isTransparentMode))
                                    Content' "Use Transparent Mode"
                                    Margin(Thickness(0, 2, 15, 0))
                                    ToolTip "You can try to toggle this if it is blank when sharing."
                                }
                                if not isTransparentMode then
                                    TextBlock'() {
                                        VerticalAlignment VerticalAlignment.Center
                                        Text "Frame delay (ms, 10~1000):"
                                    }
                                    TextBox'() {
                                        VerticalAlignment VerticalAlignment.Center
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
                                    VerticalAlignment VerticalAlignment.Center
                                    Text "Focus color:"
                                    Margin(Thickness(15, 0, 0, 0))
                                }
                                TextBox'() {
                                    VerticalAlignment VerticalAlignment.Center
                                    Text(settings.GetFieldValue(fun x -> x.FocusColor))
                                    TextChanged(fun (this, _) -> settings.UseFieldSetter (fun x -> x.FocusColor) (this.Text))
                                    BorderThickness(Thickness(0))
                                    Background(Media.SolidColorBrush Media.Colors.Transparent)
                                }
                            Button'() {
                                VerticalAlignment VerticalAlignment.Center
                                Content' "Close"
                                Click(fun _ -> focusService.CloseWindow())
                                Margin(Thickness(5, 0, 0, 0))
                                Padding(Thickness(10, 0, 10, 0))
                                BorderThickness(Thickness(0))
                                Background(Media.SolidColorBrush Media.Colors.Transparent)
                            }
                        }
                    )
                }
            ]
        }
    )
