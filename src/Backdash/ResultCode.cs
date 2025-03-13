using Backdash.Core;
using Backdash.Options;

namespace Backdash;

/// <summary>
///     Results for <see cref="INetcodeSession{TInput}" /> operations.
/// </summary>
public enum ResultCode : short
{
    /// <summary>Operation succeed.</summary>
    Ok = 0,

    /// <summary>When <see cref="PlayerHandle" /> was not valid for session.</summary>
    InvalidPlayerHandle,

    /// <summary>When <see cref="PlayerHandle.Index" /> was not known by session.</summary>
    PlayerOutOfRange,

    /// <summary>When emulator reached prediction barrier.</summary>
    /// <seealso cref="NetcodeOptions.PredictionFrames" />
    /// <seealso cref="INetcodeGameSession{TInput}.AddLocalInput" />
    PredictionThreshold,

    /// <summary>The synchronization with peer was not finished.</summary>
    NotSynchronized,

    /// <summary>Session is in rollback state.</summary>
    InRollback,

    /// <summary>Unable to send input.</summary>
    InputDropped,

    /// <summary>Max number of spectators reached.</summary>
    /// <seealso cref="NetcodeOptions" />
    /// <inheritdoc cref="Max.NumberOfSpectators" />
    TooManySpectators,

    /// <summary>
    ///     Max number of players reached.
    ///     <seealso cref="NetcodeOptions" />
    /// </summary>
    /// <inheritdoc cref="Max.NumberOfPlayers" />
    TooManyPlayers,

    /// <summary>The operations need to requested before synchronization starts.</summary>
    /// <seealso cref="INetcodeSession{TInput}.Start" />
    AlreadySynchronized,

    /// <summary>The <see cref="PlayerHandle" /> is already added to the session.</summary>
    DuplicatedPlayer,

    /// <summary>The current session type not support the requested operation.</summary>
    NotSupported,
}
