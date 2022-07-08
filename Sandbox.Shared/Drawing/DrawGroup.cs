using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Sandbox.Shared.Drawing;

public class DrawGroup
{
    private readonly SpriteBatch _spriteBatch;

    public DrawGroup(GraphicsDevice device)
    {
        _spriteBatch = new SpriteBatch(device);
        _drawables = new List<IGroupDrawable>();
    }

    public DrawGroup(GraphicsDevice device, int capacity)
    {
        _spriteBatch = new SpriteBatch(device, capacity);
        _drawables = new List<IGroupDrawable>(capacity);
    }

    private readonly List<IGroupDrawable> _drawables;

    public void AddDrawable(IGroupDrawable drawable)
    {
        _drawables.Add(drawable);
    }

    public bool RemoveDrawable(IGroupDrawable drawable)
    {
        return _drawables.Remove(drawable);
    }

    public void AddDrawables(params IGroupDrawable[] drawables)
    {
        _drawables.AddRange(drawables);
    }

    public void RemoveDrawables(params IGroupDrawable[] drawables)
    {
        foreach (var drawable in drawables)
        {
            RemoveDrawable(drawable);
        }
    }

    public IReadOnlyList<IGroupDrawable> Drawables => _drawables;

    public void Draw(GameTime gameTime)
    {
        _spriteBatch.Begin();

        foreach (var drawable in _drawables)
        {
            drawable.Draw(gameTime, _spriteBatch);
        }

        _spriteBatch.End();
    }

    public static DrawGroup CreateFrom(GraphicsDevice device, params IGroupDrawable[] drawables)
    {
        var group = new DrawGroup(device, drawables.Length);
        foreach (var drawable in drawables)
        {
            group.AddDrawable(drawable);
        }

        return group;
    }
}