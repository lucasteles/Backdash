#nullable disable

using SpaceWar.Scenes;

namespace SpaceWar;

public class Game1 : Game
{
    readonly GraphicsDeviceManager graphics;
    public SpriteBatch SpriteBatch { get; private set; }
    public GameAssets Assets { get; private set; }
    public SceneManager Scene { get; }

    public Game1()
    {
        graphics = new(this);
        Content.RootDirectory = "Content";
        Window.Title = "SpaceWar";
        IsMouseVisible = true;
        Scene = new(this, new SelectModeScene());
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

        Scene.Update(gameTime);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);
        SpriteBatch.Begin();
        Scene.Draw(SpriteBatch);
        SpriteBatch.End();
        base.Draw(gameTime);
    }

    protected override void Dispose(bool disposing)
    {
        Scene.Dispose();
        base.Dispose(disposing);
    }
}
