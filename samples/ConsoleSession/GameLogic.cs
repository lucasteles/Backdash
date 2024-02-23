using System.Numerics;

namespace ConsoleSession;

public static class GameLogic
{
    public static readonly Vector2 UpLeft = Vector2.Zero;
    public static readonly Vector2 DownRight = new Vector2(View.GridSize) - Vector2.One;

    public static GameState InitialState() => new()
    {
        Position1 = UpLeft,
        Position2 = DownRight,
    };

    public static void AdvanceState(
        ref GameState currentState,
        GameInput inputPlayer1,
        GameInput inputPlayer2
    )
    {
        currentState.Position1 = Move(currentState.Position1, inputPlayer1);
        currentState.Position2 = Move(currentState.Position2, inputPlayer2);
    }

    public static Vector2 Move(Vector2 pos, GameInput input)
    {
        var direction = Vector2.Zero;
        if (input.HasFlag(GameInput.Up))
            direction = -Vector2.UnitY;

        if (input.HasFlag(GameInput.Right))
            direction = Vector2.UnitX;

        if (input.HasFlag(GameInput.Down))
            direction = Vector2.UnitY;

        if (input.HasFlag(GameInput.Left))
            direction = -Vector2.UnitX;

        pos += direction;
        return Vector2.Clamp(pos, Vector2.Zero, new Vector2(View.GridSize - 1));
    }

    public static GameInput ReadKeyboardInput(out bool disconnectRequest)
    {
        disconnectRequest = false;

        if (!Console.KeyAvailable)
            return GameInput.None;

        var press = Console.ReadKey(true);

        // force delay while holding space
        if (press.Key is ConsoleKey.Spacebar)
        {
            Thread.Sleep(100);
            return GameInput.None;
        }

        if (press.Key is ConsoleKey.Escape)
            disconnectRequest = true;

        return press.Key switch
        {
            ConsoleKey.LeftArrow => GameInput.Left,
            ConsoleKey.RightArrow => GameInput.Right,
            ConsoleKey.UpArrow => GameInput.Up,
            ConsoleKey.DownArrow => GameInput.Down,
            _ => GameInput.None,
        };
    }
}