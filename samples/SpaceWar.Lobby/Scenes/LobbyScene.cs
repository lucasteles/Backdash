#nullable disable
using SpaceWar.Models;
using SpaceWar.Util;

namespace SpaceWar.Scenes;

public sealed class LobbyScene(string username, PlayerMode mode) : Scene
{
    LobbyState currentState = LobbyState.Loading;
    LobbyClient client;
    string error;
    User user;
    Lobby lobbyInfo;
    Task networkCall;

    public override void Initialize()
    {
        client = Services.GetService<LobbyClient>();
        networkCall = RequestLobby();
    }

    public override void Update(GameTime gameTime)
    {
        if (PendingNetworkCall())
            return;

        if (currentState is LobbyState.Loading) { }
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        switch (currentState)
        {
            case LobbyState.Loading:
                break;
            case LobbyState.Waiting:
                break;
            case LobbyState.Error:
                break;
            case LobbyState.Ready:
                break;
        }
    }


    async Task RequestLobby()
    {
        var config = Services.GetService<AppSettings>();
        user = await client.EnterLobby(config.LobbyName, username, mode);
        lobbyInfo = await client.GetLobby(user, config.LobbyName);

        currentState = LobbyState.Waiting;
    }

    bool PendingNetworkCall()
    {
        if (networkCall is null)
            return false;

        if (!networkCall.IsCompleted)
            return true;

        if (networkCall.IsFaulted)
        {
            currentState = LobbyState.Error;
            error = networkCall.Exception?.Message;
        }

        networkCall = null;
        return true;
    }

    public enum LobbyState
    {
        Loading,
        Waiting,
        Error,
        Ready,
    }
}
