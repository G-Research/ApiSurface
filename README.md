# ApiSurface

This library provides several modules for ensuring the consistency and documentation coverage of an F# assembly's public API.
It also integrates with [NerdBank.GitVersioning] to help you adhere to [Semantic Versioning](https://semver.org/) principles.

## Quick-start overview

(A more fully worked example appears later in this README.)

By the end of this overview, you have:

* automatically populated a file with a listing of your assembly's public API surface;
* run a test to assert that your assembly conforms to the API surface in that listing;
* run a test to assert that your assembly's public API surface is fully documented;
* run a test to assert that your assembly's version number differs from that in any accessible NuGet repositories by an acceptable amount.

1. Create an empty `SurfaceBaseline.txt` file next to your assembly's `.fsproj` file. (`.csproj` files are not officially supported, but have been observed to work.)
1. Add an `<EmbeddedResource Include="SurfaceBaseline.txt" />` entry to that `.fsproj` file.
1. Similarly, place a `version.json` file next to your project, using the [NerdBank.GitVersioning] format, and include it as an `EmbeddedResource`.
1. Add tests asserting that, for example, `ApiSurface.assertIdentical (assemblyUnderTest : Assembly)`. (See this repository's `ApiSurface.Test.TestSurface` for a more precise example.)
1. Run `ApiSurface.writeAssemblyBaseline (assemblyUnderTest : Assembly)`, to populate the empty `SurfaceBaseline.txt` file you made earlier. (See this repository's `ApiSurface.Test.TestSurface`.)
1. Observe that `ApiSurface.assertIdentical assembly`, `ApiSurface.assertFullyDocumented assembly`, and `MonotonicVersion.validate assembly "NuGet.PackageName"` all now run without throwing.

## The modules of `ApiSurface`

### `ApiSurface` module

The `ApiSurface` module enforces that the public API surface does not change.
The public API is serialised in a 'SurfaceBaseline.txt' file that is embedded in the assembly.
Unit tests can then ensure that the API surface of the assembly under test matches that which is encoded in the baseline text file.

You can use `ApiSurface.writeAssemblyBaseline` to manually update the `SurfaceBaseline.txt` file.
Common practice is to call this function inside a unit test which is marked `Explicit` - this ensures that the public API is easy to update with a single click, but that it is not accidentally extended (or reduced) simply by running your normal tests.

### `DocCoverage` module

The `DocCoverage` module provides a function to ensure that the entirety of the public API surface is documented. Note that there are some exceptions to this rule:

- Class constructors never need to be documented. These tend to never be more useful than 'Instantiates the type' anyway.
- All discriminated union case constructors have to be documented.
- Modules which have the same name as a type do not have to be documented. It is self-explanatory that the module simply contains function that construct/act upon the type they are named after.

It is possible to explicitly annotate a type or member as not needing documentation by adding the `[<Undocumented>]` attribute.
When applied to a type, all members of that type inherit the `[<Undocumented>]` attribute.
This can be overridden with `[<Undocumented(MembersInherit=false)>]`.

### `MonotonicVersion` module

The `MonotonicVersion` module provides a function to ensure that the `version` field in the `version.json` file is always increasing.
This check can fail if a change which increments the major or minor version is reverted.

## Semantic Versioning

Along with monitoring a project's public API, this library can be used to encourage semantic versioning.
When running the explicit test (see below) to update a project's SurfaceBaseline file, the test will also look for a `version.json` file (in the same location as the SurfaceBaseline file).
If the `version.json` file is not found, an error will be raised.
If the `version.json` file is found, the `version.json` file will be updated with a newer version using the following logic (from https://semver.org/):
* MAJOR version is incremented when backwards incompatible API changes are detected (i.e. API memebers are changed or removed)
* MINOR version is incremented when added backwards-compatible functionality changes are detected (i.e. API members are added __only__)

If no API changes are detected, the version will not be incremented by the test.
If you are using [NerdBank.GitVersioning] then the PATCH version will be incremented as required for any change, in combination with the above.

**Important!**
This isn't intended to replace a real human thinking about the version number.
Breaking behaviour changes (without API changes) still require a major version bump, but no tool can make this judgement for you; `ApiSurface` only detects the most obvious cases, where the API surface has changed.

There are not any currently known changes you can make to the API surface of your library that "should" be recognisably major version bumps but which do not result in `ApiSurface` flagging the change.
Please raise an issue if you find one.

## Different API surfaces on different frameworks

`ApiSurface` supports the rare situation of APIs which are different between .NET Framework and .NET Core, or between different versions of the .NET runtime.
Instead of giving the default `SurfaceBaseline.txt` file, you can override any given framework with one or more specially-named `SurfaceBaseline-FRAMEWORKNAME.txt` files; for example, `SurfaceBaseline-Net5.txt` or `SurfaceBaseline-NetFramework.txt`.

Note that `ApiSurface.writeAssemblyBaseline` will only write a `SurfaceBaseline.txt`, and it will only do so for the currently-executing framework.
If you wish to make this specific to .NET Framework or .NET Core, you should then manually rename the file.

For the complete list of supported frameworks and file names, see the private `frameworkBaselineFile : string` in the `ApiSurface` module.

## Compatibility notes

This project is untested on the .NET Framework; if it works on e.g. net481, this is purely by coincidence.
Similarly, netcoreapp2.1 and earlier are untested.

## Fully worked end-to-end example

Assumptions for this example:

* You have set up a project defining a namespace `MyLibrary`;
* You have set up a unit test project with a reference to that project, using [NUnit](https://nunit.org/);
* Your library defines a type called `SomeDefinedType`;
* Your library is published to a NuGet repository as `MyCompany.MyLibraryNuGetPackage`.
* After integration with ApiSurface, you are happy to let [NerdBank.GitVersioning] handle your version numbers.

If you are using a different test framework, you will need to adjust the test file accordingly.

If you are not currently publishing your library to NuGet, then you should skip the "Ensure version is monotonic" test.

### `SurfaceBaseline.txt`

Create an empty file called `SurfaceBaseline.txt` next to your main project file.
This example will eventually use ApiSurface to populate that file.

### `version.json`

Create a file called `version.json` next to your main project file, in the [NerdBank.GitVersioning] format.
(Currently only a subset of this format is actually supported; see this repository's `VersionFile.fs` for the precise schema.)

```json
{
  "version": "1.0",
  "publicReleaseRefSpec": [
    "^refs/heads/main$"
  ],
  "pathFilters": null
}
```

If your release branch is called something other than `main`, or you use release tags instead, then adjust the `publicReleaseRefSpec` accordingly.
Similarly, if an updated version of your library should be published only when certain files within the repository change, set `pathFilters` to a list of Git pathspecs.

### Within the main project file

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="MyLibrary.fs" />
    <!-- Additions for ApiSurface -->
    <EmbeddedResource Include="SurfaceBaseline.txt" />
    <EmbeddedResource Include="version.json" />
  </ItemGroup>
</Project>
```

If you are multi-targetting and you wish for different `SurfaceBaseline.txt` files for each target, you should generate `SurfaceBaseline` files for each target (renaming them to follow the naming schema as described in this README's description of the `ApiSurface` module), and include them all in the project file.

### Within the unit test project

Add a reference to the `ApiSurface` NuGet package.
Then create a new test file (here we've chosen the name `TestSurface.fs`) within your test project, with the following contents:

```f#
namespace MyLibrary.Test

open NUnit.Framework
open ApiSurface


[<TestFixture>]
module TestSurface =

    let assembly = typeof<MyLibrary.SomeDefinedType>.Assembly

    [<Test>]
    let ``Ensure API surface has not been modified`` () =
        ApiSurface.assertIdentical assembly

    [<Test; Explicit>]
    let ``Update API surface`` () =
        ApiSurface.writeAssemblyBaseline assembly

    [<Test>]
    let ``Ensure public API is fully documented`` () =
        DocCoverage.assertFullyDocumented assembly

    [<Test>]
    let ``Ensure version is monotonic`` () =
        MonotonicVersion.validate assembly "MyCompany.MyLibraryNuGetPackage"
```

[NerdBank.GitVersioning]: https://github.com/dotnet/Nerdbank.GitVersioning
