using System.Net;
using Backdash;
using Backdash.Core;
using SpaceWar.Logic;

namespace SpaceWar.Scenes;

public sealed class BattleFieldScene : IDisposable
{
    readonly GameAssets assets;
    readonly IRollbackSession<PlayerInputs, GameState> rollbackSession;
    GameSession gameSession = null!;

    readonly RollbackOptions options = new()
    {
        FrameDelay = 2,
        Log = new()
        {
            EnabledLevel = LogLevel.Error,
        },
        Protocol = new()
        {
            NumberOfSyncPackets = 10,
            DisconnectTimeout = TimeSpan.FromSeconds(3),
            DisconnectNotifyStart = TimeSpan.FromSeconds(1),
            LogNetworkStats = true,
        },
    };


    public BattleFieldScene(
        GameAssets assets,
        int port,
        IReadOnlyList<Player> players
    )
    {
        this.assets = assets;

        var localPlayer = players.FirstOrDefault(x => x.IsLocal());
        if (localPlayer is null)
            throw new InvalidOperationException("No local player defined");

        rollbackSession = RollbackNetcode.CreateSession<PlayerInputs, GameState>(port, options);
        rollbackSession.AddPlayers(players);
    }

    public BattleFieldScene(
        GameAssets assets,
        int port,
        int playerCount,
        IPEndPoint host
    )
    {
        this.assets = assets;
        rollbackSession = RollbackNetcode.CreateSpectatorSession<PlayerInputs, GameState>(
            port, host, playerCount, options
        );
    }

    public void Initialize(Game1 game)
    {
        var numPlayers = rollbackSession.NumberOfPlayers;
        GameState gs = new();
        gs.Init(game.Window, numPlayers);
        NonGameState ngs = new(numPlayers, game.Window);
        foreach (var player in rollbackSession.GetPlayers())
        {
            PlayerConnectionInfo playerInfo = new();
            ngs.Players[player.Index] = playerInfo;
            playerInfo.Handle = player;
            if (player.IsLocal())
            {
                playerInfo.ConnectProgress = 100;
                ngs.LocalPlayerHandle = player;
                ngs.SetConnectState(player, PlayerConnectState.Connecting);
            }

            ngs.StatusText.Clear();
            ngs.StatusText.Append("Connecting to peers ...");
        }

        if (rollbackSession.IsSpectating)
            gameSession = new(gs, ngs, new(assets, game.SpriteBatch), rollbackSession);

        rollbackSession.SetHandler(gameSession);

        rollbackSession.Start();
    }

    public void Update(GameTime gameTime) => gameSession.Update(gameTime);
    public void Draw() => gameSession.Draw();
    public void Dispose() => rollbackSession.Dispose();
}
