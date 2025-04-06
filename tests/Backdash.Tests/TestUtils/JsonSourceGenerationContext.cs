using System.Text.Json.Serialization;
using Backdash.Tests.Specs.Unit.Utils;

namespace Backdash.Tests.TestUtils
{

    [JsonSerializable(typeof(JsonIPAddressConverterTests.IpAddressTestType))]
    [JsonSerializable(typeof(JsonIPEndpointConverterTests.IpEndPointTestType))]
    partial class JsonSourceGenerationContext : JsonSerializerContext
    {
    }
}
