namespace SpaceWar;

public class AppSettings
{
    public required int Port;

    public readonly string LobbyName = "spacewar";

    // public readonly Uri LobbyUrl = new("https://lobby-server.fly.dev");
    public readonly Uri LobbyUrl = new("http://localhost:9999");
}
