using System.Net;
using Backdash;

public sealed class Args
{
    public int Port { get; }
    public int PlayerCount { get; }
    public string[] OtherArgs { get; }

    public Args()
    {
        var args = Environment.GetCommandLineArgs();

        if (args is not [_, { } portArg, { } playerCountArg, .. { } rest]
            || !int.TryParse(portArg, out var port)
            || !int.TryParse(playerCountArg, out var playerCount)
           )
            throw new InvalidOperationException("Invalid port argument");

        OtherArgs = rest;
        Port = port;
        PlayerCount = playerCount;
    }

    public bool IsForSpectate(out IPEndPoint hostEndpoint)
    {
        hostEndpoint = null!;
        return OtherArgs is ["spectate", { } hostArg] && IPEndPoint.TryParse(hostArg, out hostEndpoint!);
    }

    public bool IsForPlay(out NetcodePlayer[] players)
    {
        if (OtherArgs is not ["play", .. var endpoints])
        {
            players = [];
            return false;
        }

        players = GetPlayers(endpoints);
        return true;
    }

    static NetcodePlayer[] GetPlayers(string[] args)
    {
        var players = args.Select(ParsePlayer).ToArray();

        if (!players.Any(x => x.IsLocal()))
            throw new InvalidOperationException("No defined local player");

        return players;
    }

    static NetcodePlayer ParsePlayer(string address)
    {
        if (address.Equals("local", StringComparison.OrdinalIgnoreCase))
            return NetcodePlayer.CreateLocal();

        if (address.StartsWith("s:", StringComparison.OrdinalIgnoreCase))
            if (IPEndPoint.TryParse(address[2..], out var hostEndPoint))
                return NetcodePlayer.CreateSpectator(hostEndPoint);
            else
                throw new InvalidOperationException("Invalid spectator endpoint");

        if (IPEndPoint.TryParse(address, out var endPoint))
        {
            return NetcodePlayer.CreateRemote(endPoint);
        }

        throw new InvalidOperationException($"Invalid player argument: {address}");
    }
}
