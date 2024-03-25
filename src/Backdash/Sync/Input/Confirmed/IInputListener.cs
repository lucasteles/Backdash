using Backdash.Data;

namespace Backdash.Sync.Input.Confirmed;

/// <summary>
/// Listen for confirmed input
/// </summary>
public interface IInputListener<TInput> where TInput : struct
{
    /// <summary>
    /// New confirmed input event handler
    /// </summary>
    void OnConfirmed(in Frame frame,  in ConfirmedInputs<TInput> inputs);
}
