using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FontStashSharp;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using TravelTour.Core;
using TravelTour.UI;

namespace TravelTour.States
{
    // Mini-jeu de pêche complet façon Blox Fruits/Fisch :
    // lancer -> attente d'une touche -> ferrage -> barre de ferrage (garder l'indicateur
    // dans la zone qui bouge pour remplir la barre de progression). Rareté pondérée par la canne.
    public class FishingState : IGameState
    {
        enum Phase { Idle, Waiting, Hooked, Reeling, Result }

        readonly TravelTourGame _game;
        Texture2D _pixel = null!; SpriteFontBase _font = null!, _bigFont = null!;
        UIButton _backBtn = null!;

        IslandData _island = null!;
        Phase _phase = Phase.Idle;
        float _waitTimer;
        float _hookWindow;
        readonly Random _rng = new Random();

        // ── Barre de ferrage ─────────────────────────────────
        const float BAR_W = 460f;
        float _reelProgress;     // 0..100
        float _indicatorPos;     // 0..BAR_W
        float _indicatorVel;
        float _zoneCenter;       // centre de la zone cible sur la barre
        float _zoneWidth;
        float _zoneDir = 1f;
        float _zoneSpeed;
        float _reelTimeLimit;

        const float INDICATOR_ACCEL = 900f;
        const float INDICATOR_GRAVITY = 650f;
        const float FILL_RATE  = 32f;   // %/s quand dans la zone
        const float DRAIN_RATE = 14f;   // %/s quand hors zone

        string _resultText = ""; Color _resultColor; float _resultTimer;
        bool _prevKey;

        public FishingState(TravelTourGame game) => _game = game;

        public void SetIsland(IslandData island) => _island = island;

        public void Load(Texture2D pixel, SpriteFontBase font, SpriteFontBase bigFont)
        {
            _pixel = pixel; _font = font; _bigFont = bigFont;
            _backBtn = new UIButton(new Rectangle(16, 16, 140, 36), "← Retour à l'île",
                () => _game.EnterIsland(_island));
            _phase = Phase.Idle;
        }

        FishingRodData Rod => PlayerSave.GetEquippedFishingRod() ?? Catalog.FishingRods[0];

        void Cast()
        {
            var rod = Rod;
            float baseWait = 2f + (float)_rng.NextDouble() * 4f;
            _waitTimer = baseWait * (1f - rod.BiteSpeedBonus);
            _phase = Phase.Waiting;
        }

        void OnBite()
        {
            var rod = Rod;
            _hookWindow = rod.AutoSucceedCommonRare ? 1.2f : MathHelper.Max(0.35f, 0.7f - rod.RareChanceBonus * 0.3f);
            _phase = Phase.Hooked;
        }

        void StartReeling()
        {
            var rod = Rod;
            _reelProgress   = 35f;
            _indicatorPos   = BAR_W / 2f;
            _indicatorVel   = 0f;
            _zoneWidth      = MathHelper.Clamp(85f + rod.RareChanceBonus * 90f, 85f, 220f);
            _zoneCenter     = BAR_W / 2f;
            _zoneSpeed      = 90f + (float)_rng.NextDouble() * 70f;
            _zoneDir        = _rng.Next(2) == 0 ? -1f : 1f;
            _reelTimeLimit  = 12f;
            _phase = Phase.Reeling;
        }

        void UpdateReeling(float dt, bool holding)
        {
            var rod = Rod;
            _indicatorVel += (holding ? INDICATOR_ACCEL : -INDICATOR_GRAVITY) * dt;
            _indicatorVel = MathHelper.Clamp(_indicatorVel, -500f, 500f);
            _indicatorPos += _indicatorVel * dt;
            if (_indicatorPos < 0) { _indicatorPos = 0; _indicatorVel = 0; }
            if (_indicatorPos > BAR_W) { _indicatorPos = BAR_W; _indicatorVel = 0; }

            // La zone cible dérive de gauche à droite
            _zoneCenter += _zoneDir * _zoneSpeed * dt;
            if (_zoneCenter < _zoneWidth / 2f) { _zoneCenter = _zoneWidth / 2f; _zoneDir = 1f; }
            if (_zoneCenter > BAR_W - _zoneWidth / 2f) { _zoneCenter = BAR_W - _zoneWidth / 2f; _zoneDir = -1f; }

            bool inZone = Math.Abs(_indicatorPos - _zoneCenter) <= _zoneWidth / 2f;
            float fillMult = rod.AutoSucceedCommonRare ? 1.6f : 1f;
            _reelProgress += (inZone ? FILL_RATE * fillMult : -DRAIN_RATE) * dt;
            _reelProgress = MathHelper.Clamp(_reelProgress, 0f, 100f);

            _reelTimeLimit -= dt;

            if (_reelProgress >= 100f) ResolveCatch(true);
            else if (_reelProgress <= 0f || _reelTimeLimit <= 0f) ResolveCatch(false);
        }

        void ResolveCatch(bool success)
        {
            if (!success)
            {
                _resultText = "💨 Le poisson a filé...";
                _resultColor = UIHelper.TextDim;
                _resultTimer = 2f;
                _phase = Phase.Result;
                return;
            }

            var rod = Rod;
            float bonus = rod.RareChanceBonus;
            float wCommon = Math.Max(5f, 60f - bonus * 80f);
            float wRare = 25f + bonus * 30f;
            float wEpic = 12f + bonus * 30f;
            float wLegendary = 3f + bonus * 20f;
            float total = wCommon + wRare + wEpic + wLegendary;
            float roll = (float)_rng.NextDouble() * total;

            Rarity rarity =
                roll < wCommon ? Rarity.Common :
                roll < wCommon + wRare ? Rarity.Rare :
                roll < wCommon + wRare + wEpic ? Rarity.Epic : Rarity.Legendary;

            var available = FishInfo.ForIsland(_island.Name).Where(f => f.Rarity == rarity).ToList();
            if (available.Count == 0)
                available = FishInfo.ForIsland(_island.Name);
            if (available.Count == 0)
                available = Catalog.Fish;

            var fish = available[_rng.Next(available.Count)];
            bool isLegendary = fish.Rarity == Rarity.Legendary;
            PlayerSave.AddFish(fish.Name, 1, isLegendary);
            foreach (var q in Catalog.Quests) q.CheckCompleted();

            _resultText = $"🎉 Attrapé : {fish.Icon} {fish.Name} !";
            _resultColor = UIHelper.RarityColors[(int)fish.Rarity];
            _resultTimer = 2.5f;
            _phase = Phase.Result;
        }

        public void Update(GameTime gt)
        {
            float dt = (float)gt.ElapsedGameTime.TotalSeconds;
            var kb = Keyboard.GetState();
            var ms = Mouse.GetState();
            _backBtn.Update(ms);

            bool keyHeld = kb.IsKeyDown(Keys.Space) || kb.IsKeyDown(Keys.E) || ms.LeftButton == ButtonState.Pressed;
            bool keyEdge = keyHeld && !_prevKey;

            switch (_phase)
            {
                case Phase.Idle:
                    if (keyEdge) Cast();
                    break;
                case Phase.Waiting:
                    _waitTimer -= dt;
                    if (_waitTimer <= 0) OnBite();
                    break;
                case Phase.Hooked:
                    _hookWindow -= dt;
                    if (keyEdge) StartReeling();
                    else if (_hookWindow <= 0) ResolveCatch(false);
                    break;
                case Phase.Reeling:
                    UpdateReeling(dt, keyHeld);
                    break;
                case Phase.Result:
                    _resultTimer -= dt;
                    if (_resultTimer <= 0 && keyEdge) _phase = Phase.Idle;
                    break;
            }

            _prevKey = keyHeld;

            if (kb.IsKeyDown(Keys.Escape)) _game.EnterIsland(_island);
        }

        public void Draw(SpriteBatch sb)
        {
            int W = _game.GraphicsDevice.Viewport.Width;
            int H = _game.GraphicsDevice.Viewport.Height;

            sb.Draw(_pixel, new Rectangle(0, 0, W, H), new Color(10, 30, 55));
            _backBtn.Draw(sb, _pixel, _font);

            UIHelper.DrawCenteredText(sb, _bigFont, $"🎣 Pêche — {_island.Name}",
                new Rectangle(0, 16, W, 36), UIHelper.Gold, 0.8f);

            var rod = Rod;
            UIHelper.DrawCenteredText(sb, _font, $"Canne équipée : {rod.Icon} {rod.Name}  ({rod.EffectLabel()})",
                new Rectangle(0, 60, W, 22), UIHelper.TextDim, 0.8f);

            string mainText = _phase switch
            {
                Phase.Idle    => "Maintenez [E]/[Espace]/Clic pour lancer la ligne",
                Phase.Waiting => "🌊 En attente d'une touche...",
                Phase.Hooked  => "❗ FERREZ ! Appuyez maintenant !",
                Phase.Reeling => "🎣 Maintenez pour garder le poisson dans la zone !",
                Phase.Result  => _resultText,
                _ => ""
            };
            Color mainColor = _phase == Phase.Hooked ? Color.OrangeRed :
                               _phase == Phase.Result ? _resultColor : UIHelper.TextMain;

            var sz = _bigFont.MeasureString(mainText);
            sb.DrawString(_bigFont, mainText, new Vector2(W / 2f - sz.X / 2f, H / 2f - 120f), mainColor);

            if (_phase == Phase.Hooked)
            {
                int barW = 400, barH = 24;
                var barRect = new Rectangle(W / 2 - barW / 2, H / 2 + 50, barW, barH);
                UIHelper.DrawBox(sb, _pixel, barRect, UIHelper.CardBg, Color.OrangeRed, 2);
                float pct = Math.Clamp(_hookWindow / 0.7f, 0f, 1f);
                sb.Draw(_pixel, new Rectangle(barRect.X + 2, barRect.Y + 2, (int)((barW - 4) * pct), barH - 4), Color.OrangeRed * 0.8f);
            }

            if (_phase == Phase.Reeling)
            {
                int barH = 36;
                var barRect = new Rectangle(W / 2 - (int)BAR_W / 2, H / 2 - 40, (int)BAR_W, barH);
                UIHelper.DrawBox(sb, _pixel, barRect, UIHelper.CardBg, UIHelper.TextDim, 2);

                // Zone cible
                var zoneRect = new Rectangle(
                    barRect.X + (int)(_zoneCenter - _zoneWidth / 2f), barRect.Y,
                    (int)_zoneWidth, barH);
                sb.Draw(_pixel, zoneRect, new Color(80, 200, 100) * 0.55f);

                // Indicateur du joueur
                int indW = 8;
                sb.Draw(_pixel, new Rectangle(barRect.X + (int)_indicatorPos - indW / 2, barRect.Y - 4, indW, barH + 8), Color.White);

                // Barre de progression du ferrage
                var progRect = new Rectangle(W / 2 - (int)BAR_W / 2, barRect.Bottom + 14, (int)BAR_W, 20);
                UIHelper.DrawBox(sb, _pixel, progRect, UIHelper.Dark2, UIHelper.TextDim * 0.5f, 1);
                Color progColor = Color.Lerp(Color.OrangeRed, Color.LimeGreen, _reelProgress / 100f);
                sb.Draw(_pixel, new Rectangle(progRect.X + 2, progRect.Y + 2, (int)((BAR_W - 4) * _reelProgress / 100f), 16), progColor);
            }

            if (_phase == Phase.Result && _resultTimer <= 0)
                UIHelper.DrawCenteredText(sb, _font, "Maintenez [E] pour relancer la ligne",
                    new Rectangle(0, H / 2 + 60, W, 22), UIHelper.TextDim, 0.8f);
        }

        public void Dispose() { }
    }
}
