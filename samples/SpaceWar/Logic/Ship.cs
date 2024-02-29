namespace SpaceWar.Logic;

public sealed record Ship
{
    public int Id;
    public bool Active;
    public Vector2 Position;
    public Vector2 Velocity;
    public int Radius;
    public int Heading;
    public int Health;
    public int FireCooldown;
    public int MissileCooldown;
    public int Invincible;
    public int Score;
    public Missile Missile;
    public Bullet[] Bullets = new Bullet[Config.MaxBullets];
    public int Thrust;
}

public record struct Bullet
{
    public bool Active;
    public Vector2 Position;
    public Vector2 Velocity;
}

public record struct Missile
{
    public bool Active;
    public int ExplodeTimeout;
    public int DamageTime;
    public int ExplosionRadius;
    public int ProjectileRadius;
    public int Heading;
    public Vector2 Position;
    public Vector2 Velocity;

    public readonly bool IsExploding() => ExplodeTimeout is 0 && DamageTime > 0;
}