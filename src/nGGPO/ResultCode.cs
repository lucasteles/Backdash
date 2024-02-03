namespace nGGPO;

public enum ResultCode : short
{
    Ok = 0,
    InvalidSession,
    InvalidPlayerHandle,
    PlayerOutOfRange,
    PredictionThreshold,
    Unsupported,
    NotSynchronized,
    InRollback,
    InputDropped,
    InputPartiallyDropped,
    PlayerDisconnected,
    TooManySpectators,
    InvalidRequest,

    GeneralFailure = -1,
}
