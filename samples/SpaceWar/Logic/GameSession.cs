﻿using Backdash;
using Backdash.Data;

namespace SpaceWar.Logic;

public class GameSession(
    GameState gameState,
    NonGameState nonGameState,
    Renderer renderer,
    IRollbackSession<PlayerInputs> session
) : IRollbackHandler<GameState>, IDisposable
{
    readonly SynchronizedInput<PlayerInputs>[] inputs =
        new SynchronizedInput<PlayerInputs>[nonGameState.NumberOfPlayers];


    public void Update(GameTime gameTime)
    {
        if (nonGameState.Sleeping)
        {
            nonGameState.SleepTime -= gameTime.ElapsedGameTime;
            return;
        }

        UpdateStats();
        session.BeginFrame();

        if (nonGameState.LocalPlayerHandle is { } localPlayer)
        {
            var keyboard = Keyboard.GetState();
            var localInput = Inputs.ReadInputs(keyboard);

            var addInputResult = session.AddLocalInput(localPlayer, localInput);
            if (addInputResult is not ResultCode.Ok)
            {
                if (addInputResult is not ResultCode.NotSynchronized)
                    Console.WriteLine($"{DateTime.Now:o} => ERROR ADDING INPUT: {addInputResult}");

                return;
            }
        }

        var syncInputResult = session.SynchronizeInputs();
        if (syncInputResult is not ResultCode.Ok)
        {
            Console.WriteLine($"{DateTime.Now:o} => ERROR SYNC INPUTS: {syncInputResult}");
            return;
        }

        session.GetInputs(inputs);
        gameState.Update(inputs);
        session.AdvanceFrame();
    }

    public void Draw() => renderer.Draw(gameState, nonGameState);

    public void OnSessionStart()
    {
        Console.WriteLine($"{DateTime.Now:o} => GAME STARTED");
        nonGameState.SetConnectState(PlayerConnectState.Running);
    }

    public void OnSessionClose()
    {
        Console.WriteLine($"{DateTime.Now:o} => GAME CLOSED");
        nonGameState.SetConnectState(PlayerConnectState.Disconnected);
    }

    public void TimeSync(FrameSpan framesAhead)
    {
        Console.WriteLine($"{DateTime.Now:o} => Time sync requested {framesAhead.FrameCount}");
        nonGameState.SleepTime = framesAhead.Duration();
    }

    void UpdateStats()
    {
        for (var i = 0; i < nonGameState.Players.Length; i++)
        {
            ref var player = ref nonGameState.Players[i];
            if (!player.Handle.IsRemote())
                continue;
            session.GetNetworkStatus(player.Handle, ref player.PeerNetworkStatus);
        }
    }

    public void OnPeerEvent(PlayerHandle player, PeerEventInfo evt)
    {
        Console.WriteLine($"{DateTime.Now:o} => PEER EVENT: {evt} from {player}");

        if (player.IsSpectator())
            return;

        switch (evt.Type)
        {
            case PeerEvent.Connected:
                nonGameState.SetConnectState(player, PlayerConnectState.Synchronizing);
                break;
            case PeerEvent.Synchronizing:

                var progress = 100 * evt.Synchronizing.CurrentStep /
                               (float)evt.Synchronizing.TotalSteps;
                nonGameState.UpdateConnectProgress(player, (int)progress);
                break;
            case PeerEvent.Synchronized:
                nonGameState.UpdateConnectProgress(player, 100);
                break;

            case PeerEvent.ConnectionInterrupted:
                nonGameState.SetDisconnectTimeout(player, DateTime.UtcNow,
                    evt.ConnectionInterrupted.DisconnectTimeout);
                break;
            case PeerEvent.ConnectionResumed:
                nonGameState.SetConnectState(player, PlayerConnectState.Running);
                break;
            case PeerEvent.Disconnected:
                nonGameState.SetConnectState(player, PlayerConnectState.Disconnected);
                break;
        }
    }

    public static void CopyState(GameState from, GameState to)
    {
        to.Bounds = from.Bounds;
        to.FrameNumber = from.FrameNumber;

        if (to.Ships.Length is 0)
        {
            to.Ships = new(from.Ships.Length);
            for (var i = 0; i < from.Ships.Length; i++)
                to.Ships[i] = new();
        }

        for (var i = 0; i < from.Ships.Length; i++)
        {
            ref var toShip = ref to.Ships[i];
            ref var fromShip = ref from.Ships[i];
            toShip.Id = fromShip.Id;
            toShip.Position = fromShip.Position;
            toShip.Velocity = fromShip.Velocity;
            toShip.Radius = fromShip.Radius;
            toShip.Heading = fromShip.Heading;
            toShip.Health = fromShip.Health;
            toShip.Active = fromShip.Active;
            toShip.FireCooldown = fromShip.FireCooldown;
            toShip.MissileCooldown = fromShip.MissileCooldown;
            toShip.Invincible = fromShip.Invincible;
            toShip.Score = fromShip.Score;
            toShip.Thrust = fromShip.Thrust;
            toShip.Missile.Heading = fromShip.Missile.Heading;
            toShip.Missile.HitBoxTime = fromShip.Missile.HitBoxTime;
            toShip.Missile.Position = fromShip.Missile.Position;
            toShip.Missile.Velocity = fromShip.Missile.Velocity;
            toShip.Missile.ExplosionRadius = fromShip.Missile.ExplosionRadius;
            toShip.Missile.ProjectileRadius = fromShip.Missile.ProjectileRadius;
            toShip.Missile.Active = fromShip.Missile.Active;
            toShip.Missile.ExplodeTimeout = fromShip.Missile.ExplodeTimeout;
            fromShip.Bullets.CopyTo(toShip.Bullets);
        }
    }

    public void SaveState(in Frame frame, ref GameState state) => CopyState(gameState, state);

    public void LoadState(in Frame frame, in GameState gs)
    {
        Console.WriteLine($"{DateTime.Now:o} => Loading state {frame}...");
        CopyState(gs, gameState);
    }

    public void AdvanceFrame()
    {
        session.SynchronizeInputs();
        session.GetInputs(inputs);
        gameState.Update(inputs);
        session.AdvanceFrame();
    }

    public void Dispose() => stream.Dispose();
}