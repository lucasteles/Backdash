namespace LobbyServer;

public class AppSettings
{
    public required TimeSpan LobbyExpiration { get; init; }
    public required TimeSpan PurgeTimeout { get; init; }
}
