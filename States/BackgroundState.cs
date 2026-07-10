using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FontStashSharp;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using TravelTour.Core;
using TravelTour.UI;

namespace TravelTour.States
{
    public class BackgroundState : IGameState
    {
        readonly TravelTourGame _game;
        Texture2D _pixel = null!; SpriteFontBase _font = null!, _bigFont = null!;

        UIButton _backBtn = null!;
        List<UIButton> _bgBtns = new();

        static readonly (string Name, Color[] Gradient)[] Backgrounds = {
            ("Cosmos",    new[]{ new Color(2,6,24),   new Color(10,21,80),  new Color(32,8,64)   }),
            ("Volcan",    new[]{ new Color(26,5,0),   new Color(92,16,0),   new Color(200,40,0)  }),
            ("Océan",     new[]{ new Color(0,24,48),  new Color(0,51,102),  new Color(0,102,153) }),
            ("Forêt",     new[]{ new Color(0,26,0),   new Color(0,51,0),    new Color(26,77,0)   }),
            ("Néon City", new[]{ new Color(10,0,26),  new Color(26,0,48),   new Color(0,32,64)   }),
            ("Tempête",   new[]{ new Color(10,10,20), new Color(20,20,42),  new Color(30,20,40)  }),
            ("Sakura",    new[]{ new Color(42,0,24),  new Color(74,0,37),   new Color(48,13,32)  }),
            ("Néant",     new[]{ new Color(5,5,8),    new Color(10,10,15),  new Color(8,5,16)    }),
        };

        int _selected = 0;
        string _toast = ""; float _toastTimer; Color _toastColor;

        public BackgroundState(TravelTourGame game) => _game = game;

        public void Load(Texture2D pixel, SpriteFontBase font, SpriteFontBase bigFont)
        {
            _pixel = pixel; _font = font; _bigFont = bigFont;
            int W = _game.GraphicsDevice.Viewport.Width;
            int H = _game.GraphicsDevice.Viewport.Height;

            _backBtn = new UIButton(new Rectangle(16, 16, 100, 36), "← Menu",
                () => _game.ChangeState(GameState.MainMenu));

            // Selected = current
            for (int i = 0; i < Backgrounds.Length; i++)
                if (Backgrounds[i].Name == PlayerSave.SelectedBackground) _selected = i;

            RebuildBtns();
        }

        void RebuildBtns()
        {
            _bgBtns.Clear();
            int W = _game.GraphicsDevice.Viewport.Width;
            int bw = 200, bh = 120, gap = 16, cols = 4;
            int startX = W / 2 - (cols * (bw + gap)) / 2;
            int startY = 130;

            for (int i = 0; i < Backgrounds.Length; i++)
            {
                int idx = i;
                _bgBtns.Add(new UIButton(
                    new Rectangle(startX + (i % cols) * (bw + gap), startY + (i / cols) * (bh + gap), bw, bh),
                    "",
                    () =>
                    {
                        _selected = idx;
                        PlayerSave.SelectedBackground = Backgrounds[idx].Name;
                        _game.SetBackground(Backgrounds[idx].Gradient);
                        ShowToast($"Fond appliqué : {Backgrounds[idx].Name}!", UIHelper.Blue);
                    }));
            }
        }

        public void Update(GameTime gt)
        {
            _toastTimer -= (float)gt.ElapsedGameTime.TotalSeconds;
            var ms = Mouse.GetState();
            _backBtn.Update(ms);
            for (int i=0;i<_bgBtns.Count;i++) _bgBtns[i].Update(ms);
            if (Keyboard.GetState().IsKeyDown(Keys.Escape)) _game.ChangeState(GameState.MainMenu);
        }

        public void Draw(SpriteBatch sb)
        {
            int W = _game.GraphicsDevice.Viewport.Width;
            int H = _game.GraphicsDevice.Viewport.Height;
            sb.Draw(_pixel, new Rectangle(0, 0, W, H), UIHelper.Dark);

            _backBtn.Draw(sb, _pixel, _font, 0.85f);
            UIHelper.DrawCenteredText(sb, _bigFont, "🌌  ARRIÈRE PLAN",
                new Rectangle(0, 14, W, 55), new Color(0, 224, 204), 0.75f);
            UIHelper.DrawCenteredText(sb, _font, "Choisissez l'ambiance de votre page d'accueil",
                new Rectangle(0, 62, W, 30), UIHelper.TextDim, 0.85f);

            for (int i = 0; i < _bgBtns.Count; i++)
            {
                var btn = _bgBtns[i];
                var (name, grad) = Backgrounds[i];
                bool sel = i == _selected;

                // Draw gradient preview
                for (int s = 0; s < 3; s++)
                {
                    int sw = btn.Bounds.Width / 3;
                    sb.Draw(_pixel,
                        new Rectangle(btn.Bounds.X + s * sw, btn.Bounds.Y, sw + 1, btn.Bounds.Height),
                        grad[s]);
                }

                // Border
                Color border = sel ? UIHelper.Gold : new Color(50, 55, 90);
                int bw = sel ? 3 : 1;
                sb.Draw(_pixel, new Rectangle(btn.Bounds.X, btn.Bounds.Y, btn.Bounds.Width, bw), border);
                sb.Draw(_pixel, new Rectangle(btn.Bounds.X, btn.Bounds.Bottom - bw, btn.Bounds.Width, bw), border);
                sb.Draw(_pixel, new Rectangle(btn.Bounds.X, btn.Bounds.Y, bw, btn.Bounds.Height), border);
                sb.Draw(_pixel, new Rectangle(btn.Bounds.Right - bw, btn.Bounds.Y, bw, btn.Bounds.Height), border);

                // Label strip
                sb.Draw(_pixel, new Rectangle(btn.Bounds.X, btn.Bounds.Bottom - 28, btn.Bounds.Width, 28),
                    Color.Black * 0.65f);
                UIHelper.DrawCenteredText(sb, _font, name,
                    new Rectangle(btn.Bounds.X, btn.Bounds.Bottom - 26, btn.Bounds.Width, 26),
                    sel ? UIHelper.Gold : UIHelper.TextMain, 0.8f);

                if (sel)
                    UIHelper.DrawCenteredText(sb, _font, "✔",
                        new Rectangle(btn.Bounds.Right - 28, btn.Bounds.Y + 4, 24, 24),
                        UIHelper.Gold, 0.9f);

            }

            sb.DrawString(_font, $"Sélectionné : {PlayerSave.SelectedBackground}",
                new Vector2(W / 2f - 80, H - 50), UIHelper.Blue);

            DrawToast(sb, W, H);
        }

        void DrawToast(SpriteBatch sb, int W, int H)
        {
            if (_toastTimer <= 0) return;
            Vector2 ts = _font.MeasureString(_toast);
            UIHelper.DrawBox(sb, _pixel,
                new Rectangle((int)(W / 2f - ts.X / 2f - 16), H - 80, (int)ts.X + 32, 36),
                UIHelper.Dark2, _toastColor, 1);
            sb.DrawString(_font, _toast, new Vector2(W / 2f - ts.X / 2f, H - 72), _toastColor);
        }

        void ShowToast(string m, Color c) { _toast = m; _toastColor = c; _toastTimer = 2.5f; }
        public void Dispose() { }
    }
}
