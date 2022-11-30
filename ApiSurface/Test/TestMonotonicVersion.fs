namespace ApiSurface.Test

open NUnit.Framework
open NuGet.Versioning
open ApiSurface

[<TestFixture>]
module TestMonotonicVersion =

    let v = NuGetVersion.Parse

    [<Test>]
    let ``Patch version is ignored when comparing versions`` () =
        MonotonicVersion.validateVersion (v "11.0") (v "11.0.123") "MyCoolPackage"

    [<Test>]
    let ``Exact match is valid`` () =
        MonotonicVersion.validateVersion (v "11.0") (v "11.0.0") "MyCoolPackage"

    [<Test>]
    let ``Minor increase of 1 is valid`` () =
        MonotonicVersion.versionIncreaseIsInAcceptableRange (v "11.1") (v "11.0.0") "MyCoolPackage"

    [<Test>]
    let ``Major increase of 1 is valid`` () =
        MonotonicVersion.versionIncreaseIsInAcceptableRange (v "12.0") (v "11.1.0") "MyCoolPackage"

    [<Test>]
    let ``Minor increase of 2 is invalid`` () =
        Assert.Throws (
            Has.Message.StartsWith
                "Version of 'MyCoolPackage' specified in version.json (11.2) is larger than the latest version in the NuGet repository (11.0) by an unacceptable amount",
            fun () -> MonotonicVersion.versionIncreaseIsInAcceptableRange (v "11.2") (v "11.0.0") "MyCoolPackage"
        )
        |> ignore

    [<Test>]
    let ``Major increase of 2 is invalid`` () =
        Assert.Throws (
            Has.Message.StartsWith
                "Version of 'MyCoolPackage' specified in version.json (13.0) is larger than the latest version in the NuGet repository (11.1) by an unacceptable amount",
            fun () -> MonotonicVersion.versionIncreaseIsInAcceptableRange (v "13.0") (v "11.1.0") "MyCoolPackage"
        )
        |> ignore

    [<Test>]
    let ``Major and Minor increase of 1 is invalid`` () =
        Assert.Throws (
            Has.Message.StartsWith
                "Version of 'MyCoolPackage' specified in version.json (12.2) is larger than the latest version in the NuGet repository (11.1) by an unacceptable amount",
            fun () -> MonotonicVersion.versionIncreaseIsInAcceptableRange (v "12.2") (v "11.1.0") "MyCoolPackage"
        )
        |> ignore

    [<Test>]
    let ``Decreasing version throws`` () =
        Assert.Throws (
            Has.Message.StartsWith
                "Version of 'MyCoolPackage' specified in version.json (11.0) is less than the latest version in the NuGet repository (11.1)",
            fun () -> MonotonicVersion.validateVersion (v "11.0") (v "11.1.0") "MyCoolPackage"
        )
        |> ignore
