using System.Numerics;
using Backdash.Synchronizing.Random;

namespace ConsoleGame;

public static class GameLogic
{
    public const int GridSize = 5;
    public static readonly Vector2 UpLeft = Vector2.Zero;
    public static readonly Vector2 DownRight = new(GridSize - 1);
    public static readonly Vector2 Center = new(MathF.Floor(GridSize / 2f));

    public static GameState InitialState() => new()
    {
        Position1 = UpLeft,
        Position2 = DownRight,
        Score1 = 0,
        Score2 = 0,
        Target = Center,
    };

    public static void Update(
        INetcodeRandom random,
        ref GameState currentState,
        GameInput inputPlayer1,
        GameInput inputPlayer2
    )
    {
        currentState.RandomSeed = random.CurrentSeed;
        currentState.Position1 = Move(in currentState.Position1, in currentState.Position2, inputPlayer1);
        currentState.Position2 = Move(in currentState.Position2, in currentState.Position1, inputPlayer2);

        var player1Scored = currentState.Position1 == currentState.Target;
        var player2Scored = currentState.Position2 == currentState.Target;

        if (player1Scored && !player2Scored)
            currentState.Score1++;
        if (player2Scored && !player1Scored)
            currentState.Score2++;

        if (player1Scored || player2Scored)
        {
            Vector2 candidate = currentState.Target;
            while (
                candidate == currentState.Target
                || candidate == currentState.Position1
                || candidate == currentState.Position2
            )
                candidate = new(
                    random.NextInt(GridSize),
                    random.NextInt(GridSize)
                );

            currentState.Target = candidate;
        }
    }

    public static Vector2 Move(in Vector2 pos, in Vector2 notAllowed, GameInput input)
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

        var next = Vector2.Clamp(pos + direction, Vector2.Zero, new(GridSize - 1));
        return next == notAllowed ? pos : next;
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
            ConsoleKey.LeftArrow or ConsoleKey.A => GameInput.Left,
            ConsoleKey.RightArrow or ConsoleKey.D => GameInput.Right,
            ConsoleKey.UpArrow or ConsoleKey.W => GameInput.Up,
            ConsoleKey.DownArrow or ConsoleKey.S => GameInput.Down,
            _ => GameInput.None,
        };
    }
}
