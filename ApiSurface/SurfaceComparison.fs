namespace ApiSurface

type SurfaceComparison =
    | Identical
    | Different of addedHeader : string * removedHeader : string * added : string list * removed : string list


[<RequireQualifiedAccess>]
module SurfaceComparison =

    let identical = Identical

    let different addedHeader removedHeader added removed =
        Different (addedHeader, removedHeader, added, removed)

    [<CompiledName "AssertIdentical">]
    let assertIdentical =
        function
        | Identical -> ()
        | Different (addedHeader, removedHeader, added, removed) ->
            let addedText =
                if added |> List.isEmpty then
                    ""
                else
                    sprintf
                        "\n\nThe following %d member(s) %s:\n  + %s"
                        (added |> List.length)
                        addedHeader
                        (added |> String.concat "\n  + ")

            let removedText =
                if removed |> List.isEmpty then
                    ""
                else
                    sprintf
                        "\n\nThe following %d member(s) %s:\n  - %s"
                        (removed |> List.length)
                        removedHeader
                        (removed |> String.concat "\n  - ")

            failwithf "Unexpected difference.%s%s\n" addedText removedText

    [<CompiledName "AssertNoneRemoved">]
    let assertNoneRemoved (shouldPrint : bool) (comparison : SurfaceComparison) : unit =
        match comparison with
        | Identical -> ()
        | Different (addedHeader, removedHeader, added, removed) ->
            let addedText =
                if added |> List.isEmpty then
                    ""
                else
                    sprintf
                        "\n\nThe following %d member(s) %s:\n  + %s"
                        (added |> List.length)
                        addedHeader
                        (added |> String.concat "\n  + ")

            let removedText, anyRemoved =
                if removed |> List.isEmpty then
                    "", false
                else
                    sprintf
                        "\n\nThe following %d member(s) %s:\n  - %s"
                        (removed |> List.length)
                        removedHeader
                        (removed |> String.concat "\n  - "),
                    true

            if anyRemoved then
                failwithf "Unexpected difference.%s%s\n" addedText removedText
            else if shouldPrint then
                printf "Not failing: any differences below are deemed acceptable.%s%s\n" addedText removedText

    [<CompiledName "RemovedSymbols">]
    let removedSymbols (comparison : SurfaceComparison) : string list =
        match comparison with
        | Identical -> []
        | Different (_, _, _, removed) -> removed

    [<CompiledName "AddedSymbols">]
    let addedSymbols (comparison : SurfaceComparison) : string list =
        match comparison with
        | Identical -> []
        | Different (_, _, added, _) -> added
