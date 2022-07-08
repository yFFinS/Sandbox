using Microsoft.Xna.Framework;
using Sandbox.Shared.UI.Base;

namespace Sandbox.Shared.UI;

public class UiVerticalLayout : UiLayout
{
    private readonly List<UiObject> _uiObjects = new();

    private int _padding = 10;
    private HorizontalAlignment _horizontalAlignment = HorizontalAlignment.Middle;
    private VerticalAlignment _verticalAlignment = VerticalAlignment.Middle;

    public int Padding
    {
        get => _padding;
        set
        {
            _padding = value;
            UpdateLayout();
        }
    }

    public HorizontalAlignment HorizontalAlignment
    {
        get => _horizontalAlignment;
        set
        {
            _horizontalAlignment = value;
            UpdateLayout();
        }
    }

    public VerticalAlignment VerticalAlignment
    {
        get => _verticalAlignment;
        set
        {
            _verticalAlignment = value;
            UpdateLayout();
        }
    }

    public override IEnumerable<UiObject> GetObjects()
    {
        return _uiObjects;
    }

    public override void AddObject(UiObject uiObject)
    {
        _uiObjects.Add(uiObject);
        uiObject.Enabled = Enabled;
        UpdateLayout();
    }

    public override void RemoveObject(UiObject uiObject)
    {
        _uiObjects.Add(uiObject);
        UpdateLayout();
    }

    protected override void OnBoundsChanged()
    {
        UpdateLayout();
    }

    private void UpdateLayout()
    {
        // TODO: TopLeft имеет неправильные координаты, но на экране все имеет ожидаемые координаты  
        
        var topLefts = new Point[_uiObjects.Count];
        var accumulatedHeight = 0;

        foreach (var (uiObject, index) in _uiObjects.Select((obj, i) => (obj, i)))
        {
            var topLeft = Point.Zero;

            switch (HorizontalAlignment)
            {
                case HorizontalAlignment.Left:
                    topLeft.X = TopLeft.X;
                    break;
                case HorizontalAlignment.Middle:
                    topLeft.X = Center.X - uiObject.Width / 2;
                    break;
                case HorizontalAlignment.Right:
                    topLeft.X = TopLeft.X + Width - uiObject.Width;
                    break;
            }

            switch (VerticalAlignment)
            {
                case VerticalAlignment.Top:
                    topLeft.Y = TopLeft.Y;
                    break;
                case VerticalAlignment.Middle:
                    topLeft.Y = Center.Y - uiObject.Height / 2;
                    break;
                case VerticalAlignment.Bottom:
                    topLeft.Y = TopLeft.Y + Height - uiObject.Height;
                    break;
            }

            topLeft.Y += accumulatedHeight;
            topLefts[index] = topLeft;
            if (index + 1 < topLefts.Length)
            {
                accumulatedHeight += uiObject.Height + _padding;
            }
        }

        switch (VerticalAlignment)
        {
            case VerticalAlignment.Top:
                break;
            case VerticalAlignment.Middle:
                var adjustHeight = accumulatedHeight / 2;
                for (var i = 0; i < topLefts.Length; i++)
                {
                    topLefts[i].Y -= adjustHeight;
                }

                break;
            case VerticalAlignment.Bottom:
                for (var i = 0; i < topLefts.Length; i++)
                {
                    topLefts[i].Y -= _uiObjects[i].Height + Padding;
                }

                break;
        }

        for (var i = 0; i < topLefts.Length; i++)
        {
            _uiObjects[i].TopLeft = topLefts[i];
        }
    }
}