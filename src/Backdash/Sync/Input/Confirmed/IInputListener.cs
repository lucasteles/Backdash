using Backdash.Data;

namespace Backdash.Sync.Input.Confirmed;

/// <summary>
/// Listen for confirmed input
/// </summary>
public interface IInputListener<TInput> : IDisposable where TInput : unmanaged
{
    /// <summary>
    /// New confirmed input event handler
    /// </summary>
    void OnConfirmed(in Frame frame, in ConfirmedInputs<TInput> inputs);
}
