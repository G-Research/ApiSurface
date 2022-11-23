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

    [<TestCase(true, true)>]
    [<TestCase(true, false)>]
    [<TestCase(false, true)>]
    [<TestCase(false, false)>]
    let ``Can parse version file`` (hasPathFilters : bool, hasComment : bool) =
        let pathFilters = if hasPathFilters then "[\".\"]" else "null"
        let comment = if hasComment then "// comment here\n" else ""

        sprintf
            """%s{
  %s"version": "4.0",
  "publicReleaseRefSpec": %s[
    "^refs/heads/main$"
  ],
  "pathFilters": %s
}"""
            comment
            comment
            comment
            pathFilters
        |> parse
        |> shouldEqual
            {
                Version = "4.0"
                PublicReleaseRefSpec = [ "^refs/heads/main$" ]
                PathFilters = if hasPathFilters then Some [ "." ] else None
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
