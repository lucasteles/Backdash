using System.Diagnostics.CodeAnalysis;

namespace SpaceWar.Models;

public readonly struct Response<T>
{
    public T? Value { get; init; }
    public string? Error { get; init; }

    [MemberNotNullWhen(true, nameof(IsSuccess))]
    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess => Error is null;
}
