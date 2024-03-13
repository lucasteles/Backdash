namespace SpaceWar;

public class AppSettings
{
    public string LobbyName = "spacewar";
    public string Username = string.Empty;
    public readonly int Port = 8888;

    // public readonly Uri LobbyUrl = new("http://localhost:9999");
    public readonly Uri LobbyUrl = new("https://lobby-server.fly.dev");
    public readonly int LobbyPort = 8888;

    public AppSettings(string[] args)
    {
        if (args is [{ } portArg, ..] && int.TryParse(portArg, out var port))
            Port = port;

        if (args is [_, { } username, ..] && !string.IsNullOrWhiteSpace(username))
            Username = username;
    }
}
