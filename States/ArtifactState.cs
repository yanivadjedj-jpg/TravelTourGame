using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using FontStashSharp;
using System.Collections.Generic;
using System.Linq;
using TravelTour.Core;
using TravelTour.UI;

namespace TravelTour.States
{
    public class ArtifactState : IGameState
    {
        readonly TravelTourGame _game;
        Texture2D _pixel = null!;
        SpriteFontBase _font = null!, _bigFont = null!;

        UIButton _backBtn = null!;
        List<UIButton> _slotBtns = new();

        string _toast = ""; float _toastTimer; Color _toastColor;
        float _time;
        int _filterSlot = -1; // -1 = tous
        float _scrollY = 0;
        int _prevScroll;
        MouseState _curMs, _prevMs;

        static readonly ArtifactSlot[] Slots = {
            ArtifactSlot.Chapeau, ArtifactSlot.Amulette, ArtifactSlot.Bague, ArtifactSlot.Cape
        };
        static readonly string[] SlotIcons = { "🎩", "📿", "💍", "🧣" };
        static readonly Color[] SlotColors = {
            new Color(255,200,80),
            new Color(150,100,255),
            new Color(255,120,60),
            new Color(60,200,160),
        };

        public ArtifactState(TravelTourGame game) => _game = game;

        public void Load(Texture2D pixel, SpriteFontBase font, SpriteFontBase bigFont)
        {
            _pixel = pixel; _font = font; _bigFont = bigFont;
            int W = _game.GraphicsDevice.Viewport.Width;
            _backBtn = new UIButton(new Rectangle(16, 16, 110, 36), "← Menu",
                () => _game.ChangeState(GameState.MainMenu));

            // Boutons filtres par slot
            _slotBtns.Clear();
            int sw = 120, sgap = 8;
            int startX = W / 2 - (5 * (sw + sgap)) / 2;
            _slotBtns.Add(new UIButton(new Rectangle(startX, 62, sw, 28), "Tous",
                () => { _filterSlot = -1; _scrollY = 0; }));
            for (int i = 0; i < Slots.Length; i++)
            {
                int idx = i;
                _slotBtns.Add(new UIButton(
                    new Rectangle(startX + (i+1)*(sw+sgap), 62, sw, 28),
                    SlotIcons[i] + " " + Slots[i].ToString(),
                    () => { _filterSlot = idx; _scrollY = 0; }));
            }
        }

        public void Update(GameTime gt)
        {
            _time        += (float)gt.ElapsedGameTime.TotalSeconds;
            _toastTimer  -= (float)gt.ElapsedGameTime.TotalSeconds;
            _prevMs = _curMs;
            _curMs  = Mouse.GetState();

            if (_curMs.ScrollWheelValue != _prevScroll)
                _scrollY = System.Math.Max(0, _scrollY - (_curMs.ScrollWheelValue - _prevScroll) / 120f * 40f);
            _prevScroll = _curMs.ScrollWheelValue;

            _backBtn.Update(_curMs);
            foreach (var b in _slotBtns) b.Update(_curMs);
            if (Keyboard.GetState().IsKeyDown(Keys.Escape)) _game.ChangeState(GameState.MainMenu);
        }

        public void Draw(SpriteBatch sb)
        {
            int W = _game.GraphicsDevice.Viewport.Width;
            int H = _game.GraphicsDevice.Viewport.Height;
            sb.Draw(_pixel, new Rectangle(0, 0, W, H), UIHelper.Dark);

            _backBtn.Draw(sb, _pixel, _font, 0.85f);
            UIHelper.DrawCenteredText(sb, _bigFont, "🏺  ARTEFACTS",
                new Rectangle(0, 14, W, 44), new Color(255, 165, 0), 0.72f);

            sb.DrawString(_font, $"💰 {PlayerSave.Gold:N0}", new Vector2(W - 160, 18), UIHelper.Gold);

            // Filtre par slot
            for (int i = 0; i < _slotBtns.Count; i++)
            {
                bool active = i == 0 ? _filterSlot == -1 : _filterSlot == i - 1;
                Color col = i == 0 ? UIHelper.TextMain : SlotColors[i - 1];
                _slotBtns[i].NormalColor = active ? col * 0.25f : UIHelper.CardBg;
                _slotBtns[i].TextColor   = active ? col : UIHelper.TextDim;
                _slotBtns[i].Draw(sb, _pixel, _font, 0.72f);
            }

            // ── Slots équipés (panneau gauche) ─────────────────────
            DrawEquippedPanel(sb, 20, 100, 220, H - 120);

            // ── Grille artefacts ────────────────────────────────────
            var list = _filterSlot < 0
                ? Catalog.Artifacts
                : Catalog.Artifacts.Where(a => (int)a.Slot == _filterSlot).ToList();

            int cols = 3, aw = 280, ah = 120, gap = 12;
            int gridX = 260;
            int totalW = cols * (aw + gap) - gap;
            int startX2 = gridX + (W - gridX - totalW) / 2;
            int startY  = 100 - (int)_scrollY;

            for (int i = 0; i < list.Count; i++)
            {
                var a   = list[i];
                int col = i % cols, row = i / cols;
                int x = startX2 + col * (aw + gap);
                int y = startY  + row * (ah + gap);
                if (y + ah < 90 || y > H) continue;
                DrawArtifactCard(sb, a, x, y, aw, ah);
            }

            DrawToast(sb, W, H);
        }

        void DrawEquippedPanel(SpriteBatch sb, int px, int py, int pw, int ph)
        {
            UIHelper.DrawBox(sb, _pixel, new Rectangle(px, py, pw, ph),
                new Color(8,10,22), new Color(255,165,0) * 0.4f, 2);

            sb.DrawString(_font, "ÉQUIPÉ", new Vector2(px + pw/2f - _font.MeasureString("ÉQUIPÉ").X/2f, py + 8),
                new Color(255,165,0));

            int sy = py + 32;
            for (int i = 0; i < Slots.Length; i++)
            {
                var slot = Slots[i];
                Color sc = SlotColors[i];
                string slotName = SlotIcons[i] + " " + slot.ToString();

                // Fond slot
                UIHelper.DrawBox(sb, _pixel, new Rectangle(px+8, sy, pw-16, 52),
                    new Color(12,14,28), sc * 0.3f, 1);

                // Nom du slot
                sb.DrawString(_font, slotName, new Vector2(px+14, sy+4), sc * 0.8f);

                // Artefact équipé
                PlayerSave.EquippedArtifactBySlot.TryGetValue(slot.ToString(), out var eqName);
                if (eqName != null)
                {
                    var a = Catalog.Artifacts.Find(x => x.Name == eqName);
                    if (a != null)
                    {
                        sb.DrawString(_bigFont, a.Icon, new Vector2(px+14, sy+18), Color.White);
                        sb.DrawString(_font, a.Name, new Vector2(px+44, sy+20), UIHelper.TextMain);
                        sb.DrawString(_font, a.EffectLabel(), new Vector2(px+44, sy+36), sc * 0.7f);
                    }
                }
                else
                {
                    UIHelper.DrawCenteredText(sb, _font, "Vide",
                        new Rectangle(px+8, sy+18, pw-16, 20), UIHelper.TextDim * 0.4f, 0.75f);
                }
                sy += 58;
            }

            // Résumé bonus actifs
            sy += 8;
            sb.DrawString(_font, "BONUS ACTIFS", new Vector2(px + pw/2f - _font.MeasureString("BONUS ACTIFS").X/2f, sy),
                UIHelper.TextDim * 0.6f);
            sy += 20;

            var effects = new[] {
                (ArtifactEffect.HpBoost,       "HP"),
                (ArtifactEffect.AtkBoost,      "ATK"),
                (ArtifactEffect.DefBoost,      "DEF"),
                (ArtifactEffect.SpeedBoost,    "SPD"),
                (ArtifactEffect.FruitDmgBoost, "Fruit"),
                (ArtifactEffect.SwordDmgBoost, "Épée"),
                (ArtifactEffect.MeleeDmgBoost, "Mêlée"),
                (ArtifactEffect.XpBoost,       "XP"),
                (ArtifactEffect.GoldBoost,     "Or"),
                (ArtifactEffect.CooldownReduce,"CD"),
            };
            foreach (var (eff, label) in effects)
            {
                float bonus = PlayerSave.GetArtifactBonus(eff);
                if (bonus <= 0) continue;
                sb.DrawString(_font, $"  {label}: +{bonus*100:F0}%",
                    new Vector2(px+14, sy), new Color(255,165,0) * 0.9f);
                sy += 18;
                if (sy > py + ph - 10) break;
            }
        }

        void DrawArtifactCard(SpriteBatch sb, ArtifactData a, int x, int y, int w, int h)
        {
            Color rc = UIHelper.RarityColors[(int)a.Rarity];
            bool isEq = PlayerSave.EquippedArtifactBySlot.TryGetValue(a.Slot.ToString(), out var en) && en == a.Name;
            float pulse = isEq ? (float)(System.Math.Sin(_time * 3) * 0.3 + 0.7) : 1f;

            // Fond
            Color bg = isEq ? new Color(30,20,5) : a.IsOwned ? UIHelper.CardBg : new Color(8,10,22);
            UIHelper.DrawBox(sb, _pixel, new Rectangle(x, y, w, h), bg, rc * (a.IsOwned ? pulse : 0.25f), 2);

            // Barre slot couleur (top)
            int si = System.Array.IndexOf(Slots, a.Slot);
            sb.Draw(_pixel, new Rectangle(x, y, w, 3), si >= 0 ? SlotColors[si] * 0.8f : Color.Gray);

            // Icône + nom + rareté
            UIHelper.DrawCenteredText(sb, _bigFont, a.Icon,
                new Rectangle(x+4, y+6, 56, 56), Color.White * (a.IsOwned ? 1f : 0.3f), 0.72f);
            sb.DrawString(_font, a.Name,
                new Vector2(x+66, y+8), a.IsOwned ? UIHelper.TextMain : UIHelper.TextDim * 0.4f);
            sb.DrawString(_font, UIHelper.RarityNames[(int)a.Rarity],
                new Vector2(x+66, y+24), rc * (a.IsOwned ? 0.9f : 0.3f));
            sb.DrawString(_font, a.EffectLabel(),
                new Vector2(x+66, y+40), (si >= 0 ? SlotColors[si] : rc) * (a.IsOwned ? 0.85f : 0.25f));
            sb.DrawString(_font, a.SlotLabel(),
                new Vector2(x+66, y+56), UIHelper.TextDim * (a.IsOwned ? 0.65f : 0.25f));

            // Bouton bas
            int bx = x+6, by = y+h-28, bw = (w-18)/2, bh = 24;

            if (!a.IsOwned)
            {
                // Bouton acheter
                var rb = new Rectangle(bx, by, w-12, bh);
                bool hov = rb.Contains(_curMs.Position);
                bool clk = hov && _curMs.LeftButton == ButtonState.Released && _prevMs.LeftButton == ButtonState.Pressed;
                sb.Draw(_pixel, rb, hov ? new Color(40,30,5) : new Color(25,18,3));
                UIHelper.DrawBox(sb, _pixel, rb, Color.Transparent, UIHelper.Gold * 0.7f, 1);
                UIHelper.DrawCenteredText(sb, _font, $"Acheter  {a.BuyPrice:N0}💰", rb, UIHelper.Gold, 0.72f);
                if (clk) BuyArtifact(a);
            }
            else if (isEq)
            {
                // Bouton retirer
                var rb = new Rectangle(bx, by, w-12, bh);
                bool hov = rb.Contains(_curMs.Position);
                bool clk = hov && _curMs.LeftButton == ButtonState.Released && _prevMs.LeftButton == ButtonState.Pressed;
                sb.Draw(_pixel, rb, hov ? new Color(40,10,10) : new Color(20,5,5));
                UIHelper.DrawBox(sb, _pixel, rb, Color.Transparent, Color.Red * 0.5f, 1);
                UIHelper.DrawCenteredText(sb, _font, "✕ Retirer", rb, new Color(255,100,100), 0.72f);
                if (clk) { PlayerSave.UnequipArtifact(a); ShowToast($"{a.Name} retiré.", UIHelper.TextDim); }
            }
            else
            {
                // Bouton équiper
                var rb = new Rectangle(bx, by, w-12, bh);
                bool hov = rb.Contains(_curMs.Position);
                bool clk = hov && _curMs.LeftButton == ButtonState.Released && _prevMs.LeftButton == ButtonState.Pressed;
                sb.Draw(_pixel, rb, hov ? new Color(30,20,5) : new Color(15,10,2));
                UIHelper.DrawBox(sb, _pixel, rb, Color.Transparent, new Color(255,165,0) * 0.6f, 1);
                UIHelper.DrawCenteredText(sb, _font, "⚡ Équiper", rb, new Color(255,165,0), 0.72f);
                if (clk) { PlayerSave.EquipArtifact(a); ShowToast($"🏺 {a.Name} équipé!", new Color(255,165,0)); }
            }
        }

        void BuyArtifact(ArtifactData a)
        {
            if (a.IsOwned) { ShowToast("Déjà possédé!", Color.Yellow); return; }
            if (!PlayerSave.SpendGold(a.BuyPrice)) { ShowToast($"Or insuffisant! ({a.BuyPrice:N0} requis)", Color.Red); return; }
            a.IsOwned = true;
            SaveSystem.Save();
            ShowToast($"✅ {a.Icon} {a.Name} acheté!", new Color(255,165,0));
        }

        void DrawToast(SpriteBatch sb, int W, int H)
        {
            if (_toastTimer <= 0) return;
            var ts = _font.MeasureString(_toast);
            UIHelper.DrawBox(sb, _pixel,
                new Rectangle((int)(W/2f - ts.X/2f - 16), H - 60, (int)ts.X + 32, 36),
                UIHelper.Dark2, new Color(255,165,0), 1);
            sb.DrawString(_font, _toast, new Vector2(W/2f - ts.X/2f, H - 52), new Color(255,165,0));
        }

        void ShowToast(string m, Color c) { _toast = m; _toastColor = c; _toastTimer = 2.5f; }
        public void Dispose() { }
    }
}
