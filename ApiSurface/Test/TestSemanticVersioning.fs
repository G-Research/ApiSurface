namespace ApiSurface.Test

open FsUnitTyped
open NUnit.Framework
open ApiSurface

[<TestFixture>]
module TestSemanticVersioning =

    [<Test>]
    let ``No changes does not change the version`` () =
        let initialVersion =
            {
                Major = 1
                Minor = 0
            }

        let baseline = ApiSurface [ "Foo" ]
        let target = ApiSurface [ "Foo" ]

        let newVersion = ApiSurface.findNewVersion initialVersion baseline target

        newVersion |> TopLevelOperators.shouldEqual initialVersion

    [<Test>]
    let ``Only adding methods updates minor version`` () =
        let initialVersion =
            {
                Major = 1
                Minor = 0
            }

        let baseline = ApiSurface [ "Foo" ]
        let target = ApiSurface [ "Foo" ; "Bar" ]

        let newVersion = ApiSurface.findNewVersion initialVersion baseline target

        newVersion
        |> TopLevelOperators.shouldEqual
            {
                Major = 1
                Minor = 1
            }

    [<Test>]
    let ``Only removing methods updates major version`` () =
        let initialVersion =
            {
                Major = 1
                Minor = 0
            }

        let baseline = ApiSurface [ "Foo" ]
        let target = ApiSurface []

        let newVersion = ApiSurface.findNewVersion initialVersion baseline target

        newVersion
        |> TopLevelOperators.shouldEqual
            {
                Major = 2
                Minor = 0
            }

    [<Test>]
    let ``Adding and removing methods updates major version`` () =
        let initialVersion =
            {
                Major = 1
                Minor = 0
            }

        let baseline = ApiSurface [ "Foo" ]
        let target = ApiSurface [ "Bar" ]

        let newVersion = ApiSurface.findNewVersion initialVersion baseline target

        newVersion
        |> TopLevelOperators.shouldEqual
            {
                Major = 2
                Minor = 0
            }

    [<Test>]
    let ``Updating a Major version resets the Minor version`` () =
        let initialVersion =
            {
                Major = 1
                Minor = 10
            }

        let baseline = ApiSurface [ "Foo" ]
        let target = ApiSurface []

        let newVersion = ApiSurface.findNewVersion initialVersion baseline target

        newVersion
        |> TopLevelOperators.shouldEqual
            {
                Major = 2
                Minor = 0
            }
