using Microsoft.Xna.Framework.Graphics;

namespace Sandbox.Shared;

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