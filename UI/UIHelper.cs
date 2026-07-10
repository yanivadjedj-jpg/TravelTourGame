using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FontStashSharp;
using TravelTour.Core;

namespace TravelTour.UI
{
    public static class UIHelper
    {
        public static readonly Color[] RarityColors =
        {
            new Color(130, 130, 130),
            new Color(50,  160, 255),
            new Color(180, 80,  255),
            new Color(255, 195, 50),
        };
        public static readonly string[] RarityNames = { "Commun", "Rare", "Épique", "Légendaire" };

        public static Color DifficultyColor(DifficultyLevel d) => d switch
        {
            DifficultyLevel.Easy      => new Color(64, 224, 160),
            DifficultyLevel.Medium    => new Color(240, 192, 64),
            DifficultyLevel.Hard      => new Color(240, 128, 64),
            DifficultyLevel.Boss      => new Color(240, 80,  96),
            DifficultyLevel.Legendary => new Color(168, 85, 247),
            _                         => Color.White
        };
        public static string DifficultyName(DifficultyLevel d) => d switch
        {
            DifficultyLevel.Easy      => "FACILE",
            DifficultyLevel.Medium    => "MOYEN",
            DifficultyLevel.Hard      => "DIFFICILE",
            DifficultyLevel.Boss      => "BOSS",
            DifficultyLevel.Legendary => "LEGENDAIRE",
            _                         => "?"
        };

        public static readonly Color Dark     = new Color(8,  9,  15);
        public static readonly Color Dark2    = new Color(15, 17, 35);
        public static readonly Color CardBg   = new Color(20, 22, 45);
        public static readonly Color Blue     = new Color(0,  200, 255);
        public static readonly Color Gold     = new Color(240, 192, 64);
        public static readonly Color Purple   = new Color(168, 85, 247);
        public static readonly Color TextDim  = new Color(100, 120, 170);
        public static readonly Color TextMain = new Color(210, 215, 240);

        public static void DrawBox(SpriteBatch sb, Texture2D pixel,
            Rectangle rect, Color fill, Color? border = null, int borderW = 2)
        {
            sb.Draw(pixel, rect, fill);
            if (border.HasValue)
            {
                sb.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, borderW), border.Value);
                sb.Draw(pixel, new Rectangle(rect.X, rect.Bottom - borderW, rect.Width, borderW), border.Value);
                sb.Draw(pixel, new Rectangle(rect.X, rect.Y, borderW, rect.Height), border.Value);
                sb.Draw(pixel, new Rectangle(rect.Right - borderW, rect.Y, borderW, rect.Height), border.Value);
            }
        }

        public static void DrawProgressBar(SpriteBatch sb, Texture2D pixel,
            Rectangle bounds, float pct, Color fill, Color bg)
        {
            sb.Draw(pixel, bounds, bg);
            int filled = (int)(bounds.Width * System.Math.Clamp(pct, 0f, 1f));
            if (filled > 0)
                sb.Draw(pixel, new Rectangle(bounds.X, bounds.Y, filled, bounds.Height), fill);
        }

        public static void DrawCenteredText(SpriteBatch sb, SpriteFontBase font,
            string text, Rectangle bounds, Color color, float scale = 1f)
        {
            if (string.IsNullOrEmpty(text)) return;
            Vector2 size = font.MeasureString(text);
            Vector2 pos  = new Vector2(bounds.X + (bounds.Width  - size.X) / 2f,
                                       bounds.Y + (bounds.Height - size.Y) / 2f);
            sb.DrawString(font, text, pos, color);
        }

        public static void DrawWrapped(SpriteBatch sb, SpriteFontBase font,
            string text, Rectangle bounds, Color color, float scale = 1f)
        {
            string[] words = text.Split(' ');
            float spaceW = font.MeasureString(" ").X;
            float lineH  = font.MeasureString("A").Y + 4;
            float x = bounds.X, y = bounds.Y;

            foreach (string word in words)
            {
                if (word == "\n") { x = bounds.X; y += lineH; continue; }
                float ww = font.MeasureString(word).X;
                if (x + ww > bounds.Right && x > bounds.X) { x = bounds.X; y += lineH; }
                if (y + lineH > bounds.Bottom) break;
                sb.DrawString(font, word, new Vector2(x, y), color);
                x += ww + spaceW;
            }
        }
    }
}
