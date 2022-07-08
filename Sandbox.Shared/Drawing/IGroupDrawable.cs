using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Sandbox.Shared.Drawing;

public interface IGroupDrawable
{
    void Draw(GameTime gameTime, SpriteBatch batch);
}