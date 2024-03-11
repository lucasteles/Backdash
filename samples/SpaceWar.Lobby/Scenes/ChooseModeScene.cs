namespace SpaceWar.Scenes;

public sealed class ChooseModeScene(string username) : Scene
{
    public override void Initialize() { }

    public override void Update(GameTime gameTime) { }

    public override void Draw(SpriteBatch spriteBatch)
    {
        var nameSize = Assets.MainFont.MeasureString(username);
        var halfSize = nameSize / 2;
        var center = Viewport.Center.ToVector2();

        spriteBatch.DrawString(Assets.MainFont, username, center, Color.Red,
            0, halfSize, 1, SpriteEffects.None, 0);
    }
}
