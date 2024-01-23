namespace nGGPO.Playground;

public static class Helpers
{
    static void Dump(in ReadOnlySpan<byte> bytes, string source = "")
    {
        Console.Write("bin> ");
        foreach (var b in bytes)
        {
            Console.Write(b);
            Console.Write(' ');
        }

        Console.Write($"{{ Source: {source}; Size: {bytes.Length} }}");
        Console.WriteLine();
    }
}
