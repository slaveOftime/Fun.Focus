#r "nuget: Fake.DotNet.Cli,5.20.4"
#r "nuget: Fake.IO.FileSystem,5.20.4"
#r "nuget: Fake.IO.Zip,5.20.4"
#r "nuget: BlackFox.Fake.BuildTask,0.1.3"

open BlackFox.Fake
open Fake.DotNet
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators


fsi.CommandLineArgs |> Array.skip 1 |> BuildTask.setupContextFromArgv


let projFile = __SOURCE_DIRECTORY__ </> "Fun.Focus" </> "Fun.Focus.csproj"
let outputDir = __SOURCE_DIRECTORY__ </> "Publish"


let cleanTask = BuildTask.create "Clean" [] { Directory.delete outputDir }


let publishTask =
    BuildTask.create "Publish" [ cleanTask ] {
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

            !!(targetDir </> "**/*.*") |> Zip.zip targetDir (outputDir </> ("Fun.Focus-" + runtime + ".zip"))


        [ "win-x86"; "win-x64"; "win-arm64" ] |> List.iter publishAndZipFor
    }

BuildTask.runOrDefault publishTask
