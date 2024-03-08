#nullable disable

namespace SpaceWar;

public class Game1 : Game
{
    readonly GraphicsDeviceManager graphics;
    SpriteBatch spriteBatch;

    public Game1()
    {
        graphics = new(this);
        Content.RootDirectory = "Content";
        Window.Title = "SpaceWar";
        IsMouseVisible = true;
    }

    public SpriteBatch SpriteBatch => spriteBatch;

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
        GameAssets assets = new(Content, GraphicsDevice);
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
        spriteBatch.Begin();

        spriteBatch.End();
        base.Draw(gameTime);
    }
}
