namespace SpaceWar;

public class AppSettings
{
    public string LobbyName = "spacewar";
    public string Username = string.Empty;
    public int Port = 9000;

    public int LobbyPort = 8888;

    public Uri LobbyUrl = new("https://lobby-server.fly.dev");
    // public readonly Uri LobbyUrl = new("http://localhost:9999");
}
