using Microsoft.Xna.Framework.Content;
namespace SpaceWar;
public record ShipAsset(Texture2D Ship, Texture2D Missile);
public class GameAssets(ContentManager content, GraphicsDevice graphics)
{
    public readonly SpriteFont MainFont = content.Load<SpriteFont>("ui");
    public readonly Texture2D Shot = content.Load<Texture2D>("shot");
    public readonly Texture2D Thrust = content.Load<Texture2D>("thrust");
    public readonly Texture2D ExplosionSheet = content.Load<Texture2D>("explosion1");
    public readonly Texture2D Star = content.Load<Texture2D>("star_small");
    public readonly Texture2D StarBig = content.Load<Texture2D>("star_big");
    public readonly Texture2D Blank = CreateColorTexture(graphics, Color.White);
    public readonly ShipAsset[] Ships =
    [
        new(content.Load<Texture2D>("ship01"), content.Load<Texture2D>("bomb01")),
        new(content.Load<Texture2D>("ship02"), content.Load<Texture2D>("bomb02")),
        new(content.Load<Texture2D>("ship03"), content.Load<Texture2D>("bomb03")),
        new(content.Load<Texture2D>("ship04"), content.Load<Texture2D>("bomb04")),
    ];
    static Texture2D CreateColorTexture(GraphicsDevice graphicsDevice, Color color)
    {
        Texture2D result = new(graphicsDevice, 1, 1, false, SurfaceFormat.Color);
        result.SetData([color]);
        return result;
    }
}