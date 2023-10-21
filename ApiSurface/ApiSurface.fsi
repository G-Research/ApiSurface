namespace ApiSurface

open System.Reflection

/// Represents the public API surface of an assembly.
type ApiSurface = internal | ApiSurface of string list

/// The NuGet version of a library.
type internal Version =
    {
        Major : int
        Minor : int
    }

[<RequireQualifiedAccess>]
module ApiSurface =

    /// Read the SurfaceBaseline.txt embedded resource from the given assembly.
    [<CompiledName "FromAssemblyBaseline">]
    val ofAssemblyBaseline : Assembly -> ApiSurface

    /// Read the surface baseline from embedded resource from the given assembly, using the given resource filename.
    [<CompiledName "FromAssemblyBaselineWithExplicitResourceName">]
    val ofAssemblyBaselineWithExplicitResourceName : string -> Assembly -> ApiSurface

    /// Compare two API surfaces for differences.
    [<CompiledName "Compare">]
    val compare : ApiSurface -> ApiSurface -> SurfaceComparison

    /// Construct an ApiSurface from an Assembly, using a custom printer.
    [<CompiledName "FromAssemblyCustomPrint">]
    val ofAssemblyCustomPrint : (PublicType -> string list) -> Assembly -> ApiSurface

    /// Crawl a given assembly's public API surface.
    [<CompiledName "FromAssembly">]
    val ofAssembly : Assembly -> ApiSurface

    /// Format an API surface in a new-line delimited string.
    /// Each line represents a type member.
    /// This format is the same format expected of the SurfaceBaseline.txt file.
    [<CompiledName "ToString">]
    val toString : ApiSurface -> string

    /// Assert that an assembly's API surface and its embedded SurfaceBaseline.txt file
    /// are identical.
    [<CompiledName "AssertIdentical">]
    val assertIdentical : Assembly -> unit

    /// Updates an assembly's SurfaceBaseline.txt file on the disk.
    /// Expects to find '../<AssemblyName>/SurfaceBaseline.txt' somewhere in
    /// the directory ancestry tree.
    [<CompiledName "WriteAssemblyBaseline">]
    val writeAssemblyBaseline : Assembly -> unit

    /// Updates an assembly's SurfaceBaseline.txt file on the disk.
    /// Expects to find '../<supplied directory>/SurfaceBaseline.txt' somewhere in the directory tree.
    [<CompiledName "WriteAssemblyBaselineWithDirectory">]
    val writeAssemblyBaselineWithDirectory : string -> Assembly -> unit

    val internal findNewVersion : Version -> baseline : ApiSurface -> target : ApiSurface -> Version

    val internal updateVersionJson :
        baseline : ApiSurface -> Assembly -> versionFile : System.IO.Abstractions.IFileInfo -> unit
