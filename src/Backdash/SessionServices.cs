using Backdash.Core;
using Backdash.Serialization;
using Backdash.Sync.Input;
using Backdash.Sync.State;

namespace Backdash;

public sealed class SessionServices<TInput, TGameState>
    where TInput : struct
    where TGameState : IEquatable<TGameState>, new()
{
    public IBinarySerializer<TInput>? InputSerializer { get; set; }
    public IChecksumProvider<TGameState>? ChecksumProvider { get; set; }
    public ILogWriter? LogWriter { get; set; }
    public IBinarySerializer<TGameState>? StateSerializer { get; set; }
    public IInputGenerator<TInput>? InputGenerator { get; set; }
    public IStateStore<TGameState>? StateStore { get; set; }
    public Random? Random { get; set; }
}
