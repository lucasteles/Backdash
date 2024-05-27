#nullable disable
using Backdash;
using Backdash.Core;
using Backdash.Synchronizing;
using SpaceWar.Logic;

namespace SpaceWar;

public class Game1 : Game
{
    readonly GraphicsDeviceManager graphics;
    readonly IRollbackSession<PlayerInputs, GameState> rollbackSession;
    readonly SessionReplayControl replayControls = new();
    readonly KeyboardController keyboard = new();

    readonly RollbackOptions options = new()
    {
        FrameDelay = 2,
        Log = new()
        {
            EnabledLevel = LogLevel.Information,
        },
        Protocol = new()
        {
            NumberOfSyncRoundtrips = 10,
            DisconnectTimeout = TimeSpan.FromSeconds(3),
            DisconnectNotifyStart = TimeSpan.FromSeconds(1),
            LogNetworkStats = false,
            // NetworkDelay = FrameSpan.Of(3).Duration(),
        },
    };

    GameSession gameSession;
    SpriteBatch spriteBatch;
    Matrix scaleMatrix = Matrix.CreateScale(1);

    public Game1(string[] args)
    {
        graphics = new(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        rollbackSession = GameSessionFactory.ParseArgs(args, options, replayControls);
    }

    protected override void Initialize()
    {
        SetResolution();
        base.Initialize();
        rollbackSession.Start();
    }

    void SetResolution()
    {
        var screen = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
        var windowSize = Config.InternalBounds;

        // adjust size for monitor resolution
        if (windowSize.Width * 2 >= screen.Width || windowSize.Height * 2 >= screen.Height)
        {
            windowSize.Width = 640;
            windowSize.Height = 480;

            scaleMatrix = Matrix.CreateScale(
                windowSize.Width / (float)Config.InternalWidth,
                windowSize.Height / (float)Config.InternalHeight,
                1f
            );
        }

        graphics.PreferredBackBufferWidth = windowSize.Width;
        graphics.PreferredBackBufferHeight = windowSize.Height;
        graphics.ApplyChanges();
    }

    protected override void Dispose(bool disposing)
    {
        rollbackSession.Dispose();
        base.Dispose(disposing);
    }

    protected override void LoadContent()
    {
        spriteBatch = new(GraphicsDevice);
        GameAssets assets = new(Content, GraphicsDevice);
        var numPlayers = rollbackSession.NumberOfPlayers;
        NonGameState ngs = new(numPlayers);
        GameState gs = new();
        gs.Init(numPlayers);

        foreach (var player in rollbackSession.GetPlayers())
        {
            PlayerConnectionInfo playerInfo = new();
            ngs.Players[player.Index] = playerInfo;
            playerInfo.Handle = player;
            playerInfo.Name = $"player{player.Number}";
            if (player.IsLocal())
            {
                playerInfo.ConnectProgress = 100;
                ngs.LocalPlayerHandle = player;
                ngs.SetConnectState(player, PlayerConnectState.Connecting);
                ConfigurePlayerWindow(player);
            }

            ngs.StatusText.Clear();
            ngs.StatusText.Append("Connecting to peers ...");
        }

        if (rollbackSession.Mode is SessionMode.Spectating)
        {
            Window.Title = "SpaceWar - Spectator";
            Window.Position = Window.Position with
            {
                X = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width -
                    GraphicsDevice.Viewport.Width,
            };
        }

        gameSession = new(gs, ngs, new(assets, spriteBatch), rollbackSession);
        rollbackSession.SetHandler(gameSession);
    }

    void ConfigurePlayerWindow(PlayerHandle player)
    {
        Window.Title = $"SpaceWar - Player {player.Number}";
        if (graphics.IsFullScreen) return;
        const int titleBarHeight = 50;
        Point padding = new(10, titleBarHeight);
        var size = GraphicsDevice.Viewport.Bounds.Size;
        var display = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;

        var newPosition = player.Number switch
        {
            1 => padding,
            2 => new(padding.X + size.X, padding.Y),
            3 => new(padding.X, size.Y + padding.Y + titleBarHeight),
            4 => new(padding.X + size.X, size.Y + padding.Y + titleBarHeight),
            _ => Window.Position,
        };

        newPosition.X = MathHelper.Clamp(newPosition.X, 0, display.Width - size.X);
        newPosition.Y = MathHelper.Clamp(newPosition.Y, 0, display.Height - size.Y);
        Window.Position = newPosition;
    }

    protected override void Update(GameTime gameTime)
    {
        keyboard.Update();
        if (keyboard.IsKeyPressed(Keys.Escape))
            Exit();

        HandleReplayKeys();

        gameSession.Update(gameTime);
        base.Update(gameTime);
    }

    void HandleReplayKeys()
    {
        if (rollbackSession.Mode is not SessionMode.Replaying)
            return;

        if (keyboard.IsKeyPressed(Keys.Space))
            replayControls.TogglePause();

        if (keyboard.IsKeyPressed(Keys.Right))
            replayControls.Play();

        if (keyboard.IsKeyPressed(Keys.Left))
            replayControls.Play(backwards: true);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);
        spriteBatch.Begin(transformMatrix: scaleMatrix);
        gameSession.Draw();
        spriteBatch.End();
        base.Draw(gameTime);
    }
}
