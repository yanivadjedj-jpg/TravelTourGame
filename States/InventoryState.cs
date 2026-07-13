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
    public class InventoryState : IGameState
    {
        readonly TravelTourGame _game;
        Texture2D _pixel = null!;
        SpriteFontBase _font = null!, _bigFont = null!;
        UIButton _backBtn = null!;

        // Tabs: 0=Persos 1=Armes 2=Véhicules 3=Fruits 4=Capacités 5=Matériaux 6=Artefacts 7=Maîtrise 8=Pêche
        int _tab = 0;
        List<UIButton> _tabBtns = new();

        static readonly string[] TabNames  = { "👤 Persos", "⚔️ Armes", "🚗 Véhicules", "🍎 Fruits", "✨ Capacités", "🔮 Matériaux", "🏺 Artefacts", "🎖️ Maîtrise", "🎣 Pêche" };
        static readonly Color[]  TabColors = {
            new Color(240,192,64),  // gold
            new Color(200,80,80),   // red
            new Color(80,160,240),  // blue
            new Color(200,80,255),  // purple
            new Color(80,230,180),  // cyan
            new Color(160,120,80),  // brown
            new Color(255,165,0),   // orange
            new Color(120,220,255), // mastery cyan-blue
            new Color(60,170,210),  // pêche bleu océan
        };

        // Scroll
        float _scrollY = 0;
        int   _prevScroll;

        // Boutons d'action (reconstruits à chaque changement d'onglet/scroll)
        List<UIButton> _actionBtns = new();
        string _toastMsg = ""; float _toastTimer; Color _toastColor;
        MouseState _curMs, _prevMs;

        public InventoryState(TravelTourGame game) => _game = game;

        public void Load(Texture2D pixel, SpriteFontBase font, SpriteFontBase bigFont)
        {
            _pixel = pixel; _font = font; _bigFont = bigFont;
            int W = _game.GraphicsDevice.Viewport.Width;

            _backBtn = new UIButton(new Rectangle(16, 16, 110, 36), "← Menu",
                () => _game.ChangeState(GameState.MainMenu));

            _tabBtns.Clear();
            int tw = 148, tgap = 6;
            int tStartX = W / 2 - (TabNames.Length * (tw + tgap)) / 2;
            for (int i = 0; i < TabNames.Length; i++)
            {
                int idx = i;
                _tabBtns.Add(new UIButton(
                    new Rectangle(tStartX + i * (tw + tgap), 62, tw, 30),
                    TabNames[i],
                    () => { _tab = idx; _scrollY = 0; }));
            }
        }

        public void Update(GameTime gt)
        {
            float dt = (float)gt.ElapsedGameTime.TotalSeconds;
            _toastTimer -= dt;

            _prevMs = _curMs;
            _curMs  = Mouse.GetState();
            var ms  = _curMs;
            _backBtn.Update(ms);
            foreach (var b in _tabBtns.ToList()) b.Update(ms);

            // Scroll
            if (ms.ScrollWheelValue != _prevScroll)
                _scrollY = System.Math.Max(0, _scrollY - (ms.ScrollWheelValue - _prevScroll) / 120f * 40f);
            _prevScroll = ms.ScrollWheelValue;

            if (Keyboard.GetState().IsKeyDown(Keys.Escape)) _game.ChangeState(GameState.MainMenu);
        }

        void Toast(string msg, Color col) { _toastMsg = msg; _toastTimer = 2.5f; _toastColor = col; }

        public void Draw(SpriteBatch sb)
        {
            int W = _game.GraphicsDevice.Viewport.Width;
            int H = _game.GraphicsDevice.Viewport.Height;

            sb.Draw(_pixel, new Rectangle(0, 0, W, H), UIHelper.Dark);

            _backBtn.Draw(sb, _pixel, _font, 0.85f);
            UIHelper.DrawCenteredText(sb, _bigFont, "🎒  INVENTAIRE",
                new Rectangle(0, 14, W, 44), new Color(80, 200, 120), 0.72f);

            // Tabs
            for (int i = 0; i < _tabBtns.Count; i++)
            {
                _tabBtns[i].NormalColor = i == _tab ? TabColors[i] * 0.25f : UIHelper.CardBg;
                _tabBtns[i].TextColor   = i == _tab ? TabColors[i] : UIHelper.TextDim;
                _tabBtns[i].Draw(sb, _pixel, _font, 0.75f);
            }

            // Séparateur
            sb.Draw(_pixel, new Rectangle(20, 98, W - 40, 1), UIHelper.TextDim * 0.2f);

            // Contenu selon l'onglet
            int contentY = 108 - (int)_scrollY;
            switch (_tab)
            {
                case 0: DrawCharacters(sb, W, H, contentY); break;
                case 1: DrawWeapons(sb, W, H, contentY); break;
                case 2: DrawVehicles(sb, W, H, contentY); break;
                case 3: DrawFruits(sb, W, H, contentY); break;
                case 4: DrawAbilities(sb, W, H, contentY); break;
                case 5: DrawMaterials(sb, W, H, contentY); break;
                case 6: DrawArtifacts(sb, W, H, contentY); break;
                case 7: DrawMasteries(sb, W, H, contentY); break;
                case 8: DrawFishing(sb, W, H, contentY); break;
            }

            // Toast
            if (_toastTimer > 0)
            {
                float a = System.Math.Min(1f, _toastTimer);
                Vector2 ts = _font.MeasureString(_toastMsg);
                int tx = (int)(W / 2f - ts.X / 2f) - 12;
                UIHelper.DrawBox(sb, _pixel, new Rectangle(tx, H - 56, (int)ts.X + 24, 32),
                    UIHelper.Dark2, _toastColor, 1);
                sb.DrawString(_font, _toastMsg, new Vector2(W / 2f - ts.X / 2f, H - 50), _toastColor * a);
            }

            // Masque haut/bas pour le scroll
            sb.Draw(_pixel, new Rectangle(0, 0,   W, 100), UIHelper.Dark * 0.95f);
            sb.Draw(_pixel, new Rectangle(0, H-40, W, 40), UIHelper.Dark * 0.9f);

            // Compteur en bas
            string count = GetCount();
            sb.DrawString(_font, count, new Vector2(W - 200, H - 28), UIHelper.TextDim * 0.6f);
        }

        // ── Utilitaires dessin ───────────────────────────────────────

        void DrawItemCard(SpriteBatch sb, int x, int y, int w,
                          string icon, string name, string sub1, string sub2,
                          Color accent, bool equipped, System.Action? onEquip, System.Action? onUnequip)
        {
            int h = 100;
            Color bg = UIHelper.CardBg;
            UIHelper.DrawBox(sb, _pixel, new Rectangle(x, y, w, h), bg, accent * 0.7f, 2);

            // Barre verte si équipé
            if (equipped)
                sb.Draw(_pixel, new Rectangle(x, y, w, 3), new Color(80, 230, 100) * 0.9f);

            // Sprite ou emoji
            var charSprite = TravelTour.Core.SpriteLoader.Character(name);
            if (charSprite != null)
                sb.Draw(charSprite, new Rectangle(x + 4, y + 4, 52, 52), Color.White);
            else
                UIHelper.DrawCenteredText(sb, _bigFont, icon,
                    new Rectangle(x + 4, y + 8, 52, 46), Color.White, 0.75f);

            // Infos
            sb.DrawString(_font, name, new Vector2(x + 62, y + 6),  UIHelper.TextMain);
            sb.DrawString(_font, sub1, new Vector2(x + 62, y + 24), accent * 0.9f);
            sb.DrawString(_font, sub2, new Vector2(x + 62, y + 42), UIHelper.TextDim * 0.75f);

            // Bouton équiper/retirer — détection directe press→release
            if (onEquip != null || onUnequip != null)
            {
                int bx = x + 4, by = y + h - 26, bw = w - 8, bh = 22;
                var bounds = new Rectangle(bx, by, bw, bh);
                bool hov     = bounds.Contains(_curMs.Position);
                bool clicked = hov
                    && _curMs.LeftButton  == ButtonState.Released
                    && _prevMs.LeftButton == ButtonState.Pressed;

                if (equipped)
                {
                    if (clicked) onUnequip?.Invoke();
                    Color fill = hov ? new Color(20, 60, 20) : new Color(10, 40, 10);
                    UIHelper.DrawBox(sb, _pixel, bounds, fill, new Color(80, 230, 100) * 0.6f, 1);
                    UIHelper.DrawCenteredText(sb, _font, "✔ Retirer", bounds, new Color(80, 230, 100), 0.72f);
                }
                else
                {
                    if (clicked) onEquip?.Invoke();
                    Color fill = hov ? new Color(50, 20, 90) : new Color(20, 10, 40);
                    UIHelper.DrawBox(sb, _pixel, bounds, fill, UIHelper.Purple * 0.6f, 1);
                    UIHelper.DrawCenteredText(sb, _font, "⚡ Équiper", bounds, UIHelper.Purple, 0.72f);
                }
            }
        }

        int GridLayout(SpriteBatch sb, int W, int cols, int cardW, int cardH, int gap, int startY,
                       System.Action<int, int, int> drawItem, int count)
        {
            int startX = W / 2 - (cols * (cardW + gap)) / 2;
            for (int i = 0; i < count; i++)
            {
                int col = i % cols, row = i / cols;
                int x = startX + col * (cardW + gap);
                int y = startY + row * (cardH + gap);
                drawItem(i, x, y);
            }
            int rows = (count + cols - 1) / cols;
            return startY + rows * (cardH + gap);
        }

        // ── Onglets ──────────────────────────────────────────────────

        void DrawCharacters(SpriteBatch sb, int W, int H, int y0)
        {
            var owned = Catalog.Characters.Where(c => c.IsOwned).ToList();
            if (owned.Count == 0) { DrawEmpty(sb, W, y0, "Aucun personnage possédé"); return; }
            UIHelper.DrawCenteredText(sb, _font, $"{owned.Count} personnage(s)",
                new Rectangle(0, y0, W, 22), UIHelper.TextDim, 0.8f);
            var leader = TeamManager.GetLeader();
            GridLayout(sb, W, 4, 220, 100, 12, y0 + 28, (i, x, y) =>
            {
                var c = owned[i]; Color rc = UIHelper.RarityColors[(int)c.Rarity];
                bool isLeader = leader?.Name == c.Name;
                var cc = c;
                DrawItemCard(sb, x, y, 220, c.Icon, c.Name,
                    UIHelper.RarityNames[(int)c.Rarity], $"Niv.{c.Level}/{c.MaxLevel}",
                    rc, isLeader,
                    () => { PlayerSave.CurrentTeam[0] = cc.Name; SaveSystem.Save(); Toast($"⭐ {cc.Name} leader!", UIHelper.Gold); },
                    () => Toast("Enlève depuis My Team.", UIHelper.TextDim));
            }, owned.Count);
        }

        void DrawWeapons(SpriteBatch sb, int W, int H, int y0)
        {
            var owned = Catalog.Weapons.Where(w => w.IsOwned).ToList();
            if (owned.Count == 0) { DrawEmpty(sb, W, y0, "Aucune arme possédée"); return; }
            UIHelper.DrawCenteredText(sb, _font, $"{owned.Count} arme(s)  —  équipée : {(PlayerSave.GetEquippedWeapon()?.Name ?? "aucune")}",
                new Rectangle(0, y0, W, 22), UIHelper.TextDim, 0.8f);
            GridLayout(sb, W, 4, 220, 100, 12, y0 + 28, (i, x, y) =>
            {
                var w = owned[i]; Color rc = UIHelper.RarityColors[(int)w.Rarity];
                var ww = w;
                DrawItemCard(sb, x, y, 220, w.Icon, w.Name,
                    UIHelper.RarityNames[(int)w.Rarity], $"+{(int)w.GetDamage()} DMG  Niv.{w.Level}",
                    rc, w.IsEquipped,
                    () => { PlayerSave.EquipWeapon(ww.Name); Toast($"⚔️ {ww.Name} équipée!", rc); },
                    () => { PlayerSave.UnequipWeapon(); Toast("Arme retirée.", UIHelper.TextDim); });
            }, owned.Count);
        }

        void DrawVehicles(SpriteBatch sb, int W, int H, int y0)
        {
            var owned = Catalog.Vehicles.Where(v => v.IsOwned).ToList();
            if (owned.Count == 0) { DrawEmpty(sb, W, y0, "Aucun véhicule possédé"); return; }
            UIHelper.DrawCenteredText(sb, _font, $"{owned.Count} véhicule(s)  —  équipé : {(PlayerSave.GetEquippedVehicle()?.Name ?? "aucun")}",
                new Rectangle(0, y0, W, 22), UIHelper.TextDim, 0.8f);
            GridLayout(sb, W, 4, 220, 100, 12, y0 + 28, (i, x, y) =>
            {
                var v = owned[i]; Color rc = UIHelper.RarityColors[(int)v.Rarity];
                // Remplace l'emoji par le sprite si disponible
                var sprite = TravelTour.Core.SpriteLoader.Vehicle(v.Name);
                string icon = sprite == null ? v.Icon : "";
                var vv = v;
                DrawItemCard(sb, x, y, 220, icon, v.Name,
                    UIHelper.RarityNames[(int)v.Rarity], $"SPD {v.Speed}  ACC {v.Acceleration}",
                    rc, v.IsEquipped,
                    () => { PlayerSave.EquipVehicle(vv.Name); Toast($"🚗 {vv.Name} équipé!", rc); },
                    () => { PlayerSave.UnequipVehicle(); Toast("Véhicule retiré.", UIHelper.TextDim); });
                if (sprite != null)
                    sb.Draw(sprite, new Rectangle(x + 4, y + 8, 52, 52), Color.White);
            }, owned.Count);
        }

        void DrawFruits(SpriteBatch sb, int W, int H, int y0)
        {
            var owned = Catalog.Fruits.Where(f => f.IsOwned).ToList();
            if (owned.Count == 0) { DrawEmpty(sb, W, y0, "Aucun fruit possédé"); return; }
            UIHelper.DrawCenteredText(sb, _font, $"{owned.Count} fruit(s)  —  équipé : {(PlayerSave.GetEquippedFruit()?.Name ?? "aucun")}",
                new Rectangle(0, y0, W, 22), UIHelper.TextDim, 0.8f);
            GridLayout(sb, W, 4, 220, 100, 12, y0 + 28, (i, x, y) =>
            {
                var f = owned[i]; Color rc = UIHelper.RarityColors[(int)f.Rarity];
                var ff = f;
                DrawItemCard(sb, x, y, 220, f.Icon, f.Name,
                    $"{f.Type} · {UIHelper.RarityNames[(int)f.Rarity]}", $"Maîtrise {f.Mastery}/600",
                    rc, f.IsEquipped,
                    () => { PlayerSave.EquipFruit(ff.Name); Toast($"🍎 {ff.Name} équipé!", new Color(200,80,255)); },
                    () => { PlayerSave.UnequipFruit(); Toast("Fruit retiré.", UIHelper.TextDim); });
            }, owned.Count);
        }

        void DrawAbilities(SpriteBatch sb, int W, int H, int y0)
        {
            var owned = Catalog.Abilities.Where(a => a.IsOwned).ToList();
            if (owned.Count == 0) { DrawEmpty(sb, W, y0, "Aucune capacité possédée"); return; }
            UIHelper.DrawCenteredText(sb, _font, $"{owned.Count} capacité(s)",
                new Rectangle(0, y0, W, 22), UIHelper.TextDim, 0.8f);
            GridLayout(sb, W, 3, 290, 100, 12, y0 + 28, (i, x, y) =>
            {
                var a = owned[i];
                UIHelper.DrawBox(sb, _pixel, new Rectangle(x, y, 290, 100), UIHelper.CardBg, UIHelper.Purple * 0.6f, 2);
                var aSprite = TravelTour.Core.SpriteLoader.Ability(a.Name);
                if (aSprite != null)
                    sb.Draw(aSprite, new Rectangle(x + 4, y + 4, 54, 54), Color.White);
                else
                    UIHelper.DrawCenteredText(sb, _bigFont, a.Icon,
                        new Rectangle(x + 4, y + 4, 54, 54), Color.White, 0.7f);
                sb.DrawString(_font, a.Name, new Vector2(x + 64, y + 6), UIHelper.TextMain);
                string desc = a.Description.Length > 36 ? a.Description[..36] + "…" : a.Description;
                sb.DrawString(_font, desc, new Vector2(x + 64, y + 24), UIHelper.TextDim * 0.75f);
                sb.DrawString(_font, $"DMG:{a.Damage}  CD:{a.Cooldown}s  {a.ChakraCost}☯",
                    new Vector2(x + 64, y + 46), UIHelper.Purple * 0.9f);
                // Bouton "Équiper en Q/E" — simplement un indicatif
                UIHelper.DrawBox(sb, _pixel, new Rectangle(x + 4, y + 75, 282, 20),
                    new Color(20,10,35), UIHelper.Purple * 0.3f, 1);
                UIHelper.DrawCenteredText(sb, _font, "Utilisez Q / E en combat",
                    new Rectangle(x + 4, y + 75, 282, 20), UIHelper.Purple * 0.6f, 0.68f);
            }, owned.Count);
        }

        void DrawMaterials(SpriteBatch sb, int W, int H, int y0)
        {
            var mats = PlayerSave.Materials.Where(kv => kv.Value > 0).ToList();
            if (mats.Count == 0) { DrawEmpty(sb, W, y0, "Aucun matériau"); return; }

            int total = PlayerSave.Materials.Values.Sum();
            UIHelper.DrawCenteredText(sb, _font, $"{mats.Count} type(s)  —  {total} au total",
                new Rectangle(0, y0, W, 22), UIHelper.TextDim, 0.8f);

            int cols = 5, cw = 170, ch = 90, gap = 12;
            int startX = W / 2 - (cols * (cw + gap)) / 2;
            for (int i = 0; i < mats.Count; i++)
            {
                var kv = mats[i];
                int col = i % cols, row = i / cols;
                int x = startX + col * (cw + gap);
                int y = y0 + 28 + row * (ch + gap);

                MaterialInfo.Data.TryGetValue(kv.Key, out var d);
                string icon  = d.Item1 ?? "❓";
                string label = d.Item2 ?? kv.Key;
                int    rIdx  = (int)(d.Item4);
                Color  rc    = UIHelper.RarityColors[rIdx];

                UIHelper.DrawBox(sb, _pixel, new Rectangle(x, y, cw, ch), UIHelper.CardBg, rc * 0.6f, 2);
                UIHelper.DrawCenteredText(sb, _bigFont, icon,
                    new Rectangle(x + 4, y + 8, 48, 48), Color.White, 0.7f);

                // Quantité en grand
                string qty = kv.Value.ToString();
                UIHelper.DrawCenteredText(sb, _bigFont, qty,
                    new Rectangle(x + 56, y + 10, cw - 60, 36), rc, 0.65f);
                UIHelper.DrawCenteredText(sb, _font, label,
                    new Rectangle(x + 4, y + ch - 22, cw - 8, 18), UIHelper.TextDim * 0.8f, 0.7f);
            }
        }

        void DrawFishing(SpriteBatch sb, int W, int H, int y0)
        {
            var rods = Catalog.FishingRods.Where(r => r.IsOwned).ToList();
            UIHelper.DrawCenteredText(sb, _font, $"{rods.Count} canne(s)  —  équipée : {(PlayerSave.GetEquippedFishingRod()?.Name ?? "aucune")}",
                new Rectangle(0, y0, W, 22), UIHelper.TextDim, 0.8f);

            int nextY = GridLayout(sb, W, 4, 220, 100, 12, y0 + 28, (i, x, y) =>
            {
                var r = rods[i]; Color rc = UIHelper.RarityColors[(int)r.Rarity];
                var rr = r;
                DrawItemCard(sb, x, y, 220, r.Icon, r.Name,
                    UIHelper.RarityNames[(int)r.Rarity], r.EffectLabel(),
                    rc, r.IsEquipped,
                    () => { PlayerSave.EquipFishingRod(rr.Name); Toast($"🎣 {rr.Name} équipée!", rc); },
                    () => { PlayerSave.EquipFishingRod("Canne en Bois"); Toast("Canne retirée.", UIHelper.TextDim); });
            }, rods.Count);

            var fish = PlayerSave.FishInventory.Where(kv => kv.Value > 0).ToList();
            int fishY = nextY + 20;
            if (fish.Count == 0)
            {
                DrawEmpty(sb, W, fishY - 60, "Aucun poisson pêché");
                return;
            }
            UIHelper.DrawCenteredText(sb, _font, $"{fish.Count} type(s) de poisson pêché(s)",
                new Rectangle(0, fishY, W, 22), UIHelper.TextDim, 0.8f);

            int cols = 5, cw = 170, ch = 90, gap = 12;
            int startX = W / 2 - (cols * (cw + gap)) / 2;
            for (int i = 0; i < fish.Count; i++)
            {
                var kv = fish[i];
                int col = i % cols, row = i / cols;
                int x = startX + col * (cw + gap);
                int y = fishY + 28 + row * (ch + gap);

                var info = Catalog.Fish.Find(f => f.Name == kv.Key);
                string icon = info?.Icon ?? "🐟";
                Color rc = UIHelper.RarityColors[(int)(info?.Rarity ?? Rarity.Common)];

                UIHelper.DrawBox(sb, _pixel, new Rectangle(x, y, cw, ch), UIHelper.CardBg, rc * 0.6f, 2);
                UIHelper.DrawCenteredText(sb, _bigFont, icon, new Rectangle(x + 4, y + 8, 48, 48), Color.White, 0.7f);
                UIHelper.DrawCenteredText(sb, _bigFont, kv.Value.ToString(), new Rectangle(x + 56, y + 10, cw - 60, 36), rc, 0.65f);
                UIHelper.DrawCenteredText(sb, _font, kv.Key, new Rectangle(x + 4, y + ch - 22, cw - 8, 18), UIHelper.TextDim * 0.8f, 0.7f);
            }
        }

        void DrawEmpty(SpriteBatch sb, int W, int y, string msg)
        {
            UIHelper.DrawCenteredText(sb, _font, msg,
                new Rectangle(0, y + 60, W, 30), UIHelper.TextDim, 0.85f);
        }

        string GetCount()
        {
            return _tab switch {
                0 => $"{Catalog.Characters.Count(c => c.IsOwned)}/{Catalog.Characters.Count} persos",
                1 => $"{Catalog.Weapons.Count(w => w.IsOwned)}/{Catalog.Weapons.Count} armes",
                2 => $"{Catalog.Vehicles.Count(v => v.IsOwned)}/{Catalog.Vehicles.Count} véhicules",
                3 => $"{Catalog.Fruits.Count(f => f.IsOwned)}/{Catalog.Fruits.Count} fruits",
                4 => $"{Catalog.Abilities.Count(a => a.IsOwned)}/{Catalog.Abilities.Count} capacités",
                6 => $"{Catalog.Artifacts.Count(a => a.IsOwned)}/{Catalog.Artifacts.Count} artefacts",
                8 => $"{Catalog.FishingRods.Count(r => r.IsOwned)}/{Catalog.FishingRods.Count} cannes",
                _ => $"{PlayerSave.Materials.Count(kv => kv.Value > 0)} types"
            };
        }

        void DrawArtifacts(SpriteBatch sb, int W, int H, int y0)
        {
            // Redirige vers la page Artefacts dédiée
            UIHelper.DrawCenteredText(sb, _font,
                "Page dédiée aux artefacts →",
                new Rectangle(0, y0 + 30, W, 30), UIHelper.TextDim, 0.85f);
            int bx = W / 2 - 120, by = y0 + 70;
            var bounds = new Rectangle(bx, by, 240, 42);
            bool hov = bounds.Contains(_curMs.Position);
            bool clk = hov && _curMs.LeftButton == ButtonState.Released && _prevMs.LeftButton == ButtonState.Pressed;
            UIHelper.DrawBox(sb, _pixel, bounds, hov ? new Color(40,25,5) : UIHelper.CardBg, new Color(255,165,0)*0.7f, 2);
            UIHelper.DrawCenteredText(sb, _font, "🏺  Ouvrir Artefacts", bounds, new Color(255,165,0), 0.85f);
            if (clk)
            {
                // On ne peut pas appeler ChangeState depuis ici directement, mais via une action différée
                Toast("Retourne au menu → ARTEFACTS", new Color(255,165,0));
            }
        }

        void DrawMasteries(SpriteBatch sb, int W, int H, int y0)
        {
            var owned = Catalog.Characters.Where(c => c.IsOwned).ToList();
            if (owned.Count == 0) { DrawEmpty(sb, W, y0, "Aucun personnage possédé"); return; }
            UIHelper.DrawCenteredText(sb, _font, "Éliminez des ennemis pour faire progresser la maîtrise de votre perso actif",
                new Rectangle(0, y0, W, 22), UIHelper.TextDim, 0.72f);
            GridLayout(sb, W, 3, 290, 112, 12, y0 + 28, (i, x, y) =>
            {
                var c = owned[i];
                Color tierCol = c.Mastery >= 450 ? new Color(180, 220, 255)
                              : c.Mastery >= 250 ? UIHelper.Gold
                              : c.Mastery >= 100 ? new Color(200, 200, 200)
                              : new Color(150, 100, 60);
                UIHelper.DrawBox(sb, _pixel, new Rectangle(x, y, 290, 112), UIHelper.CardBg, tierCol * 0.6f, 2);
                UIHelper.DrawCenteredText(sb, _bigFont, c.Icon, new Rectangle(x + 4, y + 4, 54, 54), Color.White, 0.7f);
                sb.DrawString(_font, c.Name, new Vector2(x + 64, y + 6), UIHelper.TextMain);
                sb.DrawString(_font, $"Maîtrise {c.MasteryTier()}", new Vector2(x + 64, y + 26), tierCol);
                UIHelper.DrawProgressBar(sb, _pixel, new Rectangle(x + 64, y + 48, 214, 8), c.MasteryPct(), tierCol, new Color(20, 20, 30));
                sb.DrawString(_font, $"{c.Mastery}/600", new Vector2(x + 64, y + 58), UIHelper.TextDim * 0.8f);
                string ultStatus = c.MasteryUltimateUnlocked
                    ? $"⚡ {c.MasteryUltimateName} (touche C)"
                    : "🔒 Ultime débloquée à 450 de maîtrise";
                sb.DrawString(_font, ultStatus, new Vector2(x + 8, y + 86),
                    c.MasteryUltimateUnlocked ? new Color(255, 215, 0) : UIHelper.TextDim * 0.6f);
            }, owned.Count);
        }

        public void Dispose() { }
    }
}
