This is a simple F# type provider intended to provide resource keys for localizing Avalonia applications that are safe with respect to changes in the resource file.

The type provider reads an `.axaml` file containing a resource dictionary and provides a type with string properties for each of the string resources contained in the file.

Such file would look as follows:

```xaml
<ResourceDictionary xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:system="clr-namespace:System;assembly=System.Runtime">
    <system:String x:Key="Loc.WindowTitle">Application Main Window</system:String>
    <system:String x:Key="Loc.FileLoadedMessage">File {0} has been loaded</system:String>
</ResourceDictionary>
```

The provided type is instantiated with

```fsharp
type Loc = LocKeys<"LocStrings.axaml">
```

The localization keys can then be accessed through static properties on the `Loc` type.

Given a helper function

```fsharp
let locString key =
    match Application.Current.TryGetResource(key, null) with
    | true, resource -> resource :?> string
    | false, _ -> failwith "Resource not found"
```

this is used as such:

```fsharp
let message = String.Format(locString Loc.FileLoadedMessage, fileName)
```

---

Building:

    dotnet tool restore
    dotnet paket update
    dotnet build -c release

    dotnet paket pack nuget --version 0.0.1
