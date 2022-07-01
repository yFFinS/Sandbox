﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Sandbox.Shared.UI;

public abstract class UiObject
{
    public Rectangle Bounds { get; private set; }
    protected GraphicsDevice GraphicsDevice { get; private set; } = null!;

    public int Width
    {
        get => Bounds.Width;
        set
        {
            var newSize = new Point(value, Height);
            Resize(newSize);
        }
    }

    public int Height
    {
        get => Bounds.Height;
        set
        {
            var newSize = new Point(Width, value);
            Resize(newSize);
        }
    }

    public Point Size
    {
        get => Bounds.Size;
        set => Resize(value);
    }

    public Point TopLeft
    {
        get => Bounds.Location;
        set => Bounds = new Rectangle(value, Bounds.Location);
    }

    public Point Center
    {
        get => Bounds.Center;
        set => Bounds = new Rectangle(TopLeft + value - Bounds.Center, Bounds.Size);
    }

    private void Resize(Point newSize)
    {
        if (newSize.X <= 1 || newSize.Y <= 1)
        {
            throw new ArgumentException("Width and height should be greater or equal to 1.", nameof(newSize));
        }

        BeginResize(newSize);
        Bounds = new Rectangle(Bounds.Location, newSize);
        EndResize();
    }

    protected virtual void EndResize()
    {
    }

    protected UiObject(Rectangle bounds)
    {
        Bounds = bounds;
    }

    protected UiObject(Point topLeft) : this(topLeft, new Point(80))
    {
    }

    protected UiObject(Point topLeft, Point size)
    {
        Bounds = new Rectangle(topLeft, size);
    }

    protected UiObject() : this(new Point(0))
    {
    }

    protected virtual void BeginResize(Point newSize)
    {
    }

    public virtual void Draw()
    {
    }

    protected virtual void InitializeGraphicsDevice(GraphicsDevice device)
    {
        GraphicsDevice = device;
    }
}

public class Label : UiObject
{
    public Label(Rectangle bounds) : base(bounds)
    {
    }

    public Label(Point topLeft) : base(topLeft)
    {
    }

    public Label(Point topLeft, Point size) : base(topLeft, size)
    {
    }

    public Label()
    {
    }

    public Label(string text)
    {
        _text = text;
    }

    public Label(string text, SpriteFont font) : this(text)
    {
        _font = font;
    }

    private string _text = string.Empty;
    private SpriteBatch _spriteBatch = null!;
    private SpriteFont? _font;

    public SpriteFont Font
    {
        get => _font ??= Fonts.DefaultUiFont;
        set => _font = value;
    }

    public string Text
    {
        get => _text;
        set => _text = value;
    }

    protected override void InitializeGraphicsDevice(GraphicsDevice device)
    {
        base.InitializeGraphicsDevice(device);

        _spriteBatch = new SpriteBatch(GraphicsDevice);
    }

    public override void Draw()
    {
    }
}