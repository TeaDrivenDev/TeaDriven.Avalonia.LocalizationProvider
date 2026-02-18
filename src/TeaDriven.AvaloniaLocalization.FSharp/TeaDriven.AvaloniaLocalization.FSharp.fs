module TeaDriven.AvaloniaLocalization.FSharpImplementation

open System.IO
open System.Reflection
open System.Xml.Linq

open FSharp.Core.CompilerServices
open ProviderImplementation.ProvidedTypes

// Put any utility helpers here
[<AutoOpen>]
module internal Helpers =
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
        |> Seq.map (fun (element: XElement) -> element.Attribute(qualifiedKeyAttributeName).Value)
        |> Seq.toList

[<TypeProvider>]
type LocalizationKeyProvider (config : TypeProviderConfig) as this =
    inherit TypeProviderForNamespaces (config)

    let ns = "TeaDriven.AvaloniaLocalization.FSharp"
    let asm = Assembly.GetExecutingAssembly()

    let createType typeName (xamlFileName: string) =
        let asm = ProvidedAssembly()
        let myType = ProvidedTypeDefinition(asm, ns, typeName, Some typeof<obj>, isErased=false)

        let locKeys = File.ReadAllText xamlFileName |> getLocKeys
        for key in locKeys do
            let prop = ProvidedProperty(key.Replace(".", ""), typeof<string>, getterCode = (fun args -> <@@ key @@>), isStatic = true)
            myType.AddMember(prop)

        asm.AddTypes [ myType ]

        myType

    let myParamType =
        let t = ProvidedTypeDefinition(asm, ns, "LocKeys", Some typeof<obj>, isErased=false)
        t.DefineStaticParameters( [ProvidedStaticParameter("FileName", typeof<string>)], fun typeName args -> createType typeName (unbox<string> args.[0]))
        t
    do
        this.AddNamespace(ns, [myParamType])

[<assembly:TypeProviderAssembly>]
do ()
