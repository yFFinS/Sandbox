using Microsoft.Xna.Framework;

namespace Sandbox.Shared.UI.Base;

public abstract class UiObject : IDisposable
{
    private bool _enabled;

    public bool Enabled
    {
        get => _enabled;
        set
        {
            if (value == _enabled)
            {
                return;
            }

            _enabled = value;
            if (_enabled)
            {
                OnEnabled();
            }
            else
            {
                OnDisabled();
            }
        }
    }

    protected virtual void OnEnabled()
    {
    }

    protected virtual void OnDisabled()
    {
    }

    private Rectangle _bounds;

    public Rectangle Bounds
    {
        get => _bounds;
        set
        {
            _bounds = value;
            OnBoundsChanged();
        }
    }

    protected virtual void OnBoundsChanged()
    {
    }

    public int Width
    {
        get => Bounds.Width;
        set => Bounds = new Rectangle(Bounds.Location, new Point(value, Height));
    }

    public int Height
    {
        get => Bounds.Height;
        set => Bounds = new Rectangle(Bounds.Location, new Point(Width, value));
    }

    public Point Size
    {
        get => Bounds.Size;
        set => Bounds = new Rectangle(Bounds.Location, value);
    }

    public Point TopLeft
    {
        get => Bounds.Location;
        set => Bounds = new Rectangle(value, Bounds.Size);
    }

    public Vector2 Position => TopLeft.ToVector2();

    public Point Center
    {
        get => Bounds.Center;
        set => Bounds = new Rectangle(TopLeft + value - Bounds.Center, Bounds.Size);
    }

    protected UiObject(Rectangle bounds) : this()
    {
        Bounds = bounds;
    }

    protected UiObject(Point topLeft) : this(topLeft, new Point(80))
    {
    }

    protected UiObject(Point topLeft, Point size) : this()
    {
        Bounds = new Rectangle(topLeft, size);
    }

    protected UiObject()
    {
        Enabled = true;

        UiManager.Instance.RegisterUiObject(this);
    }

    ~UiObject()
    {
        Dispose(false);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing)
        {
            return;
        }

        Enabled = false;
        UiManager.Instance.UnregisterUiObject(this);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}