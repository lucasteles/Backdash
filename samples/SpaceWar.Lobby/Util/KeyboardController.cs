namespace SpaceWar.Util;

using Microsoft.Xna.Framework.Input;

public class KeyboardController
{
    KeyboardState previousKeyState;
    KeyboardState currentKeyState;

    public void Update()
    {
        previousKeyState = currentKeyState;
        currentKeyState = Keyboard.GetState();
    }

    public bool IsKeyDown(Keys key) => currentKeyState.IsKeyDown(key);
    public bool WasKeyDown(Keys key) => previousKeyState.IsKeyDown(key);
    public bool IsKeyPressed(Keys key) => IsKeyDown(key) && !WasKeyDown(key);
}
