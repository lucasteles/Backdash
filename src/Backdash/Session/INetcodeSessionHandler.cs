using Backdash.Core;
using Backdash.Data;
using Backdash.Serialization;

namespace Backdash;

/// <summary>
/// Defines the callback functions that your application must implement.
/// Backdash will periodically call these functions during the game.
/// </summary>
public interface INetcodeSessionHandler
{
    /// <summary>
    /// Called at start of a game session, when all the clients have synchronized.
    /// You may begin sending inputs with.
    /// </summary>
    void OnSessionStart();

    /// <summary>
    /// Called at the end of a game session, before release resources.
    /// </summary>
    void OnSessionClose();

    /// <summary>
    ///  The client should  copy the entire contents of the current game state using <paramref name="writer"/>.
    /// </summary>
    /// <param name="frame">The frame which the save occurs.</param>
    /// <param name="writer">Binary state writer.</param>
    void SaveState(in Frame frame, ref readonly BinaryBufferWriter writer);

    /// <summary>
    /// Backdash will call this function at the beginning  of a rollback.
    /// Binary reader for the state.
    /// </summary>
    /// <param name="frame">The loading frame</param>
    /// <param name="reader">Binary state reader</param>
    void LoadState(in Frame frame, ref readonly BinaryBufferReader reader);

    /// <summary>
    /// Called during a rollback after <see cref="LoadState"/>. You should advance your game
    /// state by exactly one frame.  Before each frame, call <see cref="INetcodeSession{TInput}.SynchronizeInputs"/>
    /// to retrieve the inputs you should use for that frame. After each frame, you should call <see cref="INetcodeSession{TInput}.AdvanceFrame"/> to notify
    /// Backdash that you're finished.
    /// </summary>
    void AdvanceFrame();

    /// <summary>
    /// The time synchronization has determined that this client is too far ahead of the other one
    /// and should slow down to ensure fairness.
    /// </summary>
    /// <param name="framesAhead">Indicates how many frames the client is ahead</param>
    void TimeSync(FrameSpan framesAhead);

    /// <summary>
    /// Notification that some <see cref="PeerEvent"/> has happened for a <see cref="PlayerHandle"/>
    /// </summary>
    /// <param name="player">The player owner of the event</param>
    /// <param name="evt">Event data</param>
    void OnPeerEvent(PlayerHandle player, PeerEventInfo evt);

    /// <summary>
    /// Get string representation of the state
    /// Used for Sync Test logging <see cref="RollbackNetcode.CreateSyncTestSession{TInput}"/>
    /// </summary>
    string GetStateString(in Frame frame, ref readonly BinaryBufferReader reader) =>
        $""""
         --- Begin Hex ---
         {Convert.ToHexString(reader.CurrentBuffer)}
         ---  End Hex  ---
         """";
}

sealed class EmptySessionHandler(Logger logger) : INetcodeSessionHandler
{
    public void OnSessionStart() =>
        logger.Write(LogLevel.Information, $"{DateTime.UtcNow:o} [Session Handler] Running.");

    public void OnSessionClose() =>
        logger.Write(LogLevel.Information, $"{DateTime.UtcNow:o} [Session Handler] Closing.");

    public void SaveState(in Frame frame, ref readonly BinaryBufferWriter writer) =>
        logger.Write(LogLevel.Information,
            $"{DateTime.UtcNow:o} [Session Handler] {nameof(SaveState)} called for frame {frame}");

    public void LoadState(in Frame frame, ref readonly BinaryBufferReader reader) =>
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
