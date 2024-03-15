using System.Diagnostics.CodeAnalysis;
using SpaceWar;

AppSettings settings = new();
ReadProgramArgs();

using var game = new Game1(settings);
game.Run();


void ReadProgramArgs()
{
    if (TryGetConfig(0, "SPACEWAR_PORT", out var portArg) &&
        int.TryParse(portArg, out var port))
        settings.Port = port;

    if (TryGetConfig(1, "SPACEWAR_LOBBY_URL", out var serverUrlArg)
        && Uri.TryCreate(serverUrlArg, UriKind.Absolute, out var serverUrl))
        settings.LobbyUrl = serverUrl;

    if (TryGetConfig(2, "SPACEWAR_LOBBY_PORT", out var lobbyPortArg)
        && int.TryParse(lobbyPortArg, out var lobbyPort))
        settings.LobbyPort = lobbyPort;
}

bool TryGetConfig(int argsIndex, string envName,
    [NotNullWhen(true)] out string? argValue)
{
    var tempValue = Environment.GetEnvironmentVariable(envName);
    if (!string.IsNullOrWhiteSpace(tempValue))
    {
        argValue = tempValue;
        return true;
    }

    tempValue = args.ElementAtOrDefault(argsIndex);
    if (!string.IsNullOrWhiteSpace(tempValue))
    {
        argValue = tempValue;
        return true;
    }

    argValue = null;
    return false;
}
