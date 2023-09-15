using System;
using System.Runtime.CompilerServices;
using nGGPO.Inputs;

namespace nGGPO;

public static class Extensions
{
    public static bool IsSuccess(this ErrorCode code) => code is ErrorCode.Ok;

    public static void AssertTrue(this bool value, [CallerMemberName] string? source = null)
    {
        if (!value)
            throw new InvalidOperationException($"Invalid assertion at {source}");
    }

    public static ButtonsInput SetFlag(
        this ButtonsInput flags, ButtonsInput flag, bool value) =>
        value ? flags | flag : flags & ~flag;

    public static ButtonsInputEditor GetEditor(this ButtonsInput input) => new(input);

    public static PadInput.PadButtons SetFlag(
        this PadInput.PadButtons flags,
        PadInput.PadButtons flag, bool value) =>
        value ? flags | flag : flags & ~flag;
}