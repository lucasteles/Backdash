using System.Net;
using Backdash;
using Backdash.Sync.Input;
using SpaceWar.Logic;
namespace SpaceWar;
public static class GameSessionFactory
{
    public static IRollbackSession<PlayerInputs, GameState> ParseArgs(
        string[] args, RollbackOptions options
    )
    {
        if (args is not [{ } portArg, { } playerCountArg, .. { } endpoints]
            || !int.TryParse(portArg, out var port)
            || !int.TryParse(playerCountArg, out var playerCount)
           )
            throw new InvalidOperationException("Invalid port argument");
        if (playerCount > Config.MaxShips)
            throw new InvalidOperationException("Too many players");
        if (endpoints is ["sync-test"])
            return RollbackNetcode.CreateTestSession<PlayerInputs, GameState>(
                options: options,
                services: new()
                {
                    InputGenerator = new RandomInputGenerator<PlayerInputs>(),
                }
            );
        if (endpoints is ["spectate", { } hostArg] && IPEndPoint.TryParse(hostArg, out var host))
            return RollbackNetcode.CreateSpectatorSession<PlayerInputs, GameState>(
                port, host, playerCount, options
            );
        var players = endpoints.Select((x, i) => ParsePlayer(playerCount, i + 1, x)).ToArray();
        var localPlayer = players.FirstOrDefault(x => x.IsLocal());
        if (localPlayer is null)
            throw new InvalidOperationException("No local player defined");
        var session = RollbackNetcode.CreateSession<PlayerInputs, GameState>(port, options, new()
        {
            // LogWriter = new FileLogWriter($"log_{localPlayer.Number}.log"),
        });
        session.AddPlayers(players);
        return session;
    }
    static Player ParsePlayer(int totalNumber, int number, string address)
    {
        if (address.Equals("local", StringComparison.OrdinalIgnoreCase))
            return new LocalPlayer(number);
        if (IPEndPoint.TryParse(address, out var endPoint))
        {
            if (number <= totalNumber)
                return new RemotePlayer(number, endPoint);
            return new Spectator(endPoint);
        }
        throw new InvalidOperationException($"Invalid player {number} argument: {address}");
    }
}