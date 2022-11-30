namespace ApiSurface

open System.IO
open System.Reflection
open System.Threading
open NuGet.Common
open NuGet.Configuration
open NuGet.Protocol.Core.Types
open NuGet.Versioning

[<RequireQualifiedAccess>]
module MonotonicVersion =

    // Assume that the NuGet repository is flaky, and retry a few times on failure to download.
    let retry f =
        let rec inner attempt =
            try
                f ()
            with _ when attempt <= 3 ->
                Thread.Sleep (attempt * 2000)
                inner (attempt + 1)

        inner 1

    let validateVersion (packageId : string) (currentVersion : NuGetVersion) (latestVersion : NuGetVersion) =
        let latestVersion = NuGetVersion (latestVersion.Major, latestVersion.Minor, 0)
        let latestVersionStr = latestVersion.Version.ToString 2

        if currentVersion >= latestVersion then
            printfn
                "Version of '%s' specified in version.json (%O) is >= the latest version in the NuGet repository (%s)"
                packageId
                currentVersion
                latestVersionStr
        else
            [
                sprintf
                    "Version of '%s' specified in version.json (%O) is less than the latest version in the NuGet repository (%s)"
                    packageId
                    currentVersion
                    latestVersionStr
                ""
                "Possible causes:"
                "- Do you need to merge from remote into your branch?"
                "- Has a commit which bumped the version been reverted?"
            ]
            |> String.concat "\n"
            |> failwith

    /// Checks to make sure that either the minor version has increased by at most 1, or the major version has increased by at most 1.
    /// If both have updated or one has updated by more than 1 then it suggests a mistake has been made.
    let versionIncreaseIsInAcceptableRange
        (packageId : string)
        (currentVersion : NuGetVersion)
        (latestVersion : NuGetVersion)
        =
        let majorDiff = currentVersion.Major - latestVersion.Major
        let minorDiff = currentVersion.Minor - latestVersion.Minor
        let latestVersion = NuGetVersion (latestVersion.Major, latestVersion.Minor, 0)
        let latestVersionStr = latestVersion.Version.ToString 2

        let acceptableMinorIncrease = minorDiff <= 1 && majorDiff = 0
        let acceptableMajorIncrease = majorDiff <= 1 && minorDiff <= 0

        if acceptableMajorIncrease || acceptableMinorIncrease then
            printfn
                "Version of '%s' specified in version.json (%O) is >= the latest version in the NuGet repository by an acceptable amount (%s)"
                packageId
                currentVersion
                latestVersionStr
        else
            [
                sprintf
                    "Version of '%s' specified in version.json (%O) is larger than the latest version in the NuGet repository (%s) by an unacceptable amount"
                    packageId
                    currentVersion
                    latestVersionStr
                ""
                "Possible causes:"
                "- Have you updated the API surface repeatedly?"
            ]
            |> String.concat "\n"
            |> failwith

    /// Validate the specifically-named embedded resource that is a version.json file.
    [<CompiledName "ValidateResource">]
    let validateResource (resourceName : string) (assembly : Assembly) (packageId : string) =
        let resource = Assembly.tryReadEmbeddedResource assembly resourceName

        match resource with
        | None ->
            failwithf "Unable to find a '%O' EmbeddedResource in assembly '%s'" resourceName (assembly.GetName().Name)
        | Some resource ->

        let versionFile =
            use reader = new StreamReader (resource)
            VersionFile.read reader

        let currentVersion = NuGetVersion.Parse versionFile.Version

        let settings = Settings.LoadDefaultSettings null

        let repos =
            SettingsUtility.GetEnabledSources settings
            |> Seq.map (fun s -> SourceRepository (s, Repository.Provider.GetCoreV3 ()))
            |> Array.ofSeq

        repos
        |> Seq.map (fun x -> x.PackageSource.SourceUri.ToString ())
        |> String.concat "\n"
        |> printfn "Using the following NuGet repositories:\n%s"

        let versions =
            repos
            |> Array.Parallel.collect (fun repo ->
                let metadataResource = repo.GetResource<FindPackageByIdResource> ()
                use cacheContext = new SourceCacheContext (NoCache = true)

                retry (fun () ->
                    metadataResource
                        .GetAllVersionsAsync(
                            packageId,
                            cacheContext,
                            NullLogger.Instance,
                            CancellationToken.None
                        )
                        .Result
                    |> Array.ofSeq
                )
            )
            |> Seq.distinct
            |> Seq.filter (fun v -> not v.IsPrerelease)
            |> Seq.sortDescending
            |> List.ofSeq

        match List.tryHead versions with
        | None -> printfn "Found no public versions of package '%s'" packageId
        | Some latestVersion ->
            validateVersion packageId currentVersion latestVersion
            versionIncreaseIsInAcceptableRange packageId currentVersion latestVersion

    [<CompiledName "Validate">]
    let validate (assembly : Assembly) (packageId : string) =
        validateResource "version.json" assembly packageId
