using Microsoft.Xna.Framework;

namespace Sandbox.Shared.UI.Base;

internal class UiManager
{
    private static UiManager? _instance;

    public static UiManager Instance => _instance!;

    private Point _lastMousePosition;

    public UiManager()
    {
        if (_instance is not null)
        {
            throw new InvalidOperationException();
        }

        _instance = this;
    }

    public void RegisterUiObject(UiObject uiObject)
    {
        _uiObjects.Add(uiObject);
    }

    public void UnregisterUiObject(UiObject uiObject)
    {
        _uiObjects.Remove(uiObject);
    }

    private readonly List<UiObject> _uiObjects = new();

    public void Update(GameTime gameTime)
    {
        var inputApi = Input.Api;
        var mousePosition = Input.MousePosition;
        var mouseMoved = mousePosition != _lastMousePosition;
        _lastMousePosition = mousePosition;

        if (_uiObjects.Count == 0)
        {
            return;
        }

        var buttonStates = Enumerable.Range(0, InputApi.ButtonCount)
            .Select(i => (i, inputApi.GetButtonState(i)))
            .ToArray();

        var enabledThisFrame = _uiObjects.Where(obj => obj.Enabled).ToArray();
        foreach (var uiObject in enabledThisFrame)
        {
            if (uiObject is not IUiRaycastTarget target)
            {
                continue;
            }

            var mouseState = GetMouseState(uiObject);
            var contains = target.Contains(mousePosition);

            switch (uiObject)
            {
                case IMouseMoveListener moveListener:
                {
                    if (mouseMoved)
                    {
                        moveListener.OnMouseMove(mousePosition);
                    }

                    break;
                }
                case IMouseExitListener exitListener:
                {
                    if (!contains && mouseState == MouseState.MouseIn)
                    {
                        exitListener.OnMouseExit();
                    }

                    break;
                }
                case IMouseEnterListener enterListener:
                {
                    if (contains && mouseState == MouseState.MouseOut)
                    {
                        enterListener.OnMouseEnter();
                    }

                    break;
                }
            }

            var newMouseState = contains ? MouseState.MouseIn : MouseState.MouseOut;
            SetMouseState(uiObject, newMouseState);

            if (!contains)
            {
                continue;
            }

            foreach (var (buttonIndex, buttonState) in buttonStates)
            {
                var button = (MouseButton)buttonIndex;
                switch (buttonState)
                {
                    case InputApi.FrameButtonState.ReleasedThisFrame:
                        if (uiObject is IMouseUpListener upListener)
                        {
                            upListener.OnMouseUp(mousePosition, button);
                        }

                        break;
                    case InputApi.FrameButtonState.PressedThisFrame:
                        if (uiObject is IMouseDownListener downListener)
                        {
                            downListener.OnMouseDown(mousePosition, button);
                        }

                        break;
                }
            }
        }
    }

    private enum MouseState
    {
        MouseIn,
        MouseOut
    }

    private readonly Dictionary<UiObject, MouseState> _mouseStates = new();

    private MouseState GetMouseState(UiObject uiObject)
    {
        return _mouseStates.TryGetValue(uiObject, out var currentState) ? currentState : MouseState.MouseOut;
    }

    private void SetMouseState(UiObject uiObject, MouseState mouseState)
    {
        _mouseStates[uiObject] = mouseState;
    }
}