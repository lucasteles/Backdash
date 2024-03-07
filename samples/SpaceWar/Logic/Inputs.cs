namespace SpaceWar.Logic;
[Flags]
public enum PlayerInputs : ushort
{
    None = 0,
    Thrust = 1 << 0,
    Break = 1 << 1,
    RotateLeft = 1 << 2,
    RotateRight = 1 << 3,
    Fire = 1 << 4,
    Missile = 1 << 5,
}
public readonly record struct GameInput(
    float Heading,
    float Thrust,
    bool Fire,
    bool Missile
);
public static class Inputs
{
    public static PlayerInputs ReadInputs(KeyboardState ks)
    {
        var result = PlayerInputs.None;
        if (ks.IsKeyDown(Keys.Up) || ks.IsKeyDown(Keys.W) || ks.IsKeyDown(Keys.K))
            result |= PlayerInputs.Thrust;
        if (ks.IsKeyDown(Keys.Down) || ks.IsKeyDown(Keys.S) || ks.IsKeyDown(Keys.J))
            result |= PlayerInputs.Break;
        if (ks.IsKeyDown(Keys.Left) || ks.IsKeyDown(Keys.A) || ks.IsKeyDown(Keys.H))
            result |= PlayerInputs.RotateLeft;
        if (ks.IsKeyDown(Keys.Right) || ks.IsKeyDown(Keys.D) || ks.IsKeyDown(Keys.L))
            result |= PlayerInputs.RotateRight;
        if (ks.IsKeyDown(Keys.Space))
            result |= PlayerInputs.Fire;
        if (ks.IsKeyDown(Keys.Enter))
            result |= PlayerInputs.Missile;
        return result;
    }
}
