using Backdash.Core;

namespace Backdash;

/*
 * The SessionCallbacks contains the callback functions that
 * your application must implement.  Backdash will periodically call these
 * functions during the game.  All callback functions must be implemented.
 */
public interface IRollbackHandler<TGameState> where TGameState : notnull
{
    /*
     The client should return a copy of the entire contents of the current game state
     * Optionally, the client can compute a checksum of the data and store it in
     *  the *checksum argument.
     */
    void SaveGameState(int frame, out TGameState state);

    /*
     * Backdash will call this function at the beginning
     * of a rollback.  The buffer and len parameters contain a previously
     * saved state returned from the save_game_state function.  The client
     * should make the current game state match the state contained in the
     * buffer.
     */
    void LoadGameState(in TGameState gameState);

    /*
     * advance_frame - Called during a rollback.  You should advance your game
     * state by exactly one frame.  Before each frame, call SynchronizeInput
     * to retrieve the inputs you should use for that frame.  After each frame,
     * you should call AdvanceFrame to notify Backdash that you're
     * finished.
     */
    void AdvanceFrame();

    /*
     * Notification that something has happened.
     */
    void OnEvent(RollbackEvent evt);
}

sealed class EmptySessionHandler<TState>(Logger logger)
    : IRollbackHandler<TState> where TState : notnull
{
    public void SaveGameState(int frame, out TState state)
    {
        state = default!;
        logger.Write(LogLevel.Information,
            $"{DateTime.UtcNow:o} [Session Handler] {nameof(SaveGameState)} called for frame {frame}");
    }

    public void LoadGameState(in TState gameState) =>
        logger.Write(LogLevel.Information, $"{DateTime.UtcNow:o} [Session Handler] {nameof(LoadGameState)} called");

    public void AdvanceFrame() =>
        logger.Write(LogLevel.Information, $"{DateTime.UtcNow:o} [Session Handler] {nameof(AdvanceFrame)} called");

    public void OnEvent(RollbackEvent evt) =>
        logger.Write(LogLevel.Information,
            $"{DateTime.UtcNow:o} [Session Handler] {nameof(OnEvent)} called with {evt}");
}
