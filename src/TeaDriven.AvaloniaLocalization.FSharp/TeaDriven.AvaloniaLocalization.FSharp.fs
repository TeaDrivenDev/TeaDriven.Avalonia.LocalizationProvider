namespace TeaDriven.AvaloniaLocalization.FSharp

open System.IO
open System.Reflection
open System.Xml.Linq

open FSharp.Core.CompilerServices
open ProviderImplementation.ProvidedTypes

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

    let createFlatMembers _ _ (providedType: ProvidedTypeDefinition) (locKeys: (string * string) list) =
        for key, locString in locKeys do
            let prop = ProvidedProperty(key.Replace(".", ""), typeof<string>, getterCode = (fun args -> <@@ key @@>), isStatic = true)
            prop.AddXmlDoc(locString)
            providedType.AddMember(prop)

    let createSimpleSplitMembers
        (providedAssembly: ProvidedAssembly)
        (nameSpace: string)
        (providedType: ProvidedTypeDefinition)
        (locKeys: (string * string) list) =
        let data = getSimpleSplit locKeys

        for group in data do
            let subType = ProvidedTypeDefinition(providedAssembly, nameSpace, group.Prefix, Some typeof<obj>, isErased=false)

            for item in group.Items do
                let completeKey = item.CompleteKey
                let prop = ProvidedProperty(item.PartialKey.Replace('.', '_'), typeof<string>, getterCode = (fun args -> <@@ completeKey @@>))
                prop.AddXmlDoc(item.LocString)

                subType.AddMember(prop)

            providedType.AddMember subType
            let subTypeProperty = ProvidedProperty(group.Prefix, subType, getterCode = (fun args -> <@@ subType @@>), isStatic = true)
            providedType.AddMember(subTypeProperty)

[<TypeProvider>]
type LocalizationKeyProvider(config: TypeProviderConfig) as this =
    inherit TypeProviderForNamespaces(config)

    let nameSpace = this.GetType().Namespace
    let assembly = Assembly.GetExecutingAssembly()

    let createType typeName (xamlFileName: string) createMembers =
        let providedAssembly = ProvidedAssembly()
        let providedType = ProvidedTypeDefinition(providedAssembly, nameSpace, typeName, Some typeof<obj>, isErased=false)

        let path =
            if Path.IsPathRooted xamlFileName
            then xamlFileName
            else Path.Combine(config.ResolutionFolder, xamlFileName)

        let locKeys = File.ReadAllText path |> Internal.getLocKeys

        createMembers providedAssembly nameSpace providedType locKeys

        providedAssembly.AddTypes [ providedType ]

        providedType

    let locKeysFlatType =
        let typeDefinition = ProvidedTypeDefinition(assembly, nameSpace, "LocKeysFlat", Some typeof<obj>, isErased=false)

        typeDefinition.DefineStaticParameters(
            [ ProvidedStaticParameter("FileName", typeof<string>) ],
            fun typeName args -> createType typeName (unbox<string> args[0]) Internal.createFlatMembers)

        typeDefinition

    let locKeysSplitType =
        let typeDefinition = ProvidedTypeDefinition(assembly, nameSpace, "LocKeysSimpleSplit", Some typeof<obj>, isErased=false)

        typeDefinition.DefineStaticParameters(
            [ ProvidedStaticParameter("FileName", typeof<string>) ],
            fun typeName args -> createType typeName (unbox<string> args[0]) Internal.createSimpleSplitMembers)

        typeDefinition

    do
        this.AddNamespace(nameSpace, [locKeysFlatType; locKeysSplitType])

[<assembly:TypeProviderAssembly>]
do ()
