using Backdash.Serialization;
using Backdash.Tests.TestUtils.Fixtures;

namespace Backdash.Tests.TestUtils.Assertions;

static class AssertSerialization
{
    public static bool Validate<T>(ref T value, Func<T>? ctor = null)
        where T : struct, IBinarySerializable, IEquatable<T>
    {
        var result = ctor?.Invoke() ?? new();
        try
        {
            using BinarySerializerFixture fixture = new();
            value.Serialize(fixture.Writer);
            result.Deserialize(fixture.Reader);
            return result.Equals(value);
        }
        finally
        {
            if (value is IDisposable disposableValue)
                disposableValue.Dispose();

            if (result is IDisposable disposableRes)
                disposableRes.Dispose();
        }
    }

    public static bool Offset<T>(ref T value, Func<T>? ctor = null) where T : struct, IBinarySerializable, IEquatable<T>
    {
        var result = ctor?.Invoke() ?? new();
        try
        {
            using BinarySerializerFixture fixture = new();
            value.Serialize(fixture.Writer);
            result.Deserialize(fixture.Reader);
            return fixture.ReadOffset == fixture.WriteOffset;
        }
        finally
        {
            if (value is IDisposable disposableValue)
                disposableValue.Dispose();

            if (result is IDisposable disposableRes)
                disposableRes.Dispose();
        }
    }
}
