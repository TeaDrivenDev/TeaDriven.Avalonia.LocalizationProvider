namespace TeaDriven.AvaloniaLocalization.FSharp.Runtime

open FSharp.Core.CompilerServices

// Put the TypeProviderAssemblyAttribute in the runtime DLL, pointing to the design-time DLL
[<assembly: TypeProviderAssembly("TeaDriven.AvaloniaLocalization.FSharp.DesignTime")>]
do ()
