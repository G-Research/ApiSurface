namespace ApiSurface

open System.IO
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

    /// <summary>Updates an assembly's SurfaceBaseline.txt file on the disk.</summary>
    /// <remarks>
    /// Expects to find '../&lt;AssemblyName&gt;/SurfaceBaseline.txt' somewhere in
    /// the directory ancestry tree.
    /// </remarks>
    [<CompiledName "WriteAssemblyBaseline">]
    val writeAssemblyBaseline : Assembly -> unit

    /// <summary>Updates an assembly's SurfaceBaseline.txt file on the disk.</summary>
    /// <remarks>
    /// Expects to find <c>../&lt;supplied directory&gt;/SurfaceBaseline.txt</c> somewhere in the directory tree.
    /// </remarks>
    [<CompiledName "WriteAssemblyBaselineWithDirectory">]
    val writeAssemblyBaselineWithDirectory : string -> Assembly -> unit

    val internal findNewVersion : Version -> baseline : ApiSurface -> target : ApiSurface -> Version

    val internal updateVersionJson<'fileInfo> :
        baseline : ApiSurface -> Assembly -> versionFile: 'fileInfo -> openVersionFile : ('fileInfo -> FileMode * FileAccess -> Stream) -> unit
