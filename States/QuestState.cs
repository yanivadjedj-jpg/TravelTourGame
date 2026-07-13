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
    public class QuestState : IGameState
    {
        readonly TravelTourGame _game;
        Texture2D _pixel = null!;
        SpriteFontBase _font = null!, _bigFont = null!;

        UIButton _backBtn = null!;
        List<UIButton> _catBtns = new();
        string _toast = ""; float _toastTimer; Color _toastColor;

        static readonly string[] Categories = { "Tous", "Débutant", "Combat", "Exploration", "Maîtrise", "Île" };
        static readonly Color[]  CatColors  = {
            UIHelper.TextMain,
            new Color(80, 200, 120),
            new Color(240, 80, 96),
            new Color(64, 180, 255),
            new Color(200, 80, 255),
            new Color(60, 170, 210),
        };

        int _catIdx = 0;
        float _scrollY = 0;
        int _prevScroll;
        MouseState _curMs, _prevMs;

        public QuestState(TravelTourGame game) => _game = game;

        public void Load(Texture2D pixel, SpriteFontBase font, SpriteFontBase bigFont)
        {
            _pixel = pixel; _font = font; _bigFont = bigFont;
            int W = _game.GraphicsDevice.Viewport.Width;

            _backBtn = new UIButton(new Rectangle(16, 16, 110, 36), "← Retour",
                () => _game.ExitQuest());

            _catBtns.Clear();
            int tw = 130, tgap = 8;
            int startX = W / 2 - (Categories.Length * (tw + tgap)) / 2;
            for (int i = 0; i < Categories.Length; i++)
            {
                int idx = i;
                _catBtns.Add(new UIButton(
                    new Rectangle(startX + i * (tw + tgap), 62, tw, 28),
                    Categories[i],
                    () => { _catIdx = idx; _scrollY = 0; }));
            }

            // Vérifie toutes les quêtes au chargement
            foreach (var q in Catalog.Quests) q.CheckCompleted();
        }

        public void Update(GameTime gt)
        {
            _toastTimer -= (float)gt.ElapsedGameTime.TotalSeconds;
            _prevMs = _curMs;
            _curMs  = Mouse.GetState();

            if (_curMs.ScrollWheelValue != _prevScroll)
                _scrollY = System.Math.Max(0, _scrollY - (_curMs.ScrollWheelValue - _prevScroll) / 120f * 40f);
            _prevScroll = _curMs.ScrollWheelValue;

            _backBtn.Update(_curMs);
            foreach (var b in _catBtns) b.Update(_curMs);
            if (Keyboard.GetState().IsKeyDown(Keys.Escape)) _game.ExitQuest();
        }

        public void Draw(SpriteBatch sb)
        {
            int W = _game.GraphicsDevice.Viewport.Width;
            int H = _game.GraphicsDevice.Viewport.Height;
            sb.Draw(_pixel, new Rectangle(0, 0, W, H), UIHelper.Dark);

            _backBtn.Draw(sb, _pixel, _font, 0.85f);

            UIHelper.DrawCenteredText(sb, _bigFont, "📋  QUÊTES",
                new Rectangle(0, 14, W, 44), new Color(255, 200, 60), 0.72f);

            // Stats rapides en haut à droite
            UIHelper.DrawBox(sb, _pixel, new Rectangle(W - 280, 10, 265, 46), UIHelper.Dark2, UIHelper.TextDim * 0.3f, 1);
            sb.DrawString(_font, $"⚔ {PlayerSave.EnemiesKilled} tués  👹 {PlayerSave.BossesDefeated} boss  🏰 {PlayerSave.DungeonsCompleted} donjons",
                new Vector2(W - 274, 24), UIHelper.TextDim * 0.8f);

            // Category tabs
            for (int i = 0; i < _catBtns.Count; i++)
            {
                _catBtns[i].NormalColor = i == _catIdx ? CatColors[i] * 0.25f : UIHelper.CardBg;
                _catBtns[i].TextColor   = i == _catIdx ? CatColors[i] : UIHelper.TextDim;
                _catBtns[i].Draw(sb, _pixel, _font, 0.72f);
            }

            // Quests list
            var quests = _catIdx == 0
                ? Catalog.Quests
                : Catalog.Quests.Where(q => q.Category == Categories[_catIdx]).ToList();

            int cols = 2, qw = 540, qh = 110, gap = 12;
            int startX = W / 2 - (cols * (qw + gap)) / 2;
            int startY = 100 - (int)_scrollY;

            // Séparateurs par état
            int claimed = 0, completed = 0, active = 0;
            foreach (var q in quests) {
                if (q.RewardClaimed) claimed++;
                else if (q.IsCompleted) completed++;
                else active++;
            }
            UIHelper.DrawCenteredText(sb, _font,
                $"{active} en cours  •  {completed} à réclamer  •  {claimed} terminées",
                new Rectangle(0, 92, W, 18), UIHelper.TextDim * 0.7f, 0.72f);

            for (int i = 0; i < quests.Count; i++)
            {
                var q = quests[i];
                int col = i % cols, row = i / cols;
                int x = startX + col * (qw + gap);
                int y = startY + row * (qh + gap);

                if (y + qh < 58 || y > H) continue; // culling

                DrawQuestCard(sb, q, x, y, qw, qh);
            }

            DrawToast(sb, W, H);
        }

        void DrawQuestCard(SpriteBatch sb, QuestData q, int x, int y, int w, int h)
        {
            q.CheckCompleted();

            Color catColor = CatColors[System.Array.IndexOf(Categories, q.Category) < 0 ? 0 : System.Array.IndexOf(Categories, q.Category)];
            Color stateColor = q.RewardClaimed ? UIHelper.TextDim * 0.5f :
                               q.IsCompleted   ? UIHelper.Gold :
                               catColor;

            // Fond
            Color bg = q.IsCompleted && !q.RewardClaimed
                ? new Color(30, 25, 5)
                : UIHelper.CardBg;
            UIHelper.DrawBox(sb, _pixel, new Rectangle(x, y, w, h), bg, stateColor * (q.RewardClaimed ? 0.3f : 0.7f), 2);

            // Bande couleur gauche
            sb.Draw(_pixel, new Rectangle(x, y, 4, h), stateColor * (q.RewardClaimed ? 0.3f : 0.8f));

            // Icône + nom
            UIHelper.DrawCenteredText(sb, _bigFont, q.Icon,
                new Rectangle(x + 8, y + 8, 52, 52), Color.White * (q.RewardClaimed ? 0.4f : 1f), 0.7f);
            sb.DrawString(_font, q.Name,
                new Vector2(x + 68, y + 10),
                q.RewardClaimed ? UIHelper.TextDim * 0.5f : UIHelper.TextMain);
            sb.DrawString(_font, q.Description,
                new Vector2(x + 68, y + 28),
                UIHelper.TextDim * (q.RewardClaimed ? 0.4f : 0.8f));

            // Badge catégorie
            sb.DrawString(_font, q.Category,
                new Vector2(x + w - _font.MeasureString(q.Category).X - 8, y + 10),
                catColor * (q.RewardClaimed ? 0.3f : 0.6f));

            // Barre de progression
            int pBarX = x + 68, pBarY = y + 48, pBarW = w - 76 - (q.IsCompleted && !q.RewardClaimed ? 110 : 0);
            int prog = System.Math.Min(q.GetProgress(), q.Target);
            float pct = q.ProgressPct();
            sb.DrawString(_font, $"{prog}/{q.Target}",
                new Vector2(pBarX, pBarY - 14), stateColor * 0.8f);
            sb.Draw(_pixel, new Rectangle(pBarX, pBarY, pBarW, 8), new Color(20, 22, 40));
            if (prog > 0)
                sb.Draw(_pixel, new Rectangle(pBarX, pBarY, (int)(pBarW * pct), 8),
                    q.IsCompleted ? UIHelper.Gold : stateColor);

            // Récompenses (icônes)
            int rx = pBarX, ry = y + h - 22;
            sb.DrawString(_font, "🎁 ", new Vector2(rx, ry), UIHelper.TextDim * 0.7f);
            rx += 22;
            foreach (var r in q.Rewards)
            {
                string label = r.RewardType == "gold"     ? $"+{r.Amount}💰"
                             : r.RewardType == "xp"       ? $"+{r.Amount}XP"
                             : $"+{r.Amount}{MaterialInfo.GetIcon(r.Key ?? "")}";
                sb.DrawString(_font, label + " ", new Vector2(rx, ry), UIHelper.Gold * (q.RewardClaimed ? 0.4f : 0.9f));
                rx += (int)_font.MeasureString(label + " ").X;
            }

            // Bouton RÉCLAMER
            if (q.IsCompleted && !q.RewardClaimed)
            {
                int bx = x + w - 108, by2 = y + 42, bw2 = 100, bh2 = 28;
                var bounds = new Rectangle(bx, by2, bw2, bh2);
                bool hov = bounds.Contains(_curMs.Position);
                bool clicked = hov && _curMs.LeftButton == ButtonState.Released && _prevMs.LeftButton == ButtonState.Pressed;
                sb.Draw(_pixel, bounds, hov ? new Color(80, 60, 0) : new Color(50, 38, 0));
                UIHelper.DrawBox(sb, _pixel, bounds, Color.Transparent, UIHelper.Gold * 0.8f, 2);
                UIHelper.DrawCenteredText(sb, _font, "✨ RÉCLAMER", bounds, UIHelper.Gold, 0.75f);
                if (clicked) ClaimReward(q);
            }
            else if (q.RewardClaimed)
            {
                UIHelper.DrawCenteredText(sb, _font, "✔ Complétée",
                    new Rectangle(x + w - 108, y + 42, 100, 28), UIHelper.TextDim * 0.5f, 0.72f);
            }
        }

        void ClaimReward(QuestData q)
        {
            if (!q.IsCompleted || q.RewardClaimed) return;
            q.RewardClaimed = true;
            int goldEarned = 0;
            foreach (var r in q.Rewards)
            {
                if (r.RewardType == "gold")     { PlayerSave.AddGold(r.Amount); goldEarned += r.Amount; }
                else if (r.RewardType == "xp")  { PlayerSave.AddXp(r.Amount); }
                else if (r.RewardType == "material") PlayerSave.AddMaterial(r.Key ?? "", r.Amount);
            }
            SaveSystem.Save();
            string summary = goldEarned > 0 ? $"+{goldEarned}💰" : q.Rewards.Length > 0 ? "Récompenses reçues!" : "";
            ShowToast($"✨ {q.Name} — {summary}", UIHelper.Gold);
        }

        void DrawToast(SpriteBatch sb, int W, int H)
        {
            if (_toastTimer <= 0) return;
            var ts = _font.MeasureString(_toast);
            UIHelper.DrawBox(sb, _pixel,
                new Rectangle((int)(W/2f - ts.X/2f - 16), H - 60, (int)ts.X + 32, 36),
                UIHelper.Dark2, UIHelper.Gold, 1);
            sb.DrawString(_font, _toast, new Vector2(W/2f - ts.X/2f, H - 52), UIHelper.Gold);
        }

        void ShowToast(string m, Color c) { _toast = m; _toastColor = c; _toastTimer = 3f; }
        public void Dispose() { }
    }
}
