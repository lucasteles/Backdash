using Backdash.Data;

namespace SpaceWar.Logic;

public sealed record GameState
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
        Bounds.Inflate(-Config.WindowPadding, -Config.WindowPadding);

        var width = Bounds.Right - Bounds.Left;
        var height = Bounds.Bottom - Bounds.Top;
        var r = height / 3;

        for (var i = 0; i < numberOfPlayers; i++)
        {
            var heading = (i + 1) * 360f / numberOfPlayers;
            var theta = MathHelper.ToRadians(heading);
            var (cosT, sinT) = (MathF.Cos(theta), MathF.Sin(theta));

            var x = width / 2f + r * cosT;
            var y = height / 2f + r * sinT;
            Ships[i].Id = i + 1;
            Ships[i].Position = new(x, y);
            Ships[i].Active = true;
            Ships[i].Heading = (int) ((heading + 180) % 360);
            Ships[i].Health = Config.StartingHealth;
            Ships[i].Radius = Config.ShipRadius;
        }
    }

    public GameInput GetShipAI(in Ship ship) => new(
        Heading: (ship.Heading + 5f) % 360f,
        Thrust: 0,
        Fire: false,
        Missile: false
    );

    public GameInput ParseShipInputs(PlayerInputs inputs, in Ship ship)
    {
        if (!ship.Active)
            return new();

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

        return new(heading, thrust,
            inputs.HasFlag(PlayerInputs.Fire),
            inputs.HasFlag(PlayerInputs.Missile)
        );
    }

    public void UpdateShip(in Ship ship, in GameInput inputs)
    {
        ship.Heading = (int) inputs.Heading;

        Vector2 rotation = new(
            MathF.Cos(MathHelper.ToRadians(ship.Heading)),
            MathF.Sin(MathHelper.ToRadians(ship.Heading))
        );

        if (inputs.Fire && ship.FireCooldown is 0)
            for (var i = 0; i < ship.Bullets.Length; i++)
            {
                ref var bullet = ref ship.Bullets[i];
                if (bullet.Active)
                    continue;

                bullet.Active = true;
                bullet.Position = ship.Position + rotation * ship.Radius;
                bullet.Velocity = ship.Velocity + rotation * Config.BulletSpeed;
                ship.FireCooldown = Config.BulletCooldown;
                break;
            }

        if (inputs.Missile && ship.MissileCooldown is 0 && !ship.Missile.Active)
        {
            ship.MissileCooldown = Config.MissileCooldown;
            ship.Missile.Active = true;
            ship.Missile.Heading = ship.Heading;
            ship.Missile.ProjectileRadius = Config.MissileProjectileRadius;
            ship.Missile.ExplosionRadius = Config.MissileExplosionRadius;
            ship.Missile.ExplodeTimeout = Config.MissileExplosionTimeout;
            ship.Missile.DamageTime = Config.MissileDamageTime;
            ship.Missile.Velocity = rotation * Config.MissileSpeed;
            ship.Missile.Position = ship.Position + ship.Velocity +
                                    rotation * (ship.Radius + ship.Missile.ProjectileRadius);

            ship.Velocity += ship.Missile.Velocity * -2;
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
                if (!other.Active)
                    continue;

                if (other.Missile.Active
                    && !other.Missile.IsExploding()
                    && Vector2.Distance(bullet.Position, other.Missile.Position) <
                    other.Missile.ProjectileRadius)
                {
                    other.Missile.ExplodeTimeout = 0;
                    bullet.Active = false;
                    continue;
                }

                if (Vector2.Distance(bullet.Position, other.Position) > other.Radius)
                    continue;

                ship.Score++;
                other.Health -= Config.BulletDamage;
                bullet.Active = false;
                break;
            }
        }

        if (ship.Missile.Active)
        {
            ref var missile = ref ship.Missile;
            missile.Position += missile.Velocity;

            if (missile.Velocity.Length() < Config.MissileMaxSpeed)
                missile.Velocity +=
                    Vector2.Normalize(missile.Velocity) * Config.MissileAcceleration;

            if (missile.DamageTime <= 0)
                missile.Active = false;
            else
                for (var j = 0; j < NumberOfShips; j++)
                {
                    ref var other = ref Ships[j];
                    var distance = Vector2.Distance(missile.Position, other.Position);
                    if (!missile.IsExploding())
                    {
                        if (distance - missile.ProjectileRadius > other.Radius) continue;
                        if (Config.MissileExplosionTimeout - missile.ExplodeTimeout < 30 &&
                            other.Id == ship.Id) continue;

                        missile.ExplodeTimeout = 0;
                    }

                    if (missile.ExplodeTimeout > 0) continue;
                    if (other.Invincible > 0) continue;
                    if (distance - missile.ExplosionRadius > other.Radius) continue;

                    if (other.Id != ship.Id)
                        ship.Score++;

                    other.Health -= Config.MissileDamage;
                    other.Invincible = Config.MissileInvincibleTime;
                    var pushDirection = Vector2.Normalize(other.Position - missile.Position);
                    other.Velocity = pushDirection * Config.ShipMaxThrust;
                    other.Position += other.Velocity * 2;
                }

            if (!Bounds.Contains(missile.Position))
            {
                var normal = Vector2.Zero;
                if (missile.Position.X < Bounds.Left) normal = Vector2.UnitX;
                else if (missile.Position.X > Bounds.Right) normal = -Vector2.UnitX;
                else if (missile.Position.Y < Bounds.Top) normal = Vector2.UnitY;
                else if (missile.Position.Y > Bounds.Bottom) normal = -Vector2.UnitY;
                else missile.ExplodeTimeout = 0;

                var newVelocity = Vector2.Reflect(missile.Velocity, normal);
                missile.Heading = (int) MathHelper.ToDegrees(
                    MathF.Atan2(newVelocity.Y, newVelocity.X));
                missile.Velocity = newVelocity;
            }

            if (missile.ExplodeTimeout > 0)
                missile.ExplodeTimeout--;

            if (missile.ExplodeTimeout is 0)
                missile.Velocity = Vector2.Zero;

            if (missile.IsExploding())
                missile.DamageTime--;
        }

        if (ship.FireCooldown > 0)
            ship.FireCooldown--;

        if (ship.MissileCooldown > 0)
            ship.MissileCooldown--;

        if (ship.Invincible > 0)
            ship.Invincible--;

        if (ship.Health <= 0)
            ship.Active = false;
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