using System.Net;
using System.Text.Json;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using SpaceWar.Models;

namespace SpaceWar.Util;

public sealed class LobbyClient(AppSettings appSettings)
{
    static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters =
        {
            new JsonStringEnumConverter(),
        }
    };

    readonly HttpClient client = new()
    {
        BaseAddress = appSettings.LobbyUrl,
    };

    public async Task<User> EnterLobby(string lobbyName, string username, PlayerMode mode)
    {
        var response = await client.PostAsJsonAsync("/lobby", new
        {
            lobbyName,
            port = appSettings.Port,
            username,
            mode,
        }, JsonOptions);

        if (response.StatusCode is HttpStatusCode.Conflict)
            throw new InvalidOperationException("Duplicated username");

        return await response.Content.ReadFromJsonAsync<User>()
               ?? throw new InvalidOperationException();
    }

    public async Task<Lobby> GetLobby(User user, string lobbyName)
    {
        var response = await client.PostAsJsonAsync(
            $"/lobby/{lobbyName}?token={user.UserToken}",
            JsonOptions
        );

        if (response.StatusCode is HttpStatusCode.Forbidden)
            throw new InvalidOperationException("Not authorized");

        return await response.Content.ReadFromJsonAsync<Lobby>()
               ?? throw new InvalidOperationException();
    }
}
