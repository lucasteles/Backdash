using Backdash.Data;
using Backdash.Serialization;
using Backdash.Synchronizing.Input.Confirmed;
using SpaceWar.Logic;

namespace SpaceWar;

/// <summary>
/// Sample of an INGENUOUS implementation for saving inputs.
/// </summary>
sealed class InputsFileListener(string filename) : IInputListener<PlayerInputs>
{
    const int InputSize = sizeof(PlayerInputs);
    readonly FileStream fileStream = File.Create(filename);
    readonly byte[] inputBuffer = new byte[InputSize];

    public void OnSessionStart(in IBinarySerializer<PlayerInputs> serializer)
    {
        fileStream.SetLength(0);
        fileStream.Seek(0, SeekOrigin.Begin);
    }

    public void OnSessionClose() => fileStream.Flush();

    public void OnConfirmed(in Frame frame, ReadOnlySpan<PlayerInputs> inputs)
    {
        var buffer = inputBuffer.AsSpan();
        for (var i = 0; i < inputs.Length; i++)
        {
            var input = (ushort)inputs[i];
            buffer.Clear();

            if (!input.TryFormat(buffer, out _))
                throw new InvalidOperationException("unable to save input");

            fileStream.Write(buffer);
        }

        fileStream.Write("\n"u8);
    }

    public void Dispose() => fileStream.Dispose();

    public static IEnumerable<ConfirmedInputs<PlayerInputs>> GetInputs(int players, string file)
    {
        if (!File.Exists(file))
            throw new InvalidOperationException("Invalid replay file");

        using var replayStream = File.OpenRead(file);
        var buffer = new byte[InputSize * players];
        var inputsBuffer = new PlayerInputs[players];
        var lineBreak = new byte[1];

        while (replayStream.Read(buffer) > 0)
        {
            for (var i = 0; i < players; i++)
            {
                var slice = buffer.AsSpan().Slice(i * InputSize, InputSize);
                if (ushort.TryParse(slice, out var value))
                    inputsBuffer[i] = (PlayerInputs)value;
            }

            yield return new(inputsBuffer.AsSpan()[..players]);

            if (replayStream.Read(lineBreak) is 0 || lineBreak[0] != '\n')
                throw new InvalidOperationException("invalid replay file content");
        }
    }
}
