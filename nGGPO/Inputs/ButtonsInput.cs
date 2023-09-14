using System;
using System.Collections.Generic;
using System.Text;

// ReSharper disable InconsistentNaming

namespace nGGPO.Inputs;

[Flags]
public enum ButtonsInput : short
{
    None = 0,
    Select = 1 << 0,
    Up = 1 << 1,
    Down = 1 << 2,
    Left = 1 << 3,
    Right = 1 << 4,
    X = 1 << 5,
    Y = 1 << 6,
    A = 1 << 7,
    B = 1 << 8,
    LB = 1 << 9,
    RB = 1 << 10,
    LT = 1 << 11,
    RT = 1 << 12,
    L3 = 1 << 13,
    R3 = 1 << 14,

    UpLeft = Up | Left,
    UpRight = Up | Right,
    DownLeft = Down | Left,
    DownRight = Down | Right,
}

public struct ButtonsInputEditor
{
    ButtonsInput Input;

    public ButtonsInputEditor(ButtonsInput input) => Input = input;

    public void Reset() => Input = ButtonsInput.None;

    public bool IsEmpty => Input is ButtonsInput.None;

    public bool Up
    {
        get => Input.HasFlag(ButtonsInput.Up);
        set => Input = Input.SetFlag(ButtonsInput.Up, value);
    }

    public bool Down
    {
        get => Input.HasFlag(ButtonsInput.Down);
        set => Input = Input.SetFlag(ButtonsInput.Down, value);
    }

    public bool Left
    {
        get => Input.HasFlag(ButtonsInput.Left);
        set => Input = Input.SetFlag(ButtonsInput.Left, value);
    }

    public bool Right
    {
        get => Input.HasFlag(ButtonsInput.Right);
        set => Input = Input.SetFlag(ButtonsInput.Right, value);
    }

    public bool X
    {
        get => Input.HasFlag(ButtonsInput.X);
        set => Input = Input.SetFlag(ButtonsInput.X, value);
    }

    public bool Y
    {
        get => Input.HasFlag(ButtonsInput.Y);
        set => Input = Input.SetFlag(ButtonsInput.Y, value);
    }

    public bool A
    {
        get => Input.HasFlag(ButtonsInput.A);
        set => Input = Input.SetFlag(ButtonsInput.A, value);
    }

    public bool B
    {
        get => Input.HasFlag(ButtonsInput.B);
        set => Input = Input.SetFlag(ButtonsInput.B, value);
    }

    public bool LB
    {
        get => Input.HasFlag(ButtonsInput.LB);
        set => Input = Input.SetFlag(ButtonsInput.LB, value);
    }

    public bool RB
    {
        get => Input.HasFlag(ButtonsInput.RB);
        set => Input = Input.SetFlag(ButtonsInput.RB, value);
    }

    public bool LT
    {
        get => Input.HasFlag(ButtonsInput.LT);
        set => Input = Input.SetFlag(ButtonsInput.LT, value);
    }

    public bool RT
    {
        get => Input.HasFlag(ButtonsInput.RT);
        set => Input = Input.SetFlag(ButtonsInput.RT, value);
    }

    public bool L3
    {
        get => Input.HasFlag(ButtonsInput.L3);
        set => Input = Input.SetFlag(ButtonsInput.L3, value);
    }

    public bool R3
    {
        get => Input.HasFlag(ButtonsInput.R3);
        set => Input = Input.SetFlag(ButtonsInput.R3, value);
    }

    public bool Select
    {
        get => Input.HasFlag(ButtonsInput.Select);
        set => Input = Input.SetFlag(ButtonsInput.Select, value);
    }


    public bool UpLeft
    {
        get => Input.HasFlag(ButtonsInput.UpLeft);
        set => Input = Input.SetFlag(ButtonsInput.UpLeft, value);
    }

    public bool UpRight
    {
        get => Input.HasFlag(ButtonsInput.UpRight);
        set => Input = Input.SetFlag(ButtonsInput.UpRight, value);
    }

    public bool DownLeft
    {
        get => Input.HasFlag(ButtonsInput.DownLeft);
        set => Input = Input.SetFlag(ButtonsInput.DownLeft, value);
    }

    public bool DownRight
    {
        get => Input.HasFlag(ButtonsInput.DownRight);
        set => Input = Input.SetFlag(ButtonsInput.DownRight, value);
    }

    public static implicit operator ButtonsInput(ButtonsInputEditor @this) => @this.Input;
    public static implicit operator ButtonsInputEditor(ButtonsInput buttons) => new(buttons);

    public override string ToString()
    {
        var builder = new StringBuilder("[");
        var dpad = new string[4];
        if (Left) dpad[0] = "←";
        if (Up) dpad[1] = "↑";
        if (Down) dpad[2] = "↓";
        if (Right) dpad[3] = "→";

        builder.Append(string.Concat(dpad) switch
        {
            "↑→" => "↗",
            "←↑" => "↖",
            "←↓" => "↙",
            "↓→" => "↘",
            var other => other,
        });

        builder.Append("] {");

        List<string> buttons = new();
        if (X) buttons.Add(nameof(X));
        if (Y) buttons.Add(nameof(Y));
        if (A) buttons.Add(nameof(A));
        if (B) buttons.Add(nameof(B));
        if (LB) buttons.Add(nameof(LB));
        if (RB) buttons.Add(nameof(RB));
        if (LT) buttons.Add(nameof(LT));
        if (RT) buttons.Add(nameof(RT));
        if (L3) buttons.Add(nameof(L3));
        if (R3) buttons.Add(nameof(R3));
        if (Select) buttons.Add(nameof(Select));

        builder.Append(string.Join(',', buttons));

        builder.Append("}");
        return builder.ToString();
    }
}