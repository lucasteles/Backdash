using Backdash;
using SpaceWar.Logic;
using SpaceWar.Models;

namespace SpaceWar.Scenes;

public sealed class BattleSessionScene : Scene
{
    readonly IReadOnlyList<Peer> peersInfo;
    readonly INetcodeSession<PlayerInputs> netcodeSession;
    GameSession gameSession = null!;

    public BattleSessionScene(
        INetcodeSession<PlayerInputs> netcodeSession,
        IReadOnlyList<Peer> peersInfo
    )
    {
        this.peersInfo = peersInfo;
        this.netcodeSession = netcodeSession;

        if (netcodeSession.IsRemote() && !netcodeSession.TryGetLocalPlayer(out _))
            throw new InvalidOperationException("No local player defined");
    }

    public override void Initialize()
    {
        var numPlayers = netcodeSession.NumberOfPlayers;
        NonGameState ngs = new(numPlayers);
        GameState gs = new();
        gs.Init(numPlayers);

        foreach (var player in netcodeSession.GetPlayers())
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
        gameSession = new(gs, ngs, new(Assets, spriteBatch), netcodeSession);
        netcodeSession.SetHandler(gameSession);
        netcodeSession.Start();
    }

    public override void Update(GameTime gameTime) => gameSession.Update(gameTime.TotalGameTime);
    public override void Draw(SpriteBatch spriteBatch) => gameSession.Draw();
    protected override void Dispose(bool disposing) => netcodeSession.Dispose();
}
