namespace Backdash.GamePad;

[Flags, Serializable]
public enum PadButtonInputs : short
{
    None = 0,
    Select = 1 << 0,

    Up = 1 << 1,
    Down = 1 << 2,
    Left = 1 << 3,
    Right = 1 << 4,

    UpLeft = Up | Left,
    UpRight = Up | Right,
    DownLeft = Down | Left,
    DownRight = Down | Right,

    X = 1 << 5,
    Y = 1 << 6,
    A = 1 << 7,
    B = 1 << 8,

    LeftBumper = 1 << 9,
    RightBumper = 1 << 10,
    LeftTrigger = 1 << 11,
    RightTrigger = 1 << 12,
    LeftStickButton = 1 << 13,
    RightStickButton = 1 << 14,
}
