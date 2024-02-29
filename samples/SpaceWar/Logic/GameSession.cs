using Backdash;
using Backdash.Data;

namespace SpaceWar.Logic;

public class GameSession(
    GameState gameState,
    NonGameState nonGameState,
    Renderer renderer,
    IRollbackSession<PlayerInputs> session
) : IRollbackHandler<GameState>
{
    readonly SynchronizedInput<PlayerInputs>[] inputs =
        new SynchronizedInput<PlayerInputs>[nonGameState.NumberOfPlayers];

    public void Update(GameTime gameTime)
    {
        session.BeginFrame();

        if (nonGameState.LocalPlayerHandle is { } localPlayer)
        {
            var keyboard = Keyboard.GetState();
            var localInput = Inputs.ReadInputs(keyboard);

            if (session.AddLocalInput(localPlayer, localInput) is not ResultCode.Ok)
                return;
        }

        if (session.SynchronizeInputs() is not ResultCode.Ok)
            return;

        session.GetInputs(inputs);
        gameState.Update(inputs);
        session.AdvanceFrame();
    }

    public void Draw() => renderer.Draw(gameState, nonGameState);

    public void OnSessionStart()
    {
        Console.WriteLine("GAME STARTED");
        nonGameState.SetConnectState(PlayerConnectState.Running);
    }

    public void OnSessionClose()
    {
        Console.WriteLine("GAME CLOSED");
        nonGameState.SetConnectState(PlayerConnectState.Disconnected);
    }

    public void TimeSync(FrameSpan framesAhead)
    {
        Console.WriteLine("> Syncing...");
        Thread.Sleep(framesAhead.Duration);
    }

    public void OnPeerEvent(PlayerHandle player, PeerEventInfo evt)
    {
        Console.WriteLine($"PEER EVENT: {evt} from {player}");

        if (player.IsSpectator())
            return;

        switch (evt.Type)
        {
            case PeerEvent.Connected:
                nonGameState.SetConnectState(player, PlayerConnectState.Synchronizing);
                break;
            case PeerEvent.Synchronizing:

                var progress = 100 * evt.Synchronizing.CurrentStep /
                               (float) evt.Synchronizing.TotalSteps;
                nonGameState.UpdateConnectProgress(player, (int) progress);
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

        if (to.Ships is null or {Length: 0})
            to.Ships = new Ship[from.Ships.Length];

        for (var i = 0; i < from.Ships.Length; i++)
        {
            ref var toShip = ref to.Ships[i];
            ref var fromShip = ref from.Ships[i];
            toShip ??= new();
            toShip.Id = fromShip.Id;
            toShip.Position = fromShip.Position;
            toShip.Velocity = fromShip.Velocity;
            toShip.Radius = fromShip.Radius;
            toShip.Heading = fromShip.Heading;
            toShip.Health = fromShip.Health;
            toShip.FireCooldown = fromShip.FireCooldown;
            toShip.MissileCooldown = fromShip.MissileCooldown;
            toShip.Invincible = fromShip.Invincible;
            toShip.Score = fromShip.Score;
            toShip.Missile = fromShip.Missile;
            toShip.Thrust = fromShip.Thrust;
            fromShip.Bullets.CopyTo(toShip.Bullets.AsMemory());
        }
    }

    public void SaveState(int frame, ref GameState state) => CopyState(gameState, state);

    public void LoadState(in GameState gs)
    {
        Console.WriteLine("Loading state...");
        CopyState(gs, gameState);
    }

    public void AdvanceFrame()
    {
        session.SynchronizeInputs();
        session.GetInputs(inputs);
        gameState.Update(inputs);
        session.AdvanceFrame();
    }
}