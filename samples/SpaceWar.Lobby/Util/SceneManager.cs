using SpaceWar.Scenes;

namespace SpaceWar.Util;

public sealed class SceneManager : IDisposable
{
    Scene currentScene;
    readonly Game1 game;

    public SceneManager(Game1 game, Scene startScene)
    {
        this.game = game;
        currentScene = startScene;
        currentScene.Configure(this.game);
        currentScene.Initialize();
    }

    public void LoadScene(Scene newScene)
    {
        currentScene.Dispose();
        currentScene = newScene;
        currentScene.Configure(game);
        currentScene.Initialize();
    }

    public void Dispose() => currentScene.Dispose();
    public void Update(GameTime gameTime) => currentScene.Update(gameTime);
    public void Draw(SpriteBatch spriteBatch) => currentScene.Draw(spriteBatch);
}
