namespace TeaDriven.AvaloniaLocalization.FSharp

open System.IO
open System.Reflection
open System.Xml.Linq

open FSharp.Core.CompilerServices
open ProviderImplementation.ProvidedTypes

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

type Mode = Flat | SimpleSplit

[<TypeProvider>]
type LocalizationKeyProvider(config: TypeProviderConfig) as this =
    inherit TypeProviderForNamespaces(config)

    let nameSpace = this.GetType().Namespace
    let assembly = Assembly.GetExecutingAssembly()

    let createType typeName (xamlFileName: string) mode =
        let providedAssembly = ProvidedAssembly()
        let providedType = ProvidedTypeDefinition(providedAssembly, nameSpace, typeName, Some typeof<obj>, isErased=false)

        let path =
            if Path.IsPathRooted xamlFileName
            then xamlFileName
            else Path.Combine(config.ResolutionFolder, xamlFileName)

        let locKeys = File.ReadAllText path |> Internal.getLocKeys
        for key, _ in locKeys do
            let prop = ProvidedProperty(key.Replace(".", ""), typeof<string>, getterCode = (fun args -> <@@ key @@>), isStatic = true)
            providedType.AddMember(prop)

        providedAssembly.AddTypes [ providedType ]

        providedType

    let myParamType =
        let t = ProvidedTypeDefinition(assembly, nameSpace, "LocKeys", Some typeof<obj>, isErased=false)

        let parameters =
            [
                ProvidedStaticParameter("FileName", typeof<string>)
                ProvidedStaticParameter("Mode", typeof<Mode>)
            ]

        t.DefineStaticParameters(
            parameters,
            fun typeName args -> createType typeName (unbox<string> args[0]) (unbox<Mode> args[1]))

        t
    do
        this.AddNamespace(nameSpace, [myParamType])

[<assembly:TypeProviderAssembly>]
do ()
