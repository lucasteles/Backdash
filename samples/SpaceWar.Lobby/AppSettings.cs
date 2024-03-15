namespace SpaceWar;

public class AppSettings
{
    public string LobbyName = "spacewar";
    public string Username = string.Empty;
    public readonly int Port = 9000;


    // public readonly Uri LobbyUrl = new("https://lobby-server.fly.dev");
    public readonly Uri LobbyUrl = new("http://localhost:9999");
    public readonly int LobbyPort = 8888;

    public AppSettings(string[] args)
    {
        if (args is [{ } portArg, ..] && int.TryParse(portArg, out var port))
            Port = port;

        if (args is [_, { } serverUrlArg, ..] &&
            Uri.TryCreate(serverUrlArg, UriKind.Absolute, out var serverUrl))
            LobbyUrl = serverUrl;

        if (args is [_, _, { } lobbyPortArg, ..] && int.TryParse(lobbyPortArg, out var lobbyPort))
            LobbyPort = lobbyPort;
    }
}
