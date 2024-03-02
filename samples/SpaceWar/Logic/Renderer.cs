namespace SpaceWar.Logic;

public sealed class Renderer(
    GameAssets gameAssets,
    SpriteBatch spriteBatch
)
{
    // string status = string.Empty;
    // public void SetStatusText(string text) => status = text;

    public void Draw(GameState gs, NonGameState ngs)
    {
        DrawBackground(ngs.Background);
        for (var i = 0; i < gs.NumberOfShips; i++)
        {
            DrawShip(i, gs);
            DrawScore(i, gs);
            // DrawConnectState(gs.Ships[i], ngs.Players[i]);
        }
    }

    void DrawShip(int num, GameState gs)
    {
        var ship = gs.Ships[num];
        var sprite = gameAssets.Ships[num];
        var shipSize = ship.Radius * 2;

        if (!ship.Active) return;

        Rectangle shipRect = new(
            (int)ship.Position.X,
            (int)ship.Position.Y,
            shipSize, shipSize
        );

        var rotation = MathHelper.ToRadians(ship.Heading);

        if (ship.Thrust > 0)
            spriteBatch.Draw(
                gameAssets.Thrust, ship.Position, null, Color.White, rotation,
                new Vector2(
                    gameAssets.Thrust.Bounds.Width + ship.Radius / 2f,
                    gameAssets.Thrust.Bounds.Height - ship.Radius / 2f),
                1, SpriteEffects.None, 0);

        for (var i = 0; i < ship.Bullets.Length; i++)
        {
            ref var bullet = ref ship.Bullets[i];
            if (!bullet.Active) continue;

            spriteBatch.Draw(gameAssets.Shot, bullet.Position, null, Color.White,
                0, gameAssets.Shot.Bounds.Size.ToVector2() / 2, 1.5f,
                SpriteEffects.None, 0);
        }

        if (ship.Missile.Active)
            if (!ship.Missile.IsExploding())
            {
                var missileSize = ship.Missile.ProjectileRadius * 2f;
                var missileScale = missileSize / sprite.Missile.Bounds.Height;

                spriteBatch.Draw(
                    sprite.Missile, ship.Missile.Position, null, Color.White,
                    MathHelper.ToRadians(ship.Missile.Heading),
                    sprite.Missile.Bounds.Size.ToVector2() / 2,
                    missileScale,
                    SpriteEffects.None, 0);
            }
            else
            {
                var explosionSize = ship.Missile.ExplosionRadius * 2;
                Rectangle explosionRect = new(
                    (int)ship.Missile.Position.X,
                    (int)ship.Missile.Position.Y,
                    explosionSize, explosionSize
                );

                var spriteStep = (int)MathHelper.Lerp(
                    0, MissileExplosionSpriteMap.Length - 1,
                    ship.Missile.HitBoxTime / (float)Config.MissileHitBoxTimeout
                );

                var missileSource = MissileExplosionSpriteMap[spriteStep];
                missileSource.Inflate(-5, -5);

                spriteBatch.Draw(gameAssets.ExplosionSheet, explosionRect, missileSource,
                    Color.White, 0, missileSource.Size.ToVector2() / 2,
                    SpriteEffects.None, 0);
            }

        spriteBatch.Draw(sprite.Ship, shipRect, null, Color.White, rotation,
            sprite.Ship.Bounds.Size.ToVector2() / 2, SpriteEffects.None, 1);

        DrawBar(
            new(
                shipRect.Left - ship.Radius, shipRect.Bottom - ship.Radius / 2,
                shipRect.Width, Config.ShipLifeBarHeight
            ),
            Color.Green, ship.Health, Config.StartingHealth
        );
    }

    void DrawBackground(Background background)
    {
        for (var i = 0; i < background.StarMap.Length; i++)
        {
            ref var star = ref background.StarMap[i];
            var texture = star.Big ? gameAssets.StarBig : gameAssets.Star;
            spriteBatch.Draw(texture, star.Position, null,
                Color.DarkGray, 0, Vector2.Zero, 0.7f, SpriteEffects.None, 0);
        }
    }

    void DrawScore(int num, GameState gs)
    {
        const int padding = 4;
        var score = gs.Ships[num].Score;
        var bounds = gs.Bounds;
        var text = score.ToString();
        var size = gameAssets.MainFont.MeasureString(text);

        Vector2 scorePosition =
            num switch
            {
                0 => new(bounds.Left + padding, bounds.Top + padding),
                1 => new(bounds.Right - padding - size.X, bounds.Top + padding),
                2 => new(bounds.Left + padding, bounds.Bottom - padding - size.Y),
                3 => new(bounds.Right - padding - size.X, bounds.Bottom - padding - size.Y),
                _ => throw new ArgumentOutOfRangeException(nameof(num), num, null),
            };

        var color = Colors[num];

        spriteBatch.DrawString(
            gameAssets.MainFont,
            text,
            scorePosition,
            color
        );
    }

    void DrawBar(Rectangle position, Color color, float actual, float total, int padding = 1)
    {
        spriteBatch.Draw(gameAssets.Blank, position, null, Color.LightGray, 0, Vector2.Zero,
            SpriteEffects.None, 0);

        position.Inflate(padding * -2, padding * -2);
        position.Offset(padding, padding);

        spriteBatch.Draw(gameAssets.Blank, position, null, Color.DarkGray, 0, Vector2.Zero,
            SpriteEffects.None, 0);

        Rectangle value = new(
            position.X, position.Y,
            (int)(actual / total * position.Width),
            position.Height
        );

        spriteBatch.Draw(gameAssets.Blank, value, null, color,
            0, Vector2.Zero, SpriteEffects.None, 0);
    }

    static readonly Rectangle[] MissileExplosionSpriteMap =
    [
        new(1, 1, 89, 89),
        new(93, 1, 89, 89),
        new(185, 1, 89, 89),
        new(277, 1, 89, 89),
        new(1, 93, 89, 89),
        new(93, 93, 89, 89),
        new(185, 93, 89, 89),
        new(277, 93, 89, 89),
    ];

    static readonly Color[] Colors =
    [
        Color.Green,
        Color.Red,
        Color.Cyan,
        Color.MediumPurple,
    ];
}