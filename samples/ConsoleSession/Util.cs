using System.Diagnostics;
using Backdash.Core;
using Backdash.Serialization;
using Backdash.Serialization.Buffer;

namespace ConsoleSession;

public sealed class TraceLogWriter : ILogWriter
{
    public void Write(LogLevel level, char[] chars, int size) =>
        Trace.WriteLine(new string(chars.AsSpan()[..size]));

    public ValueTask WriteAsync(LogLevel level, char[] chars, int size)
    {
        Write(level, chars, size);
        return ValueTask.CompletedTask;
    }

    public void Dispose()
    {
    }
}

public sealed class MyStateSerializer : BinarySerializer<GameState>
{
    protected override void Serialize(in BinarySpanWriter writer, in GameState data)
    {
        writer.Write(data.Position1);
        writer.Write(data.Position2);
    }

    protected override void Deserialize(in BinarySpanReader reader, ref GameState result)
    {
        result.Position1 = reader.ReadVector2();
        result.Position2 = reader.ReadVector2();
    }
}