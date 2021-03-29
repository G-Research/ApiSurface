namespace ApiSurface

open System.IO.Abstractions
open System.Text.RegularExpressions
open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open System.Reflection
open System.IO

type ApiSurface = internal | ApiSurface of string list

type internal Version =
    {
        Major : int
        Minor : int
    }

[<RequireQualifiedAccess>]
module ApiSurface =

    let private readBaseline (stream : Stream) : ApiSurface =
        use reader = new StreamReader (stream)

        reader.ReadToEnd().Split [| '\n' |]
        |> Seq.map (fun s -> s.Trim ())
        |> Seq.filter ((<>) "")
        |> Seq.sortBy (fun s -> s.ToLowerInvariant ())
        |> List.ofSeq
        |> ApiSurface

    /// In the rare case that you have several different baselines depending on what framework you are running under,
    /// you can use a more specific name for your baseline files.
    let private frameworkBaselineFile =
        let desc = RuntimeInformation.FrameworkDescription

        if desc.StartsWith ".NET Core" then
            "SurfaceBaseline-NetCore.txt"
        elif desc.StartsWith ".NET Framework" then
            "SurfaceBaseline-NetFramework.txt"
        else
            // e.g. "SurfaceBaseline-Net5.txt"
            let frameworkNumber = Regex("^\\.NET ([0-9]+)\.").Match desc

            if frameworkNumber.Success then
                sprintf "SurfaceBaseline-Net%s.txt" frameworkNumber.Groups.[0].Value
            else
                failwithf "Unknown runtime framework: %s" desc

    let surfaces (assembly : Assembly) =
        assembly.GetManifestResourceNames ()
        |> Seq.choose Option.ofObj
        |> Seq.filter (fun i -> i.Contains "SurfaceBaseline" && i.Contains ".txt")
        |> Seq.toList

    /// Get the possible filenames for the surface baseline file.
    /// The earlier a file appears in this list, the more preferable it is as a name.
    /// Guaranteed to return a nonempty list.
    let private possibleBaselineResourceNames (assembly : Assembly) : string list =
        let frameworkIAmRunning = frameworkBaselineFile
        // When changing this logic, be sure to update the ApiSurface README.
        [
            frameworkIAmRunning
            // Here because this is how the files may appear as embedded resources, not because we expect people to
            // name their files this way
            sprintf "%s.%s" (assembly.GetName().Name) frameworkIAmRunning
            sprintf "%s.SurfaceBaseline.txt" (assembly.GetName().Name)

            // The overwhelmingly most common case
            "SurfaceBaseline.txt"
        ]

    let private readApiSurfaceResource (assembly : Assembly) (possibleFilenames : string list) : ApiSurface =
        possibleFilenames
        |> List.tryPick (assembly.GetManifestResourceStream >> Option.ofObj)
        |> function
            | Some s -> s
            | None -> failwithf "Unable to find SurfaceBaseline resource, tried: %A" possibleFilenames
        |> readBaseline

    [<CompiledName "FromAssemblyBaseline">]
    let ofAssemblyBaseline (assembly : Assembly) : ApiSurface =
        let surface =
            match surfaces assembly with
            | [] ->
                assembly.GetManifestResourceNames ()
                |> Seq.choose Option.ofObj
                |> List.ofSeq
                |> failwithf "Assembly '%+A' has no surface baseline (resources: %+A)" assembly
            | [ x ] -> x
            | xs ->
                // Go for the one that contains our current framework name if possible.
                match xs |> List.filter (fun i -> i.Contains frameworkBaselineFile) with
                | [] ->
                    let guess = xs |> List.maxBy (fun i -> i.Length)

                    eprintfn
                        "Assembly '%+A' contains multiple candidate baselines. Taking best guess from '%+A': %s."
                        assembly
                        xs
                        guess

                    guess
                | [ x ] -> x
                | ys -> failwithf "Assembly '%+A' has ambiguous surface baseline files: %+A" assembly ys

        readBaseline (assembly.GetManifestResourceStream surface)

    [<CompiledName "FromAssemblyBaselineWithExplicitResourceName">]
    let ofAssemblyBaselineWithExplicitResourceName (baselineResource : string) (assembly : Assembly) : ApiSurface =
        [
            baselineResource
            // The file may appear like this as an embedded resource
            sprintf "%s.%s" (assembly.GetName().Name) baselineResource
        ]
        |> readApiSurfaceResource assembly

    let findDifferences baseline target =
        let baselineSet = baseline |> Set.ofList
        let targetSet = target |> Set.ofList

        let added = Set.difference targetSet baselineSet |> Set.toList
        let removed = Set.difference baselineSet targetSet |> Set.toList

        (added, removed)

    [<CompiledName "Compare">]
    let compare (ApiSurface baseline) (ApiSurface target) =
        let added, removed = findDifferences baseline target

        match added, removed with
        | [], [] -> SurfaceComparison.identical
        | _, _ ->
            SurfaceComparison.different
                "have been added (i.e. are NOT present in the baseline)"
                "have been removed (i.e. are present in the baseline)"
                added
                removed

    [<CompiledName "FromAssemblyCustomPrint">]
    let ofAssemblyCustomPrint (printType : PublicType -> string list) (assembly : Assembly) : ApiSurface =
        assembly.GetExportedTypes ()
        |> Seq.filter (fun t ->
            // We exclude compiler-generated types. This includes anonymous records.
            isNull <| t.GetCustomAttribute typeof<CompilerGeneratedAttribute>
        )
        |> Seq.collect (fun t -> PublicType.ofType t |> printType)
        |> Seq.sortBy (fun i -> i.ToLowerInvariant ())
        |> List.ofSeq
        |> ApiSurface

    [<CompiledName "FromAssembly">]
    let ofAssembly (assembly : Assembly) : ApiSurface =
        ofAssemblyCustomPrint PublicType.print assembly

    [<CompiledName "ToString">]
    let toString (ApiSurface surface) : string =
        surface |> String.concat "\n" |> sprintf "%s"

    [<CompiledName "AssertIdentical">]
    let assertIdentical (assembly : Assembly) =
        assembly
        |> ofAssembly
        |> compare (ofAssemblyBaseline assembly)
        |> SurfaceComparison.assertIdentical

    let findBaselineResources assembly =
        assembly |> Assembly.findProjectFiles possibleBaselineResourceNames

    let findBaselineResourcesWithDirectory directory assembly =
        assembly
        |> Assembly.findProjectFilesWithDirectory directory possibleBaselineResourceNames

    let writeNewBaseline assembly resourcePath =
        use writer = new StreamWriter (resourcePath, false)

        assembly |> ofAssembly |> toString |> writer.Write

    let private readCurrentVersion (version : IFileInfo) =
        use stream = version.OpenRead ()
        use reader = new StreamReader (stream)
        let versionFile = VersionFile.read reader

        let versionParts = versionFile.Version.Split '.'

        try
            {
                Major = int versionParts.[0]
                Minor = int versionParts.[1]
            }
        with _ ->
            // GitVersioning will verify the json file during build, before the test runs, so it's not expected that anyone will see this error.
            failwithf
                "Version in the version.json file must be of the form 'x.x' when x represents a whole number; it was %s."
                versionFile.Version


    let private writeUpdatedVersion (version : Version) (versionFile : IFileInfo) =
        let updatedFile =
            use stream = versionFile.OpenRead ()
            use reader = new StreamReader (stream)
            let versionFile = VersionFile.read reader

            { versionFile with
                Version = sprintf "%d.%d" version.Major version.Minor
            }

        use writer = versionFile.OpenWrite ()
        use writer = new StreamWriter (writer)
        updatedFile |> VersionFile.write writer

    let internal findNewVersion currentVersion (ApiSurface baseline) (ApiSurface target) =

        let added, removed = findDifferences baseline target

        match added, removed with
        // If anything was removed, it's a breaking change (this will also cover updated methods)
        | _, r when r |> List.isEmpty |> not ->
            {
                Major = currentVersion.Major + 1
                Minor = 0
            }
        // If only things were added to the API, it is a non-breaking change
        | a, _ when a |> List.isEmpty |> not ->
            { currentVersion with
                Minor = currentVersion.Minor + 1
            }
        // If the api was not changed, then GitVersioning will handle the patch version
        | _ -> currentVersion

    let updateVersionJson (baseline : ApiSurface) (assembly : Assembly) (versionFile : IFileInfo) : unit =
        let currentVersion = readCurrentVersion versionFile
        printfn "Current version %d.%d" currentVersion.Major currentVersion.Minor

        let updatedVersion = findNewVersion currentVersion baseline (ofAssembly assembly)

        if currentVersion = updatedVersion then
            printfn "No version increment inferred."
        else
            printfn "Updated version to %d.%d" updatedVersion.Major updatedVersion.Minor

        writeUpdatedVersion updatedVersion versionFile

    let writeAssembly
        (baseline : ApiSurface)
        (possibleBaselineResources : string list)
        (versionFiles : IFileInfo list)
        (assembly : Assembly)
        : unit
        =

        // Check if any of those resource files exists already, in order; use the first one that matches.
        // If none exists (including the ultimate fallback, "SurfaceBaseline.txt"!), fall back to SurfaceBaseline.txt.
        // That's because it's going to be pretty rare that anyone needs two different baselines, so it seems reasonable
        // on first run to make SurfaceBaseline.txt rather than the most specific possible.
        let baselinePath =
            possibleBaselineResources
            |> Seq.tryFind File.Exists
            |> Option.defaultValue (Seq.last possibleBaselineResources)

        let versionFile =
            match versionFiles with
            | [] ->
                printfn "No version.json file found. Skipping version.json check."
                None
            | [ x ] -> Some x
            | (x :: _) as all ->
                eprintfn
                    "Multiple version.json files (%+A) found. Choosing '%s' as the nearest to the project file."
                    all
                    x.FullName

                Some x

        match versionFile with
        | Some versionFile ->
            printfn "Updating version.json file: %s" versionFile.FullName
            updateVersionJson baseline assembly versionFile
        | None -> ()

        printfn "Updating baseline file: %s" baselinePath
        writeNewBaseline assembly baselinePath

    [<CompiledName "WriteAssemblyBaselineWithDirectory">]
    let writeAssemblyBaselineWithDirectory (dir : string) (assembly : Assembly) : unit =
        let possibleBaselineResources = findBaselineResourcesWithDirectory dir assembly

        let versionFiles =
            VersionFile.findVersionFilesWithDirectory (FileSystem ()) dir assembly

        writeAssembly (ofAssemblyBaseline assembly) possibleBaselineResources versionFiles assembly

    [<CompiledName "WriteAssemblyBaseline">]
    let writeAssemblyBaseline (assembly : Assembly) : unit =
        let possibleBaselineResources = findBaselineResources assembly
        let versionFiles = VersionFile.findVersionFiles (FileSystem ()) assembly
        writeAssembly (ofAssemblyBaseline assembly) possibleBaselineResources versionFiles assembly
