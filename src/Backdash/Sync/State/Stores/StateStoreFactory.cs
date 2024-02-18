using Backdash.Serialization;

namespace Backdash.Sync.State.Stores;

static class StateStoreFactory
{
    public static IStateStore<TState> Create<TState>(IBinarySerializer<TState>? stateSerializer = null)
        where TState : IEquatable<TState> =>
        stateSerializer is null
            ? new ArrayStateStore<TState>()
            : new BinaryStateStore<TState>(stateSerializer);
}
