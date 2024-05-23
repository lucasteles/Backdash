using Backdash.Serialization;

namespace Backdash.Synchronizing.State.Stores;

static class StateStoreFactory
{
    public static IStateStore<TState> Create<TState>(IBinarySerializer<TState>? stateSerializer = null)
        where TState : notnull, new() =>
        stateSerializer is null
            ? new ArrayStateStore<TState>()
            : new BinaryStateStore<TState>(stateSerializer);
}
