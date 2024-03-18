namespace SpaceWar;

public static class Extensions
{
    public static Vector2 RoundTo(this Vector2 vector, int digits = 2) =>
        new(MathF.Round(vector.X, digits), MathF.Round(vector.Y, digits));
}
