using Microsoft.Xna.Framework.Graphics;

namespace Checkers;

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

public static class Fonts
{
    private static FontApi _api = null!;

    public static void SetApi(FontApi api)
    {
        _api = api;
    }

    public static SpriteFont DefaultUiFont => _api.DefaultUiFont;
    public static SpriteFont? TryGetFont(string fontName) => _api.TryGetFont(fontName);
    public static void AddFont(string name, SpriteFont font) => _api.AddFont(name, font);
}