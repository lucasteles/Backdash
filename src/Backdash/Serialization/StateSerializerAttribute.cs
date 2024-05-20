#pragma warning disable S2326
namespace Backdash.Serialization;

/// <summary>
/// Enable game state serializer source generator for <typeparam name="TState">TState</typeparam>
/// </summary>
/// <typeparam name="TState">Game State Type</typeparam>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class StateSerializerAttribute<TState> : Attribute;
