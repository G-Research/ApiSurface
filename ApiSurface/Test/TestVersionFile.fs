namespace ApiSurface.Test

open System.IO
open System.IO.Abstractions.TestingHelpers
open ApiSurface
open FsCheck.FSharp
open NUnit.Framework
open FsUnitTyped
open FsCheck

[<TestFixture>]
module TestVersionFile =

    let parse (s : string) : VersionFile =
        use stream = new MemoryStream ()
        let writer = new StreamWriter (stream)
        writer.Write s
        writer.Flush ()
        stream.Seek (0L, SeekOrigin.Begin) |> ignore
        let reader = new StreamReader (stream)
        VersionFile.read reader

    let serialise (file : VersionFile) : string =
        use stream = new MemoryStream ()
        let writer = new StreamWriter (stream)
        VersionFile.write writer file
        writer.Flush ()
        stream.Seek (0L, SeekOrigin.Begin) |> ignore
        let reader = new StreamReader (stream)
        reader.ReadToEnd ()

    [<TestCase true>]
    [<TestCase false>]
    let ``Can parse version file with path filters`` (hasComment : bool) =
        let comment = if hasComment then "// comment here\n" else ""

        sprintf
            """%s{
  %s"version": "4.0",
  "publicReleaseRefSpec": %s[
    "^refs/heads/main$"
  ],
  "pathFilters": ["."]
}"""
            comment
            comment
            comment
        |> parse
        |> shouldEqual
            {
                Version = "4.0"
                PublicReleaseRefSpec = [ "^refs/heads/main$" ]
                PathFilters = Some [ "." ]
            }

    [<TestCase true>]
    [<TestCase false>]
    let ``Can parse version file with null path filters`` (hasComment : bool) =
        let comment = if hasComment then "// comment here\n" else ""

        sprintf
            """%s{
  %s"version": "4.0",
  "publicReleaseRefSpec": %s[
    "^refs/heads/main$"
  ],
  "pathFilters": null
}"""
            comment
            comment
            comment
        |> parse
        |> shouldEqual
            {
                Version = "4.0"
                PublicReleaseRefSpec = [ "^refs/heads/main$" ]
                PathFilters = None
            }


    [<TestCase true>]
    [<TestCase false>]
    let ``Can parse version file with omitted path filters`` (hasComment : bool) =
        let comment = if hasComment then "// comment here\n" else ""

        sprintf
            """%s{
  %s"version": "4.0",
  "publicReleaseRefSpec": %s[
    "^refs/heads/main$"
  ]
}"""
            comment
            comment
            comment
        |> parse
        |> shouldEqual
            {
                Version = "4.0"
                PublicReleaseRefSpec = [ "^refs/heads/main$" ]
                PathFilters = None
            }

    let versionFiles =
        List.allPairs
            [ [] ; [ "^refs/heads/main$" ] ; [ "foo" ; "bar" ] ]
            [
                None
                Some []
                Some [ "pathFilter1" ]
                Some [ "pathFilter1" ; "pathFilter2" ]
            ]
        |> List.allPairs [ "4.0" ; "5.3" ]
        |> List.map (fun (version, (refSpec, pathFilters)) ->
            {
                Version = version
                PublicReleaseRefSpec = refSpec
                PathFilters = pathFilters
            }
        )
        |> List.map TestCaseData

    [<TestCaseSource "versionFiles">]
    let ``Can write version file`` (versionFile : VersionFile) =
        versionFile |> serialise |> parse |> shouldEqual versionFile

    let versionFileGen : Gen<VersionFile> =
        gen {
            let! major = ArbMap.defaults |> ArbMap.generate<int>
            let major = abs major
            let! minor = ArbMap.defaults |> ArbMap.generate<int>
            let minor = abs minor
            let! releaseRefSpec = ArbMap.defaults |> ArbMap.generate<NonNull<string> list>
            let! pathFilters = ArbMap.defaults |> ArbMap.generate<NonNull<string> list option>

            return
                {
                    Version = sprintf "%i.%i" major minor
                    PublicReleaseRefSpec = releaseRefSpec |> List.map (fun spec -> spec.Get)
                    PathFilters = pathFilters |> Option.map (List.map (fun filter -> filter.Get))
                }
        }

    let private incrementVersion (version : string) =
        match version.Split '.' with
        | [| major ; _minor |] -> sprintf "%i.0" (int major + 1)
        | _ -> failwithf "Unrecognised version string: %s" version

    [<Test>]
    let ``version JSON file can be written`` () =
        use standardOutput = new RedirectOutput ()

        let property (versionFile : VersionFile) =
            let fs = MockFileSystem ()
            let location = fs.FileInfo.FromFileName "version.json"

            versionFile
            |> serialise
            |> fun s -> fs.File.WriteAllText (location.FullName, s)

            let baseline = ApiSurface [ "something" ; "something else" ]
            ApiSurface.updateVersionJson baseline typeof<ApiSurface>.Assembly location

            let output = fs.File.ReadAllText location.FullName

            parse output
            |> shouldEqual
                { versionFile with
                    Version = incrementVersion versionFile.Version
                }

        Prop.forAll (Arb.fromGen versionFileGen) property |> Check.QuickThrowOnFailure
