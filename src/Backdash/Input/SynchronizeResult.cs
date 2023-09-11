using Backdash.Data;

namespace Backdash.Input;

public readonly record struct SynchronizeResult(ResultCode Code, DisconnectFlags Disconnects);
