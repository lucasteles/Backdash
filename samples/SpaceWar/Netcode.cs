using System.Net;
using Backdash;
using Backdash.Core;
using Backdash.Synchronizing.Input;
using Backdash.Synchronizing.Input.Confirmed;
using SpaceWar.Logic;

namespace SpaceWar;

public static class Netcode
{
    public static INetcodeSession<PlayerInputs> ParseArgs(string[] args)
    {
        if (args is not [{ } portArg, { } playerCountArg, .. { } lastArgs]
            || !int.TryParse(portArg, out var port)
            || !int.TryParse(playerCountArg, out var playerCount))
            throw new InvalidOperationException("Invalid port argument");

        if (playerCount > Config.MaxShips)
            throw new InvalidOperationException("Too many players");

        // create rollback session builder
        var builder = RollbackNetcode
                .WithInputType<PlayerInputs>()
                .WithPort(port)
                .WithPlayerCount(playerCount)
                .WithInputDelayFrames(2)
                .WithLogLevel(LogLevel.Warning)
                .ConfigureProtocol(options =>
                {
                    options.NumberOfSyncRoundtrips = 10;
                    options.DisconnectTimeout = TimeSpan.FromSeconds(3);
                    options.DisconnectNotifyStart = TimeSpan.FromSeconds(1);
                    options.LogNetworkStats = false;
                    // options.NetworkLatency = Backdash.Data.FrameSpan.Of(3).Duration();
                })
            ;

        switch (lastArgs)
        {
            case ["local-only", ..]:
                return builder
                    .ForLocal()
                    .Build();

            case ["spectate", { } hostArg] when IPEndPoint.TryParse(hostArg, out var host):
                return builder
                    .ForSpectator(options => options.HostEndPoint = host)
                    .Build();

            case ["replay", { } replayFile]:
                return builder
                    .ForReplay(options =>
                        options.InputList = SaveInputsToFileListener.GetInputs(playerCount, replayFile).ToArray()
                    )
                    .Build();

            case ["sync-test", ..]:
                return builder
                    .ForSyncTest(options => options
                        .UseJsonStateViewer()
                        .UseRandomInputProvider()
                    )
                    .Build();

            // defaults to remote session
            default:
                // save confirmed inputs to file
                if (lastArgs is ["--save-to", { } filename, .. var argsAfterSave])
                {
                    builder.WithInputListener(new SaveInputsToFileListener(filename));
                    lastArgs = argsAfterSave;
                }

                var players = lastArgs.Select((x, i) => ParsePlayer(playerCount, i + 1, x)).ToArray();
                var localPlayer = players.FirstOrDefault(x => x.IsLocal());

                if (localPlayer is null)
                    throw new InvalidOperationException("No local player defined");

                var session = RollbackNetcode.CreateSession<PlayerInputs>(port, options, new()
                {
                    // LogWriter = new Backdash.Core.FileTextLogWriter($"log_{localPlayer.Number}.log"),
                    InputListener = saveInputsListener,
                });

                session.AddPlayers(players);

                return session;
        }
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
