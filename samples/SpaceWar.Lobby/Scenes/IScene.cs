namespace SpaceWar.Scenes;

public interface IScene : IDisposable
{
    void Initialize(Game1 game);
    void Update(GameTime gameTime);
    void Draw(SpriteBatch spriteBatch);
}
