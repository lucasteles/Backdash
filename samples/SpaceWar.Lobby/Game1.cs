#nullable disable

using SpaceWar.Scenes;
using SpaceWar.Util;

namespace SpaceWar;

public class Game1 : Game
{
    readonly AppSettings appSettings;
    readonly GraphicsDeviceManager graphics;
    public SpriteBatch SpriteBatch { get; private set; }
    public SceneManager SceneManager { get; private set; }

    public Game1(AppSettings appSettings)
    {
        this.appSettings = appSettings;
        graphics = new(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        Window.Title = "SpaceWar";
        graphics.PreferredBackBufferWidth = 1024;
        graphics.PreferredBackBufferHeight = 768;
        graphics.ApplyChanges();
        base.Initialize();
        SceneManager = new(this, startScene: new ChooseNameScene());
        Services.AddService(SceneManager);
        Services.AddService(appSettings);
        Services.AddService(new LobbyClient(appSettings));
    }

    protected override void LoadContent()
    {
        SpriteBatch = new(GraphicsDevice);
        Services.AddService(new GameAssets(Content, GraphicsDevice));
    }

    protected override void Update(GameTime gameTime)
    {
        if (Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        SceneManager.Update(gameTime);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);
        SpriteBatch.Begin();
        SceneManager.Draw(SpriteBatch);
        SpriteBatch.End();
        base.Draw(gameTime);
    }

    protected override void Dispose(bool disposing)
    {
        SceneManager.Dispose();
        base.Dispose(disposing);
    }
}
