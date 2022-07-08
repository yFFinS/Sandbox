using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Sandbox.Shared.Drawing;
using Sandbox.Shared.UI.Base;

namespace Sandbox.Shared.UI;

public class UiPanel : UiObject, IGroupDrawable
{
    public Texture2D? Background { get; set; }
    public Color BackgroundColor { get; set; }

    public void Draw(GameTime gameTime, SpriteBatch batch)
    {
        if (Background is not null)
        {
            batch.Draw(Background, Bounds, BackgroundColor);
        }
    }

    public static UiPanel CreatePlainPanel(GraphicsDevice device, Color color)
    {
        var panel = new UiPanel
        {
            Background = TextureFactory.CreateFilledRectTexture(device, Color.White),
            BackgroundColor = color
        };
        return panel;
    }
}