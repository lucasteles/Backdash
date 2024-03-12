using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace LobbyServer;

[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
public class FromAuthorizationHeaderAttribute : FromHeaderAttribute
{
    public FromAuthorizationHeaderAttribute() => Name = HeaderNames.Authorization;
}
