using System.Numerics;
namespace ConsoleGame;

public static class GameLogic
{
    public const int GridSize = 5;
    static readonly Vector2 UpLeft = Vector2.Zero;
    static readonly Vector2 DownRight = new(GridSize - 1);
    static readonly Vector2 Center = new(MathF.Floor(GridSize / 2f));
    public static GameState InitialState() => new()
    {
        Position1 = UpLeft,
        Position2 = DownRight,
        Score1 = 0,
        Score2 = 0,
        Target = Center,
    };
    public static void AdvanceState(
        ref GameState currentState,
        GameInput inputPlayer1,
        GameInput inputPlayer2
    )
    {
        currentState.Position1 = Move(currentState.Position1, inputPlayer1);
        currentState.Position2 = Move(currentState.Position2, inputPlayer2);
        var player1Scored = currentState.Position1 == currentState.Target;
        var player2Scored = currentState.Position2 == currentState.Target;
        if (player1Scored && !player2Scored)
            currentState.Score1++;
        if (player2Scored && !player1Scored)
            currentState.Score2++;
        if (player1Scored || player2Scored)
        {
            var currentTarget = currentState.Target;
            var maxDistance = 0;
            for (var col = 0; col < GridSize; col++)
            {
                for (var row = 0; row < GridSize; row++)
                {
                    Vector2 candidate = new(col, row);
                    var distance1 = (int)Vector2.Distance(currentState.Position1, candidate);
                    var distance2 = (int)Vector2.Distance(currentState.Position2, candidate);
                    var distanceT = (int)Vector2.Distance(candidate, currentTarget);
                    if (MathF.Abs(distance1 - distance2) > 1 || distanceT < maxDistance)
                        continue;
                    maxDistance = distanceT;
                    currentState.Target = candidate;
                }
            }
        }
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
        return Vector2.Clamp(pos, Vector2.Zero, new Vector2(GridSize - 1));
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
