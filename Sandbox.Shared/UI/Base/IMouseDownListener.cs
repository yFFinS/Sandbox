using Microsoft.Xna.Framework;

namespace Sandbox.Shared.UI.Base;

public interface IMouseDownListener
{
    void OnMouseDown(Point position, MouseButton button);
}