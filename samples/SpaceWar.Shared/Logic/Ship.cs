using Backdash.Serialization;
using Backdash.Serialization.Numerics;

namespace SpaceWar.Logic;

public sealed record Ship : IBinarySerializable
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
    public readonly Bullet[] Bullets = new Bullet[Config.MaxBullets];

    public void Serialize(ref readonly BinaryBufferWriter writer)
    {
        writer.Write(in Id);
        writer.Write(in Active);
        writer.Write(in Position);
        writer.Write(in Velocity);
        writer.Write(in Radius);
        writer.Write(in Heading);
        writer.Write(in Health);
        writer.Write(in FireCooldown);
        writer.Write(in MissileCooldown);
        writer.Write(in Invincible);
        writer.Write(in Score);
        writer.Write(in Thrust);

        // Caution: WriteStruct not normalize endianness
        writer.WriteStruct(in Missile);
        writer.WriteStruct(in Bullets);
    }

    public void Deserialize(ref readonly BinaryBufferReader reader)
    {
        Id = reader.ReadByte();
        Active = reader.ReadBoolean();
        Position = reader.ReadVector2();
        Velocity = reader.ReadVector2();
        Radius = reader.ReadInt32();
        Heading = reader.ReadInt32();
        Health = reader.ReadInt32();
        FireCooldown = reader.ReadInt32();
        MissileCooldown = reader.ReadInt32();
        Invincible = reader.ReadInt32();
        Score = reader.ReadInt32();
        Thrust = reader.ReadInt32();

        reader.ReadStruct(ref Missile);
        reader.ReadStruct(in Bullets);
    }
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
