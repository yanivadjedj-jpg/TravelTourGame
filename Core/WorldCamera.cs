using Microsoft.Xna.Framework;

namespace TravelTour.Core
{
    // Caméra 2.5D : suit une position monde et fournit une matrice SpriteBatch
    // + un facteur d'échelle par sprite selon sa profondeur (Y), pour simuler
    // de la perspective sans pipeline 3D (aucun BasicEffect/Model dans ce projet).
    public class WorldCamera
    {
        public Vector2 Position;
        public float   Zoom = 1f;

        public Matrix GetTransform(int viewportW, int viewportH) =>
            Matrix.CreateTranslation(-Position.X, -Position.Y, 0f) *
            Matrix.CreateScale(Zoom, Zoom, 1f) *
            Matrix.CreateTranslation(viewportW / 2f, viewportH / 2f, 0f);

        // worldY proche de horizonY (loin) => sprite plus petit ; proche de nearY (près) => sprite normal/grossi.
        public static float DepthScaleFor(float worldY, float horizonY, float nearY)
        {
            float t = MathHelper.Clamp((worldY - horizonY) / System.MathF.Max(1f, nearY - horizonY), 0f, 1f);
            return MathHelper.Lerp(0.55f, 1.15f, t);
        }
    }
}
