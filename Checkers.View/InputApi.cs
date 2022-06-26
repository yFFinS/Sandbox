using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Checkers.View;

internal class InputApi
{
    internal enum FrameKeyState
    {
        Released = 0,
        Pressed = 1,
        PressedThisFrame = 2,
        ReleasedThisFrame = 3
    }

    internal enum FrameButtonState
    {
        Released = 0,
        Pressed = 1,
        PressedThisFrame = 2,
        ReleasedThisFrame = 3
    }

    private readonly Dictionary<Keys, FrameKeyState> _keyStates;

    private readonly FrameButtonState[] _buttons = new FrameButtonState[ButtonCount];
    public const int ButtonCount = 3;

    public Point MousePosition { get; private set; }

    public InputApi()
    {
        _keyStates = EnumHelper.CreateValueMap<Keys, FrameKeyState>();
    }

    public void Update(GameTime gameTime)
    {
        ProcessMouse(Mouse.GetState());
        ProcessKeyboard(Keyboard.GetState());
    }

    private void ProcessKeyboard(KeyboardState keyboardState)
    {
        var pressedKeys = keyboardState.GetPressedKeys().ToHashSet();
        var releasedKeys = _keyStates.Keys.Where(key => !pressedKeys.Contains(key)).ToArray();
        foreach (var pressedKey in pressedKeys)
        {
            _keyStates[pressedKey] = _keyStates[pressedKey] switch
            {
                FrameKeyState.Released or FrameKeyState.ReleasedThisFrame => FrameKeyState.PressedThisFrame,
                _ => FrameKeyState.Pressed
            };
        }

        foreach (var releasedKey in releasedKeys)
        {
            _keyStates[releasedKey] = _keyStates[releasedKey] switch
            {
                FrameKeyState.Pressed or FrameKeyState.PressedThisFrame => FrameKeyState.ReleasedThisFrame,
                _ => FrameKeyState.Released
            };
        }
    }

    private void ProcessMouse(MouseState mouseState)
    {
        void ProcessButton(int index, ButtonState inputState)
        {
            _buttons[index] = _buttons[index] switch
            {
                FrameButtonState.Released or FrameButtonState.ReleasedThisFrame => inputState == ButtonState.Pressed
                    ? FrameButtonState.PressedThisFrame
                    : FrameButtonState.Released,
                _ => inputState == ButtonState.Released
                    ? FrameButtonState.ReleasedThisFrame
                    : FrameButtonState.Pressed
            };
        }

        ProcessButton(0, mouseState.LeftButton);
        ProcessButton(1, mouseState.RightButton);
        ProcessButton(2, mouseState.MiddleButton);
        MousePosition = mouseState.Position;
    }

    public FrameKeyState GetKeyState(Keys key) => _keyStates[key];
    public FrameButtonState GetButtonState(int index) => _buttons[index];
}