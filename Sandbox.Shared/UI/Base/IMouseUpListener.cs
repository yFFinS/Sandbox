using Microsoft.Xna.Framework;

namespace Sandbox.Shared.UI.Base;

public interface IMouseUpListener
{
    void OnMouseUp(Point position, MouseButton button);
}