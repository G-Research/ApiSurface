namespace ApiSurface.Test

open NUnit.Framework
open FsUnit
open ApiSurface

[<TestFixture>]
module TestSurfaceComparison =

    [<Test>]
    let ``Test assertIdentical with identical result`` () =
        SurfaceComparison.identical |> SurfaceComparison.assertIdentical

    [<Test>]
    let ``Test assertIdentical with added members`` () =
        fun () ->
            SurfaceComparison.different "have been added" "shouldn't see this" [ "Foo" ; "Bar" ] []
            |> SurfaceComparison.assertIdentical
        |> should
            (throwWithMessage "Unexpected difference.\n\nThe following 2 member(s) have been added:\n  + Foo\n  + Bar\n")
            typeof<exn>

    [<Test>]
    let ``Test assertIdentical with removed members`` () =
        fun () ->
            SurfaceComparison.different "shouldn't see this" "have been removed" [] [ "Foo" ; "Bar" ]
            |> SurfaceComparison.assertIdentical
        |> should
            (throwWithMessage
                "Unexpected difference.\n\nThe following 2 member(s) have been removed:\n  - Foo\n  - Bar\n")
            typeof<exn>

    [<Test>]
    let ``Test assertIdentical with both added/removed members`` () =
        fun () ->
            SurfaceComparison.different
                "have been ADDED"
                "have been REMOVED"
                [ "Worlds" ; "Shelter" ]
                [ "Quux" ; "Hello" ]
            |> SurfaceComparison.assertIdentical
        |> should
            (throwWithMessage
                "Unexpected difference.\n\nThe following 2 member(s) have been ADDED:\n  + Worlds\n  + Shelter\n\nThe following 2 member(s) have been REMOVED:\n  - Quux\n  - Hello\n")
            typeof<exn>
