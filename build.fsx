#r "nuget: Fake.DotNet.Cli,5.23.0"
#r "nuget: Fake.IO.FileSystem,5.23.0"
#r "nuget: Fake.IO.Zip,5.23.0"
#r "nuget: Fun.Build,0.1.8"

open Fun.Build
open Fake.DotNet
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators


let version = "0.0.3"

let projFile = __SOURCE_DIRECTORY__ </> "Fun.Focus" </> "Fun.Focus.fsproj"
let outputDir = __SOURCE_DIRECTORY__ </> "Publish"
let distDir = outputDir </> "dist"


let setVersion () =
    File.writeString
        false
        "Directory.Build.props"
        $"""
<Project>
    <PropertyGroup>
        <AssemblyVersion>{version}</AssemblyVersion>
        <FileVersion>{version}</FileVersion>
        <Version>{version}</Version>
        <Company>slaveOftime</Company>
        <Authors>slaveOftime</Authors>
        <Product>Fun.Focus</Product>
        <Description>Utils for sharing screen with a specific area</Description>
        <PackageTags>WPF,Windows,Share screen</PackageTags>
    </PropertyGroup>
</Project>
        """


pipeline "Publish" {
    stage "Clean" {
        run (fun _ -> Directory.delete distDir)
        run (fun _ -> setVersion ())
    }
    stage "Bundle" {
        run (fun _ ->
            DotNet.publish
                (fun options ->
                    { options with
                        OutputPath = Some distDir
                        SelfContained = Some false
                    }
                )
                projFile

            !!(distDir </> "**/*.*") |> Zip.zip distDir (outputDir </> $"Fun.Focus-{version}.zip")
        )
    }
    runIfOnlySpecified false
}
