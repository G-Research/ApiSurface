namespace ApiSurface.Test

open System.IO
open ApiSurface
open NUnit.Framework
open FsUnitTyped

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
