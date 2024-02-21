namespace Backdash;

public enum ResultCode : short
{
    Ok = 0,
    InvalidPlayerHandle,
    PlayerOutOfRange,
    PredictionThreshold,
    NotSynchronized,
    InRollback,
    InputDropped,
    PlayerDisconnected,
    TooManySpectators,
    TooManyPlayers,
    InvalidRequest,
    Duplicated,
}
