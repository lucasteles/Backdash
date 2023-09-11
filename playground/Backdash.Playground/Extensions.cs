using System.Text.Json;

namespace Backdash.Playground;

public static class Extensions
{
    public static void Dump<T>(this T value) =>
        Console.WriteLine(
            JsonSerializer.Serialize(value, new JsonSerializerOptions
            {
                IncludeFields = true,
            }));
}
