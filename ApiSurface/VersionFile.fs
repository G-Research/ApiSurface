namespace ApiSurface

open System.IO
open System.Text.Json
open System.Reflection

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
    /// Pass e.g. `fs.FileInfo.FromFileName` (from System.IO.Abstractions) or `FileInfo` (from System.IO)
    /// as the `fromFileName` argument.
    let inline findVersionFiles<^fileInfo when ^fileInfo : (member Exists : bool)> (fromFileName : string -> 'fileInfo) (assembly : Assembly) : 'fileInfo list =
        let filenames = assembly |> Assembly.findProjectFiles (fun _ -> [ "version.json" ])
        filenames |> List.map fromFileName |> List.filter (fun f -> (^fileInfo : (member Exists : bool) f))

    /// Find version.json files above this assembly, but stopping when we hit a directory with
    /// the given name.
    /// Pass e.g. `fs.FileInfo.FromFileName` (from System.IO.Abstractions) or `FileInfo` (from System.IO)
    /// as the `fromFileName` argument.
    let inline findVersionFilesWithDirectory<'fileInfo when ^fileInfo : (member Exists : bool)> (fromFileName : string -> 'fileInfo) (dir : string) (assembly : Assembly) : 'fileInfo list =
        let filenames =
            assembly
            |> Assembly.findProjectFilesWithDirectory dir (fun _ -> [ "version.json" ])

        filenames |> List.map fromFileName |> List.filter (fun f -> (^fileInfo : (member Exists : bool) f))
