using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Backdash.JsonConverters;
using Backdash.Tests.TestUtils;

namespace Backdash.Tests.Specs.Unit.Utils;

using static JsonSerializer;

public class JsonIPAddressConverterTests
{
    static readonly Faker faker = new();


    public record IpAddressTestType([property: JsonConverter(typeof(JsonIPAddressConverter))] IPAddress Data);

    [Fact]
    public void ShouldParseIPv4()
    {
        var expected = faker.Internet.IpAddress();
        var value = Deserialize<IpAddressTestType>($$"""{"Data": "{{expected.ToString()}}"}""", JsonSourceGenerationContext.Default.IpAddressTestType);
        value!.Data.Should().Be(expected);
    }

    [Fact]
    public void ShouldParseIPv6()
    {
        var expected = faker.Internet.Ipv6Address();
        var value = Deserialize<IpAddressTestType>($$"""{"Data": "{{expected.ToString()}}"}""", JsonSourceGenerationContext.Default.IpAddressTestType);
        value!.Data.Should().Be(expected);
    }

    [Fact]
    public void ShouldSerializeIPv4()
    {
        var value = faker.Internet.IpAddress();
        var result = Serialize(new IpAddressTestType(value), JsonSourceGenerationContext.Default.IpAddressTestType);
        var expected = $$"""{"Data":"{{value}}"}""";
        result.Should().Be(expected);
    }

    [Fact]
    public void ShouldSerializeIPv6()
    {
        var value = faker.Internet.Ipv6Address();
        var result = Serialize(new IpAddressTestType(value), JsonSourceGenerationContext.Default.IpAddressTestType);
        var expected = $$"""{"Data":"{{value}}"}""";
        result.Should().Be(expected);
    }
}
