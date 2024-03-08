#nullable disable

namespace SpaceWar;

public class Game1 : Game
{
    readonly GraphicsDeviceManager graphics;

    public SpriteBatch SpriteBatch { get; private set; }
    public GameAssets Assets { get; private set; }

    public Game1()
    {
        graphics = new(this);
        Content.RootDirectory = "Content";
        Window.Title = "SpaceWar";
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
        SpriteBatch = new(GraphicsDevice);
        Assets = new(Content, GraphicsDevice);
    }

    protected override void Update(GameTime gameTime)
    {
        if (Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);
        SpriteBatch.Begin();

        SpriteBatch.End();
        base.Draw(gameTime);
    }
}
