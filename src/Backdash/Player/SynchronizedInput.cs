using System.Runtime.InteropServices;

namespace Backdash;

/// <summary>
/// Synchronized input result
/// </summary>
/// <param name="Input">The input value</param>
/// <param name="Disconnected">Is <see langword="true"/> if input owner is disconnected</param>
/// <typeparam name="T">Type of the Input</typeparam>
[Serializable]
[StructLayout(LayoutKind.Sequential)]
public readonly record struct SynchronizedInput<T>(T Input, bool Disconnected) where T : unmanaged
{
    /// <summary>
    /// Returns the input associated with this type
    /// </summary>
    public static implicit operator T(SynchronizedInput<T> input) => input.Input;

    /// <summary>
    /// Returns non-disconnected input associated with this type
    /// </summary>
    public static implicit operator SynchronizedInput<T>(T input) => new(input, false);
}
