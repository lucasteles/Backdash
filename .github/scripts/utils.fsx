open System
open System.Threading.Tasks

let shell processName (args: _ list) =
    let psi =
        Diagnostics.ProcessStartInfo(
            processName,
            args,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        )

    use proc = Diagnostics.Process.Start psi
    let output, err = Text.StringBuilder(), Text.StringBuilder()
    proc.OutputDataReceived.Add(fun args -> output.Append(args.Data) |> ignore)
    proc.ErrorDataReceived.Add(fun args -> err.Append(args.Data) |> ignore)
    proc.BeginErrorReadLine()
    proc.BeginOutputReadLine()
    proc.WaitForExit()

    if (err.Length > 0) then
        failwith $"Error calling {processName}: {err}"

    output.ToString()


let await (t: Task<_>) = t.GetAwaiter().GetResult()

