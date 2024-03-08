using SpaceWar.Scenes;

namespace SpaceWar;

public sealed class SceneManager : IDisposable
{
    readonly Game1 game;
    IScene currentScene;

    public SceneManager(Game1 game, IScene currentScene)
    {
        this.game = game;
        this.currentScene = currentScene;
        this.currentScene.Initialize(game);
    }

    public void ChangeScene(IScene newScene)
    {
        currentScene.Dispose();
        newScene.Initialize(game);
        currentScene = newScene;
    }

    public void Dispose() => currentScene.Dispose();
    public void Update(GameTime gameTime) => currentScene.Update(gameTime);
    public void Draw(SpriteBatch spriteBatch) => currentScene.Draw(spriteBatch);
}
