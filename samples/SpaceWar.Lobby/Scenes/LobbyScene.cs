#nullable disable
using System.Diagnostics;
using Backdash;
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
    bool ready;
    long lastRefresh = Stopwatch.GetTimestamp();

    readonly TimeSpan refreshInterval = TimeSpan.FromSeconds(2);
    readonly KeyboardController keyboard = new();

    public override void Initialize()
    {
        client = Services.GetService<LobbyClient>();
        networkCall = RequestLobby();
        keyboard.Update();
    }

    public override void Update(GameTime gameTime)
    {
        keyboard.Update();

        if (PendingNetworkCall())
            return;


        if (mode is PlayerMode.Player && keyboard.IsKeyPressed(Keys.Enter))
        {
            networkCall = ToggleReady();
            return;
        }

        if (currentState is LobbyState.Waiting
            && Stopwatch.GetElapsedTime(lastRefresh) > refreshInterval)
            _ = RefreshLobby();
    }

    async Task ToggleReady()
    {
        await client.ToggleReady(user);
        ready = true;
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        var center = Viewport.Center.ToVector2();

        switch (currentState)
        {
            case LobbyState.Loading or LobbyState.Starting:
                DrawLoading(spriteBatch, center);
                break;
            case LobbyState.Error:
                DrawError(spriteBatch);
                break;
            case LobbyState.Waiting:
                DrawLobby(spriteBatch);
                break;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (user is null) return;
        try
        {
            client.LeaveLobby(user).GetAwaiter().GetResult();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    void DrawLobby(SpriteBatch spriteBatch)
    {
        const int padding = 15;
        const int halfPadding = padding / 2;
        const int lineWidth = 2;
        const float smTextScale = 0.6f;
        var lineColor = Color.DarkGray;
        var left = Viewport.Left + padding;
        var top = Viewport.Top + padding;

        spriteBatch.DrawString(Assets.MainFont, lobbyInfo.Name, new Vector2(left, top),
            Color.Cyan);

        Color usernameColor;
        if (mode is PlayerMode.Spectator)
            usernameColor = Color.LightBlue;
        else
            usernameColor = ready ? Color.Lime : Color.Orange;

        var usernameSize =
            Assets.MainFont.MeasureString(user.Username);
        spriteBatch.DrawString(Assets.MainFont, user.Username,
            new Vector2(Viewport.Right - padding - usernameSize.X, top),
            usernameColor, 0, Vector2.Zero, 1, SpriteEffects.None, 0);
        top += padding + (int)usernameSize.Y;

        Rectangle line = new(Viewport.Left, top, Viewport.Width, lineWidth);
        spriteBatch.Draw(Assets.Blank, line, lineColor);
        top += lineWidth + halfPadding;

        var note = ready ? "waiting other players..." : "press enter to start.";
        var noteSize = Assets.MainFont.MeasureString(note);
        spriteBatch.DrawString(Assets.MainFont, note, new(Viewport.Center.X, top),
            Color.Bisque, 0, new(noteSize.X / 2, 0), smTextScale, SpriteEffects.None, 0);
        top += (int)(noteSize.Y * smTextScale) + halfPadding;

        line = new(Viewport.Left, top, Viewport.Width, lineWidth);
        spriteBatch.Draw(Assets.Blank, line, lineColor);
        top += lineWidth;

        var splitHeight = Viewport.Height - top;
        line = new(Viewport.Center.X, top, lineWidth, splitHeight);
        spriteBatch.Draw(Assets.Blank, line, lineColor);
        splitHeight -= padding;
        top += halfPadding;

        Rectangle playersRect =
            new(left - halfPadding, top, Viewport.Width / 2 - padding, splitHeight);
        Rectangle spectatorsRect = new(Viewport.Center.X + lineWidth + halfPadding, top,
            Viewport.Width / 2 - lineWidth - padding, splitHeight);
        spriteBatch.Draw(Assets.Blank, playersRect, Color.DarkSlateGray);
        spriteBatch.Draw(Assets.Blank, spectatorsRect, Color.DarkSlateGray);

        const string playersTitle = "Players";
        const string spectatorsTitle = "Spectators";
        var playersTitleSize = Assets.MainFont.MeasureString(playersTitle);
        var spectatorsTitleSize = Assets.MainFont.MeasureString(spectatorsTitle);

        spriteBatch.DrawString(Assets.MainFont, playersTitle,
            new(playersRect.Center.X - playersTitleSize.X / 2, top), Color.MediumSeaGreen);
        spriteBatch.DrawString(Assets.MainFont, spectatorsTitle,
            new(spectatorsRect.Center.X - spectatorsTitleSize.X / 2, top), Color.MediumSeaGreen);

        top += (int)Math.Max(playersTitleSize.Y, spectatorsTitleSize.Y);
        top += halfPadding;
        line = new(playersRect.Left, top, playersRect.Width, halfPadding);
        spriteBatch.Draw(Assets.Blank, line, Color.Black);
        line = new(spectatorsRect.Left, top, spectatorsRect.Width, halfPadding);
        spriteBatch.Draw(Assets.Blank, line, Color.Black);

        top += padding;
        spectatorsRect.Inflate(-padding, -padding);
        playersRect.Inflate(-padding, -padding);

        var playerCount = lobbyInfo.Players.Length;
        var spectatorCount = lobbyInfo.Spectators.Length;
        var maxCount = Math.Max(playerCount, spectatorCount);

        for (var i = 0; i < maxCount; i++)
        {
            if (i < playerCount)
            {
                ref var player = ref lobbyInfo.Players[i];

                Rectangle statusBlock = new(
                    playersRect.Left, top + (int)usernameSize.Y / 3,
                    (int)usernameSize.Y / 2, (int)usernameSize.Y / 2
                );
                spriteBatch.Draw(Assets.Blank, statusBlock, null,
                    player.Ready ? Color.LimeGreen : Color.Orange,
                    0, Vector2.Zero, SpriteEffects.None, 0);

                spriteBatch.DrawString(Assets.MainFont, player.Username,
                    new(playersRect.Left + statusBlock.Width + padding, top), Color.White);
            }

            if (i < spectatorCount)
            {
                ref var player = ref lobbyInfo.Spectators[i];

                Rectangle statusBlock = new(
                    spectatorsRect.Left, top + (int)usernameSize.Y / 3,
                    (int)usernameSize.Y / 2, (int)usernameSize.Y / 2
                );
                spriteBatch.Draw(Assets.Blank, statusBlock, null, Color.LightBlue,
                    0, Vector2.Zero, SpriteEffects.None, 0);

                spriteBatch.DrawString(Assets.MainFont, player.Username,
                    new(spectatorsRect.Left + statusBlock.Width + padding, top), Color.White);
            }

            top += (int)usernameSize.Y + halfPadding;
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
        user = await client.EnterLobby(Config.LobbyName, username, mode);
        await RefreshLobby();

        Window.Title = $"Space War - {user.Username}";
        currentState = LobbyState.Waiting;
    }

    async Task RefreshLobby()
    {
        lastRefresh = Stopwatch.GetTimestamp();
        lobbyInfo = await client.GetLobby(user);

        if (lobbyInfo.Ready)
            switch (mode)
            {
                case PlayerMode.Player:
                    StartPlayerBattleScene();
                    break;
                case PlayerMode.Spectator:
                    StartSpectatorBattleScene();
                    break;
            }
    }

    void StartPlayerBattleScene()
    {
        currentState = LobbyState.Starting;
        List<Player> players = [];

        for (var i = 0; i < lobbyInfo.Players.Length; i++)
        {
            var player = lobbyInfo.Players[i];
            var playerNumber = i + 1;

            players.Add(player.PeerId == user.PeerId
                ? new LocalPlayer(playerNumber)
                : new RemotePlayer(playerNumber, player.Endpoint.ToIPEndpoint()));
        }

        if (lobbyInfo.SpectatorMapping.SingleOrDefault(m => m.Host == user.PeerId)
            is { Watchers: { } spectatorIds })
        {
            var spectators = lobbyInfo.Spectators.Where(s => spectatorIds.Contains(s.PeerId));
            foreach (var spectator in spectators)
                players.Add(new Spectator(spectator.Endpoint.ToIPEndpoint()));
        }

        LoadScene(new BattleScene(Config.Port, players));
    }

    void StartSpectatorBattleScene()
    {
        var hostId = lobbyInfo.SpectatorMapping
            .SingleOrDefault(x => x.Watchers.Contains(user.PeerId))
            ?.Host;
        var host = lobbyInfo.Players.Single(x => x.PeerId == hostId);
        var hostEndpoint = host.Endpoint.ToIPEndpoint();
        var playerCount = lobbyInfo.Players.Length;
        LoadScene(new BattleScene(Config.Port, playerCount, hostEndpoint));
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
        Starting,
        Error,
    }
}
