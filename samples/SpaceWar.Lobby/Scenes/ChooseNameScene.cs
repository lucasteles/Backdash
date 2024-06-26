using System.Text;
using System.Text.RegularExpressions;

namespace SpaceWar.Scenes;

public sealed class ChooseNameScene : Scene
{
    const int MaxNameSize = 40;
    const string Label = "Username";

    readonly StringBuilder username = new();
    readonly KeyboardController keyboard = new();
    readonly TimeSpan totalCursorBlinkTime = TimeSpan.FromMilliseconds(530);

    TimeSpan currentCursorBlinkTime;
    bool showCursor = true;
    Vector2 cursorSize;

    void OnTextInput(object? sender, TextInputEventArgs e)
    {
        if (username.Length > 0 && e.Key is Keys.Back or Keys.Delete)
        {
            username.Length--;
            return;
        }

        if (!char.IsLetterOrDigit(e.Character) && e.Character is not ('_' or '-'))
            return;

        username.Append(e.Character);
        username.Length = MathHelper.Clamp(username.Length, 0, MaxNameSize);
    }

    public override void Initialize()
    {
        var currentUser = string.IsNullOrWhiteSpace(Config.Username)
            ? Environment.UserName
            : Config.Username;

        username.Append(Regex.Replace(currentUser.ToLower(), "[^a-zA-Z0-9]", "_"));
        cursorSize = Assets.MainFont.MeasureString(" ");
        Window.TextInput += OnTextInput;
        keyboard.Update();
    }

    public override void Update(GameTime gameTime)
    {
        currentCursorBlinkTime += gameTime.ElapsedGameTime;

        if (currentCursorBlinkTime > totalCursorBlinkTime)
        {
            showCursor = !showCursor;
            currentCursorBlinkTime = default;
        }

        keyboard.Update();
        if (keyboard.IsKeyPressed(Keys.Enter))
        {
            Config.Username = username.ToString();
            LoadScene(new ChooseModeScene());
        }
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        var nameSize = Assets.MainFont.MeasureString(username);
        var halfSize = nameSize / 2;
        var center = Viewport.Center.ToVector2();

        spriteBatch.DrawString(Assets.MainFont, username, center, Color.White,
            0, halfSize, 1, SpriteEffects.None, 0);

        if (showCursor)
        {
            Rectangle cursorRect = new(
                (int)(center.X + halfSize.X),
                (int)(center.Y - cursorSize.Y / 2),
                (int)cursorSize.X,
                (int)(cursorSize.Y * 0.9)
            );
            spriteBatch.Draw(Assets.Blank, cursorRect, Color.White);
        }

        var labelSize = Assets.MainFont.MeasureString(Label);
        spriteBatch.DrawString(Assets.MainFont, Label,
            new(center.X, center.Y - cursorSize.Y * 1.5f),
            Color.Yellow, 0, labelSize / 2, 1, SpriteEffects.None, 0);
    }

    protected override void Dispose(bool disposing) => Window.TextInput -= OnTextInput;
}
