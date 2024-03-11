#nullable disable
using SpaceWar.Models;
using SpaceWar.Util;

namespace SpaceWar.Scenes;

public sealed class LobbyScene(string username, PlayerMode mode) : Scene
{
    LobbyState currentState = LobbyState.Loading;
    LobbyClient client;
    string errorMessage;
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

        if (currentState is LobbyState.Waiting) { }
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        var center = Viewport.Center.ToVector2();

        switch (currentState)
        {
            case LobbyState.Loading:
                DrawLoading(spriteBatch, center);
                break;
            case LobbyState.Error:
                DrawError(spriteBatch);
                break;
            case LobbyState.Waiting or LobbyState.Ready:
                break;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (user is null)
            return;

        try
        {
            client.LeaveLobby(user).GetAwaiter().GetResult();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    void DrawLoading(SpriteBatch spriteBatch, Vector2 center)
    {
        const string loadingText = "Loading...";
        var size = Assets.MainFont.MeasureString(loadingText);
        spriteBatch.DrawString(Assets.MainFont, loadingText,
            new(center.X, center.Y),
            Color.White, 0, size / 2, 1, SpriteEffects.None, 0);
    }

    void DrawError(SpriteBatch spriteBatch)
    {
        const string errorLabel = "Failure: ";
        const int padding = 15;
        var size = Assets.MainFont.MeasureString(errorLabel);
        spriteBatch.DrawString(Assets.MainFont, errorLabel,
            new Vector2(Viewport.Left + padding, Viewport.Top + padding),
            Color.Red, 0, Vector2.Zero, 1, SpriteEffects.None, 0);

        spriteBatch.DrawString(Assets.MainFont, errorMessage,
            new Vector2(Viewport.Left + padding, Viewport.Top + size.Y + padding * 2),
            Color.Orange, 0, Vector2.Zero, 0.5f, SpriteEffects.None, 0);
    }

    async Task RequestLobby()
    {
        var config = Services.GetService<AppSettings>();
        user = await client.EnterLobby(config.LobbyName, username, mode);
        lobbyInfo = await client.GetLobby(user);
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
            errorMessage =
                networkCall.Exception?.InnerException?.Message
                ?? networkCall.Exception?.Message;
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
