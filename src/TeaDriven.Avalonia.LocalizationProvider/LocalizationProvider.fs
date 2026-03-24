namespace TeaDriven.Avalonia.LocalizationProvider

open System.IO
open System.Reflection
open System.Xml.Linq

open Avalonia

open FSharp.Core.CompilerServices
open ProviderImplementation.ProvidedTypes

type ListStructure = | Flat = 0 | Grouped = 1
type ReturnMode = | Keys = 0 | Values = 1

[<AutoOpen>]
module Prelude =
    let locString key =
        match Application.Current.TryGetResource(key, null) with
        | true, resource -> resource :?> string
        | false, _ -> $"[ERROR: Resource {key} not found]"

[<RequireQualifiedAccess>]
module internal Internal =
    let getLocKeys xaml =
        let systemNamespaceString = "clr-namespace:System;assembly=System.Runtime"
        let stringElementName = "String"

        let xamlNamespaceString = "http://schemas.microsoft.com/winfx/2006/xaml"
        let keyAttributeName = "Key"

        let document = XDocument.Parse(xaml)

        let systemNamespace = XNamespace.Get systemNamespaceString
        let qualifiedStringElementName = systemNamespace + stringElementName

        let xamlNamespace = XNamespace.Get xamlNamespaceString
        let qualifiedKeyAttributeName = xamlNamespace + keyAttributeName

        document.Root.Descendants(qualifiedStringElementName)
        |> Seq.map (fun (element: XElement) -> element.Attribute(qualifiedKeyAttributeName).Value, element.Value)
        |> Seq.toList

    let getSimpleSplit locKeys =
        locKeys
        |> List.map
            (fun (locKey: string, locString: string) ->
                locKey.Split([| '.' |], 2), (locKey, locString))
        |> List.groupBy (fst >> Array.head)
        |> List.map
            (fun (prefix, items) ->
                {|
                    Prefix = prefix
                    Items =
                        items
                        |> List.map
                            (fun (keyParts, (completeKey, locString)) ->
                                {|
                                    PartialKey = keyParts[1]
                                    CompleteKey = completeKey
                                    LocString = locString
                                |})
                |})

    let determineGetter returnMode key =
        if returnMode = ReturnMode.Values
        then
            (fun args ->
                <@@
                    let applicationInstance = Application.Current

                    let methodInfo = typeof<Application>.GetMethod("TryGetResource")

                    // Parameters of Avalonia.Application.TryGetResource()
                    // key: string, theme: Styling.ThemeVariant, value: byref<obj>
                    let parameters: obj array = [| key; null; null |]

                    let success = methodInfo.Invoke(applicationInstance, parameters) :?> bool

                    if success
                    then
                        // The out value is now at index 2 of the array
                        parameters[2] :?> string
                    else $"[ERROR: Resource {key} not found]"
                @@>)
        else (fun args -> <@@ key @@>)

    let createFlatMembers _ _ (providedType: ProvidedTypeDefinition) (returnMode: ReturnMode) (locKeys: (string * string) list) =
        for key, locString in locKeys do
            let prop = ProvidedProperty(key.Replace(".", ""), typeof<string>, getterCode = determineGetter returnMode key, isStatic = true)
            prop.AddXmlDoc(locString)
            providedType.AddMember(prop)

    let createGroupedMembers
        (providedAssembly: ProvidedAssembly)
        (nameSpace: string)
        (providedType: ProvidedTypeDefinition)
        (returnMode: ReturnMode)
        (locKeys: (string * string) list) =
        let data = getSimpleSplit locKeys

        for group in data do
            let subType = ProvidedTypeDefinition(providedAssembly, nameSpace, group.Prefix, Some typeof<obj>, isErased=false)

            for item in group.Items do
                let prop =
                    ProvidedProperty(
                        item.PartialKey.Replace('.', '_'),
                        typeof<string>,
                        getterCode = determineGetter returnMode item.CompleteKey)
                prop.AddXmlDoc(item.LocString)

                subType.AddMember(prop)

            providedType.AddMember subType
            let subTypeProperty = ProvidedProperty(group.Prefix, subType, getterCode = (fun args -> <@@ subType @@>), isStatic = true)
            providedType.AddMember(subTypeProperty)

[<TypeProvider>]
type LocalizationProvider(config: TypeProviderConfig) as this =
    inherit TypeProviderForNamespaces(config)

    let nameSpace = this.GetType().Namespace
    let assembly = Assembly.GetExecutingAssembly()

    let createType typeName xamlFileName returnMode createMembers =
        let providedAssembly = ProvidedAssembly()
        let providedType = ProvidedTypeDefinition(providedAssembly, nameSpace, typeName, Some typeof<obj>, isErased=false)

        let path =
            if Path.IsPathRooted xamlFileName
            then xamlFileName
            else Path.Combine(config.ResolutionFolder, xamlFileName)

        let locKeys = File.ReadAllText path |> Internal.getLocKeys

        createMembers providedAssembly nameSpace providedType returnMode locKeys

        providedAssembly.AddTypes [ providedType ]

        providedType

    let localizationType =
        let typeDefinition = ProvidedTypeDefinition(assembly, nameSpace, "Localizer", Some typeof<obj>, isErased=false)

        typeDefinition.DefineStaticParameters(
            [
                ProvidedStaticParameter("FileName", typeof<string>)
                ProvidedStaticParameter("ListStructure", typeof<ListStructure>)
                ProvidedStaticParameter("ReturnMode", typeof<ReturnMode>)
            ],
            fun typeName args ->
                let fileName = unbox<string> args[0]
                let listStructure = unbox<ListStructure> args[1]
                let returnMode = unbox<ReturnMode> args[2]

                let createMembers =
                    if listStructure = ListStructure.Flat
                    then Internal.createFlatMembers
                    else Internal.createGroupedMembers

                createType typeName fileName returnMode createMembers)

        typeDefinition

    do
        this.AddNamespace(nameSpace, [ localizationType ])

[<assembly:TypeProviderAssembly>]
do ()
