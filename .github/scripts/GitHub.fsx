#r "nuget: Octokit"
#load "utils.fsx"

open Utils
open Octokit
open System.Text.RegularExpressions

let private githubCredentials =
    fsi.CommandLineArgs
    |> Array.skip 1
    |> Array.tryExactlyOne
    |> Option.defaultValue (shell "gh" [ "auth"; "token" ])
    |> Credentials

let client =
    GitHubClient(ProductHeaderValue("backdash-scripts"), Credentials = githubCredentials)

let listPackages (namePattern: string) =
    task {
        let! userPackages = client.Packages.GetAllForActiveUser PackageType.Nuget

        let filteredPackages =
            userPackages
            |> Seq.filter (fun x -> Regex.IsMatch(x.Name, namePattern))
            |> Seq.toList

        return filteredPackages
    }
