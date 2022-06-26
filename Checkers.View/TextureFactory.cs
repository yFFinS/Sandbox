using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Checkers.View;

internal static class TextureFactory
{
    public static Texture2D CreateFilledRectTexture(GraphicsDevice device, Color color, int width = 1, int height = 1)
    {
        var texture = new Texture2D(device, width, height);
        texture.SetData(new[] { color });
        return texture;
    }

    public static Texture2D CreateFilledCircleTexture(GraphicsDevice device, Color color, int radius, int size)
    {
        var texture = new Texture2D(device, size, size);
        var colorData = new Color[size * size];

        var padding = (size - 2 * radius) / 2;
        
        for (var x = 0; x < size; x++)
        {
            for (var y = 0; y < size; y++)
            {
                var index = x * size + y;
                var pos = new Vector2(x - radius - padding, y - radius - padding);
                colorData[index] = pos.LengthSquared() <= radius * radius ? color : Color.Transparent;
            }
        }

        texture.SetData(colorData);
        return texture;
    }
}