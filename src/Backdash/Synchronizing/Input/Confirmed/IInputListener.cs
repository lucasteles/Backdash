using Backdash.Data;
using Backdash.Serialization;

namespace Backdash.Synchronizing.Input.Confirmed;

/// <summary>
/// Listen for confirmed input
/// </summary>
public interface IInputListener<TInput> : IDisposable where TInput : unmanaged
{
    /// <summary>
    /// Session Started
    /// </summary>
    void OnSessionStart(in IBinarySerializer<TInput> serializer);

    /// <summary>
    /// New confirmed input event handler
    /// </summary>
    void OnConfirmed(in Frame frame, ReadOnlySpan<TInput> inputs);

    /// <summary>
    /// Session End
    /// </summary>
    void OnSessionClose();
}
