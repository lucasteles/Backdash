using SpaceWar.Models;

namespace SpaceWar.Scenes;

public sealed class LobbyScene(string username, PlayerMode mode) : Scene
{
    public const string LobbyName = "spacewar";

    public override void Initialize() { }

    public override void Update(GameTime gameTime) { }

    public override void Draw(SpriteBatch spriteBatch) { }
}
