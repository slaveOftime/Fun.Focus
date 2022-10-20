namespace Fun.Focus

open System.Configuration
open Newtonsoft.Json


[<CLIMutable>]
type SettingsData =
    {
        Title: string
        Top: float
        Left: float
        Width: float
        Height: float
        FocusColor: string
        FrameDelay: int
        /// In lastest Windows11, when share a window which is not active or visible, it will become blank.
        TransparentMode: bool
    }

    static member DefaultValue = {
        Title = "Fun.Focus"
        Top = 100
        Left = 100
        Width = 1260
        Height = 720
        FocusColor = "#abff26"
        FrameDelay = 100
        TransparentMode = false
    }


type Settings() as this =
    inherit ApplicationSettingsBase()

    static member val Default = ApplicationSettingsBase.Synchronized(new Settings()) :?> Settings with get, set

    [<UserScopedSettingAttribute>]
    [<DefaultSettingValueAttribute("{}")>]
    member _.RawValue
        with get () = this[nameof this.RawValue] :?> string
        and set (x: string) = this[nameof this.RawValue] <- x

    member _.Value
        with get () =
            try
                if this.RawValue = "{}" then
                    SettingsData.DefaultValue
                else
                    JsonConvert.DeserializeObject<SettingsData>(this.RawValue)
            with _ ->
                SettingsData.DefaultValue

        and set (x: SettingsData) = this.RawValue <- JsonConvert.SerializeObject(x)
