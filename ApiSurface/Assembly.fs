namespace ApiSurface

open System
open System.IO
open System.Reflection

/// Helper functions for dealing with the Assembly type.
[<RequireQualifiedAccess>]
module Assembly =

    /// Attempt to retrieve an embedded resource from an assembly.
    let internal tryReadEmbeddedResource (assembly : Assembly) (resourcePath : string) : Stream option =
        let resourcePath = resourcePath.Replace ("/", ".")
        let assemblyResourceName ns = sprintf "%s.%s" ns resourcePath

        let namespaces =
            assembly.GetTypes ()
            |> Seq.toList
            |> List.choose (fun i -> i.Namespace |> Option.ofObj)
            |> Set.ofList
            |> Set.filter (fun i -> i.StartsWith ("<", StringComparison.Ordinal) |> not)
            |> Set.add (assembly.GetName().Name)

        namespaces
        |> Set.add "RootNamespace"
        |> Set.map assemblyResourceName
        |> Set.add resourcePath
        |> Seq.tryPick (assembly.GetManifestResourceStream >> Option.ofObj)

    /// You are not expected to use this function directly.
    ///
    /// Finds all 'fileNames' in ancestor directories of the assembly's location, where
    /// we specifically look for the given parentDir.
    let findProjectFilesWithDirectory (parentDir : string) fileNames (assembly : Assembly) =
        let rec findParentDir name (dir : DirectoryInfo) =
            if isNull dir then
                failwithf
                    "No '%s' directory found above assembly: '%s'.\nDo you have 'Shadow-copy assemblies being tested' enabled in your unit test runner?"
                    name
                    assembly.Location

            let projectFiles (dir : DirectoryInfo) =
                [
                    Path.Combine [| dir.FullName ; name + ".fsproj" |]
                    Path.Combine [| dir.FullName ; name + ".csproj" |]
                ]

            let candidateSubDir = Path.Combine [| dir.FullName ; name |]

            if dir.Name = name && dir |> projectFiles |> List.exists File.Exists then
                // Found a directory called "foo" with a "foo.fsproj" or "foo.csproj" file in it
                dir.FullName
            elif candidateSubDir |> DirectoryInfo |> projectFiles |> List.exists File.Exists then
                // Found a directory called "bar" with a sub dir called "foo" with the project file in it. Return "bar\foo"
                candidateSubDir
            else
                findParentDir name dir.Parent // Recurse up

        fileNames assembly
        |> List.map (fun f -> Path.Combine (findParentDir parentDir (FileInfo assembly.Location).Directory, f))

    /// You are not expected to use this function directly.
    ///
    /// Finds all 'fileNames' in ancestor directories of 'assembly'.
    let findProjectFiles (fileNames : Assembly -> string list) (assembly : Assembly) =
        findProjectFilesWithDirectory (assembly.GetName().Name) fileNames assembly
