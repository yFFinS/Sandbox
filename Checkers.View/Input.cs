using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Checkers.View;

public static class Input
{
    private static InputApi _api = null!;

    public static Point MousePosition => _api.MousePosition;

    internal static void SetApi(InputApi api)
    {
        _api = api;
    }

    public static bool IsKeyUp(Keys key)
    {
        return _api.GetKeyState(key) == InputApi.FrameKeyState.ReleasedThisFrame;
    }

    public static bool IsKeyDown(Keys key)
    {
        return _api.GetKeyState(key) == InputApi.FrameKeyState.PressedThisFrame;
    }

    public static bool IsKey(Keys key)
    {
        var frameKeyState = _api.GetKeyState(key);
        return frameKeyState is InputApi.FrameKeyState.Pressed or InputApi.FrameKeyState.PressedThisFrame;
    }

    public static bool IsButtonUp(int button)
    {
        return _api.GetButtonState(button) == InputApi.FrameButtonState.ReleasedThisFrame;
    }

    public static bool IsButtonDown(int button)
    {
        return _api.GetButtonState(button) == InputApi.FrameButtonState.PressedThisFrame;
    }

    public static bool IsButton(int button)
    {
        var frameButtonState = _api.GetButtonState(button);
        return frameButtonState is InputApi.FrameButtonState.Pressed or InputApi.FrameButtonState.PressedThisFrame;
    }
}