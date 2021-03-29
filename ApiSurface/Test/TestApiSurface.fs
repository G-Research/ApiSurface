namespace ApiSurface.Test

open NUnit.Framework
open FsUnit
open ApiSurface

[<TestFixture>]
module TestApiSurface =

    let sampleAssembly = typeof<ApiSurface.SampleAssembly.Class1>.Assembly

    [<Test>]
    let ``Test ofAssembly`` () =

        let (ApiSurface actual) = sampleAssembly |> ApiSurface.ofAssembly

        let expected = Sample.publicSurface

        CollectionAssert.AreEqual (expected, actual)

    [<Test>]
    let ``Test toString`` () =

        [ "Foo" ; "Bar" ]
        |> ApiSurface
        |> ApiSurface.toString
        |> should equal "Foo\nBar"

    [<Test>]
    let ``Test compare for identical surfaces`` () =

        [ "Foo" ; "Bar" ]
        |> ApiSurface
        |> ApiSurface.compare (ApiSurface [ "Foo" ; "Bar" ])
        |> SurfaceComparison.assertIdentical

    [<Test>]
    let ``Test compare for identical surfaces (different order)`` () =

        [ "Bar" ; "Foo" ]
        |> ApiSurface
        |> ApiSurface.compare (ApiSurface [ "Foo" ; "Bar" ])
        |> SurfaceComparison.assertIdentical

    [<Test>]
    let ``Test compare for different surfaces (not in baseline)`` () =

        fun () ->
            [ "Bar" ; "Foo" ]
            |> ApiSurface
            |> ApiSurface.compare (ApiSurface [ "Foo" ])
            |> SurfaceComparison.assertIdentical
        |> should
            (throwWithMessage
                "Unexpected difference.\n\nThe following 1 member(s) have been added (i.e. are NOT present in the baseline):\n  + Bar\n")
            typeof<exn>

    [<Test>]
    let ``Test compare for different surfaces (not in target)`` () =

        fun () ->
            [ "Foo" ]
            |> ApiSurface
            |> ApiSurface.compare (ApiSurface [ "Foo" ; "Bar" ])
            |> SurfaceComparison.assertIdentical
        |> should
            (throwWithMessage
                "Unexpected difference.\n\nThe following 1 member(s) have been removed (i.e. are present in the baseline):\n  - Bar\n")
            typeof<exn>

    [<Test>]
    let ``Test ofAssemblyBaseline`` () =

        let (ApiSurface actual) = sampleAssembly |> ApiSurface.ofAssemblyBaseline
        let expected = Sample.publicSurface @ [ "Bar" ; "Foo" ]
        CollectionAssert.AreEqual (expected, actual)

    [<Test>]
    let ``Test ofAssemblyBaselineWithExplicitFilename`` () =

        let (ApiSurface actual) =
            sampleAssembly
            |> ApiSurface.ofAssemblyBaselineWithExplicitResourceName "SurfaceBaseline.txt"

        let expected = Sample.publicSurface @ [ "Bar" ; "Foo" ]
        CollectionAssert.AreEqual (expected, actual)
