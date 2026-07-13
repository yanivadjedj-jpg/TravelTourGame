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
    // Mini-jeu de pêche complet : lancer -> attente d'une touche -> QTE de ferrage.
    // Rareté du poisson pondérée par la canne équipée.
    public class FishingState : IGameState
    {
        enum Phase { Idle, Waiting, Biting, Result }

        readonly TravelTourGame _game;
        Texture2D _pixel = null!; SpriteFontBase _font = null!, _bigFont = null!;
        UIButton _backBtn = null!;

        IslandData _island = null!;
        Phase _phase = Phase.Idle;
        float _waitTimer;
        float _biteWindow;
        readonly Random _rng = new Random();

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
            _biteWindow = rod.AutoSucceedCommonRare ? 1.2f : MathHelper.Max(0.35f, 0.7f - rod.RareChanceBonus * 0.3f);
            _phase = Phase.Biting;
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

            bool keyNow = kb.IsKeyDown(Keys.Space) || kb.IsKeyDown(Keys.E);
            bool keyEdge = keyNow && !_prevKey;
            _prevKey = keyNow;

            switch (_phase)
            {
                case Phase.Idle:
                    if (keyEdge) Cast();
                    break;
                case Phase.Waiting:
                    _waitTimer -= dt;
                    if (_waitTimer <= 0) OnBite();
                    break;
                case Phase.Biting:
                    _biteWindow -= dt;
                    if (keyEdge) ResolveCatch(true);
                    else if (_biteWindow <= 0) ResolveCatch(false);
                    break;
                case Phase.Result:
                    _resultTimer -= dt;
                    if (_resultTimer <= 0 && keyEdge) _phase = Phase.Idle;
                    break;
            }

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
                Phase.Idle    => "Appuyez sur [E] ou [Espace] pour lancer la ligne",
                Phase.Waiting => "🌊 En attente d'une touche...",
                Phase.Biting  => "❗ MAINTENANT ! Appuyez sur [Espace] !",
                Phase.Result  => _resultText,
                _ => ""
            };
            Color mainColor = _phase == Phase.Biting ? Color.OrangeRed :
                               _phase == Phase.Result ? _resultColor : UIHelper.TextMain;

            var sz = _bigFont.MeasureString(mainText);
            sb.DrawString(_bigFont, mainText, new Vector2(W / 2f - sz.X / 2f, H / 2f - sz.Y / 2f), mainColor);

            if (_phase == Phase.Biting)
            {
                int barW = 400, barH = 24;
                var barRect = new Rectangle(W / 2 - barW / 2, H / 2 + 50, barW, barH);
                UIHelper.DrawBox(sb, _pixel, barRect, UIHelper.CardBg, Color.OrangeRed, 2);
                float pct = Math.Clamp(_biteWindow / 0.7f, 0f, 1f);
                sb.Draw(_pixel, new Rectangle(barRect.X + 2, barRect.Y + 2, (int)((barW - 4) * pct), barH - 4), Color.OrangeRed * 0.8f);
            }

            if (_phase == Phase.Result && _resultTimer <= 0)
                UIHelper.DrawCenteredText(sb, _font, "Appuyez sur [E] pour relancer la ligne",
                    new Rectangle(0, H / 2 + 60, W, 22), UIHelper.TextDim, 0.8f);
        }

        public void Dispose() { }
    }
}
