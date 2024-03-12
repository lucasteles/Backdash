using System.Net;
using System.Text.Json;
using Backdash.JsonConverters;

namespace Backdash.Tests.Specs.Unit.Utils;

using static JsonSerializer;

public class JsonIPEndpointConverterTests
{
    static readonly Faker faker = new();

    static readonly JsonSerializerOptions options = new()
    {
        Converters =
        {
            new JsonIPEndPointConverter(),
        },
    };

    record TestType(IPEndPoint Data);

    [Fact]
    public void ShouldParseIPv4()
    {
        var expected = faker.Internet.IpEndPoint();
        var value = Deserialize<TestType>($$"""{"Data": "{{expected}}"}""", options);
        value!.Data.Should().Be(expected);
    }

    [Fact]
    public void ShouldParseIPv6()
    {
        var expected = faker.Internet.Ipv6EndPoint();
        var value = Deserialize<TestType>($$"""{"Data": "{{expected}}"}""", options);
        value!.Data.Should().Be(expected);
    }

    [Fact]
    public void ShouldSerializeIPv4()
    {
        var value = faker.Internet.IpEndPoint();
        var result = Serialize(new TestType(value), options);
        var expected = $$"""{"Data":"{{value}}"}""";
        result.Should().Be(expected);
    }

    [Fact]
    public void ShouldSerializeIPv6()
    {
        var value = faker.Internet.Ipv6EndPoint();
        var result = Serialize(new TestType(value), options);
        var expected = $$"""{"Data":"{{value}}"}""";
        result.Should().Be(expected);
    }
}
