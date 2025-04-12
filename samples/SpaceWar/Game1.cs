#nullable disable
using Backdash;
using SpaceWar.Logic;

namespace SpaceWar;

public class Game1 : Game
{
    readonly GraphicsDeviceManager graphics;
    readonly INetcodeSession<PlayerInputs> session;
    readonly KeyboardController keyboard = new();

    GameSession gameSession;
    SpriteBatch spriteBatch;
    Matrix scaleMatrix = Matrix.CreateScale(1);

    bool paused;

    public Game1(INetcodeSession<PlayerInputs> netcodeSession)
    {
        graphics = new(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        session = netcodeSession;
    }

    protected override void Initialize()
    {
        SetResolution();
        base.Initialize();
        session.Start();
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
        session.Dispose();
        base.Dispose(disposing);
    }

    protected override void LoadContent()
    {
        spriteBatch = new(GraphicsDevice);
        GameAssets assets = new(Content, GraphicsDevice);
        var numPlayers = session.NumberOfPlayers;
        NonGameState ngs = new(numPlayers);
        GameState gs = new();
        gs.Init(numPlayers);

        foreach (var player in session.GetPlayers())
        {
            PlayerConnectionInfo playerInfo = new();
            ngs.Players[player.Index] = playerInfo;
            playerInfo.Handle = player;
            playerInfo.Name = $"player{player.Number}";
            if (player.IsLocal())
            {
                playerInfo.ConnectProgress = 100;
                ngs.SetConnectState(player, PlayerConnectState.Connecting);

                if (ngs.LocalPlayerHandle is null)
                {
                    ConfigurePlayerWindow(player);
                    ngs.LocalPlayerHandle = player;
                }
                // used for local session, 2nd player that mirrors the player 1
                else if (ngs.MirrorPlayerHandle is null)
                    ngs.MirrorPlayerHandle = player;
                else
                    throw new InvalidOperationException("Too many local players");
            }

            ngs.StatusText.Clear();
            ngs.StatusText.Append("Connecting to peers ...");
        }

        if (session.IsSpectator())
        {
            Window.Title = "SpaceWar - Spectator";
            Window.Position = Window.Position with
            {
                X = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width -
                    GraphicsDevice.Viewport.Width,
            };
        }

        gameSession = new(gs, ngs, new(assets, spriteBatch), session);
        session.SetHandler(gameSession);
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

        HandleNonGameKeys();

        if (!paused)
            gameSession.Update(gameTime.ElapsedGameTime);

        base.Update(gameTime);
    }

    void HandleNonGameKeys()
    {
        if (keyboard.IsKeyPressed(Keys.Escape))
            Exit();

        if (session.IsRemote() || session.IsSpectator())
            return;

        if (session.IsReplay())
        {
            if (keyboard.IsKeyPressed(Keys.Space))
            {
                session.ReplayController.TogglePause();
                return;
            }

            if (keyboard.IsKeyPressed(Keys.Right))
            {
                session.ReplayController.Play();
                return;
            }

            if (keyboard.IsKeyPressed(Keys.Left))
            {
                session.ReplayController.Play(isBackwards: true);
                return;
            }

            return;
        }

        if (keyboard.IsKeyPressed(Keys.P))
        {
            paused = !paused;
            return;
        }

        if (!paused) return;

        if (keyboard.IsKeyPressed(Keys.Back))
        {
            session.LoadFrame(session.CurrentFrame - 5);
            return;
        }

        if (keyboard.IsKeyPressed(Keys.Left))
        {
            session.LoadFrame(session.CurrentFrame.Previous());
            return;
        }

        if (keyboard.IsKeyPressed(Keys.Right))
        {
            gameSession.Update(FrameTime.Step);
        }
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
