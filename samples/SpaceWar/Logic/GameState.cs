using Backdash.Data;

namespace SpaceWar.Logic;

public record struct Bullet
{
    public bool Active;
    public Vector2 Position;
    public Vector2 Velocity;
}

public sealed record Ship
{
    public Vector2 Position;
    public Vector2 Velocity;
    public int Radius;
    public int Heading;
    public int Health;
    public int Cooldown;
    public int Score;
    public Bullet[] Bullets = new Bullet[Config.MaxBullets];
    public int Thrust;
}

public class GameState
{
    public Ship[] Ships = [];
    public Rectangle Bounds;
    public int FrameNumber;
    public int NumberOfShips => Ships.Length;

    public void Init(GameWindow window, int numberOfPlayers)
    {
        Ships = new Ship[numberOfPlayers];
        for (var i = 0; i < numberOfPlayers; i++)
            Ships[i] = new();

        FrameNumber = 0;
        Bounds = window.ClientBounds with {X = 0, Y = 0};
        Bounds.Inflate(Config.WindowPadding, Config.WindowPadding);

        var width = Bounds.Right - Bounds.Left;
        var height = Bounds.Bottom - Bounds.Top;
        var r = height / 4;

        for (var i = 0; i < numberOfPlayers; i++)
        {
            var heading = i * 360 / numberOfPlayers;
            var theta = heading * MathF.PI / 180;
            var (cosT, sinT) = (MathF.Cos(theta), MathF.Sin(theta));

            var x = width / 2f + r * cosT;
            var y = height / 2f + r * sinT;
            Ships[i].Position = new(x, y);
            Ships[i].Heading = (heading + 180) % 360;
            Ships[i].Health = Config.StartingHealth;
            Ships[i].Radius = Config.ShipRadius;
        }

        Bounds.Inflate(Config.WindowPadding, Config.WindowPadding);
    }

    public GameInput GetShipAI(in Ship ship) => new(
        Heading: (ship.Heading + 5f) % 360f,
        Thrust: 0,
        Fire: false
    );

    public GameInput ParseShipInputs(PlayerInputs inputs, in Ship ship)
    {
        float heading;
        if (inputs.HasFlag(PlayerInputs.RotateRight))
            heading = (ship.Heading + Config.RotateIncrement) % 360f;
        else if (inputs.HasFlag(PlayerInputs.RotateLeft))
            heading = (ship.Heading - Config.RotateIncrement + 360) % 360;
        else
            heading = ship.Heading;

        float thrust;
        if (inputs.HasFlag(PlayerInputs.Thrust))
            thrust = Config.ShipThrust;
        else if (inputs.HasFlag(PlayerInputs.Break))
            thrust = -Config.ShipThrust;
        else
            thrust = 0;

        return new(heading, thrust, inputs.HasFlag(PlayerInputs.Fire));
    }

    public void UpdateShip(in Ship ship, in GameInput inputs)
    {
        ship.Heading = (int) inputs.Heading;

        Vector2 rotation = new(
            MathF.Cos(MathHelper.ToRadians(ship.Heading)),
            MathF.Sin(MathHelper.ToRadians(ship.Heading))
        );

        if (ship.Cooldown is 0 && inputs.Fire)
            for (var i = 0; i < ship.Bullets.Length; i++)
            {
                ref var bullet = ref ship.Bullets[i];
                if (bullet.Active)
                    continue;

                bullet.Active = true;
                bullet.Position = ship.Position + rotation * ship.Radius;
                bullet.Velocity = ship.Velocity + rotation * Config.BulletSpeed;
                ship.Cooldown = Config.BulletCooldown;
                break;
            }

        ship.Thrust = Math.Sign(inputs.Thrust);

        if (inputs.Thrust != 0)
        {
            ship.Velocity += rotation * inputs.Thrust;
            var magnitude = ship.Velocity.Length();
            if (magnitude > Config.ShipMaxThrust)
                ship.Velocity = ship.Velocity * Config.ShipMaxThrust / magnitude;
        }

        ship.Position += ship.Velocity;

        if (ship.Position.X - ship.Radius < Bounds.Left ||
            ship.Position.X + ship.Radius > Bounds.Right)
        {
            ship.Velocity.X *= -1;
            ship.Position.X += ship.Velocity.X * 2;
        }

        if (ship.Position.Y - ship.Radius < Bounds.Top ||
            ship.Position.Y + ship.Radius > Bounds.Bottom)
        {
            ship.Velocity.Y *= -1;
            ship.Position.Y += ship.Velocity.Y * 2;
        }

        for (var i = 0; i < ship.Bullets.Length; i++)
        {
            ref var bullet = ref ship.Bullets[i];
            if (!bullet.Active)
                continue;

            bullet.Position += bullet.Velocity;

            if (!Bounds.Contains(bullet.Position))
            {
                bullet.Active = false;
                continue;
            }

            for (var j = 0; j < NumberOfShips; j++)
            {
                ref var other = ref Ships[j];
                if (Vector2.Distance(bullet.Position, other.Position) > other.Radius)
                    continue;

                ship.Score++;
                other.Health -= Config.BulletDamage;
                bullet.Active = false;
                break;
            }
        }

        if (ship.Cooldown > 0)
            ship.Cooldown--;
    }

    public void Update(SynchronizedInput<PlayerInputs>[] inputs)
    {
        FrameNumber++;

        for (var i = 0; i < NumberOfShips; i++)
        {
            ref var ship = ref Ships[i];

            var gameInput = inputs[i].Disconnected
                ? GetShipAI(in ship)
                : ParseShipInputs(inputs[i], in ship);

            UpdateShip(in ship, in gameInput);
        }
    }

// ReSharper disable NonReadonlyMemberInGetHashCode
    // public override int GetHashCode()
    // {
    //     HashCode hash = new();
    //     hash.Add(FrameNumber);
    //     hash.Add(NumberOfShips);
    //     hash.Add(Bounds);
    //     for (var i = 0; i < NumberOfShips; i++)
    //         hash.Add(Ships[i]);
    //
    //     return hash.ToHashCode();
    // }
}