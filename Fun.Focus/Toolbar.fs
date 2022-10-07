[<AutoOpen>]
module Fun.Focus.Toolbar

open System
open System.Windows
open System.Windows.Controls
open Microsoft.Extensions.DependencyInjection
open Fun.SunUI
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
                    Background(Media.SolidColorBrush Media.Colors.Transparent)
                    Margin(Thickness(10, 0, 0, 0))
                    Padding(Thickness(0, 3, 0, 0))
                }
                Rectangle'() {
                    With(fun this -> Grid.SetColumn(this, 1))
                    Fill(Media.SolidColorBrush Media.Colors.Transparent)
                    MouseDown(fun _ -> focusService.StartDrag())
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
                            Text "Focus color:"
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
                            Click(fun _ -> focusService.CloseWindow())
                            Margin(Thickness(5, 0, 0, 0))
                            Padding(Thickness(10, 0, 10, 2))
                            BorderThickness(Thickness(0))
                            Background(Media.SolidColorBrush Media.Colors.Transparent)
                        }
                    ]
                }
            ]
        }
    )
