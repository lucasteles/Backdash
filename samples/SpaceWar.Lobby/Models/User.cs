namespace SpaceWar.Models;

public sealed class User
{
    public required Guid UserId { get; init; }
    public required Guid UserToken { get; init; }
    public required string Username { get; init; }
    public bool Ready { get; init; }
}
