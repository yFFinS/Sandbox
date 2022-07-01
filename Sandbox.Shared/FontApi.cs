using Microsoft.Xna.Framework.Graphics;

namespace Sandbox.Shared;

public class FontApi
{
    private readonly Dictionary<string, SpriteFont> _fonts = new();

    private string? _defaultUiFontName;
    public SpriteFont DefaultUiFont => _fonts[_defaultUiFontName!];

    public void SetDefaultUiFont(string name)
    {
        _defaultUiFontName = name;
    }

    public SpriteFont? TryGetFont(string fontName)
    {
        return _fonts.TryGetValue(fontName, out var font) ? font : null;
    }

    public void AddFont(string name, SpriteFont font)
    {
        _fonts[name] = font;
    }
}