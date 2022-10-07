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
let supportedRuntimes = [ "win-x86"; "win-x64"; "win-arm64" ]


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
        run (fun _ -> supportedRuntimes |> List.iter (fun x -> Directory.delete (outputDir </> x)))
        run (fun _ -> setVersion ())
    }
    stage "Bundle" {
        run (fun _ ->
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
        )
    }
    runIfOnlySpecified
}
