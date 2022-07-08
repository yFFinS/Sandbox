using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Sandbox.Shared.Drawing;
using Sandbox.Shared.UI.Base;

namespace Sandbox.Shared.UI;

public class UiText : UiObject, IGroupDrawable
{
    public UiText(Rectangle bounds) : base(bounds)
    {
    }

    public UiText(Point topLeft) : base(topLeft)
    {
    }

    public UiText(Point topLeft, Point size) : base(topLeft, size)
    {
    }

    public UiText()
    {
    }

    public UiText(string text)
    {
        Text = text;
    }

    public UiText(string text, SpriteFont font) : this(text)
    {
        Font = font;
    }

    private string _text = string.Empty;
    private SpriteFont? _font;
    private float _fontScale = 1;

    public float Padding { get; set; }
    public float FontScale
    {
        get => _fontScale;
        set
        {
            _fontScale = value;
            UpdateTextSize();
        }
    }

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
        set => _font = value;
    }

    public string Text
    {
        get => _text;
        set
        {
            _text = value;
            if (_font is not null)
            {
                UpdateTextSize();
            }
        }
    }

    private void UpdateTextSize()
    {
        TextSize = Font.MeasureString(Text) * FontScale;
    }

    public Color TextColor { get; set; } = Color.White;

    public void Draw(GameTime gameTime, SpriteBatch batch)
    {
        if (string.IsNullOrWhiteSpace(Text))
        {
            return;
        }

        var drawPosition = Position;
        switch (HorizontalTextAlignment)
        {
            case HorizontalAlignment.Left:
                drawPosition.X += Padding;
                break;
            case HorizontalAlignment.Middle:
                drawPosition.X += (Width - TextSize.X) / 2;
                break;
            case HorizontalAlignment.Right:
                drawPosition.X += Width - TextSize.X - Padding;
                break;
        }

        switch (VerticalTextAlignment)
        {
            case VerticalAlignment.Top:
                drawPosition.Y += Padding;
                break;
            case VerticalAlignment.Middle:
                drawPosition.Y += (Height - TextSize.Y) / 2;
                break;
            case VerticalAlignment.Bottom:
                drawPosition.Y += Height - TextSize.Y - Padding;
                break;
        }
        batch.DrawString(Font, Text, drawPosition, TextColor, 0, Vector2.Zero, new Vector2(_fontScale),
            SpriteEffects.None, 0);
    }

    public HorizontalAlignment HorizontalTextAlignment { get; set; } = HorizontalAlignment.Middle;
    public VerticalAlignment VerticalTextAlignment { get; set; } = VerticalAlignment.Middle;
}

public enum HorizontalAlignment
{
    Left,
    Middle,
    Right
}

public enum VerticalAlignment
{
    Top,
    Middle,
    Bottom
}