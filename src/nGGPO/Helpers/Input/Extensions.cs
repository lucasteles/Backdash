namespace nGGPO.Helpers.Input;

public static class Extensions
{
    public static ButtonsInput SetFlag(
        this ButtonsInput flags, ButtonsInput flag, bool value) =>
        value ? flags | flag : flags & ~flag;

    public static PadInput.PadButtons SetFlag(
        this PadInput.PadButtons flags,
        PadInput.PadButtons flag, bool value) =>
        value ? flags | flag : flags & ~flag;

    public static ButtonsInputEditor GetEditor(this ButtonsInput input) => new(input);
}
