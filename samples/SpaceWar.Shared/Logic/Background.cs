namespace SpaceWar.Logic;

#pragma warning disable S3887
public record struct BackgroundStar(
    bool Big,
    Vector2 Position
);

public sealed class Background
{
    public readonly BackgroundStar[] StarMap;

    public Background()
    {
        StarMap = new BackgroundStar[80];
        var rand = Random.Shared;
        for (var i = 0; i < StarMap.Length; i++)
        {
            StarMap[i].Position = new(
                rand.NextSingle() * Config.InternalWidth,
                rand.NextSingle() * Config.InternalHeight
            );
            StarMap[i].Big = rand.NextSingle() > 0.95f;
        }
    }
}
