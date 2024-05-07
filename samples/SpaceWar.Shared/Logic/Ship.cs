using Backdash.Data;
namespace SpaceWar.Logic;

public sealed record Ship
{
    public byte Id;
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
    public int Thrust;
    public Missile Missile;
    public readonly Array<Bullet> Bullets = new(Config.MaxBullets);
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
    public int HitBoxTime;
    public int ExplosionRadius;
    public int ProjectileRadius;
    public int Heading;
    public Vector2 Position;
    public Vector2 Velocity;
    public readonly bool IsExploding() => ExplodeTimeout is 0 && HitBoxTime > 0;
}
