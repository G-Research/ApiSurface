namespace ApiSurface

open System.Reflection
open NuGet.Versioning

/// Module containing functions to validate the version in the version.json
[<RequireQualifiedAccess>]
module MonotonicVersion =

    val internal validateVersion :
        currentVersion : NuGetVersion -> latestVersion : NuGetVersion -> packageId : string -> unit

    val internal versionIncreaseIsInAcceptableRange :
        currentVersion : NuGetVersion -> latestVersion : NuGetVersion -> packageId : string -> unit

    /// Ensure the version in version.json is increasing monotonically
    [<CompiledName "Validate">]
    val validate : Assembly -> packageId : string -> unit

    /// Ensure the version in the specified version.json resource is increasing monotonically.
    /// This should be the name of a resource as passed to "Assembly.GetAssemblyResourceStream".
    [<CompiledName "ValidateResource">]
    val validateResource : resourceName : string -> Assembly -> packageId : string -> unit
