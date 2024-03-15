using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Backdash.JsonConverters;
using SpaceWar.Models;

namespace SpaceWar.Services;

public sealed class LobbyHttpClient(AppSettings appSettings)
{
    static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters =
        {
            new JsonStringEnumConverter(),
            new JsonIPAddressConverter(),
            new JsonIPEndPointConverter(),
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
            username,
            mode,
        }, JsonOptions);

        if (response.StatusCode is HttpStatusCode.UnprocessableEntity)
            throw new InvalidOperationException("Already started");

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<User>(JsonOptions)
                     ?? throw new InvalidOperationException();

        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Add("token", result.Token.ToString());
        return result;
    }

    public async Task<Lobby> GetLobby(User user) =>
        await client.GetFromJsonAsync<Lobby>($"/lobby/{user.LobbyName}", JsonOptions)
        ?? throw new InvalidOperationException();

    public async Task LeaveLobby(User user)
    {
        var response = await client.DeleteAsync($"/lobby/{user.LobbyName}");
        response.EnsureSuccessStatusCode();
    }

    public async Task ToggleReady(User user)
    {
        var response = await client.PutAsync($"/lobby/{user.LobbyName}", null);
        response.EnsureSuccessStatusCode();
    }
}
