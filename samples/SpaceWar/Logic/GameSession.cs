using Backdash.Data;

namespace SpaceWar.Logic;

public class GameSession(GameState gameState, NonGameState nonGameState, Renderer renderer)
{
    readonly SynchronizedInput<PlayerInputs>[] inputs =
        new SynchronizedInput<PlayerInputs>[nonGameState.NumberOfPlayers];

    public void Update(GameTime gameTime)
    {
        var keyboard = Keyboard.GetState();
        var localInput = Inputs.ReadInputs(keyboard);

        inputs[0] = localInput;
        gameState.Update(inputs);
    }

    public void Draw() => renderer.Draw(gameState, nonGameState);
}