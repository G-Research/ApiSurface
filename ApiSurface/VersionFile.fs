namespace ApiSurface

open System.IO
open System.IO.Abstractions
open System.Text.Json
open System.Reflection
open System.Text.Json.Serialization

/// A record representing the layout of a version.json file, e.g. as consumed by NerdBank.GitVersioning.
type VersionFile =
    {
        /// The version number (e.g. "1.0")
        Version : string
        /// The collection of Git references which are to be considered relevant to this package.
        /// For example, "^refs/heads/main$".
        PublicReleaseRefSpec : string list
        /// The collection of paths which are to be considered relevant to this package.
        PathFilters : string list option
    }

[<RequireQualifiedAccess>]
module VersionFile =

    let private readOptions =
        JsonSerializerOptions (
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            IgnoreNullValues = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        )

    let private writeOptions =
        JsonSerializerOptions (WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase)

    /// Read and parse a stream representing a version file.
    let read (reader : StreamReader) : VersionFile =
        JsonSerializer.Deserialize<VersionFile> (reader.ReadToEnd (), readOptions)

    /// Write a version file into a stream.
    let write (writer : StreamWriter) (versionFile : VersionFile) : unit =
        JsonSerializer.Serialize (versionFile, writeOptions) |> writer.Write

    /// Find version.json files referenced within this assembly.
    let findVersionFiles (fs : IFileSystem) (assembly : Assembly) : IFileInfo list =
        let filenames = assembly |> Assembly.findProjectFiles (fun _ -> [ "version.json" ])
        filenames |> List.filter File.Exists |> List.map fs.FileInfo.FromFileName

    /// Find version.json files above this assembly, but stopping when we hit a directory with
    /// the given name.
    let findVersionFilesWithDirectory (fs : IFileSystem) (dir : string) (assembly : Assembly) : IFileInfo list =
        let filenames =
            assembly
            |> Assembly.findProjectFilesWithDirectory dir (fun _ -> [ "version.json" ])

        filenames |> List.filter File.Exists |> List.map fs.FileInfo.FromFileName
