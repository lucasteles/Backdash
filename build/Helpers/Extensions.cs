using JetBrains.Annotations;
namespace Helpers;
public static class Extensions
{
    public static T LocalTool<T>(this T tool,
        Solution solution,
        string localTool = "",
        Arguments arguments = null
    )
        where T : ToolSettings =>
        tool
            .SetProcessWorkingDirectory(solution.Directory)
            .SetProcessToolPath(DotNetPath)
            .SetProcessArgumentConfigurator(
                args => new Arguments().Add(localTool).Concatenate(args)
                    .Concatenate(arguments ?? new()));
    [CanBeNull]
    public static Project FindProject(this Solution sln, string name) =>
        sln.AllProjects.SingleOrDefault(x => name.Equals(x.Name, StringComparison.Ordinal));
}
