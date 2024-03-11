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

    public async Task<Lobby> GetLobby(User user) =>
        await client.GetFromJsonAsync<Lobby>
        (
            $"/lobby/{user.LobbyName}?token={user.Token}",
            JsonOptions
        )
        ?? throw new InvalidOperationException();

    public async Task LeaveLobby(User user)
    {
        var response = await client.DeleteAsync($"/lobby/{user.LobbyName}?token={user.Token}");
        response.EnsureSuccessStatusCode();
    }

    public async Task ToggleReady(User user)
    {
        var response = await client.PutAsync($"/lobby/{user.LobbyName}?token={user.Token}", null);
        response.EnsureSuccessStatusCode();
    }
}
