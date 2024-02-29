#nullable disable

using Backdash;
using SpaceWar.Logic;

namespace SpaceWar;

public class Game1 : Game
{
    readonly GraphicsDeviceManager graphics;
    GameSession gameSession;
    SpriteBatch spriteBatch;

    public Game1()
    {
        graphics = new(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        graphics.PreferredBackBufferWidth = 1024;
        graphics.PreferredBackBufferHeight = 768;
        graphics.ApplyChanges();
        base.Initialize();
    }

    protected override void LoadContent()
    {
        spriteBatch = new(GraphicsDevice);

        var numPlayers = 1;
        GameAssets assets = new(Content, GraphicsDevice);
        GameState gs = new();
        gs.Init(Window, numPlayers);
        NonGameState ngs = new(numPlayers, PlayerHandle.Local(1), Window);

        gameSession = new(gs, ngs, new(assets, spriteBatch));
    }

    protected override void Update(GameTime gameTime)
    {
        // if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
        //     Keyboard.GetState().IsKeyDown(Keys.Escape))
        //     Exit();

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
}