namespace SpaceWar.Logic;

public sealed class Renderer(
    GameAssets gameAssets,
    SpriteBatch spriteBatch
)
{
    // string status = string.Empty;
    // public void SetStatusText(string text) => status = text;

    static readonly Color[] Colors =
    [
        Color.Green,
        Color.Red,
        Color.Cyan,
        Color.MediumPurple,
    ];

    public void Draw(GameState gs, NonGameState ngs)
    {
        for (var i = 0; i < gs.NumberOfShips; i++)
        {
            ref var ship = ref gs.Ships[i];

            var shipSprites = gameAssets.Ships[i];
            DrawBackground(ngs.Background);
            DrawShip(ship, shipSprites);
            DrawScore(ship.Score, i, gs.Bounds);
            // DrawConnectState(gs.Ships[i], ngs.Players[i]);
        }
    }

    void DrawScore(int score, int num, Rectangle bounds)
    {
        const int padding = 4;
        Vector2 position =
            num switch
            {
                0 => new(bounds.Left + padding, bounds.Top + padding),
                1 => new(bounds.Right - padding, bounds.Top + padding),
                2 => new(bounds.Left + padding, bounds.Bottom - padding),
                3 => new(bounds.Right - padding, bounds.Bottom - padding),
                _ => throw new ArgumentOutOfRangeException(nameof(num), num, null),
            };

        var color = Colors[num];

        spriteBatch.DrawString(
            gameAssets.MainFont,
            score.ToString(),
            position,
            color
        );
    }

    void DrawShip(Ship ship, ShipAsset sprite)
    {
        var shipSize = ship.Radius * 2;

        Rectangle shipRect = new(
            (int) ship.Position.X,
            (int) ship.Position.Y,
            shipSize, shipSize
        );

        var rotation = MathHelper.ToRadians(ship.Heading);

        spriteBatch.Draw(
            sprite.Ship,
            shipRect,
            null,
            Color.White,
            rotation,
            sprite.Ship.Bounds.Size.ToVector2() / 2,
            SpriteEffects.None, 0
        );

        if (ship.Thrust > 0)
        {
            spriteBatch.Draw(
                gameAssets.Thrust,
                ship.Position,
                null,
                Color.White,
                rotation,
                new Vector2(
                    gameAssets.Thrust.Bounds.Width + ship.Radius / 2f,
                    gameAssets.Thrust.Bounds.Height - ship.Radius / 2f
                ),
                1,
                SpriteEffects.None, 1
            );
        }

        for (var i = 0; i < ship.Bullets.Length; i++)
        {
            ref var bullet = ref ship.Bullets[i];
            if (!bullet.Active) continue;

            spriteBatch.Draw(gameAssets.Shot, bullet.Position, null, Color.White,
                rotation, gameAssets.Shot.Bounds.Size.ToVector2() / 2, 1.5f,
                SpriteEffects.None, 0);
        }
    }


    void DrawBackground(Background background)
    {
        for (var i = 0; i < background.StarMap.Length; i++)
        {
            ref var star = ref background.StarMap[i];
            var texture = star.Big ? gameAssets.StarBig : gameAssets.Star;
            spriteBatch.Draw(texture, star.Position, null,
                Color.DarkGray, 0, Vector2.Zero, 0.7f, SpriteEffects.None, 1);
        }
    }
}