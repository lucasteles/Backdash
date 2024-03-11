using SpaceWar.Models;
using SpaceWar.Util;

namespace SpaceWar.Scenes;

public sealed class ChooseModeScene(string username) : Scene
{
    const string TitleLabel = "What do you want?";
    const string PlayLabel = "Play";
    const string SpectatorLabel = "Spectate";

    readonly KeyboardController keyboard = new();

    PlayerMode selectedMode = PlayerMode.Player;
    Vector2 buttonSize = new(300);

    public override void Initialize() => keyboard.Update();

    public override void Update(GameTime gameTime)
    {
        keyboard.Update();
        if (keyboard.IsKeyPressed(Keys.Left) || keyboard.IsKeyPressed(Keys.Right))
            selectedMode = selectedMode switch
            {
                PlayerMode.Player => PlayerMode.Spectator,
                PlayerMode.Spectator => PlayerMode.Player,
                _ => throw new InvalidOperationException(),
            };

        if (keyboard.IsKeyPressed(Keys.Enter))
            LoadScene(new LobbyScene(username, selectedMode));
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        var center = Viewport.Center.ToVector2();

        DrawButton(spriteBatch, PlayLabel,
            new(center.X - 50 - buttonSize.X, center.Y - buttonSize.Y / 2),
            selectedMode is PlayerMode.Player);

        DrawButton(spriteBatch, SpectatorLabel,
            new(center.X + 50, center.Y - buttonSize.Y / 2),
            selectedMode is PlayerMode.Spectator);

        var labelSize = Assets.MainFont.MeasureString(TitleLabel);
        spriteBatch.DrawString(Assets.MainFont, TitleLabel,
            new(center.X, center.Y - buttonSize.Y),
            Color.Yellow, 0, labelSize / 2, 1, SpriteEffects.None, 0);
    }

    void DrawButton(SpriteBatch spriteBatch, string text, Vector2 position, bool selected)
    {
        var textSize = Assets.MainFont.MeasureString(text);
        var color = selected ? Color.LimeGreen : Color.Gray;

        Rectangle block = new(position.ToPoint(), buttonSize.ToPoint());
        var bounds = block;
        bounds.Inflate(-8, -8);

        spriteBatch.Draw(Assets.Blank, block, color);
        spriteBatch.Draw(Assets.Blank, bounds, Color.Black);
        spriteBatch.DrawString(Assets.MainFont, text,
            bounds.Center.ToVector2(), color, 0,
            textSize / 2, 1, SpriteEffects.None, 0);
    }
}
