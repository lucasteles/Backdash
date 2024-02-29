#nullable disable

using System.Net;
using Backdash;
using Backdash.Core;
using SpaceWar.Logic;

namespace SpaceWar;

public class Game1 : Game
{
    readonly GraphicsDeviceManager graphics;
    readonly IRollbackSession<PlayerInputs, GameState> rollbackSession;

    readonly RollbackOptions options = new()
    {
        FrameDelay = 2,
        Log = new()
        {
            EnabledLevel = LogLevel.Information,
        },
        Protocol = new()
        {
            NumberOfSyncPackets = 10,
        },
    };

    GameSession gameSession;
    SpriteBatch spriteBatch;

    public Game1(string[] args)
    {
        graphics = new(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        rollbackSession = GameSessionParser.ParseArgs(args, options);
    }

    protected override void Initialize()
    {
        graphics.PreferredBackBufferWidth = 1024;
        graphics.PreferredBackBufferHeight = 768;
        graphics.ApplyChanges();
        base.Initialize();
        rollbackSession.Start();
    }

    protected override void LoadContent()
    {
        spriteBatch = new(GraphicsDevice);
        GameAssets assets = new(Content, GraphicsDevice);

        var numPlayers = rollbackSession.NumberOfPlayers;
        GameState gs = new();
        gs.Init(Window, numPlayers);

        NonGameState ngs = new(numPlayers, Window);
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
                Window.Title = $"SpaceWar - Player {player.Number}";
            }
        }

        gameSession = new(gs, ngs, new(assets, spriteBatch), rollbackSession);
        rollbackSession.SetHandler(gameSession);
    }

    protected override void Update(GameTime gameTime)
    {
        if (Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        gameSession.Update(gameTime);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);
        spriteBatch.Begin();

        gameSession.Draw();

        spriteBatch.End();
        base.Draw(gameTime);
    }

    protected override void Dispose(bool disposing)
    {
        rollbackSession.Dispose();
        base.Dispose(disposing);
    }
}