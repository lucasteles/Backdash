#pragma warning disable S2589

namespace Backdash.Serialization.Encoding;

static class RunLengthEncoding
{
    public static bool Encode(ReadOnlySpan<byte> input, Span<byte> output, out int totalWritten)
    {
        var outputIndex = 0;
        var count = 1;

        for (var i = 1; i < input.Length; i++)
            if (input[i] == input[i - 1] && count < byte.MaxValue)
                count++;
            else
            {
                if (outputIndex + 2 > output.Length)
                {
                    totalWritten = outputIndex;
                    return false;
                }

                output[outputIndex++] = (byte)count;
                output[outputIndex++] = input[i - 1];
                count = 1;
            }

        output[outputIndex++] = (byte)count;
        output[outputIndex++] = input[^1];
        totalWritten = outputIndex;
        return true;
    }

    public static bool Decode(ReadOnlySpan<byte> input, Span<byte> output, out int totalRead)
    {
        var outputIndex = 0;

        for (var i = 0; i < input.Length; i += 2)
        {
            var count = input[i];
            var value = input[i + 1];

            if (outputIndex + count > output.Length)
            {
                totalRead = outputIndex;
                return false;
            }

            for (var j = 0; j < count; j++)
                output[outputIndex++] = value;
        }

        totalRead = outputIndex;
        return true;
    }
}
