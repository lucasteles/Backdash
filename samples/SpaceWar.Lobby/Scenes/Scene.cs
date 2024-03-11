#nullable disable
using SpaceWar.Util;

namespace SpaceWar.Scenes;

public abstract class Scene : IDisposable
{
    protected GameServiceContainer Services { get; private set; }
    protected Rectangle Viewport { get; private set; }
    protected GameAssets Assets { get; private set; }
    protected GameWindow Window { get; private set; }
    protected AppSettings Config { get; private set; }

    public void Configure(Game1 game)
    {
        Services = game.Services;
        Viewport = game.GraphicsDevice.Viewport.Bounds;
        Assets = game.Services.GetService<GameAssets>();
        Config = Services.GetService<AppSettings>();
        Window = game.Window;
    }

    protected void LoadScene(Scene scene)
    {
        var sceneManager = Services.GetService<SceneManager>();
        sceneManager.LoadScene(scene);
    }

    public abstract void Initialize();
    public abstract void Update(GameTime gameTime);
    public abstract void Draw(SpriteBatch spriteBatch);

    protected virtual void Dispose(bool disposing) { }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
