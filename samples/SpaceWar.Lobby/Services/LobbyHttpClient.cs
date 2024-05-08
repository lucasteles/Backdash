using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using Backdash.JsonConverters;
using SpaceWar.Models;

namespace SpaceWar.Services;

public sealed class LobbyHttpClient(AppSettings appSettings)
{
    static readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web)
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
        BaseAddress = appSettings.ServerUrl,
    };

    public async Task<User> EnterLobby(string lobbyName, string username, PlayerMode mode)
    {
        var localEndpoint = await GetLocalEndpoint();
        var response = await client.PostAsJsonAsync("/lobby", new
        {
            lobbyName,
            username,
            mode,
            localEndpoint,
        }, jsonOptions);

        if (response.StatusCode is HttpStatusCode.UnprocessableEntity)
            throw new InvalidOperationException("Already started");

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<User>(jsonOptions)
                     ?? throw new InvalidOperationException();

        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Add("token", result.Token.ToString());
        return result;
    }

    public async Task<Lobby> GetLobby(User user) =>
        await client.GetFromJsonAsync<Lobby>($"/lobby/{user.LobbyName}", jsonOptions)
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

    async Task<IPEndPoint?> GetLocalEndpoint()
    {
        try
        {
            using Socket socket = new(AddressFamily.InterNetwork, SocketType.Dgram, 0);
            await socket.ConnectAsync("8.8.8.8", 65530);
            if (socket.LocalEndPoint is not IPEndPoint { Address: { } ipAddress })
                return null;

            return new(ipAddress, appSettings.LocalPort);
        }
        catch (Exception)
        {
            // skip
        }

        return null;
    }
}
