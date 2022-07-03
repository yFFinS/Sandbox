using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Sandbox.Shared;

public static class Input
{
    public static Point MousePosition => Api.MousePosition;

    internal static void SetApi(InputApi api)
    {
        Api = api;
    }

    internal static InputApi Api { get; private set; } = null!;

    public static bool IsKeyUp(Keys key)
    {
        return Api.GetKeyState(key) == InputApi.FrameKeyState.ReleasedThisFrame;
    }

    public static bool IsKeyDown(Keys key)
    {
        return Api.GetKeyState(key) == InputApi.FrameKeyState.PressedThisFrame;
    }

    public static bool IsKey(Keys key)
    {
        var frameKeyState = Api.GetKeyState(key);
        return frameKeyState is InputApi.FrameKeyState.Pressed or InputApi.FrameKeyState.PressedThisFrame;
    }

    public static bool IsButtonUp(MouseButton button)
    {
        return Api.GetButtonState((int)button) == InputApi.FrameButtonState.ReleasedThisFrame;
    }

    public static bool IsButtonDown(MouseButton button)
    {
        return Api.GetButtonState((int)button) == InputApi.FrameButtonState.PressedThisFrame;
    }

    public static bool IsButton(int button)
    {
        var frameButtonState = Api.GetButtonState(button);
        return frameButtonState is InputApi.FrameButtonState.Pressed or InputApi.FrameButtonState.PressedThisFrame;
    }
}