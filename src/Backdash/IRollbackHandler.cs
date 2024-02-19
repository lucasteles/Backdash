using Backdash.Core;

namespace Backdash;

/*
 * The SessionCallbacks contains the callback functions that
 * your application must implement.  Backdash will periodically call these
 * functions during the game.  All callback functions must be implemented.
 */
public interface IRollbackHandler<TGameState> where TGameState : notnull
{
    void Start();

    /*
     * The client should return a copy of the entire contents of the current game state
     */
    void SaveGameState(int frame, out TGameState state);

    /*
     * Backdash will call this function at the beginning  of a rollback.
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
    void TimeSync(int framesAhead);

    void OnPeerEvent(PlayerHandle player, PeerEventInfo evt);
}

sealed class EmptySessionHandler<TState>(Logger logger)
    : IRollbackHandler<TState> where TState : notnull
{
    public void Start() =>
        logger.Write(LogLevel.Information, $"{DateTime.UtcNow:o} [Session Handler] Running.");

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

    public void TimeSync(int framesAhead) =>
        logger.Write(LogLevel.Information,
            $"{DateTime.UtcNow:o} [Session Handler] Need to sync: {framesAhead} frames ahead");

    public void OnPeerEvent(PlayerHandle player, PeerEventInfo evt) =>
        logger.Write(LogLevel.Information,
            $"{DateTime.UtcNow:o} [Session Handler] {nameof(OnPeerEvent)} called with {evt} for {player}");
}
