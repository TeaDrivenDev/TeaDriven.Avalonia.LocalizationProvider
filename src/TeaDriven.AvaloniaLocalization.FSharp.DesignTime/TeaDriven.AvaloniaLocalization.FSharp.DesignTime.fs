module TeaDriven.AvaloniaLocalization.FSharpImplementation

open System
open System.Collections.Generic
open System.IO
open System.Reflection
open System.Xml.Linq

open FSharp.Quotations
open FSharp.Core.CompilerServices
open MyNamespace
open ProviderImplementation
open ProviderImplementation.ProvidedTypes

// Put any utility helpers here
[<AutoOpen>]
module internal Helpers =
    let x = 1

[<TypeProvider>]
type BasicErasingProvider (config : TypeProviderConfig) as this =
    inherit TypeProviderForNamespaces (config, assemblyReplacementMap=[("TeaDriven.AvaloniaLocalization.FSharp.DesignTime", "TeaDriven.AvaloniaLocalization.FSharp.Runtime")], addDefaultProbingLocation=true)

    let ns = "MyNamespace"
    let asm = Assembly.GetExecutingAssembly()

    // check we contain a copy of runtime files, and are not referencing the runtime DLL
    do assert (typeof<DataSource>.Assembly.GetName().Name = asm.GetName().Name)

    let createTypes () =
        let myType = ProvidedTypeDefinition(asm, ns, "MyType", Some typeof<obj>)

        let ctor = ProvidedConstructor([], invokeCode = fun args -> <@@ "My internal state" :> obj @@>)
        myType.AddMember(ctor)

        let ctor2 = ProvidedConstructor([ProvidedParameter("InnerState", typeof<string>)], invokeCode = fun args -> <@@ (%%(args.[0]):string) :> obj @@>)
        myType.AddMember(ctor2)

        let innerState = ProvidedProperty("InnerState", typeof<string>, getterCode = fun args -> <@@ (%%(args.[0]) :> obj) :?> string @@>)
        myType.AddMember(innerState)

        let meth = ProvidedMethod("StaticMethod", [], typeof<DataSource>, isStatic=true, invokeCode = (fun args -> Expr.Value(null, typeof<DataSource>)))
        myType.AddMember(meth)

        let nameOf =
            let param = ProvidedParameter("p", typeof<Expr<int>>)
            param.AddCustomAttribute {
                new CustomAttributeData() with
                    member __.Constructor = typeof<ReflectedDefinitionAttribute>.GetConstructor([||])
                    member __.ConstructorArguments = [||] :> _
                    member __.NamedArguments = [||] :> _
            }
            ProvidedMethod("NameOf", [ param ], typeof<string>, isStatic = true, invokeCode = fun args ->
                <@@
                    match (%%args.[0]) : Expr<int> with
                    | Microsoft.FSharp.Quotations.Patterns.ValueWithName (_, _, n) -> n
                    | e -> failwithf "Invalid quotation argument (expected ValueWithName): %A" e
                @@>)
        myType.AddMember(nameOf)

        [myType]

    do
        this.AddNamespace(ns, createTypes())

[<TypeProvider>]
type BasicGenerativeProvider (config : TypeProviderConfig) as this =
    inherit TypeProviderForNamespaces (config, assemblyReplacementMap=[("TeaDriven.AvaloniaLocalization.FSharp.DesignTime", "TeaDriven.AvaloniaLocalization.FSharp.Runtime")])

    let ns = "TeaDriven.AvaloniaLocalization.FSharp"
    let asm = Assembly.GetExecutingAssembly()

    // check we contain a copy of runtime files, and are not referencing the runtime DLL
    do assert (typeof<DataSource>.Assembly.GetName().Name = asm.GetName().Name)

    let createType typeName (count:int) =
        let asm = ProvidedAssembly()
        let myType = ProvidedTypeDefinition(asm, ns, typeName, Some typeof<obj>, isErased=false)

        let ctor = ProvidedConstructor([], invokeCode = fun args -> <@@ "My internal state" :> obj @@>)
        myType.AddMember(ctor)

        let ctor2 = ProvidedConstructor([ProvidedParameter("InnerState", typeof<string>)], invokeCode = fun args -> <@@ (%%(args.[1]):string) :> obj @@>)
        myType.AddMember(ctor2)

        for i in 1 .. count do
            let prop = ProvidedProperty("Property" + string i, typeof<int>, getterCode = fun args -> <@@ i @@>)
            myType.AddMember(prop)

        let meth = ProvidedMethod("StaticMethod", [], typeof<DataSource>, isStatic=true, invokeCode = (fun args -> Expr.Value(null, typeof<DataSource>)))
        myType.AddMember(meth)
        asm.AddTypes [ myType ]

        myType

    let myParamType =
        let t = ProvidedTypeDefinition(asm, ns, "GenerativeProvider", Some typeof<obj>, isErased=false)
        t.DefineStaticParameters( [ProvidedStaticParameter("Count", typeof<int>)], fun typeName args -> createType typeName (unbox<int> args.[0]))
        t
    do
        this.AddNamespace(ns, [myParamType])

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
    inherit TypeProviderForNamespaces (config, assemblyReplacementMap=[("TeaDriven.AvaloniaLocalization.FSharp.DesignTime", "TeaDriven.AvaloniaLocalization.FSharp.Runtime")])

    let ns = "TeaDriven.AvaloniaLocalization.FSharp"
    let asm = Assembly.GetExecutingAssembly()

    // check we contain a copy of runtime files, and are not referencing the runtime DLL
    do assert (typeof<DataSource>.Assembly.GetName().Name = asm.GetName().Name)

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
