namespace ApiSurface.Test

open NUnit.Framework
open FsUnitTyped
open ApiSurface

[<TestFixture>]
module TestDocCoverage =

    let sampleAssembly = typeof<ApiSurface.DocumentationSample.MyTinyType>.Assembly

    [<Test>]
    let ``Test ofAssemblySurface and toString`` () =
        let coverage = DocCoverage.ofAssemblySurface sampleAssembly |> DocCoverage.toString
        let actual = coverage.Split '\n' |> Set.ofArray
        let expectedButAbsent = Set.difference Sample.allIncludingInternal actual
        let unexpected = Set.difference actual Sample.allIncludingInternal

        // Due to differences of framework, certain specific members are allowed not to appear on the surface.

        if not <| Set.isSubset unexpected Sample.allowableExtraInternal then
            Set.difference unexpected Sample.allowableExtraInternal
            |> String.concat "\n"
            |> failwithf "Unexpectedly received members: %s"

        expectedButAbsent |> shouldBeEmpty

    [<Test>]
    let ``Test ofAssemblyXml`` () =
        let ofXml = DocCoverage.ofAssemblyXml sampleAssembly |> DocCoverage.toString
        let expected = Sample.publicDocumentedSurface

        let actual = ofXml.Split '\n' |> Set.ofArray

        actual |> shouldEqual expected

    [<Test>]
    let ``Test comparing with identical coverage`` () =
        DocCoverage.assertFullyDocumented sampleAssembly

    [<Test>]
    let ``Test comparing with different coverage`` () =

        let left =
            """<?xml version="1.0" encoding="utf-8"?>
            <doc>
            <assembly><name>ApiSurface.DocumentationSample</name></assembly>
            <members>
            <member name="F:ApiSurface.DocumentationSample.Class1.myField">

            </member>
            </members>
            </doc>
            """

        let right =
            """<?xml version="1.0" encoding="utf-8"?>
            <doc>
            <assembly><name>ApiSurface.DocumentationSample</name></assembly>
            <members>
            <member name="P:ApiSurface.DocumentationSample.Class1.X">

            </member>
            </members>
            </doc>
            """

        let exc =
            Assert.Throws<exn> (fun () ->
                DocCoverage.ofXml "left.xml" left
                |> DocCoverage.compare (DocCoverage.ofXml "right.xml" right)
                |> SurfaceComparison.assertIdentical
            )

        exc.Message
        |> shouldEqual
            "Unexpected difference.\n\nThe following 1 member(s) are only in 'left.xml':\n  + F:ApiSurface.DocumentationSample.Class1.myField\n\nThe following 1 member(s) are only in 'right.xml':\n  - P:ApiSurface.DocumentationSample.Class1.X\n"
