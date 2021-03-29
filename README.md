# ApiSurface

This library provides several modules for ensuring the consistency and documentation coverage of an F# assembly's public API.

## How to get started

1. Create an empty `SurfaceBaseline.txt` file next to your assembly's `.fsproj` file. (`.csproj` files are not officially supported, but have been observed to work.)
1. Add an `<EmbeddedResource Include="SurfaceBaseline.txt" />` entry to that `.fsproj` file.
1. Following the example of this repository's `ApiSurface.Test.TestSurface`, create tests as follows:
    ```fsharp
    namespace MyLibrary.Test
    
    open NUnit.Framework
    open ApiSurface
    open MyLibrary
    
    [<TestFixture>]
    module TestSurface =
    
        let assembly = typeof<TypeFromMyLibrary>.Assembly
    
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
            MonotonicVersion.validate assembly "NuGet.PackageName"
    ```
1. Run the `Explicit` test called `Update API surface`, to populate the empty `SurfaceBaseline.txt` file you made earlier.
1. Similarly, place a `version.json` file next to your project, using the [NerdBank.GitVersioning] format, and include it as an `EmbeddedResource`.
1. Run the `Ensure version is monotonic` test, to check that your `version.json` file correctly specifies a version number which is valid with respect to the current release of your NuGet package.

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

## Fully worked end-to-end example

### Within the project file

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
  </PropertyGroup>
  <ItemGroup>
    <EmbeddedResource Include="SurfaceBaseline.txt" />
    <EmbeddedResource Include="version.json" />
  </ItemGroup>
</Project>
```

If you are multi-targetting and you wish for different `SurfaceBaseline.txt` files for each target, you should generate `SurfaceBaseline` files for each target (renaming them to follow the naming schema as described in the summary of this README), and include them all in the project file.

### The version.json file

Add a version.json file to the root of the project, following [NerdBank.GitVersioning] convention.

### Within the unit test project

1. Add a reference to `ApiSurface`.
2. Then add the following files to your test project:

#### TestSurface.fs

```f#
namespace MyLibrary.Test

open NUnit.Framework
open ApiSurface


[<TestFixture>]
module TestSurface =

    let assembly = typeof<_ MyLibrary.SomeDefinedType>.Assembly

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

## Development tips

There are pull request checks on this repo, enforcing [Fantomas](https://github.com/fsprojects/fantomas/)-compliant formatting.
After checking out the repo, you may wish to add a pre-push hook to ensure locally that formatting is complete, rather than having to wait for the CI checks to tell you that you haven't formatted your code.
Consider performing the following command to set this up in the repo:
```bash
git config core.hooksPath hooks/
```
Before your first push (but only once), you will need to install the [.NET local tools](https://docs.microsoft.com/en-us/dotnet/core/tools/local-tools-how-to-use) which form part of the pre-push hook:
```bash
dotnet tool restore
```

In future, some commits (such as big-bang formatting commits) may be recorded for convenience in `.git-blame-ignore-revs`.
Consider performing the following command to have `git blame` ignore these commits, when we ever create any:
```bash
git config blame.ignoreRevsFile .git-blame-ignore-revs
```
