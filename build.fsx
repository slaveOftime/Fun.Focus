#r "nuget: Fake.DotNet.Cli,5.23.0"
#r "nuget: Fake.IO.FileSystem,5.23.0"
#r "nuget: Fake.IO.Zip,5.23.0"
#r "nuget: BlackFox.Fake.BuildTask,0.1.3"

open BlackFox.Fake
open Fake.DotNet
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators


fsi.CommandLineArgs |> Array.skip 1 |> BuildTask.setupContextFromArgv


let version = "0.0.2"

let projFile = __SOURCE_DIRECTORY__ </> "Fun.Focus" </> "Fun.Focus.csproj"
let outputDir = __SOURCE_DIRECTORY__ </> "Publish"
let supportedRuntimes = [ "win-x86"; "win-x64"; "win-arm64" ]


let setVersionTask =
    BuildTask.create "SetVersion" [] {
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
    }


let cleanTask =
    BuildTask.create "Clean" [] { supportedRuntimes |> List.iter (fun x -> Directory.delete (outputDir </> x)) }


let publishTask =
    BuildTask.create "Publish" [ cleanTask; setVersionTask ] {
        let publishAndZipFor runtime =
            let targetDir = outputDir </> runtime
            DotNet.publish
                (fun options ->
                    { options with
                        Runtime = Some runtime
                        OutputPath = Some targetDir
                        SelfContained = Some false
                    }
                )
                projFile

            !!(targetDir </> "**/*.*") |> Zip.zip targetDir (outputDir </> $"Fun.Focus-{version}-{runtime}.zip")

        supportedRuntimes |> List.iter publishAndZipFor
    }

BuildTask.runOrDefault publishTask
