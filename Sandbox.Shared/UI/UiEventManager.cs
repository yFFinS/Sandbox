using Microsoft.Xna.Framework;

namespace Sandbox.Shared.UI;

public class UiEventManager
{
    private Point _lastMousePosition;

    public UiObject? Root { get; set; }

    public void Update(GameTime gameTime)
    {
        var mousePosition = Input.MousePosition;
        var mouseMoved = mousePosition != _lastMousePosition;
        _lastMousePosition = mousePosition;

        if (Root is null)
        {
            return;
        }

        if (mouseMoved)
        {
            DispatchEvent(obj => obj.OnMouseEnter(), obj =>
            {
                if (IsCurrentMouseState(obj, MouseState.MouseOut) && IsInBounds(obj, mousePosition))
                {
                    SetMouseState(obj, MouseState.MouseIn);
                    return true;
                }

                return false;
            });

            DispatchEvent(obj => obj.OnMouseExit(), obj =>
            {
                if (IsCurrentMouseState(obj, MouseState.MouseIn) && !IsInBounds(obj, mousePosition))
                {
                    SetMouseState(obj, MouseState.MouseOut);
                    return true;
                }

                return false;
            });

            DispatchEvent(obj => obj.OnMouseMove(mousePosition),
                obj => IsCurrentMouseState(obj, MouseState.MouseIn) && IsInBounds(obj, mousePosition));
        }

        DispatchButtonEvent(mousePosition, MouseButton.Left);
        DispatchButtonEvent(mousePosition, MouseButton.Right);
        DispatchButtonEvent(mousePosition, MouseButton.Middle);
    }

    private void DispatchButtonEvent(Point mousePosition, MouseButton button)
    {
        var inputApi = Input.Api;
        var buttonState = inputApi.GetButtonState((int)button);

        switch (buttonState)
        {
            case InputApi.FrameButtonState.Released or InputApi.FrameButtonState.ReleasedThisFrame:
                DispatchEvent(obj => obj.OnMouseUp(mousePosition, button),
                    obj => IsInBounds(obj, mousePosition));
                break;
            case InputApi.FrameButtonState.Pressed or InputApi.FrameButtonState.PressedThisFrame:
                DispatchEvent(obj => obj.OnMouseDown(mousePosition, button),
                    obj => IsInBounds(obj, mousePosition));
                break;
            default:
                throw new ArgumentOutOfRangeException();
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

    private bool IsCurrentMouseState(UiObject uiObject, MouseState mouseState)
    {
        if (!_mouseStates.TryGetValue(uiObject, out var currentState))
        {
            return mouseState == MouseState.MouseOut;
        }

        return currentState == mouseState;
    }

    private void SetMouseState(UiObject uiObject, MouseState mouseState)
    {
        _mouseStates[uiObject] = mouseState;
    }

    private void DispatchEvent(Func<UiObject, bool> eventCaller, Func<UiObject, bool>? eventFilter = null)
    {
        var queue = new Queue<UiObject>();
        queue.Enqueue(Root!);

        while (queue.Count > 0)
        {
            var uiObject = queue.Dequeue();
            if ((eventFilter?.Invoke(uiObject) ?? true) && eventCaller(uiObject))
            {
                break;
            }

            foreach (var child in uiObject.Children)
            {
                queue.Enqueue(child);
            }
        }
    }
}