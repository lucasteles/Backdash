namespace Helpers;

public static class Commands
{
    public static Tool BrowserTool => GetTool(
        Platform switch
        {
            PlatformFamily.Windows => "explorer",
            PlatformFamily.OSX => "open",
            _ => new[] { "google-chrome", "firefox" }
                .FirstOrDefault(CommandExists),
        });

    public static void OpenBrowser(AbsolutePath path)
    {
        Assert.FileExists(path);
        try
        {
            BrowserTool($"{path.ToString().DoubleQuoteIfNeeded()}");
        }
        catch (Exception e)
        {
            if (!IsWin) // Windows explorer always return 1
                Log.Error(e, "Unable to open report");
        }
    }

    public static Tool GetTool(string name) =>
        ToolResolver.TryGetEnvironmentTool(name) ??
        ToolResolver.GetPathTool(name);

    public static IProcess RunCommand(string command, params string[] args) =>
        ProcessTasks.StartProcess(command,
            string.Join(" ", args.Select(a => a.DoubleQuoteIfNeeded())),
            NukeBuild.RootDirectory);

    public static bool CommandExists(string command)
    {
        using var process = RunCommand("which", command);
        process.WaitForExit();
        return process.ExitCode == 0;
    }
}