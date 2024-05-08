#nullable disable

using Backdash;
using SpaceWar.Models;
using SpaceWar.Services;

namespace SpaceWar.Scenes;

public sealed class LobbyScene(PlayerMode mode) : Scene
{
    LobbyState currentState = LobbyState.Loading;
    LobbyHttpClient client;
    string errorMessage;
    User user;
    Lobby lobbyInfo;
    Task networkCall;
    bool ready;
    bool connected;
    LobbyUdpClient lobbyUdpClient;

    readonly TimeSpan refreshInterval = TimeSpan.FromSeconds(2);
    readonly TimeSpan pingInterval = TimeSpan.FromMilliseconds(300);
    readonly KeyboardController keyboard = new();
    readonly CancellationTokenSource cts = new();

    public override void Initialize()
    {
        client = Services.GetService<LobbyHttpClient>();
        networkCall = RequestLobby();
        lobbyUdpClient = new(Config.LocalPort, Config.ServerUrl, Config.ServerUdpPort);
        keyboard.Update();

        StartPingTimer();
        StartLobbyRefreshTimer();
    }

    public override void Update(GameTime gameTime)
    {
        if (currentState is LobbyState.Error) return;

        keyboard.Update();

        if (user is not null && !Window.Title.Contains(user.Username))
            Window.Title = $"Space War {Config.LocalPort} - {user.Username}";

        if (PendingNetworkCall())
            return;

        CheckPlayersReady();

        if (mode is PlayerMode.Player && keyboard.IsKeyPressed(Keys.Enter))
            networkCall = ToggleReady();
    }

    async Task ToggleReady()
    {
        if (!AllReachable()) return;

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

    const int Padding = 15;
    const int HalfPadding = Padding / 2;
    const float SmallTextScale = 0.6f;

    void DrawLobby(SpriteBatch spriteBatch)
    {
        const int lineWidth = 2;
        var lineColor = Color.DarkGray;
        var left = Viewport.Left + Padding;
        var top = Viewport.Top + Padding;

        spriteBatch.DrawString(Assets.MainFont, lobbyInfo.Name, new(left, top),
            Color.Gold);

        Color usernameColor;
        if (mode is PlayerMode.Spectator)
            usernameColor = Color.White;
        else if (!connected)
            usernameColor = StatusColors.Connecting;
        else
            usernameColor = ready ? StatusColors.Ready : StatusColors.Reachable;

        var usernameSize =
            Assets.MainFont.MeasureString(user.Username);
        spriteBatch.DrawString(Assets.MainFont, user.Username,
            new(Viewport.Right - Padding - usernameSize.X, top),
            usernameColor, 0, Vector2.Zero, 1, SpriteEffects.None, 0);
        top += Padding + (int)usernameSize.Y;

        Rectangle line = new(Viewport.Left, top, Viewport.Width, lineWidth);
        spriteBatch.Draw(Assets.Blank, line, lineColor);
        top += lineWidth + HalfPadding;

        string note;
        if (mode is PlayerMode.Spectator)
            note = "waiting players start...";
        else if (!AllReachable())
            note = "connecting to players...";
        else
            note = ready ? "waiting other players..." : "press enter to start.";

        var noteSize = Assets.MainFont.MeasureString(note);
        spriteBatch.DrawString(Assets.MainFont, note, new(Viewport.Center.X, top),
            Color.Bisque, 0, new(noteSize.X / 2, 0), SmallTextScale, SpriteEffects.None, 0);
        top += (int)(noteSize.Y * SmallTextScale) + HalfPadding;

        line = new(Viewport.Left, top, Viewport.Width, lineWidth);
        spriteBatch.Draw(Assets.Blank, line, lineColor);
        top += lineWidth;

        var footerHeight = DrawFooter(spriteBatch);

        var splitHeight = Viewport.Height - top - footerHeight;
        line = new(Viewport.Center.X, top, lineWidth, splitHeight);
        spriteBatch.Draw(Assets.Blank, line, lineColor);
        splitHeight -= Padding;
        top += HalfPadding;

        line = new(Viewport.Left, top + splitHeight + HalfPadding, Viewport.Width, lineWidth);
        spriteBatch.Draw(Assets.Blank, line, lineColor);

        Rectangle playersRect = new(left - HalfPadding, top,
            Viewport.Width / 2 - Padding, splitHeight);
        Rectangle spectatorsRect = new(Viewport.Center.X + lineWidth + HalfPadding, top,
            Viewport.Width / 2 - lineWidth - Padding, splitHeight);
        var sessionBackgroundColor = Color.DarkSlateGray;
        var sessionTitleColor = Color.MediumSeaGreen;
        spriteBatch.Draw(Assets.Blank, playersRect, sessionBackgroundColor);
        spriteBatch.Draw(Assets.Blank, spectatorsRect, sessionBackgroundColor);

        const string playersTitle = "Players";
        const string spectatorsTitle = "Spectators";
        var playersTitleSize = Assets.MainFont.MeasureString(playersTitle);
        var spectatorsTitleSize = Assets.MainFont.MeasureString(spectatorsTitle);

        spriteBatch.DrawString(Assets.MainFont, playersTitle,
            new(playersRect.Center.X - playersTitleSize.X / 2, top), sessionTitleColor);
        spriteBatch.DrawString(Assets.MainFont, spectatorsTitle,
            new(spectatorsRect.Center.X - spectatorsTitleSize.X / 2, top), sessionTitleColor);

        top += (int)Math.Max(playersTitleSize.Y, spectatorsTitleSize.Y);
        top += HalfPadding;
        line = new(playersRect.Left, top, playersRect.Width, HalfPadding);
        spriteBatch.Draw(Assets.Blank, line, Color.Black);
        line = new(spectatorsRect.Left, top, spectatorsRect.Width, HalfPadding);
        spriteBatch.Draw(Assets.Blank, line, Color.Black);

        top += Padding;
        spectatorsRect.Inflate(-Padding, -Padding);
        playersRect.Inflate(-Padding, -Padding);

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

                Color statusColor;
                if (player.PeerId == user.PeerId)
                    statusColor = usernameColor;
                else if (!player.Connected)
                    statusColor = StatusColors.Connecting;
                else if (!lobbyUdpClient.IsKnown(player.PeerId))
                    statusColor = StatusColors.Connected;
                else
                    statusColor = player.Ready ? StatusColors.Ready : StatusColors.Reachable;

                spriteBatch.Draw(Assets.Blank, statusBlock, null, statusColor,
                    0, Vector2.Zero, SpriteEffects.None, 0);

                spriteBatch.DrawString(Assets.MainFont, player.Username,
                    new(playersRect.Left + statusBlock.Width + Padding, top), Color.White);
            }

            if (i < spectatorCount)
            {
                ref var spectator = ref lobbyInfo.Spectators[i];

                Rectangle statusBlock = new(
                    spectatorsRect.Left, top + (int)usernameSize.Y / 3,
                    (int)usernameSize.Y / 2, (int)usernameSize.Y / 2
                );

                Color statusColor;
                if (spectator.PeerId == user.PeerId)
                    statusColor = StatusColors.Reachable;
                else if (!spectator.Connected)
                    statusColor = StatusColors.Connecting;
                else if (!lobbyUdpClient.IsKnown(spectator.PeerId))
                    statusColor = StatusColors.Connected;
                else
                    statusColor = StatusColors.Reachable;

                spriteBatch.Draw(Assets.Blank, statusBlock, null, statusColor,
                    0, Vector2.Zero, SpriteEffects.None, 0);

                spriteBatch.DrawString(Assets.MainFont, spectator.Username,
                    new(spectatorsRect.Left + statusBlock.Width + Padding, top), Color.White);
            }

            top += (int)usernameSize.Y + HalfPadding;
        }
    }

    int DrawFooter(SpriteBatch spriteBatch)
    {
        var charSize = Assets.MainFont.MeasureString(" ") * SmallTextScale;
        Point blockSize = new((int)charSize.Y / 2, (int)charSize.Y / 2);
        Point position = new(
            Viewport.Left + HalfPadding,
            Viewport.Bottom - (int)charSize.Y - HalfPadding
        );

        DrawLegend(StatusColors.Connecting, nameof(StatusColors.Connecting));
        DrawLegend(StatusColors.Connected, nameof(StatusColors.Connected));
        DrawLegend(StatusColors.Reachable, nameof(StatusColors.Reachable));
        DrawLegend(StatusColors.Ready, nameof(StatusColors.Ready));

        return (int)charSize.Y + Padding;

        void DrawLegend(Color color, string name)
        {
            var nameSize = Assets.MainFont.MeasureString(name) * SmallTextScale;

            Rectangle statusRect = new(
                position.X, position.Y + HalfPadding,
                blockSize.X, blockSize.Y
            );

            spriteBatch.Draw(Assets.Blank, statusRect, null, color,
                0, Vector2.Zero, SpriteEffects.None, 0);

            spriteBatch.DrawString(Assets.MainFont, name,
                new(position.X + statusRect.Width + HalfPadding, position.Y),
                Color.LightGray, 0, Vector2.Zero, SmallTextScale, SpriteEffects.None, 0);

            position.X += (int)nameSize.X + blockSize.X + Padding * 2;
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
            new(Viewport.Left + padding, Viewport.Top + padding),
            Color.Red, 0, Vector2.Zero, 1, SpriteEffects.None, 0);

        spriteBatch.DrawString(Assets.MainFont, errorMessage,
            new(Viewport.Left + padding, Viewport.Top + size.Y + padding * 2),
            Color.Orange, 0, Vector2.Zero, 0.5f, SpriteEffects.None, 0);
    }

    async Task RequestLobby()
    {
        user = await client.EnterLobby(Config.LobbyName, Config.Username, mode);
        await RefreshLobby();

        if (Array.Exists(lobbyInfo.Spectators, s => s.PeerId == user.PeerId))
            mode = PlayerMode.Spectator;

        currentState = LobbyState.Waiting;
    }

    async Task RefreshLobby()
    {
        lobbyInfo = await client.GetLobby(user);

        await lobbyUdpClient.HandShake(user);

        if (connected) return;
        connected = lobbyInfo.Players.SingleOrDefault(x => x.PeerId == user.PeerId) is
        { Connected: true };
    }

    void CheckPlayersReady()
    {
        if (lobbyInfo?.Ready == false) return;

        cts.Cancel();
        lobbyUdpClient.Stop();

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

    bool AllReachable()
    {
        if (lobbyInfo is null || lobbyInfo.Players.Length <= 1)
            return false;

        for (var i = 0; i < lobbyInfo.Players.Length; i++)
        {
            var peer = lobbyInfo.Players[i];

            if (peer.PeerId == user.PeerId)
                continue;

            if (!peer.Connected || !lobbyUdpClient.IsKnown(peer.PeerId))
                return false;
        }

        return true;
    }

    void StartPlayerBattleScene()
    {
        if (lobbyInfo is null) return;

        currentState = LobbyState.Starting;
        List<Player> players = [];

        for (var i = 0; i < lobbyInfo.Players.Length; i++)
        {
            var player = lobbyInfo.Players[i];
            var playerNumber = i + 1;

            players.Add(player.PeerId == user.PeerId
                ? new LocalPlayer(playerNumber)
                : new RemotePlayer(playerNumber,
                    lobbyUdpClient.GetFallbackEndpoint(user, player)));
        }

        if (lobbyInfo.SpectatorMapping.SingleOrDefault(m => m.Host == user.PeerId)
            is { Watchers: { } spectatorIds })
        {
            var spectators = lobbyInfo.Spectators.Where(s => spectatorIds.Contains(s.PeerId));
            foreach (var spectator in spectators)
            {
                var spectatorEndpoint = lobbyUdpClient.GetFallbackEndpoint(user, spectator);
                players.Add(new Spectator(spectatorEndpoint));
            }
        }

        LoadScene(new BattleSessionScene(Config.LocalPort, players, lobbyInfo.Players));
    }

    void StartSpectatorBattleScene()
    {
        var hostId = lobbyInfo.SpectatorMapping
            .SingleOrDefault(x => x.Watchers.Contains(user.PeerId))
            ?.Host;
        var host = lobbyInfo.Players.Single(x => x.PeerId == hostId);
        var playerCount = lobbyInfo.Players.Length;
        var hostEndpoint = lobbyUdpClient.GetFallbackEndpoint(user, host);


        Window.Title = $"Space War {Config.LocalPort} - {user.Username} watching {host.Username}";
        LoadScene(new BattleSessionScene(
            Config.LocalPort, playerCount,
            hostEndpoint, lobbyInfo.Players)
        );
    }

    bool PendingNetworkCall()
    {
        if (networkCall is null)
            return false;

        if (!networkCall.IsCompleted)
            return true;

        if (networkCall.IsFaulted)
            SetError(networkCall.Exception);

        networkCall = null;
        return true;
    }

    void SetError(Exception ex)
    {
        currentState = LobbyState.Error;
        errorMessage =
            ex?.InnerException?.Message
            ?? networkCall.Exception?.Message;
    }

    public void StartPingTimer() => Task.Run(async () =>
    {
        using PeriodicTimer timer = new(pingInterval);

        try
        {
            while (await timer.WaitForNextTickAsync(cts.Token))
            {
                if (lobbyInfo is null || lobbyInfo.Ready) continue;
                await Task.WhenAll(
                    lobbyUdpClient.Ping(user, lobbyInfo.Players, cts.Token),
                    lobbyUdpClient.Ping(user, lobbyInfo.Spectators, cts.Token)
                );
            }
        }
        catch (OperationCanceledException)
        {
            // skip
        }
        catch (Exception ex)
        {
            SetError(ex);
        }
    });

    public void StartLobbyRefreshTimer() => Task.Run(async () =>
    {
        using PeriodicTimer timer = new(refreshInterval);

        try
        {
            while (await timer.WaitForNextTickAsync(cts.Token))
            {
                if (currentState is not LobbyState.Waiting) continue;
                await RefreshLobby();
            }
        }
        catch (OperationCanceledException)
        {
            // skip
        }
        catch (Exception ex)
        {
            SetError(ex);
        }
    });

    protected override void Dispose(bool disposing)
    {
        try
        {
            cts.Dispose();
            lobbyUdpClient.Dispose();
            if (user is not null && lobbyInfo is { Ready: false })
                client.LeaveLobby(user).GetAwaiter().GetResult();
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error leaving lobby: {e}");
        }
    }

    public enum LobbyState
    {
        Loading,
        Waiting,
        Starting,
        Error,
    }

    static class StatusColors
    {
        public static readonly Color Connecting = Color.Red;
        public static readonly Color Connected = Color.Orange;
        public static readonly Color Reachable = Color.SkyBlue;
        public static readonly Color Ready = Color.Lime;
    }
}
