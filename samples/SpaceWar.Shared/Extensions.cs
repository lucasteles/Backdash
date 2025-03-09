using Backdash.Serialization;

namespace SpaceWar;

public static class Extensions
{
    public static Vector2 RoundTo(this Vector2 vector, int digits = 2) =>
        new(MathF.Round(vector.X, digits), MathF.Round(vector.Y, digits));

    public static void Write(this BinaryBufferWriter writer, in Rectangle rect)
    {
        writer.Write(rect.X);
        writer.Write(rect.Y);
        writer.Write(rect.Width);
        writer.Write(rect.Height);
    }

    public static void Write(this BinaryBufferWriter writer, in Vector2 rect)
    {
        writer.Write(rect.X);
        writer.Write(rect.Y);
    }

    public static void Read(this BinaryBufferReader reader, ref Vector2 vector)
    {
        reader.Read(ref vector.X);
        reader.Read(ref vector.Y);
    }

    public static void Read(this BinaryBufferReader reader, ref Rectangle rect)
    {
        rect.X = reader.ReadInt32();
        rect.Y = reader.ReadInt32();
        rect.Width = reader.ReadInt32();
        rect.Height = reader.ReadInt32();
    }
}
