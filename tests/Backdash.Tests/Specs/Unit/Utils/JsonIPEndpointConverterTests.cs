using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Backdash.JsonConverters;
using Backdash.Tests.TestUtils;

namespace Backdash.Tests.Specs.Unit.Utils;

using static JsonSerializer;

public class JsonIPEndpointConverterTests
{
    static readonly Faker faker = new();

    public record IpEndPointTestType([property: JsonConverter(typeof(JsonIPEndPointConverter))] IPEndPoint Data);

    [Fact]
    public void ShouldParseIPv4()
    {
        var expected = faker.Internet.IpEndPoint();
        var value = Deserialize<IpEndPointTestType>($$"""{"Data": "{{expected}}"}""", JsonSourceGenerationContext.Default.IpEndPointTestType);
        value!.Data.Should().Be(expected);
    }

    [Fact]
    public void ShouldParseIPv6()
    {
        var expected = faker.Internet.Ipv6EndPoint();
        var value = Deserialize<IpEndPointTestType>($$"""{"Data": "{{expected}}"}""", JsonSourceGenerationContext.Default.IpEndPointTestType);
        value!.Data.Should().Be(expected);
    }

    [Fact]
    public void ShouldSerializeIPv4()
    {
        var value = faker.Internet.IpEndPoint();
        var result = Serialize(new IpEndPointTestType(value), JsonSourceGenerationContext.Default.IpEndPointTestType);
        var expected = $$"""{"Data":"{{value}}"}""";
        result.Should().Be(expected);
    }

    [Fact]
    public void ShouldSerializeIPv6()
    {
        var value = faker.Internet.Ipv6EndPoint();
        var result = Serialize(new IpEndPointTestType(value), JsonSourceGenerationContext.Default.IpEndPointTestType);
        var expected = $$"""{"Data":"{{value}}"}""";
        result.Should().Be(expected);
    }
}
