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
    TooManySpectators,
    TooManyPlayers,
    InvalidRequest,
    Duplicated,
    NotSupported,
}
