using Microsoft.Xna.Framework;

namespace Chess.View;

public static class GameTimeExtensions
{
    public static float DeltaTime(this GameTime gameTime)
    {
        return (float) gameTime.ElapsedGameTime.TotalMilliseconds / 1000f;
    }
}