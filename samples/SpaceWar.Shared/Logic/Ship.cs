using Backdash.Serialization;

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
    public Missile Missile = new();
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
        writer.Write(in Missile);

        // Caution: WriteStruct not normalize endianness
        writer.WriteStruct(in Bullets);
    }

    public void Deserialize(ref readonly BinaryBufferReader reader)
    {
        reader.Read(ref Id);
        reader.Read(ref Active);
        reader.Read(ref Position);
        reader.Read(ref Velocity);
        reader.Read(ref Radius);
        reader.Read(ref Heading);
        reader.Read(ref Health);
        reader.Read(ref FireCooldown);
        reader.Read(ref MissileCooldown);
        reader.Read(ref Invincible);
        reader.Read(ref Score);
        reader.Read(ref Thrust);
        reader.Read(Missile);

        reader.ReadStruct(in Bullets);
    }
}

public record struct Bullet
{
    public bool Active;
    public Vector2 Position;
    public Vector2 Velocity;
}

public record Missile : IBinarySerializable
{
    public bool Active;
    public int ExplodeTimeout;
    public int HitBoxTime;
    public int ExplosionRadius;
    public int ProjectileRadius;
    public int Heading;
    public Vector2 Position;
    public Vector2 Velocity;
    public bool IsExploding() => ExplodeTimeout is 0 && HitBoxTime > 0;

    public void Serialize(ref readonly BinaryBufferWriter writer)
    {
        writer.Write(in Active);
        writer.Write(in ExplodeTimeout);
        writer.Write(in HitBoxTime);
        writer.Write(in ExplosionRadius);
        writer.Write(in ProjectileRadius);
        writer.Write(in Heading);
        writer.Write(in Position);
        writer.Write(in Velocity);
    }

    public void Deserialize(ref readonly BinaryBufferReader reader)
    {
        reader.Read(ref Active);
        reader.Read(ref ExplodeTimeout);
        reader.Read(ref HitBoxTime);
        reader.Read(ref ExplosionRadius);
        reader.Read(ref ProjectileRadius);
        reader.Read(ref Heading);
        reader.Read(ref Position);
        reader.Read(ref Velocity);
    }
}
