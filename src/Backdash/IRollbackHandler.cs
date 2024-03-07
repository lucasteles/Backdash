using Backdash.Core;
using Backdash.Data;
namespace Backdash;
/*
 * The SessionCallbacks contains the callback functions that
 * your application must implement.  Backdash will periodically call these
 * functions during the game.  All callback functions must be implemented.
 */
public interface IRollbackHandler<TState> where TState : notnull
{
    void OnSessionStart();
    void OnSessionClose();
    /*
     * The client should return a copy of the entire contents of the current game state
     */
    void SaveState(in Frame frame, ref TState state);
    /*
     * Backdash will call this function at the beginning  of a rollback.
     */
    void LoadState(in Frame frame, in TState gameState);
    void ClearState(ref TState gameState)
    {
        // NOP
    }
    /*
     * advance_frame - Called during a rollback.  You should advance your game
     * state by exactly one frame.  Before each frame, call SynchronizeInput
     * to retrieve the inputs you should use for that frame.  After each frame,
     * you should call AdvanceFrame to notify Backdash that you're
     * finished.
     */
    void AdvanceFrame();
    void TimeSync(FrameSpan framesAhead);
    void OnPeerEvent(PlayerHandle player, PeerEventInfo evt);
}
sealed class EmptySessionHandler<TState>(Logger logger)
    : IRollbackHandler<TState> where TState : notnull, new()
{
    public void OnSessionStart() =>
        logger.Write(LogLevel.Information, $"{DateTime.UtcNow:o} [Session Handler] Running.");
    public void OnSessionClose() =>
        logger.Write(LogLevel.Information, $"{DateTime.UtcNow:o} [Session Handler] Closing.");
    public void SaveState(in Frame frame, ref TState state) =>
        logger.Write(LogLevel.Information,
            $"{DateTime.UtcNow:o} [Session Handler] {nameof(SaveState)} called for frame {frame}");
    public void LoadState(in Frame frame, in TState gameState) =>
        logger.Write(LogLevel.Information, $"{DateTime.UtcNow:o} [Session Handler] {nameof(LoadState)} called");
    public void AdvanceFrame() =>
        logger.Write(LogLevel.Information, $"{DateTime.UtcNow:o} [Session Handler] {nameof(AdvanceFrame)} called");
    public void TimeSync(FrameSpan framesAhead) =>
        logger.Write(LogLevel.Information,
            $"{DateTime.UtcNow:o} [Session Handler] Need to sync: {framesAhead} frames ahead");
    public void OnPeerEvent(PlayerHandle player, PeerEventInfo evt) =>
        logger.Write(LogLevel.Information,
            $"{DateTime.UtcNow:o} [Session Handler] {nameof(OnPeerEvent)} called with {evt} for {player}");
}
