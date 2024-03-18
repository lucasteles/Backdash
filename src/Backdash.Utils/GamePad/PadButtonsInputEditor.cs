using System.Text;

namespace Backdash.GamePad;

public sealed class PadButtonInputsEditor(PadButtonInputs input)
{
    public PadButtonInputs Input = input;
    public void Reset() => Input = PadButtonInputs.None;
    public bool IsEmpty => Input is PadButtonInputs.None;
    public PadButtonInputsEditor() : this(PadButtonInputs.None) { }

    public bool Up
    {
        get => Input.HasFlag(PadButtonInputs.Up);
        set => Input = Input.SetFlag(PadButtonInputs.Up, value);
    }

    public bool Down
    {
        get => Input.HasFlag(PadButtonInputs.Down);
        set => Input = Input.SetFlag(PadButtonInputs.Down, value);
    }

    public bool Left
    {
        get => Input.HasFlag(PadButtonInputs.Left);
        set => Input = Input.SetFlag(PadButtonInputs.Left, value);
    }

    public bool Right
    {
        get => Input.HasFlag(PadButtonInputs.Right);
        set => Input = Input.SetFlag(PadButtonInputs.Right, value);
    }

    public bool X
    {
        get => Input.HasFlag(PadButtonInputs.X);
        set => Input = Input.SetFlag(PadButtonInputs.X, value);
    }

    public bool Y
    {
        get => Input.HasFlag(PadButtonInputs.Y);
        set => Input = Input.SetFlag(PadButtonInputs.Y, value);
    }

    public bool A
    {
        get => Input.HasFlag(PadButtonInputs.A);
        set => Input = Input.SetFlag(PadButtonInputs.A, value);
    }

    public bool B
    {
        get => Input.HasFlag(PadButtonInputs.B);
        set => Input = Input.SetFlag(PadButtonInputs.B, value);
    }

    public bool LeftBumper
    {
        get => Input.HasFlag(PadButtonInputs.LeftBumper);
        set => Input = Input.SetFlag(PadButtonInputs.LeftBumper, value);
    }

    public bool RightBumper
    {
        get => Input.HasFlag(PadButtonInputs.RightBumper);
        set => Input = Input.SetFlag(PadButtonInputs.RightBumper, value);
    }

    public bool LeftTrigger
    {
        get => Input.HasFlag(PadButtonInputs.LeftTrigger);
        set => Input = Input.SetFlag(PadButtonInputs.LeftTrigger, value);
    }

    public bool RightTrigger
    {
        get => Input.HasFlag(PadButtonInputs.RightTrigger);
        set => Input = Input.SetFlag(PadButtonInputs.RightTrigger, value);
    }

    public bool LeftStickButton
    {
        get => Input.HasFlag(PadButtonInputs.LeftStickButton);
        set => Input = Input.SetFlag(PadButtonInputs.LeftStickButton, value);
    }

    public bool RightStickButton
    {
        get => Input.HasFlag(PadButtonInputs.RightStickButton);
        set => Input = Input.SetFlag(PadButtonInputs.RightStickButton, value);
    }

    public bool Select
    {
        get => Input.HasFlag(PadButtonInputs.Select);
        set => Input = Input.SetFlag(PadButtonInputs.Select, value);
    }

    public bool UpLeft
    {
        get => Input.HasFlag(PadButtonInputs.UpLeft);
        set => Input = Input.SetFlag(PadButtonInputs.UpLeft, value);
    }

    public bool UpRight
    {
        get => Input.HasFlag(PadButtonInputs.UpRight);
        set => Input = Input.SetFlag(PadButtonInputs.UpRight, value);
    }

    public bool DownLeft
    {
        get => Input.HasFlag(PadButtonInputs.DownLeft);
        set => Input = Input.SetFlag(PadButtonInputs.DownLeft, value);
    }

    public bool DownRight
    {
        get => Input.HasFlag(PadButtonInputs.DownRight);
        set => Input = Input.SetFlag(PadButtonInputs.DownRight, value);
    }

    public static implicit operator PadButtonInputs(PadButtonInputsEditor @this) => @this.Input;
    public static implicit operator PadButtonInputsEditor(PadButtonInputs padButtons) => new(padButtons);

    public override string ToString()
    {
        var builder = new StringBuilder();
        Span<char> dpad = stackalloc char[4];
        if (Left) dpad[0] = '←';
        if (Up) dpad[1] = '↑';
        if (Down) dpad[2] = '↓';
        if (Right) dpad[3] = '→';
        builder.Append(dpad switch
        {
            ['\0', '↑', '\0', '→'] => "↗",
            ['←', '↑', '\0', '\0'] => "↖",
            ['←', '\0', '↓', '\0'] => "↙",
            ['\0', '\0', '↓', '→'] => "↘",
            _ => string.Concat(string.Empty, dpad),
        });
        builder.Append(" + ");
        const char sep = ',';
        if (X)
        {
            builder.Append(nameof(X)[0]);
            builder.Append(sep);
        }

        if (Y)
        {
            builder.Append(nameof(Y)[0]);
            builder.Append(sep);
        }

        if (A)
        {
            builder.Append(nameof(A)[0]);
            builder.Append(sep);
        }

        if (B)
        {
            builder.Append(nameof(B)[0]);
            builder.Append(sep);
        }

        if (LeftBumper)
        {
            builder.Append(nameof(LeftBumper));
            builder.Append(sep);
        }

        if (RightBumper)
        {
            builder.Append(nameof(RightBumper));
            builder.Append(sep);
        }

        if (LeftTrigger)
        {
            builder.Append(nameof(LeftTrigger));
            builder.Append(sep);
        }

        if (RightTrigger)
        {
            builder.Append(nameof(RightTrigger));
            builder.Append(sep);
        }

        if (LeftStickButton)
        {
            builder.Append(nameof(LeftStickButton));
            builder.Append(sep);
        }

        if (RightStickButton)
        {
            builder.Append(nameof(RightStickButton));
            builder.Append(sep);
        }

        if (Select)
        {
            builder.Append(nameof(Select));
            builder.Append(sep);
        }

        return builder.ToString();
    }
}
