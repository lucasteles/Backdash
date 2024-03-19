using Backdash.Core;

namespace Backdash;

/// <summary>
/// Results for <see cref="IRollbackSession{TInput}"/> operations.
/// </summary>
public enum ResultCode : short
{
    /// <summary>Operation succeed.</summary>
    Ok = 0,

    /// <summary>When <see cref="PlayerHandle"/> was not valid for session.</summary>
    InvalidPlayerHandle,

    /// <summary>When <see cref="PlayerHandle.Index"/> was not known by session.</summary>
    PlayerOutOfRange,

    /// <summary>When emulator reached prediction barrier.</summary>
    /// <seealso cref="RollbackOptions.PredictionFrames"/>
    /// <seealso cref="IRollbackSession{TInput}.AddLocalInput"/>
    PredictionThreshold,

    /// <summary>The synchronization with peer was not finished.</summary>
    NotSynchronized,

    /// <summary>Session is in rollback state.</summary>
    InRollback,

    /// <summary>Unable to send input.</summary>
    InputDropped,

    /// <summary>Max number of spectators reached.</summary>
    /// <seealso cref="RollbackOptions"/>
    /// <inheritdoc cref="Max.NumberOfSpectators"/>
    TooManySpectators,

    /// <summary>
    /// Max number of players reached.
    /// <seealso cref="RollbackOptions"/>
    /// </summary>
    /// <inheritdoc cref="Max.NumberOfPlayers"/>
    TooManyPlayers,

    /// <summary>The operations need to requested before synchronization starts.</summary>
    /// <seealso cref="IRollbackSession{TInput, TGameState}.Start"/>
    AlreadySynchronized,

    /// <summary>The <see cref="PlayerHandle"/> is already added to the session.</summary>
    DuplicatedPlayer,

    /// <summary>The current session type not support the requested operation.</summary>
    NotSupported,
}
