using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;

namespace TravelTour.Entities
{
    // Bateau piloté librement en vue du dessus (mer ouverte, WorldSeaState).
    // Modèle "normal" façon bateau : avance/recule toujours dans l'axe de son cap
    // (pas de glisse latérale libre), tourne moins vite à l'arrêt qu'en pleine vitesse.
    public class Boat
    {
        public Vector2 Position;
        public Vector2 Velocity => Heading * _speed;
        public float   Rotation;   // radians, 0 = vers la droite

        public Rectangle Bounds => new((int)Position.X - 32, (int)Position.Y - 20, 64, 40);

        float _speed; // scalaire signé le long du cap : + avant, - marche arrière

        const float ACCEL       = 260f;
        const float MAX_SPEED   = 300f;
        const float MAX_REVERSE = 120f;
        const float FRICTION    = 160f; // décélération naturelle quand on relâche les commandes
        const float TURN_SPEED  = 2.2f; // rad/s à pleine vitesse

        Vector2 Heading => new Vector2(MathF.Cos(Rotation), MathF.Sin(Rotation));

        public void Update(KeyboardState kb, float dt)
        {
            bool thrust = kb.IsKeyDown(Keys.W) || kb.IsKeyDown(Keys.Up);
            bool brake  = kb.IsKeyDown(Keys.S) || kb.IsKeyDown(Keys.Down);
            bool left   = kb.IsKeyDown(Keys.A) || kb.IsKeyDown(Keys.Left);
            bool right  = kb.IsKeyDown(Keys.D) || kb.IsKeyDown(Keys.Right);

            if (thrust) _speed = Math.Min(_speed + ACCEL * dt, MAX_SPEED);
            else if (brake) _speed = Math.Max(_speed - ACCEL * dt, -MAX_REVERSE);
            else
            {
                if (_speed > 0) _speed = Math.Max(0f, _speed - FRICTION * dt);
                else if (_speed < 0) _speed = Math.Min(0f, _speed + FRICTION * dt);
            }

            // Un bateau tourne mieux quand il avance ; presque à l'arrêt, il pivote très peu.
            float turnFactor = MathHelper.Clamp(Math.Abs(_speed) / MAX_SPEED, 0.2f, 1f);
            if (left)  Rotation -= TURN_SPEED * turnFactor * dt;
            if (right) Rotation += TURN_SPEED * turnFactor * dt;

            Position += Heading * _speed * dt;
        }
    }
}
