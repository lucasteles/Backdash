namespace SpaceWar.Logic;

[Flags]
public enum PlayerInputs : byte
{
    None = 0,
    Thrust = 1 << 0,
    Break = 1 << 1,
    RotateLeft = 1 << 2,
    RotateRight = 1 << 3,
    Fire = 1 << 4,
    Bomb = 1 << 5,
}

public readonly record struct GameInput(
    float Heading,
    float Thrust,
    bool Fire
);

public static class Inputs
{
    public static PlayerInputs ReadInputs(KeyboardState keyboardState)
    {
        var result = PlayerInputs.None;

        if (keyboardState.IsKeyDown(Keys.Up) || keyboardState.IsKeyDown(Keys.W))
            result |= PlayerInputs.Thrust;

        if (keyboardState.IsKeyDown(Keys.Down) || keyboardState.IsKeyDown(Keys.S))
            result |= PlayerInputs.Break;

        if (keyboardState.IsKeyDown(Keys.Left) || keyboardState.IsKeyDown(Keys.A))
            result |= PlayerInputs.RotateLeft;

        if (keyboardState.IsKeyDown(Keys.Right) || keyboardState.IsKeyDown(Keys.D))
            result |= PlayerInputs.RotateRight;

        if (keyboardState.IsKeyDown(Keys.Space))
            result |= PlayerInputs.Fire;

        if (keyboardState.IsKeyDown(Keys.Enter))
            result |= PlayerInputs.Bomb;

        return result;
    }
}