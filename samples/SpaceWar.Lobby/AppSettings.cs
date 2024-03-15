using System.Reflection;
using System.Text.Json;

namespace SpaceWar;

[Serializable]
public class AppSettings
{
    public string Username { get; set; } = string.Empty;
    public required string LobbyName { get; set; }
    public required Uri ServerUrl { get; set; }
    public int LocalPort { get; set; }
    public int ServerUdpPort { get; set; }

    public void ParseArgs(string[] args)
    {
        if (args is []) return;

        var argsDict = args
            .Chunk(2)
            .Where(a => a[0].StartsWith('-'))
            .Select(a => a is [{ } key, { } value]
                ? (Key: key.TrimStart('-'), Value: value)
                : throw new InvalidOperationException("Bad arguments")
            ).ToDictionary(x => x.Key, x => x.Value, StringComparer.InvariantCultureIgnoreCase);

        if (argsDict.TryGetValue(nameof(LocalPort), out var portArg) &&
            int.TryParse(portArg, out var port) && port > 0)
            LocalPort = port;

        if (argsDict.TryGetValue(nameof(ServerUdpPort), out var lobbyPortArg) &&
            int.TryParse(lobbyPortArg, out var lobbyPort) && lobbyPort > 0)
            ServerUdpPort = lobbyPort;

        if (argsDict.TryGetValue(nameof(ServerUrl), out var serverUrl) &&
            Uri.TryCreate(serverUrl, UriKind.Absolute, out var serverUri))
            ServerUrl = serverUri;

        if (argsDict.TryGetValue(nameof(Username), out var usernameArg) &&
            !string.IsNullOrWhiteSpace(usernameArg))
            Username = usernameArg;
    }

    public static AppSettings LoadFromJson(string file)
    {
        var settingsFile = Path.Combine(
            Path.GetDirectoryName(AppContext.BaseDirectory)
            ?? Directory.GetCurrentDirectory(),
            file
        );

        return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(settingsFile))
               ?? throw new InvalidOperationException($"unable to read {file}");
    }
}
