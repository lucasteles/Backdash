using System.Net;
using Backdash;
using Backdash.Core;
using SpaceWar.Logic;
using SpaceWar.Models;

namespace SpaceWar.Scenes;

public sealed class BattleSessionScene : Scene
{
    readonly IReadOnlyList<Peer> peersInfo;
    readonly INetcodeSession<PlayerInputs> rollbackSession;
    GameSession gameSession = null!;

    readonly NetcodeOptions options = new()
    {
        FrameDelay = 2,
        Log = new()
        {
            EnabledLevel = LogLevel.Warning,
        },
        Protocol = new()
        {
            NumberOfSyncRoundtrips = 10,
            DisconnectTimeout = TimeSpan.FromSeconds(3),
            DisconnectNotifyStart = TimeSpan.FromSeconds(1),
            LogNetworkStats = false,
        },
    };

    public BattleSessionScene(int port, IReadOnlyList<Player> players,
        IReadOnlyList<Peer> peersInfo)
    {
        this.peersInfo = peersInfo;
        var localPlayer = players.FirstOrDefault(x => x.IsLocal());
        if (localPlayer is null)
            throw new InvalidOperationException("No local player defined");

        rollbackSession = RollbackNetcode.CreateSession<PlayerInputs>(port, options);
        rollbackSession.AddPlayers(players);
    }

    public BattleSessionScene(
        int port, int playerCount,
        IPEndPoint host,
        IReadOnlyList<Peer> peersInfo
    )
    {
        this.peersInfo = peersInfo;
        rollbackSession = RollbackNetcode.CreateSpectatorSession<PlayerInputs>(
            port, host, playerCount, options
        );
    }

    public override void Initialize()
    {
        var numPlayers = rollbackSession.NumberOfPlayers;
        NonGameState ngs = new(numPlayers);
        GameState gs = new();
        gs.Init(numPlayers);

        foreach (var player in rollbackSession.GetPlayers())
        {
            PlayerConnectionInfo playerInfo = new();
            ngs.Players[player.Index] = playerInfo;
            playerInfo.Handle = player;
            playerInfo.Name = peersInfo[player.Index].Username;
            if (player.IsLocal())
            {
                playerInfo.ConnectProgress = 100;
                ngs.SetConnectState(player, PlayerConnectState.Connecting);

                if (ngs.LocalPlayerHandle is null)
                    ngs.LocalPlayerHandle = player;
                // used for local session, 2nd player that mirrors the player 1
                else if (ngs.MirrorPlayerHandle is null)
                    ngs.MirrorPlayerHandle = player;
                else
                    throw new InvalidOperationException("Too many local players");
            }

            ngs.StatusText.Clear();
            ngs.StatusText.Append("Connecting to peers ...");
        }

        var spriteBatch = Services.GetService<SpriteBatch>();
        gameSession = new(gs, ngs, new(Assets, spriteBatch), rollbackSession);
        rollbackSession.SetHandler(gameSession);
        rollbackSession.Start();
    }

    public override void Update(GameTime gameTime) => gameSession.Update(gameTime);
    public override void Draw(SpriteBatch spriteBatch) => gameSession.Draw();
    protected override void Dispose(bool disposing) => rollbackSession.Dispose();
}
