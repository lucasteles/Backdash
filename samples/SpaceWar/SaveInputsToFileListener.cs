using Backdash.Data;
using Backdash.Synchronizing.Input.Confirmed;
using SpaceWar.Logic;

namespace SpaceWar;

sealed class SaveInputsToFileListener(string filename) : IInputListener<PlayerInputs>
{
    const int InputSize = sizeof(PlayerInputs);
    readonly FileStream fileStream = File.Create(filename);
    readonly byte[] inputBuffer = new byte[InputSize];

    public void OnConfirmed(in Frame frame, in ConfirmedInputs<PlayerInputs> inputs)
    {
        for (var i = 0; i < inputs.Count; i++)
        {
            var input = (ushort)inputs.Inputs[i];
            Array.Clear(inputBuffer);
            if (!input.TryFormat(inputBuffer, out _))
                throw new InvalidOperationException("unable to save input");

            fileStream.Write(inputBuffer);
        }

        fileStream.Write("\n"u8);
    }

    public void Dispose() => fileStream.Dispose();

    public static IEnumerable<ConfirmedInputs<PlayerInputs>> GetInputs(int players, string file)
    {
        using var replayStream = File.OpenRead(file);
        var buffer = new byte[InputSize * players];
        var inputsBuffer = new PlayerInputs[players];
        var lineBreak = new byte[1];

        while (replayStream.Read(buffer) > 0)
        {
            for (var i = 0; i < players; i++)
            {
                if (ushort.TryParse(buffer.AsSpan().Slice(i * InputSize, InputSize), out var value))
                    inputsBuffer[i] = (PlayerInputs)value;
            }

            yield return new(inputsBuffer.AsSpan()[..players]);

            if (replayStream.Read(lineBreak) is 0 || lineBreak[0] != '\n')
                throw new InvalidOperationException("invalid replay file content");
        }
    }
}
