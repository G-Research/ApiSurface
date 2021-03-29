namespace ApiSurface

/// Represents a comparison between two API surfaces or two documentation
/// coverages.
type SurfaceComparison

[<RequireQualifiedAccess>]
module SurfaceComparison =

    val internal identical : SurfaceComparison

    val internal different :
        addedHeader : string ->
        removedHeader : string ->
        added : string list ->
        removed : string list ->
            SurfaceComparison

    /// Throws if a SurfaceComparison represents a non-identical result.
    [<CompiledName "AssertIdentical">]
    val assertIdentical : SurfaceComparison -> unit

    /// Throws if a SurfaceComparison represents a result in which something was removed from the baseline.
    /// If the supplied boolean is true and nothing was removed from the baseline, additionally prints out a summary of
    /// the differences.
    [<CompiledName "AssertNoneRemoved">]
    val assertNoneRemoved : shouldPrint : bool -> SurfaceComparison -> unit

    /// Returns the list of symbols that are only present in the baseline but not the current surface.
    /// (For example, if used to compare the entire assembly surface with the documented assembly surface, this is
    /// effectively the list of undocumented symbols.)
    [<CompiledName "RemovedSymbols">]
    val removedSymbols : SurfaceComparison -> string list

    /// Returns the list of symbols that are only present in the current surface but not the baseline.
    [<CompiledName "AddedSymbols">]
    val addedSymbols : SurfaceComparison -> string list
