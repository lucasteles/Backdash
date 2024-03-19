namespace Backdash.GamePad;

public static class Extensions
{
    public static PadButtonInputs SetFlag(
        this PadButtonInputs flags, PadButtonInputs flag, bool value) =>
        value ? flags | flag : flags & ~flag;

    public static PadInputs.PadButtons SetFlag(
        this PadInputs.PadButtons flags,
        PadInputs.PadButtons flag, bool value) =>
        value ? flags | flag : flags & ~flag;

    public static PadButtonInputsEditor GetEditor(this PadButtonInputs input) => new(input);
}
