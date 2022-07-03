using Microsoft.Xna.Framework;

namespace Sandbox.Shared.UI;

public class UiManager
{
    private Point _lastMousePosition;

    internal void RegisterUiObject(UiObject uiObject)
    {
        _uiObjects.Add(uiObject);
    }

    internal void UnregisterUiObject(UiObject uiObject)
    {
        _uiObjects.Remove(uiObject);
    }

    private readonly List<UiObject> _uiObjects = new();

    public void Draw(GameTime gameTime)
    {

    }

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

        foreach (var uiObject in _uiObjects)
        {
            if (uiObject is not IUiRaycastTarget target)
            {
                continue;
            }

            var mouseState = GetMouseState(uiObject);
            var contains = target.Contains(mousePosition);

            if (uiObject is IMouseMoveListener moveListener)
            {
                if (mouseMoved)
                {
                    moveListener.OnMouseMove(mousePosition);
                }
            }

            if (uiObject is IMouseExitListener exitListener)
            {
                if (!contains && mouseState == MouseState.MouseIn)
                {
                    exitListener.OnMouseExit();
                }
            }

            if (uiObject is IMouseEnterListener enterListener)
            {
                if (contains && mouseState == MouseState.MouseOut)
                {
                    enterListener.OnMouseEnter();
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

    private static bool IsInBounds(UiObject uiObject, Point position)
    {
        return uiObject.Bounds.Contains(position);
    }

    private MouseState GetMouseState(UiObject uiObject)
    {
        return _mouseStates.TryGetValue(uiObject, out var currentState) ? currentState : MouseState.MouseOut;
    }

    private void SetMouseState(UiObject uiObject, MouseState mouseState)
    {
        _mouseStates[uiObject] = mouseState;
    }
}