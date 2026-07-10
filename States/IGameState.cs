using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FontStashSharp;

namespace TravelTour.States
{
    public interface IGameState
    {
        void Update(GameTime gt);
        void Draw(SpriteBatch sb);
        void Dispose();
    }
}
