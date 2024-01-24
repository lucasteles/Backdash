using System.Runtime.CompilerServices;
using System.Text;
using nGGPO.Inputs;

namespace nGGPO;

static class Extensions
{
    public static bool IsSuccess(this ResultCode code) => code is ResultCode.Ok;

    public static void AssertTrue(this bool value, [CallerMemberName] string? source = null)
    {
        if (!value)
            throw new InvalidOperationException($"Invalid assertion at {source}");
    }

    public static ButtonsInput SetFlag(
        this ButtonsInput flags, ButtonsInput flag, bool value) =>
        value ? flags | flag : flags & ~flag;

    public static PadInput.PadButtons SetFlag(
        this PadInput.PadButtons flags,
        PadInput.PadButtons flag, bool value) =>
        value ? flags | flag : flags & ~flag;

    public static ButtonsInputEditor GetEditor(this ButtonsInput input) => new(input);

    public static void Reverse(this StringBuilder sb)
    {
        var end = sb.Length - 1;
        var start = 0;

        while (end - start > 0)
        {
            (sb[end], sb[start]) = (sb[start], sb[end]);
            start++;
            end--;
        }
    }
}
