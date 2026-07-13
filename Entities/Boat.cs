using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;

namespace TravelTour.Entities
{
    // Bateau piloté librement en vue du dessus (mer ouverte, WorldSeaState).
    public class Boat
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public float   Rotation;   // radians, 0 = vers la droite

        public Rectangle Bounds => new((int)Position.X - 32, (int)Position.Y - 20, 64, 40);

        const float ACCEL      = 620f;
        const float MAX_SPEED  = 520f;
        const float TURN_SPEED = 3.0f;   // rad/s
        const float DRAG       = 0.985f; // inertie par frame (appliqué en dt) — proche de 1 = peu de friction

        public void Update(KeyboardState kb, float dt)
        {
            bool thrust = kb.IsKeyDown(Keys.W) || kb.IsKeyDown(Keys.Up);
            bool brake  = kb.IsKeyDown(Keys.S) || kb.IsKeyDown(Keys.Down);
            bool left   = kb.IsKeyDown(Keys.A) || kb.IsKeyDown(Keys.Left);
            bool right  = kb.IsKeyDown(Keys.D) || kb.IsKeyDown(Keys.Right);

            if (left)  Rotation -= TURN_SPEED * dt;
            if (right) Rotation += TURN_SPEED * dt;

            var heading = new Vector2(MathF.Cos(Rotation), MathF.Sin(Rotation));

            if (thrust) Velocity += heading * ACCEL * dt;
            if (brake)  Velocity -= heading * ACCEL * 0.6f * dt;

            float dragFactor = MathF.Pow(DRAG, dt * 60f);
            Velocity *= dragFactor;

            if (Velocity.LengthSquared() > MAX_SPEED * MAX_SPEED)
                Velocity = Vector2.Normalize(Velocity) * MAX_SPEED;

            Position += Velocity * dt;
        }
    }
}
