using Microsoft.Xna.Framework;

namespace Sandbox.Shared.UI.Base;

public interface IUiRaycastTarget
{
    bool Contains(Point position);
}