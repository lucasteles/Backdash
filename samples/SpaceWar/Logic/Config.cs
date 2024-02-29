namespace SpaceWar.Logic;

public static class Config
{
    public const int MaxShips = 4;
    public const int WindowPadding = 8;
    public const int StartingHealth = 100;
    public const int RotateIncrement = 3;
    public const int ShipRadius = 30;
    public const float ShipThrust = 0.06f;
    public const float ShipMaxThrust = 4.0f;
    public const int ShipLifeBarHeight = 10;

    public const int MaxBullets = 30;
    public const int BulletSpeed = 5;
    public const int BulletCooldown = 12;
    public const int BulletDamage = 2;

    public const int MissileSpeed = 1;
    public const int MissileMaxSpeed = 14;
    public const float MissileAcceleration = 0.1f;
    public const int MissileDamage = 15;
    public const int MissileExplosionRadius = 120;
    public const int MissileProjectileRadius = 12;
    public const int MissileInvincibleTime = 15;
    public const int MissileExplosionTimeout = 60 * 5;
    public const int MissileDamageTime = 24;
    public const int MissileCooldown = 60 * 4;
}