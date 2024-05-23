#r "nuget: Octokit"
#load "GitHub.fsx"

open Utils
open System.Threading.Tasks

let deletePackages pattern =
    task {
        let! packages = GitHub.listPackages pattern

        do!
            packages
            |> List.map (fun p ->
                printfn $"Deleting {p.Name}"
                GitHub.client.Packages.DeleteForActiveUser(p.PackageType.Value, p.Name))
            |> Task.WhenAll
    }


deletePackages "^Backdash.*" |> await
