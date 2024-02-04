using nGGPO.Data;

namespace nGGPO.Input;

public readonly record struct SynchronizeResult(ResultCode Code, DisconnectFlags Disconnects);
