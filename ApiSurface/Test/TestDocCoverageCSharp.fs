namespace ApiSurface.Test

open ApiSurface
open NUnit.Framework

[<TestFixture>]
module TestDocCoverageCSharp =

    let sampleAssembly = typeof<ApiSurface.CSharpExample.MyEnum>.Assembly

    [<Test>]
    let ``Test comparing with identical coverage`` () =
        DocCoverage.assertFullyDocumented sampleAssembly
