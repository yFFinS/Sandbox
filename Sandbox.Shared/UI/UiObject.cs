﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Sandbox.Shared.UI;


public abstract class UiObject
{
    public UiObject? Parent { get; internal set; }
    internal readonly List<UiObject> Children = new();

    public Rectangle Bounds { get; private set; }
    protected GraphicsDevice GraphicsDevice { get; private set; } = null!;

    private Texture2D? _backgroundTexture;
    private Color _backgroundColor = Color.White;

    protected SpriteBatch SpriteBatch { get; private set; } = null!;

    protected virtual void BeginBackgroundChange(Texture2D? newTexture, Color newColor)
    {
    }

    protected virtual void EndBackgroundChange()
    {
    }

    protected internal virtual bool OnMouseEnter()
    {
        return false;
    }

    protected internal virtual bool OnMouseExit()
    {
        return false;
    }

    protected internal virtual bool OnMouseMove(Point position)
    {
        return false;
    }

    protected internal virtual bool OnMouseDown(Point position, MouseButton button)
    {
        return false;
    }

    protected internal virtual bool OnMouseUp(Point position, MouseButton button)
    {
        return false;
    }

    public Texture2D? BackgroundTexture
    {
        get
        {
            if (_backgroundTexture is null)
            {
                return BackgroundTexture = TextureFactory.CreateFilledRectTexture(GraphicsDevice, Color.White);
            }

            return _backgroundTexture;
        }
        set
        {
            BeginBackgroundChange(value, BackgroundColor);
            _backgroundTexture = value;
            EndBackgroundChange();
        }
    }

    public Color BackgroundColor
    {
        get => _backgroundColor;
        set
        {
            BeginBackgroundChange(BackgroundTexture, value);
            _backgroundColor = value;
            EndBackgroundChange();
        }
    }

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

    protected virtual void DrawBackground()
    {
        if (_backgroundTexture is null)
        {
            return;
        }

    }

    public virtual void Draw()
    {
    }

    protected virtual void InitializeGraphicsDevice(GraphicsDevice device)
    {
        GraphicsDevice = device;
        SpriteBatch = new SpriteBatch(GraphicsDevice);
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

    public Vector2 TextSize { get; private set; }

    public SpriteFont Font
    {
        get
        {
            if (_font is null)
            {
                return Font = Fonts.DefaultUiFont;
            }

            return _font;
        }
        set
        {
            BeginFontChange(value);
            _font = value;
            EndFontChange();
        }
    }

    public string Text
    {
        get => _text;
        set
        {
            BeginTextChange(value);
            _text = value;
            EndTextChange();
        }
    }

    protected virtual void BeginFontChange(SpriteFont newFont)
    {
        TextSize = newFont.MeasureString(Text);
    }

    protected virtual void EndFontChange()
    {
    }

    protected virtual void BeginTextChange(string newText)
    {
        TextSize = Font.MeasureString(newText);
    }

    protected virtual void EndTextChange()
    {
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