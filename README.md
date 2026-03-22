# TeaDrivenDev.AvaloniaLocalization.FSharp

This is a simple F# type provider to help with localizing Avalonia applications that are safe with respect to changes in the resource file.

The type provider reads an `.axaml` file containing a resource dictionary and provides a type with string properties for each of the string resources contained in the file.

Such a file would look as follows:

```xaml
<ResourceDictionary xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:system="clr-namespace:System;assembly=System.Runtime">
    <system:String x:Key="Loc.WindowTitle">Application Main Window</system:String>
    <system:String x:Key="Loc.ShowingXFiles.Format">Showing {0} files</system:String>
    <system:String x:Key="Log.LoadingComplete">LoadingComplete</system:String>
</ResourceDictionary>
```

The provided type `Localizer` has three parameters:
- `FileName` is the Avalonia XAML file used to determine the available localization string resources. The path must be relative to the code file in which the provided type is instantiated.
- `ListStructure` determines the type structure in which the resources are presented.
  - `ListStructure.Flat` presents all of the resources in a single flat list, with all dots removed from the names.
    - In the above example, the localization type would have the properties `LocWindowTitle`, `LocShowingXFilesFormat`, and `LogLoadingComplete`.
  - `ListStructure.Grouped` groups the resources by the part before the first dot, with all subsequent dots replaced by underscores. This allows for a better logical organization of a non-trivial number of string resources.
    - In the above example, the localization type would have two properties:
      - `Loc` with sub-properties `WindowTitle` and `ShowingXFiles_Format`
      - `Log` with sub-property `LoadingComplete`
- `ReturnMode` determines whether the properties return the resource keys or the actual current resource values.
  - `ReturnMode.Keys` causes the resource keys to be returned.
  - `ReturnMode.Values` causes the resource values to be returned.

The recommended parameterization is using `ListStructure.Grouped` and `ReturnMode.Values`:

```fsharp
type Loc = Localizer< @"Localization\LocStrings.axaml", ListStructure.Grouped, ReturnMode.Values >
```
This is used as such:
```fsharp
let message = String.Format(Loc.Loc.ShowingXFiles_Format, numberOfFiles)
```

When using `ReturnMode.Keys`, the actual resource values can be retrieved using the following function:

```fsharp
let locString key =
    match Application.Current.TryGetResource(key, null) with
    | true, resource -> resource :?> string
    | false, _ -> failwith "Resource not found"
```
With `ReturnMode.Keys` and e.g. `ListStructure.Flat`, the above usage then becomes:
```fsharp
let message = String.Format(locString Loc.LocShowingXFilesFormat, numberOfFiles)
```

---

Building:

    dotnet tool restore
    dotnet paket update
    dotnet build -c release

    dotnet paket pack nuget --version 0.0.1
