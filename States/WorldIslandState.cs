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
    // Exploration à pied d'une île de l'Événement Monde : rencontres de mobs (-> Combat
    // existant, mêmes récompenses que les donjons classiques), PNJ de quête, zone de pêche.
    public class WorldIslandState : IGameState
    {
        readonly TravelTourGame _game;
        Texture2D _pixel = null!; SpriteFontBase _font = null!, _bigFont = null!;
        UIButton _backBtn = null!;
        UIButton _questBtn = null!;

        IslandData _island = null!;
        WorldCamera _camera = new();
        Vector2 _playerPos;
        const float MOVE_SPEED = 220f;
        const float INTERACT_RADIUS = 70f;

        int WORLD_W = 1000, WORLD_H = 700;
        bool _prevInteract;
        string _npcId = "";

        string _toast = ""; float _toastTimer; Color _toastColor;

        // ── Marchand de fruits aléatoire (façon "Blox Fruit Dealer") ──
        FruitData? _vendorOffer;
        bool _vendorShowingOffer;

        public WorldIslandState(TravelTourGame game) => _game = game;

        public void SetIsland(IslandData island) => _island = island;

        public void Load(Texture2D pixel, SpriteFontBase font, SpriteFontBase bigFont)
        {
            _pixel = pixel; _font = font; _bigFont = bigFont;
            _npcId = "npc_" + _island.Name;

            _backBtn = new UIButton(new Rectangle(16, 16, 140, 36), "⛵ Retour au bateau",
                () => _game.ChangeState(GameState.WorldSea));
            _questBtn = new UIButton(new Rectangle(164, 16, 130, 36), "📋 Quêtes",
                () => _game.OpenQuest(GameState.WorldIsland, _island));

            _playerPos = new Vector2(200, 500);
            _camera.Position = _playerPos;

            PlayerSave.VisitIsland(_island.Name);
            foreach (var q in Catalog.Quests) q.CheckCompleted();

            var unowned = Catalog.Fruits.Where(f => !f.IsOwned).ToList();
            _vendorOffer = unowned.Count > 0 ? unowned[new Random().Next(unowned.Count)] : null;
            _vendorShowingOffer = false;
        }

        void ShowToast(string m, Color c) { _toast = m; _toastColor = c; _toastTimer = 2.5f; }

        Vector2 NpcPos    => new Vector2(250, 350);
        Vector2 VendorPos => new Vector2(450, 350);

        static int VendorPrice(FruitData f) => f.BuyPrice > 0 ? f.BuyPrice : f.Rarity switch
        {
            Rarity.Common    => 3000,
            Rarity.Rare      => 8000,
            Rarity.Epic      => 20000,
            Rarity.Legendary => 45000,
            Rarity.Mythical  => 90000,
            _ => 3000
        };

        public void Update(GameTime gt)
        {
            float dt = (float)gt.ElapsedGameTime.TotalSeconds;
            var kb = Keyboard.GetState();
            var ms = Mouse.GetState();
            _backBtn.Update(ms);
            _questBtn.Update(ms);
            _toastTimer -= dt;

            var move = Vector2.Zero;
            if (kb.IsKeyDown(Keys.A) || kb.IsKeyDown(Keys.Left))  move.X -= 1;
            if (kb.IsKeyDown(Keys.D) || kb.IsKeyDown(Keys.Right)) move.X += 1;
            if (kb.IsKeyDown(Keys.W) || kb.IsKeyDown(Keys.Up))    move.Y -= 1;
            if (kb.IsKeyDown(Keys.S) || kb.IsKeyDown(Keys.Down))  move.Y += 1;
            if (move != Vector2.Zero) move.Normalize();
            _playerPos += move * MOVE_SPEED * dt;
            _playerPos.X = Math.Clamp(_playerPos.X, 20, WORLD_W - 20);
            _playerPos.Y = Math.Clamp(_playerPos.Y, 20, WORLD_H - 20);
            _camera.Position = Vector2.Lerp(_camera.Position, _playerPos, Math.Min(1f, dt * 6f));

            bool interactNow = kb.IsKeyDown(Keys.E);
            bool interactEdge = interactNow && !_prevInteract;
            _prevInteract = interactNow;

            if (interactEdge)
            {
                // PNJ de quête
                if (Vector2.Distance(_playerPos, NpcPos) <= INTERACT_RADIUS)
                {
                    PlayerSave.TalkToNpc(_npcId);
                    foreach (var q in Catalog.Quests) q.CheckCompleted();
                    _game.OpenQuest(GameState.WorldIsland, _island);
                }
                // Zone de pêche
                else if (_island.HasFishingSpot && Vector2.Distance(_playerPos, _island.FishingSpotPosition) <= INTERACT_RADIUS)
                {
                    _game.EnterFishing(_island);
                }
                // Marchand de fruits aléatoire
                else if (Vector2.Distance(_playerPos, VendorPos) <= INTERACT_RADIUS)
                {
                    if (_vendorOffer == null)
                        ShowToast("🛒 Le marchand n'a rien à vendre pour l'instant.", UIHelper.TextDim);
                    else if (!_vendorShowingOffer)
                    {
                        _vendorShowingOffer = true;
                    }
                    else
                    {
                        int price = VendorPrice(_vendorOffer);
                        if (PlayerSave.SpendGold(price))
                        {
                            _vendorOffer.IsOwned = true;
                            if (!PlayerSave.OwnedFruits.Contains(_vendorOffer.Name)) PlayerSave.OwnedFruits.Add(_vendorOffer.Name);
                            ShowToast($"✅ {_vendorOffer.Icon} {_vendorOffer.Name} acheté au marchand !", Color.Green);
                            foreach (var q in Catalog.Quests) q.CheckCompleted();
                            _vendorOffer = null;
                            _vendorShowingOffer = false;
                        }
                        else
                        {
                            ShowToast($"Or insuffisant ! ({price:N0} requis, tu as {PlayerSave.Gold:N0})", Color.Red);
                        }
                    }
                }
                else
                {
                    // Mobs de l'île
                    foreach (var spawn in _island.MobSpawns)
                    {
                        if (Vector2.Distance(_playerPos, spawn.Position) <= INTERACT_RADIUS)
                        {
                            var dungeon = Catalog.Dungeons.Find(d => d.Name == _island.LinkedDungeonName);
                            if (dungeon != null) _game.StartIslandDungeon(dungeon, _island);
                            break;
                        }
                    }
                }
            }

            if (kb.IsKeyDown(Keys.Escape)) _game.ChangeState(GameState.WorldSea);
        }

        public void Draw(SpriteBatch sb)
        {
            int W = _game.GraphicsDevice.Viewport.Width;
            int H = _game.GraphicsDevice.Viewport.Height;

            sb.End();
            sb.Begin(transformMatrix: _camera.GetTransform(W, H), samplerState: SamplerState.PointClamp);

            var bgTex = SpriteLoader.IslandTerrain(_island.BgSpriteKey);
            if (bgTex != null)
            {
                sb.Draw(bgTex, new Rectangle(0, 0, WORLD_W, WORLD_H), Color.White);
            }
            else
            {
                var themeColor = _island.Theme switch
                {
                    IslandTheme.Hunter => new Color(60, 20, 30),
                    IslandTheme.Pirate => new Color(20, 45, 60),
                    IslandTheme.Ninja  => new Color(25, 45, 25),
                    _ => new Color(30, 30, 30)
                };
                sb.Draw(_pixel, new Rectangle(0, 0, WORLD_W, WORLD_H), themeColor);
                for (int gx = 0; gx < WORLD_W; gx += 80)
                    sb.Draw(_pixel, new Rectangle(gx, 0, 1, WORLD_H), Color.White * 0.05f);
                for (int gy = 0; gy < WORLD_H; gy += 80)
                    sb.Draw(_pixel, new Rectangle(0, gy, WORLD_W, 1), Color.White * 0.05f);
            }

            // PNJ
            DrawMarker(sb, NpcPos, "🧑", "PNJ de quête", UIHelper.Blue);

            // Zone de pêche
            if (_island.HasFishingSpot)
                DrawMarker(sb, _island.FishingSpotPosition, "🎣", "Pêche", new Color(80, 180, 220));

            // Marchand de fruits aléatoire
            DrawMarker(sb, VendorPos, "🛍️", "Marchand", new Color(220, 170, 40));

            // Mobs
            foreach (var spawn in _island.MobSpawns)
                DrawMarker(sb, spawn.Position, spawn.IsBoss ? "👹" : "👺", spawn.EnemyName, spawn.IsBoss ? Color.OrangeRed : new Color(200, 90, 90));

            // Joueur
            float depthScale = WorldCamera.DepthScaleFor(_playerPos.Y, 0, WORLD_H);
            int psz = (int)(48 * depthScale);
            sb.Draw(_pixel, new Rectangle((int)_playerPos.X - psz / 2, (int)_playerPos.Y - psz / 2, psz, psz), new Color(240, 220, 80));

            sb.End();
            sb.Begin(samplerState: SamplerState.PointClamp);

            _backBtn.Draw(sb, _pixel, _font);
            _questBtn.Draw(sb, _pixel, _font);
            UIHelper.DrawCenteredText(sb, _bigFont, $"{_island.Icon} {_island.Name}",
                new Rectangle(0, 16, W, 36), UIHelper.Gold, 0.8f);

            if (_toastTimer > 0)
            {
                float alpha = _toastTimer < 0.5f ? _toastTimer / 0.5f : 1f;
                var ts = _font.MeasureString(_toast);
                UIHelper.DrawBox(sb, _pixel, new Rectangle(W / 2 - (int)ts.X / 2 - 12, H - 100, (int)ts.X + 24, 30),
                    UIHelper.Dark2 * alpha, _toastColor * alpha, 1);
                sb.DrawString(_font, _toast, new Vector2(W / 2f - ts.X / 2f, H - 92), _toastColor * alpha);
            }

            if (_vendorShowingOffer && _vendorOffer != null)
            {
                Color rc = UIHelper.RarityColors[(int)_vendorOffer.Rarity];
                int price = VendorPrice(_vendorOffer);
                var panel = new Rectangle(W / 2 - 170, H - 200, 340, 90);
                UIHelper.DrawBox(sb, _pixel, panel, UIHelper.CardBg, rc, 2);
                sb.DrawString(_bigFont, _vendorOffer.Icon, new Vector2(panel.X + 10, panel.Y + 8), Color.White);
                sb.DrawString(_font, $"{_vendorOffer.Name}", new Vector2(panel.X + 60, panel.Y + 10), UIHelper.TextMain);
                sb.DrawString(_font, UIHelper.RarityNames[(int)_vendorOffer.Rarity], new Vector2(panel.X + 60, panel.Y + 30), rc);
                sb.DrawString(_font, $"{price:N0} 💰 — [E] pour acheter", new Vector2(panel.X + 60, panel.Y + 50), UIHelper.Gold);
            }

            UIHelper.DrawCenteredText(sb, _font, "WASD/Flèches : se déplacer  —  E : interagir  —  Échap : retour au bateau",
                new Rectangle(0, H - 26, W, 22), UIHelper.TextDim, 0.75f);
        }

        void DrawMarker(SpriteBatch sb, Vector2 pos, string icon, string label, Color color)
        {
            float depthScale = WorldCamera.DepthScaleFor(pos.Y, 0, WORLD_H);
            int r = (int)(30 * depthScale);
            sb.Draw(_pixel, new Rectangle((int)pos.X - r, (int)pos.Y - r, r * 2, r * 2), color * 0.8f);
            var sz = _bigFont.MeasureString(icon);
            sb.DrawString(_bigFont, icon, new Vector2(pos.X - sz.X / 2f, pos.Y - sz.Y / 2f), Color.White);
        }

        public void Dispose() { }
    }
}
