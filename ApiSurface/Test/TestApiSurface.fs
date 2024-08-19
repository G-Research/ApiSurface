namespace ApiSurface.Test

open NUnit.Framework
open FsUnitTyped
open ApiSurface

[<TestFixture>]
module TestApiSurface =

    let sampleAssembly = typeof<ApiSurface.SampleAssembly.Class1>.Assembly

    [<Test>]
    let ``Test ofAssembly`` () =

        let (ApiSurface actual) = sampleAssembly |> ApiSurface.ofAssembly

        let expected = Sample.publicSurface

        actual |> shouldEqual expected

    [<Test>]
    let ``Test toString`` () =

        [ "Foo" ; "Bar" ] |> ApiSurface |> ApiSurface.toString |> shouldEqual "Foo\nBar"

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

        let exc =
            Assert.Throws<exn> (fun () ->
                [ "Bar" ; "Foo" ]
                |> ApiSurface
                |> ApiSurface.compare (ApiSurface [ "Foo" ])
                |> SurfaceComparison.assertIdentical
            )

        exc.Message
        |> shouldEqual
            "Unexpected difference.\n\nThe following 1 member(s) have been added (i.e. are NOT present in the baseline):\n  + Bar\n"

    [<Test>]
    let ``Test compare for different surfaces (not in target)`` () =

        let exc =
            Assert.Throws<exn> (fun () ->
                [ "Foo" ]
                |> ApiSurface
                |> ApiSurface.compare (ApiSurface [ "Foo" ; "Bar" ])
                |> SurfaceComparison.assertIdentical
            )

        exc.Message
        |> shouldEqual
            "Unexpected difference.\n\nThe following 1 member(s) have been removed (i.e. are present in the baseline):\n  - Bar\n"

    [<Test>]
    let ``Test ofAssemblyBaseline`` () =

        let (ApiSurface actual) = sampleAssembly |> ApiSurface.ofAssemblyBaseline
        let expected = Sample.publicSurface @ [ "Bar" ; "Foo" ]
        actual |> shouldEqual expected

    [<Test>]
    let ``Test ofAssemblyBaselineWithExplicitFilename`` () =

        let (ApiSurface actual) =
            sampleAssembly
            |> ApiSurface.ofAssemblyBaselineWithExplicitResourceName "SurfaceBaseline.txt"

        let expected = Sample.publicSurface @ [ "Bar" ; "Foo" ]
        actual |> shouldEqual expected
