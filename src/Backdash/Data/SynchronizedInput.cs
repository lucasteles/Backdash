namespace Backdash.Data;

public readonly record struct SynchronizedInput<T>(T Input, bool Disconnected) where T : struct
{
    public static implicit operator T(SynchronizedInput<T> input) => input.Input;
}
