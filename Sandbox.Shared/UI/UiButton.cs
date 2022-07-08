using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Sandbox.Shared.Drawing;
using Sandbox.Shared.UI.Base;

namespace Sandbox.Shared.UI;

public class UiButton : UiObject, IUiRaycastTarget, IMouseDownListener, IGroupDrawable
{
    public readonly UiText UiText;
    public readonly UiPanel UiPanel;

    public event Action? Clicked;

    public MouseButton ClickButton { get; set; } = MouseButton.Left;

    public void OnMouseDown(Point position, MouseButton button)
    {
        if (button == ClickButton)
        {
            Clicked?.Invoke();
        }
    }

    public virtual bool Contains(Point position)
    {
        return Bounds.Contains(position);
    }

    public UiButton(GraphicsDevice device, Color backgroundColor, string text)
    {
        UiPanel = UiPanel.CreatePlainPanel(device, backgroundColor);
        UiText = new UiText(text);
    }

    public void Draw(GameTime gameTime, SpriteBatch batch)
    {
        UiPanel.Draw(gameTime, batch);
        UiText.Draw(gameTime, batch);
    }

    protected override void OnBoundsChanged()
    {
        UiPanel.Bounds = Bounds;
        UiText.Bounds = Bounds;
    }
}