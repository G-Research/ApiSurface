namespace ApiSurface

open System
open System.Collections.Generic
open System.IO
open System.IO.Abstractions
open System.Text.Json
open System.Text.Json.Serialization
open System.Reflection

module private VersionFileConverter =
    let rec read
        (options : JsonSerializerOptions)
        (reader : Utf8JsonReader byref)
        (version : string option)
        (publicReleaseRefSpec : string list option)
        (pathFilters : string list option)
        : string * string list * string list option
        =
        match reader.TokenType with
        | JsonTokenType.Comment ->
            match options.ReadCommentHandling with
            | JsonCommentHandling.Skip -> read options &reader version publicReleaseRefSpec pathFilters
            | JsonCommentHandling.Allow ->
                raise (ArgumentException "JsonCommentHandling.Allow is forbidden when reading JSON.")
            | JsonCommentHandling.Disallow ->
                raise (
                    JsonException (
                        "Encountered a comment, but JsonCommentHandling is set to Disallow. Set to Skip if comments should be handled."
                    )
                )
            | _ ->
                raise (
                    ArgumentOutOfRangeException (
                        sprintf "Unexpected enum value for JsonCommentHandling: %O" options.ReadCommentHandling
                    )
                )
        | JsonTokenType.EndObject ->
            match version, publicReleaseRefSpec with
            | Some version, Some publicReleaseRefSpec -> version, publicReleaseRefSpec, pathFilters
            | _, _ -> raise (JsonException "Both fields `version` and `publicReleaseRefSpec` are mandatory.")
        | JsonTokenType.PropertyName ->
            let propertyName =
                let s = reader.GetString ()

                if options.PropertyNameCaseInsensitive then
                    s.ToLowerInvariant ()
                else
                    s

            match propertyName with
            | "version" ->
                match version with
                | Some existingVersion -> raise (JsonException ("Version property supplied multiple times"))
                | None ->
                    if not (reader.Read ()) then
                        raise (JsonException ())

                    let version = reader.GetString ()

                    if not (reader.Read ()) then
                        raise (JsonException ())

                    read options &reader (Some version) publicReleaseRefSpec pathFilters
            | "publicReleaseRefSpec" ->
                match publicReleaseRefSpec with
                | Some _ -> raise (JsonException "publicReleaseRefSpec property supplied multiple times")
                | None ->
                    let arr =
                        JsonSerializer.Deserialize<string array> (&reader, options) |> Array.toList

                    if not (reader.Read ()) then
                        raise (JsonException ())

                    read options &reader version (Some arr) pathFilters
            | "pathFilters" ->
                match pathFilters with
                | Some _ -> raise (JsonException "pathFilters property supplied multiple times")
                | None ->
                    let arr =
                        JsonSerializer.Deserialize<string array> (&reader, options)
                        |> Option.ofObj
                        |> Option.map Array.toList

                    if not (reader.Read ()) then
                        raise (JsonException ())

                    read options &reader version publicReleaseRefSpec arr
            | _ -> raise (JsonException (sprintf "Unexpected property %s" propertyName))
        | _ -> raise (JsonException (sprintf "Unexpected token type %O" reader.TokenType))


type private VersionFileConverter () =
    inherit JsonConverter<VersionFile> ()

    override this.Read (reader : Utf8JsonReader byref, ty : Type, options : JsonSerializerOptions) : VersionFile =
        if reader.TokenType <> JsonTokenType.StartObject then
            raise (JsonException "VersionFile should be a JSON object")

        if not (reader.Read ()) then
            raise (JsonException ())

        let version, publicReleaseRefSpec, pathFilters =
            VersionFileConverter.read options &reader None None None

        {
            Version = version
            PublicReleaseRefSpec = publicReleaseRefSpec
            PathFilters = pathFilters
        }

    override this.Write (writer : Utf8JsonWriter, value : VersionFile, options : JsonSerializerOptions) : unit =
        let dto = Dictionary<string, obj> ()
        dto.["version"] <- value.Version
        dto.["publicReleaseRefSpec"] <- value.PublicReleaseRefSpec |> List.toArray
        dto.["pathFilters"] <- value.PathFilters |> Option.map List.toArray |> Option.toObj
        JsonSerializer.Serialize (writer, dto, options)

/// A record representing the layout of a version.json file, e.g. as consumed by NerdBank.GitVersioning.
and [<JsonConverter(typeof<VersionFileConverter>)>] VersionFile =
    {
        /// The version number (e.g. "1.0.1")
        [<JsonPropertyName("version")>]
        Version : string
        /// The collection of Git references which are to be considered relevant to this package.
        /// For example, "^refs/heads/main$".
        [<JsonPropertyName("publicReleaseRefSpec")>]
        PublicReleaseRefSpec : string list
        /// The collection of paths which are to be considered relevant to this package.
        [<JsonPropertyName("pathFilters")>]
        PathFilters : string list option
    }

[<RequireQualifiedAccess>]
module VersionFile =

    /// Read and parse a stream representing a version file.
    let read (reader : StreamReader) : VersionFile =
        let options =
            JsonSerializerOptions (
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
                IgnoreNullValues = false
            )

        options.Converters.Add (VersionFileConverter ())

        JsonSerializer.Deserialize<VersionFile> (reader.ReadToEnd (), options)

    /// Write a version file into a stream.
    let write (writer : StreamWriter) (versionFile : VersionFile) : unit =
        let options = JsonSerializerOptions (WriteIndented = true)
        options.Converters.Add (VersionFileConverter ())

        versionFile
        //|> VersionFileSerialised.ofVersionFile
        |> fun vf -> JsonSerializer.Serialize (vf, options)
        |> writer.Write

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
