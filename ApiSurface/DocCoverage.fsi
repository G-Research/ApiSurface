namespace ApiSurface

open System.Reflection

/// Represents either an assembly's public API surface or a parsed .NET XML
/// documentation file.
type DocCoverage

[<RequireQualifiedAccess>]
module DocCoverage =

    /// Map the exposed types and members of an assembly into a list of
    /// member names expected to be present in the corresponding .XML
    /// documentation file.
    [<CompiledName "FromAssemblySurface">]
    val ofAssemblySurface : Assembly -> DocCoverage

    /// Read the .XML documentation file that lives beside a given assembly.
    /// Will throw if the file is not present.
    [<CompiledName "FromAssemblyXml">]
    val ofAssemblyXml : Assembly -> DocCoverage

    /// Read a .XML documentation file.
    [<CompiledName "FromXml">]
    val ofXml : source : string -> xml : string -> DocCoverage

    /// Format a DocCoverage as a new-line delimited string.
    /// Each line represents a XML documentation 'member name', e.g.
    /// "T:ApiSurface.DocCoverageModule"
    [<CompiledName "ToString">]
    val toString : DocCoverage -> string

    /// Compare two DocCoverages for differences. The first argument is considered the "baseline".
    [<CompiledName "Compare">]
    val compare : DocCoverage -> DocCoverage -> SurfaceComparison

    /// Asserts that an assembly's public API surface, and its corresponding
    /// .XML documentation file, both cover exactly the same area.
    [<CompiledName "AssertFullyDocumented">]
    val assertFullyDocumented : Assembly -> unit
