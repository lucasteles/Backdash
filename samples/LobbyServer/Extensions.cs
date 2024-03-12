using System.Net;
using System.Text.RegularExpressions;

namespace LobbyServer;

public static class Extensions
{
    public static IPAddress? GetRemoteClientIP(this HttpContext context)
    {
        var headers = context.Request.Headers;
        IPAddress? result;

        if (headers.TryGetValue("fly-client-ip", out var clientIPHeader)
            && IPAddress.TryParse(clientIPHeader, out var clientIP))
            result = clientIP;
        else
            result = context.Connection.RemoteIpAddress;

        return result?.MapToIPv4();
    }

    public static string NormalizeName(this string name) =>
        Regex.Replace(name.Trim().ToLower(), "[^a-zA-Z0-9]", "_");

}
