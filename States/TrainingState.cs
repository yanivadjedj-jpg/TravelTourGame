using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FontStashSharp;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using TravelTour.Core;
using TravelTour.UI;

namespace TravelTour.States
{
    public class TrainingState : IGameState
    {
        readonly TravelTourGame _game;
        Texture2D _pixel = null!; SpriteFontBase _font = null!, _bigFont = null!;

        List<UIButton> _dungeonBtns = new();
        UIButton _backBtn = null!;
        string _toast = ""; float _toastTimer; Color _toastColor;
        int _scrollY = 0;
        int _maxScrollY = 0;

        static readonly Color[] DiffColors =
        {
            new Color(64, 224, 160),
            new Color(240, 192, 64),
            new Color(240, 128, 64),
            new Color(240, 80, 96),
            new Color(168, 85, 247),
        };

        public TrainingState(TravelTourGame game) => _game = game;

        public void Load(Texture2D pixel, SpriteFontBase font, SpriteFontBase bigFont)
        {
            _pixel = pixel; _font = font; _bigFont = bigFont;
            Rebuild();
        }

        void Rebuild()
        {
            _dungeonBtns.Clear();
            int W = _game.GraphicsDevice.Viewport.Width;
            int H = _game.GraphicsDevice.Viewport.Height;
            _backBtn = new UIButton(new Rectangle(16, 16, 100, 36), "← Menu",
                () => _game.ChangeState(GameState.MainMenu));

            int dw = 220, dh = 95, gap = 12, cols = 4;
            int totalW = cols * (dw + gap) - gap;
            int startX = W / 2 - totalW / 2;
            int startY = 105;

            int rows = (Catalog.Dungeons.Count + cols - 1) / cols;
            _maxScrollY = System.Math.Max(0, startY + rows * (dh + gap) - H + 20);

            for (int i = 0; i < Catalog.Dungeons.Count; i++)
            {
                int idx = i;
                var d = Catalog.Dungeons[i];
                int col = i % cols, row = i / cols;
                int x = startX + col * (dw + gap);
                int y = startY + row * (dh + gap);
                bool locked = d.RequiredRank > PlayerSave.Rank ||
                              (d.RequiredLevel > 0 && PlayerSave.PlayerLevel < d.RequiredLevel);
                bool classLocked = d.IsClassDungeon && PlayerSave.ClassDungeonDone;

                _dungeonBtns.Add(new UIButton(
                    new Rectangle(x, y, dw, dh), "",
                    () =>
                    {
                        if (locked)
                        {
                            if (d.RequiredLevel > 0 && PlayerSave.PlayerLevel < d.RequiredLevel)
                                ShowToast($"Niveau {d.RequiredLevel} requis! (tu es niv.{PlayerSave.PlayerLevel})", Color.Red);
                            else
                                ShowToast($"Rang {PlayerSave.RankNames[d.RequiredRank]} requis!", Color.Red);
                            return;
                        }
                        if (classLocked) { ShowToast("Classe déjà choisie!", Color.Yellow); return; }
                        _game.StartDungeon(d);
                    },
                    UIHelper.CardBg, new Color(25, 28, 55)
                ) { Enabled = !locked && !classLocked });
            }
        }

        MouseState _prevMs;

        public void Update(GameTime gt)
        {
            _toastTimer -= (float)gt.ElapsedGameTime.TotalSeconds;
            var ms = Mouse.GetState();

            // Scroll avec molette
            int scrollDelta = ms.ScrollWheelValue - _prevMs.ScrollWheelValue;
            if (scrollDelta != 0)
            {
                _scrollY = System.Math.Clamp(_scrollY - scrollDelta / 3, 0, _maxScrollY);
                ApplyScroll();
            }
            _prevMs = ms;

            _backBtn.Update(ms);
            foreach (var b in _dungeonBtns) b.Update(ms);
            if (Keyboard.GetState().IsKeyDown(Keys.Escape)) _game.ChangeState(GameState.MainMenu);
        }

        void ApplyScroll()
        {
            int W = _game.GraphicsDevice.Viewport.Width;
            int dw = 220, dh = 95, gap = 12, cols = 4;
            int totalW = cols * (dw + gap) - gap;
            int startX = W / 2 - totalW / 2;
            for (int i = 0; i < _dungeonBtns.Count; i++)
            {
                int col = i % cols, row = i / cols;
                _dungeonBtns[i].Bounds = new Rectangle(
                    startX + col * (dw + gap),
                    105 + row * (dh + gap) - _scrollY,
                    dw, dh);
            }
        }

        public void Draw(SpriteBatch sb)
        {
            int W = _game.GraphicsDevice.Viewport.Width;
            int H = _game.GraphicsDevice.Viewport.Height;
            sb.Draw(_pixel, new Rectangle(0, 0, W, H), UIHelper.Dark);

            _backBtn.Draw(sb, _pixel, _font, 0.85f);

            UIHelper.DrawCenteredText(sb, _bigFont, "🏰  ENTRAÎNEMENT",
                new Rectangle(0, 18, W, 60), new Color(240, 80, 96), 0.8f);
            UIHelper.DrawCenteredText(sb, _font, "Conquérez des donjons — récoltez des matériaux",
                new Rectangle(0, 62, W, 30), UIHelper.TextDim, 0.85f);

            for (int i = 0; i < Catalog.Dungeons.Count && i < _dungeonBtns.Count; i++)
            {
                var d   = Catalog.Dungeons[i];
                var btn = _dungeonBtns[i];

                // Ne dessine pas si hors écran
                if (btn.Bounds.Bottom < 90 || btn.Bounds.Top > H) continue;

                bool locked = d.RequiredRank > PlayerSave.Rank ||
                              (d.RequiredLevel > 0 && PlayerSave.PlayerLevel < d.RequiredLevel);
                bool classDone = d.IsClassDungeon && PlayerSave.ClassDungeonDone;

                Color dcol = d.IsClassDungeon ? new Color(180, 80, 255) : DiffColors[(int)d.Difficulty];
                Color cardBg = d.IsClassDungeon ? new Color(20, 8, 35) : UIHelper.CardBg;
                UIHelper.DrawBox(sb, _pixel, btn.Bounds, cardBg, dcol * (locked || classDone ? 0.3f : 1f), 2);

                // Donjon de classe : brillance animée
                if (d.IsClassDungeon && !locked && !classDone)
                    sb.Draw(_pixel, new Rectangle(btn.Bounds.X + 2, btn.Bounds.Y + 2, btn.Bounds.Width - 4, 3),
                        new Color(180, 80, 255) * 0.7f);

                // Icon
                UIHelper.DrawCenteredText(sb, _bigFont, d.Icon,
                    new Rectangle(btn.Bounds.X + 4, btn.Bounds.Y + 2, 50, 50), Color.White, 0.65f);

                // Info
                int tx = btn.Bounds.X + 58, ty = btn.Bounds.Y + 8;
                sb.DrawString(_font, d.Name, new Vector2(tx, ty),
                    d.IsClassDungeon ? new Color(220, 160, 255) : UIHelper.TextMain);
                sb.DrawString(_font, d.IsClassDungeon ? "⭐ CLASSE" : UIHelper.DifficultyName(d.Difficulty),
                    new Vector2(tx, ty + 18), dcol * (locked ? 0.5f : 1f));

                // Condition d'accès
                string req = classDone ? "✔ Classe choisie" :
                             d.RequiredLevel > 0 ? $"Niv. {d.RequiredLevel}" :
                             $"Rang {PlayerSave.RankNames[d.RequiredRank]}";
                sb.DrawString(_font, req, new Vector2(tx, ty + 36), UIHelper.TextDim);
                sb.DrawString(_font, $"💰+{d.GoldReward} 🗡️{d.EnemyCount}",
                    new Vector2(tx, ty + 54), UIHelper.Gold * 0.8f);

                if (locked) sb.Draw(_pixel, btn.Bounds, Color.Black * 0.55f);
            }

            // Indicateur de scroll
            if (_maxScrollY > 0)
            {
                float scrollPct = _maxScrollY > 0 ? (float)_scrollY / _maxScrollY : 0f;
                int trackH = H - 120;
                int thumbH = 40;
                int thumbY = 100 + (int)((trackH - thumbH) * scrollPct);
                sb.Draw(_pixel, new Rectangle(W - 10, 100, 6, trackH), new Color(30, 33, 60));
                sb.Draw(_pixel, new Rectangle(W - 10, thumbY, 6, thumbH), UIHelper.Blue * 0.7f);
            }

            // Stats panel
            int panX = W - 240, panY = 100;
            UIHelper.DrawBox(sb, _pixel, new Rectangle(panX, panY, 220, 220),
                UIHelper.Dark2, UIHelper.Blue, 1);
            sb.DrawString(_font, "VOS STATS", new Vector2(panX + 12, panY + 10), UIHelper.Blue);
            // Or en grand et lisible
            sb.DrawString(_font, $"💰 {PlayerSave.Gold:N0} or",
                new Vector2(panX + 12, panY + 28), UIHelper.Gold);
            DrawStatBar(sb, panX + 12, panY + 50,  196, "Rang",    PlayerSave.Rank / 6f,        UIHelper.Gold);
            DrawStatBar(sb, panX + 12, panY + 85,  196, "Or",      System.Math.Min(1f, PlayerSave.Gold / 50000f), UIHelper.Gold);
            DrawStatBar(sb, panX + 12, panY + 105, 196, "Cristal", PlayerSave.Materials.TryGetValue("CristalFeu", out int cf) ? cf / 20f : 0f, new Color(240, 80, 60));
            DrawStatBar(sb, panX + 12, panY + 140, 196, "Essence", PlayerSave.Materials.TryGetValue("EssenceOmbres", out int es) ? es / 20f : 0f, UIHelper.Purple);

            DrawToast(sb, W, H);
        }

        void DrawStatBar(SpriteBatch sb, int x, int y, int w, string label, float pct, Color col)
        {
            sb.DrawString(_font, label, new Vector2(x, y), UIHelper.TextDim);
            UIHelper.DrawProgressBar(sb, _pixel, new Rectangle(x, y + 16, w, 8),
                System.Math.Clamp(pct, 0, 1), col, new Color(20, 22, 40));
        }

        void DrawToast(SpriteBatch sb, int W, int H)
        {
            if (_toastTimer <= 0) return;
            Vector2 ts = _font.MeasureString(_toast);
            UIHelper.DrawBox(sb, _pixel,
                new Rectangle((int)(W / 2f - ts.X / 2f - 16), H - 60, (int)ts.X + 32, 36),
                UIHelper.Dark2, _toastColor, 1);
            sb.DrawString(_font, _toast, new Vector2(W / 2f - ts.X / 2f, H - 52), _toastColor);
        }

        void ShowToast(string m, Color c) { _toast = m; _toastColor = c; _toastTimer = 2.5f; }
        public void Dispose() { }
    }
}
