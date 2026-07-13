using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FontStashSharp;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using TravelTour.Core;
using TravelTour.Entities;
using TravelTour.UI;

namespace TravelTour.States
{
    // Mer ouverte de l'Événement Monde : le joueur pilote librement son bateau
    // entre les 9 îles (vue du dessus, caméra 2.5D façon WorldCamera).
    public class WorldSeaState : IGameState
    {
        readonly TravelTourGame _game;
        Texture2D _pixel = null!; SpriteFontBase _font = null!, _bigFont = null!;
        UIButton _backBtn = null!;
        UIButton _questBtn = null!;

        Boat _boat = new();
        WorldCamera _camera = new();

        const float SEA_SCALE = 3f;      // étale les SeaPosition (compactes) sur une grande carte
        const float DOCK_RADIUS = 90f;
        int WORLD_W, WORLD_H;

        IslandData? _nearestIsland;
        float _nearestDist;
        bool _prevInteract;

        string _toast = ""; float _toastTimer; Color _toastColor;

        public WorldSeaState(TravelTourGame game) => _game = game;

        public void Load(Texture2D pixel, SpriteFontBase font, SpriteFontBase bigFont)
        {
            _pixel = pixel; _font = font; _bigFont = bigFont;

            _backBtn = new UIButton(new Rectangle(16, 16, 100, 36), "← Menu",
                () => _game.ChangeState(GameState.MainMenu));
            _questBtn = new UIButton(new Rectangle(124, 16, 130, 36), "📋 Quêtes",
                () => _game.OpenQuest(GameState.WorldSea));

            WORLD_W = 4000; WORLD_H = 1100;
            _boat.Position = new Vector2(200, 700);
            _boat.Rotation = 0f;
            _camera.Position = _boat.Position;
        }

        Vector2 IslandWorldPos(IslandData isl) => isl.SeaPosition * SEA_SCALE;

        public void Update(GameTime gt)
        {
            float dt = (float)gt.ElapsedGameTime.TotalSeconds;
            var kb = Keyboard.GetState();
            var ms = Mouse.GetState();
            _backBtn.Update(ms);
            _questBtn.Update(ms);
            _toastTimer -= dt;

            _boat.Update(kb, dt);
            _boat.Position = new Vector2(
                Math.Clamp(_boat.Position.X, 0, WORLD_W),
                Math.Clamp(_boat.Position.Y, 0, WORLD_H));

            _camera.Position = Vector2.Lerp(_camera.Position, _boat.Position, Math.Min(1f, dt * 4f));

            // Île la plus proche
            _nearestIsland = null;
            _nearestDist = float.MaxValue;
            foreach (var isl in WorldEventCatalog.Islands)
            {
                float d = Vector2.Distance(_boat.Position, IslandWorldPos(isl));
                if (d < _nearestDist) { _nearestDist = d; _nearestIsland = isl; }
            }

            bool interactNow = kb.IsKeyDown(Keys.E);
            if (interactNow && !_prevInteract && _nearestIsland != null && _nearestDist <= DOCK_RADIUS)
            {
                if (PlayerSave.Rank >= _nearestIsland.RequiredRank)
                    _game.EnterIsland(_nearestIsland);
                else
                    ShowToast($"🔒 Rang {PlayerSave.RankNames[_nearestIsland.RequiredRank]} requis", Color.OrangeRed);
            }
            _prevInteract = interactNow;

            if (kb.IsKeyDown(Keys.Escape)) _game.ChangeState(GameState.MainMenu);
        }

        void ShowToast(string m, Color c) { _toast = m; _toastColor = c; _toastTimer = 2.5f; }

        public void Draw(SpriteBatch sb)
        {
            int W = _game.GraphicsDevice.Viewport.Width;
            int H = _game.GraphicsDevice.Viewport.Height;

            sb.End();
            sb.Begin(transformMatrix: _camera.GetTransform(W, H), samplerState: SamplerState.PointClamp);

            // Mer : dégradé de bandes bleues (fallback sans sprite dédié)
            var seaTex = SpriteLoader.Sea();
            if (seaTex != null)
            {
                sb.Draw(seaTex, new Rectangle(-200, -200, WORLD_W + 400, WORLD_H + 400), Color.White);
            }
            else
            {
                for (int y = -200; y < WORLD_H + 200; y += 40)
                {
                    float t = (y + 200) / (float)(WORLD_H + 400);
                    var c = Color.Lerp(new Color(10, 40, 90), new Color(20, 90, 150), t);
                    sb.Draw(_pixel, new Rectangle(-200, y, WORLD_W + 400, 42), c);
                }
            }

            // Îles (docks)
            foreach (var isl in WorldEventCatalog.Islands)
            {
                var pos = IslandWorldPos(isl);
                bool unlocked = PlayerSave.Rank >= isl.RequiredRank;
                var themeColor = isl.Theme switch
                {
                    IslandTheme.Hunter => new Color(180, 60, 60),
                    IslandTheme.Pirate => new Color(60, 120, 180),
                    IslandTheme.Ninja  => new Color(90, 150, 90),
                    _ => Color.Gray
                };
                Color tint = unlocked ? Color.White : Color.Gray * 0.6f;
                int r = 46;
                var mapIcon = SpriteLoader.IslandTerrain(isl.MapIconKey);
                if (mapIcon != null)
                {
                    int iw = r * 3, ih = r * 3;
                    sb.Draw(mapIcon, new Rectangle((int)pos.X - iw / 2, (int)pos.Y - ih / 2, iw, ih), null,
                        tint, 0f, Vector2.Zero, SpriteEffects.None, 0f);
                    if (!unlocked)
                    {
                        var lockSz = _bigFont.MeasureString("🔒");
                        sb.DrawString(_bigFont, "🔒", new Vector2(pos.X - lockSz.X / 2f, pos.Y - lockSz.Y / 2f), Color.White);
                    }
                }
                else
                {
                    sb.Draw(_pixel, new Rectangle((int)pos.X - r, (int)pos.Y - r, r * 2, r * 2), (unlocked ? themeColor : Color.DimGray) * 0.85f);
                    string label = unlocked ? isl.Icon : "🔒";
                    var lsz = _bigFont.MeasureString(label);
                    sb.DrawString(_bigFont, label, new Vector2(pos.X - lsz.X / 2f, pos.Y - lsz.Y / 2f), tint);
                }
                var nsz = _font.MeasureString(isl.Name);
                sb.DrawString(_font, isl.Name, new Vector2(pos.X - nsz.X / 2f, pos.Y + r + 4), UIHelper.TextMain * (unlocked ? 1f : 0.6f));
            }

            // Bateau
            var boatTex = SpriteLoader.Boat();
            var bpos = _boat.Position;
            if (boatTex != null)
            {
                sb.Draw(boatTex, bpos, null, Color.White, _boat.Rotation,
                    new Vector2(boatTex.Width / 2f, boatTex.Height / 2f), 1f, SpriteEffects.None, 0f);
            }
            else
            {
                // Fallback : triangle simple (rectangle tourné) pointant dans la direction du cap
                sb.Draw(_pixel, bpos, null, new Color(140, 90, 50), _boat.Rotation,
                    new Vector2(0.5f, 0.5f), new Vector2(64, 28), SpriteEffects.None, 0f);
            }

            sb.End();
            sb.Begin(samplerState: SamplerState.PointClamp);

            // HUD
            _backBtn.Draw(sb, _pixel, _font);
            _questBtn.Draw(sb, _pixel, _font);
            UIHelper.DrawCenteredText(sb, _bigFont, "🌍 Événement Monde",
                new Rectangle(0, 16, W, 36), UIHelper.Gold, 0.8f);

            if (_nearestIsland != null && _nearestDist <= DOCK_RADIUS)
            {
                bool unlocked = PlayerSave.Rank >= _nearestIsland.RequiredRank;
                string prompt = unlocked
                    ? $"[E] Accoster à {_nearestIsland.Name}"
                    : $"🔒 {_nearestIsland.Name} — Rang {PlayerSave.RankNames[_nearestIsland.RequiredRank]} requis";
                var psz = _font.MeasureString(prompt);
                UIHelper.DrawBox(sb, _pixel, new Rectangle(W / 2 - (int)psz.X / 2 - 16, H - 90, (int)psz.X + 32, 34),
                    UIHelper.CardBg, unlocked ? UIHelper.Gold : Color.OrangeRed, 2);
                sb.DrawString(_font, prompt, new Vector2(W / 2 - psz.X / 2f, H - 82), unlocked ? UIHelper.Gold : Color.OrangeRed);
            }

            if (_toastTimer > 0)
            {
                float alpha = _toastTimer < 0.5f ? _toastTimer / 0.5f : 1f;
                var ts = _font.MeasureString(_toast);
                UIHelper.DrawBox(sb, _pixel, new Rectangle(W / 2 - (int)ts.X / 2 - 12, H - 140, (int)ts.X + 24, 30),
                    UIHelper.Dark2 * alpha, _toastColor * alpha, 1);
                sb.DrawString(_font, _toast, new Vector2(W / 2f - ts.X / 2f, H - 132), _toastColor * alpha);
            }

            UIHelper.DrawCenteredText(sb, _font, "WASD/Flèches : piloter le bateau  —  E : accoster",
                new Rectangle(0, H - 26, W, 22), UIHelper.TextDim, 0.75f);
        }

        public void Dispose() { }
    }
}
