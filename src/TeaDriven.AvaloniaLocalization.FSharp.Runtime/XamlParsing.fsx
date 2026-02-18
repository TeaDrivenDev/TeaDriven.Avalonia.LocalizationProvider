open System.Xml.Linq


let input =
    """
<ResourceDictionary xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:system="clr-namespace:System;assembly=System.Runtime">
    <system:String x:Key="Loc.WindowTitle">Maysternya</system:String>
    <system:String x:Key="Loc.SteamDirectory">Steam Directory</system:String>
    <system:String x:Key="Loc.SteamDirectoryInfoMessage">This is the directory of the Steam library your SCS truck simulators are installed in. By default this is 'C:\Program Files (x86)\Steam', but if your games are installed in another location, this must be the directory that contains 'steamapps'.</system:String>
    <system:String x:Key="Loc.HashFsExtractor">HashFS Extractor</system:String>
    <system:String x:Key="Loc.Download">Download</system:String>
    <system:String x:Key="Loc.ExtractorInfoMessage">The HashFS extractor is used to get metadata (name, author, description, version) for mods packaged as HashFS, as well as the installed game versions. Using it is optional, but recommended; only the display of some mods and the game versions will be affected if it is missing.

If using the extractor, version 2026-02-15 or newer is required; using an older version will cause the application to not work at all.</system:String>
    <system:String x:Key="Loc.SelectExtractorExecutable">Select Extractor executable</system:String>
    <system:String x:Key="Loc.Extractor">Extractor</system:String>
    <system:String x:Key="Loc.WorkshopButNoModDirectories">Workshop directory found, but no ETS2 or ATS mod directories</system:String>
    <system:String x:Key="Loc.WorkshopDirectoryNotFound">Workshop directory not found</system:String>
    <system:String x:Key="Loc.Ets2">ETS2</system:String>
    <system:String x:Key="Loc.Ets2Version.Format">ETS2 (version {0})</system:String>
    <system:String x:Key="Loc.Ets2ModsDirectoryNotFound">ETS2 mod directory not found</system:String>
    <system:String x:Key="Loc.Ats">ATS</system:String>
    <system:String x:Key="Loc.AtsVersion.Format">ATS (version {0})</system:String>
    <system:String x:Key="Loc.AtsModsDirectoryNotFound">ATS mod directory not found</system:String>
    <system:String x:Key="Loc.RefreshList">Refresh list</system:String>
    <system:String x:Key="Loc.Id">ID</system:String>
    <system:String x:Key="Loc.Name">Name</system:String>
    <system:String x:Key="Loc.Author">Author</system:String>
    <system:String x:Key="Loc.Version">Version</system:String>
    <system:String x:Key="Loc.UsableWith">Usable with</system:String>
    <system:String x:Key="Loc.RemoveRestrictionButtonToolTip">Temporarily remove version restriction</system:String>
    <system:String x:Key="Loc.NoDisplayName">[No display name]</system:String>
    <system:String x:Key="Loc.MetadataIn.Format">[Metadata in {0}]</system:String>
    <system:String x:Key="Loc.PackageNotFound.Format">[Package {0} not found]</system:String>
</ResourceDictionary>
"""

let systemNamespaceString = "clr-namespace:System;assembly=System.Runtime"
let stringElementName = "String"

let xamlNamespaceString = "http://schemas.microsoft.com/winfx/2006/xaml"
let keyAttributeName = "Key"

let document = XDocument.Parse(input)
let systemNamespacePrefix = document.Root.GetPrefixOfNamespace(systemNamespaceString)

#nowarn "3391"
let systemNamespace = XNamespace.Get systemNamespaceString
let qualifiedElementName = systemNamespace + stringElementName

let xamlNamespace = XNamespace.Get xamlNamespaceString
let qualifiedAttributeName = xamlNamespace + keyAttributeName

document.Root.Descendants(qualifiedElementName)
|> Seq.map (fun (element: XElement) -> element.Attribute(qualifiedAttributeName).Value)
|> Seq.toList
