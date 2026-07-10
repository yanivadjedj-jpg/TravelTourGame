using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FontStashSharp;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using System.Linq;
using TravelTour.Core;
using TravelTour.UI;

namespace TravelTour.States
{
    public class BoutiqueState : IGameState
    {
        readonly TravelTourGame _game;
        Texture2D _pixel = null!; SpriteFontBase _font = null!, _bigFont = null!;

        UIButton _backBtn = null!;
        List<UIButton> _tabBtns   = new();
        List<UIButton> _subTabs   = new();
        List<UIButton> _itemBtns  = new();

        int _tab    = 0; // 0=Améliorations, 1=Shop
        int _subTab = 0; // Améliorations: 0=Armes 1=Skins 2=Véhicules 3=Capacités
                         // Shop: 0=Armes 1=Skins 2=Véhicules

        string _toast = ""; float _toastTimer; Color _toastColor;
        MouseState _prevMs;

        // Tuto Fruits
        bool _fruitTutoSeen  = false;
        bool _fruitTutoShown = false;  // true quand l'overlay est visible
        UIButton _fruitTutoBtn = null!;

        readonly string[] _tabNames    = { "⚡ Capacités & Améliorations", "🛒 Armes, Skins & Véhicules", "🍎 Fruits du Démon", "💰 Vente de Matériaux" };
        readonly string[][] _subNames  = {
            new[]{ "🗡️ Armes", "👘 Skins", "🚗 Véhicules", "✨ Capacités" },
            new[]{ "🗡️ Armes", "👘 Skins", "🚗 Véhicules" },
            new[]{ "🌿 Naturel", "⚡ Élémentaire", "🐉 Bête" },
            System.Array.Empty<string>()
        };

        public BoutiqueState(TravelTourGame game) => _game = game;

        public void Load(Texture2D pixel, SpriteFontBase font, SpriteFontBase bigFont)
        {
            _pixel = pixel; _font = font; _bigFont = bigFont;
            RebuildAll();
        }

        void RebuildAll()
        {
            int W = _game.GraphicsDevice.Viewport.Width;
            int H = _game.GraphicsDevice.Viewport.Height;

            _backBtn = new UIButton(new Rectangle(16, 16, 100, 36), "← Menu",
                () => _game.ChangeState(GameState.MainMenu));

            _fruitTutoBtn = new UIButton(
                new Rectangle(W / 2 - 130, H / 2 + 160, 260, 44),
                "✅ J'ai compris — Allons-y !",
                () => { _fruitTutoSeen = true; _fruitTutoShown = false; },
                new Color(255, 140, 0) * 0.3f, new Color(255, 140, 0) * 0.5f
            ) { TextColor = new Color(255, 220, 80) };

            // Main tabs
            _tabBtns.Clear();
            int tabW = 228, tabGap = 8;
            int tabsTotalW = _tabNames.Length * tabW + (_tabNames.Length - 1) * tabGap;
            for (int i = 0; i < _tabNames.Length; i++)
            {
                int idx = i;
                _tabBtns.Add(new UIButton(
                    new Rectangle(W / 2 - tabsTotalW / 2 + i * (tabW + tabGap), 70, tabW, 34),
                    _tabNames[i],
                    () => { _tab = idx; _subTab = 0; RebuildSubTabs(); RebuildItems(); }));
            }

            RebuildSubTabs();
            RebuildItems();
        }

        void RebuildSubTabs()
        {
            int W = _game.GraphicsDevice.Viewport.Width;
            _subTabs.Clear();
            string[] names = _subNames[_tab];
            int sw = 150;
            int startX = W / 2 - names.Length * (sw + 8) / 2;
            for (int i = 0; i < names.Length; i++)
            {
                int idx = i;
                _subTabs.Add(new UIButton(
                    new Rectangle(startX + i * (sw + 10), 114, sw, 30),
                    names[i],
                    () => { _subTab = idx; RebuildItems(); }));
            }
        }

        void RebuildItems()
        {
            _itemBtns.Clear();
            int W = _game.GraphicsDevice.Viewport.Width;
            int H = _game.GraphicsDevice.Viewport.Height;
            int iw = 220, ih = 90, gap = 14, cols = 4;
            int startX = W / 2 - (cols * (iw + gap)) / 2;
            int startY = 158;
            int i = 0;

            void MakeBtn(string label, bool owned, int price, Rarity rar, System.Action act)
            {
                int col = i % cols, row = i / cols;
                int x = startX + col * (iw + gap), y = startY + row * (ih + gap);
                _itemBtns.Add(new UIButton(
                    new Rectangle(x, y, iw, ih), "",
                    act, UIHelper.CardBg));
                i++;
            }

            if (_tab == 2) // FRUITS — bouton explicite par carte
            {
                FruitType filter = _subTab switch { 1 => FruitType.Élémentaire, 2 => FruitType.Bête, _ => FruitType.Naturel };
                foreach (var f in Catalog.Fruits)
                {
                    if (f.Type != filter) continue;
                    var ff = f;
                    int col = i % cols, row = i / cols;
                    int x = startX + col * (iw + gap), y = startY + row * (ih + gap);
                    // Bouton d'action centré en bas de la carte (48px de hauteur)
                    string btnLabel = !ff.IsOwned
                        ? (ff.BuyPrice == 0 ? "DROP BOSS" : $"ACHETER  {ff.BuyPrice:N0} or")
                        : ff.IsEquipped ? "✔ RETIRER" : "⚡ ÉQUIPER";
                    Color btnNorm = !ff.IsOwned
                        ? (ff.BuyPrice == 0 ? new Color(60, 40, 10) : new Color(40, 30, 5))
                        : ff.IsEquipped ? new Color(10, 40, 10) : new Color(50, 20, 80);
                    Color btnHov = !ff.IsOwned
                        ? new Color(100, 70, 10) : ff.IsEquipped ? new Color(20, 80, 20) : new Color(100, 40, 160);
                    _itemBtns.Add(new UIButton(
                        new Rectangle(x + 4, y + ih - 30, iw - 8, 26), btnLabel,
                        () => BuyOrEquipFruit(ff), btnNorm, btnHov
                    ) { TextColor = !ff.IsOwned ? UIHelper.Gold : ff.IsEquipped ? Color.LimeGreen : UIHelper.Purple });
                    i++;
                }
            }
            else if (_tab == 1) // SHOP
            {
                if (_subTab == 0) foreach (var w in Catalog.Weapons)
                    { var ww = w; MakeBtn(w.Name, w.IsOwned, w.BuyPrice, w.Rarity, () => BuyWeapon(ww)); }
                else if (_subTab == 1) foreach (var s in Catalog.Characters)
                    { var ss = s; MakeBtn(s.Name, s.IsOwned, s.BuyPrice, s.Rarity, () => BuySkin(ss)); }
                else foreach (var v in Catalog.Vehicles)
                    { var vv = v; MakeBtn(v.Name, v.IsOwned, v.BuyPrice, v.Rarity, () => BuyVehicle(vv)); }
            }
            else // AMÉLIORATIONS
            {
                if (_subTab == 0) foreach (var w in Catalog.Weapons)
                    { var ww = w; MakeBtn(w.Name, w.IsOwned, 0, w.Rarity, () => UpgradeWeapon(ww)); }
                else if (_subTab == 1) foreach (var c in Catalog.Characters)
                    { var cc = c; MakeBtn(c.Name, c.IsOwned, 0, c.Rarity, () => UpgradeChar(cc)); }
                else if (_subTab == 2) foreach (var v in Catalog.Vehicles)
                    { var vv = v; MakeBtn(v.Name, v.IsOwned, 0, v.Rarity, () => UpgradeVehicle(vv)); }
                else foreach (var ab in Catalog.Abilities)
                    { var aa = ab; MakeBtn(ab.Name, ab.IsOwned, ab.BuyPrice, Rarity.Epic, () => BuyAbility(aa)); }
            }
        }

        void BuyWeapon(WeaponData w)
        {
            if (w.IsOwned) { ShowToast("Déjà possédé!", Color.Yellow); return; }
            if (!PlayerSave.SpendGold(w.BuyPrice)) { ShowToast("Or insuffisant!", Color.Red); return; }
            w.IsOwned = true;
            ShowToast($"✅ {w.Name} acheté!", Color.Green);
            RebuildItems();
        }
        void BuySkin(CharacterData c)
        {
            if (c.IsOwned) { ShowToast("Déjà possédé!", Color.Yellow); return; }
            if (!PlayerSave.SpendGold(c.BuyPrice)) { ShowToast("Or insuffisant!", Color.Red); return; }
            c.IsOwned = true;
            ShowToast($"✅ {c.Name} acheté!", Color.Green);
            RebuildItems();
        }
        void BuyVehicle(VehicleData v)
        {
            if (v.IsOwned) { ShowToast("Déjà possédé!", Color.Yellow); return; }
            if (!PlayerSave.SpendGold(v.BuyPrice)) { ShowToast("Or insuffisant!", Color.Red); return; }
            v.IsOwned = true;
            ShowToast($"✅ {v.Name} acheté!", Color.Green);
            RebuildItems();
        }
        void BuyAbility(AbilityData ab)
        {
            if (ab.IsOwned) { ShowToast("Déjà possédée!", Color.Yellow); return; }
            if (!PlayerSave.SpendGold(ab.BuyPrice)) { ShowToast("Or insuffisant!", Color.Red); return; }
            ab.IsOwned = true;
            ShowToast($"✅ {ab.Name} achetée!", Color.Green);
            RebuildItems();
        }
        void BuyOrEquipFruit(FruitData f)
        {
            if (!f.IsOwned)
            {
                if (f.BuyPrice == 0)
                {
                    ShowToast($"🏆 {f.Name} — Drop Boss uniquement!", Color.Orange);
                    return;
                }
                if (!PlayerSave.SpendGold(f.BuyPrice))
                {
                    ShowToast($"Or insuffisant ! ({f.BuyPrice:N0} requis, tu as {PlayerSave.Gold:N0})", Color.Red);
                    return;
                }
                f.IsOwned = true;
                if (!PlayerSave.OwnedFruits.Contains(f.Name)) PlayerSave.OwnedFruits.Add(f.Name);
                ShowToast($"✅ {f.Icon} {f.Name} acheté !", Color.Green);
            }
            else if (f.IsEquipped)
            {
                PlayerSave.UnequipFruit();
                ShowToast($"{f.Icon} {f.Name} retiré.", Color.Gray);
            }
            else
            {
                PlayerSave.EquipFruit(f.Name);
                ShowToast($"⚡ {f.Icon} {f.Name} équipé !", new Color(255, 160, 0));
            }

            // Rebuild APRES que la liste soit déjà copiée par .ToList() dans Update
            RebuildItems();
        }
        void UpgradeWeapon(WeaponData w)
        {
            if (!w.IsOwned) { ShowToast("Non possédé!", Color.Red); return; }
            if (w.Level >= w.MaxLevel) { ShowToast("Niveau max!", Color.Yellow); return; }
            if (w.Costs.Count > 0)
            {
                foreach (var c in w.Costs)
                    if (!PlayerSave.HasMaterial(c.Material, c.Quantity))
                    { ShowToast($"Matériaux insuffisants! ({c.Material})", Color.Red); return; }
                foreach (var c in w.Costs) PlayerSave.ConsumeMaterial(c.Material, c.Quantity);
            }
            w.Level++;
            ShowToast($"⚡ {w.Name} → Niv. {w.Level}!", UIHelper.Blue);
        }
        void UpgradeChar(CharacterData c)
        {
            if (!c.IsOwned) { ShowToast("Non possédé!", Color.Red); return; }
            if (c.Level >= c.MaxLevel) { ShowToast("Niveau max!", Color.Yellow); return; }
            if (c.UpgradeCosts?.Length > 0)
                foreach (var uc in c.UpgradeCosts)
                    if (!PlayerSave.HasMaterial(uc.Material, uc.Quantity))
                    { ShowToast($"Matériaux insuffisants!", Color.Red); return; }
            c.Level++;
            ShowToast($"⚡ {c.Name} → Niv. {c.Level}!", UIHelper.Blue);
        }
        void UpgradeVehicle(VehicleData v)
        {
            if (!v.IsOwned) { ShowToast("Non possédé!", Color.Red); return; }
            if (v.Level >= v.MaxLevel) { ShowToast("Niveau max!", Color.Yellow); return; }
            v.Level++;
            ShowToast($"⚡ {v.Name} → Niv. {v.Level}!", UIHelper.Blue);
        }

        MouseState _curMs;

        public void Update(GameTime gt)
        {
            _toastTimer -= (float)gt.ElapsedGameTime.TotalSeconds;
            _prevMs = _curMs;
            _curMs  = Mouse.GetState();
            var ms  = _curMs;

            // Déclenche le tuto Fruits la première fois
            if (_tab == 2 && !_fruitTutoSeen && !_fruitTutoShown)
                _fruitTutoShown = true;

            // Si tuto visible : gérer son bouton, bloquer le reste
            if (_fruitTutoShown)
            {
                _fruitTutoBtn.Update(ms);
                return;
            }

            _backBtn.Update(ms);
            foreach (var b in _tabBtns.ToList())  b.Update(ms);
            foreach (var b in _subTabs.ToList())  b.Update(ms);
            foreach (var b in _itemBtns.ToList()) b.Update(ms);
            if (Keyboard.GetState().IsKeyDown(Keys.Escape)) _game.ChangeState(GameState.MainMenu);
        }

        public void Draw(SpriteBatch sb)
        {
            int W = _game.GraphicsDevice.Viewport.Width;
            int H = _game.GraphicsDevice.Viewport.Height;
            sb.Draw(_pixel, new Rectangle(0, 0, W, H), UIHelper.Dark);

            _backBtn.Draw(sb, _pixel, _font, 0.85f);
            UIHelper.DrawCenteredText(sb, _bigFont, "🏪  BOUTIQUE",
                new Rectangle(0, 14, W, 50), UIHelper.Purple, 0.75f);

            // Gold
            sb.DrawString(_font, $"💰 {PlayerSave.Gold:N0}",
                new Vector2(W - 180, 16), UIHelper.Gold);

            // Main tabs
            for (int i = 0; i < _tabBtns.Count; i++)
            {
                _tabBtns[i].NormalColor = i == _tab ? UIHelper.Purple * 0.25f : UIHelper.CardBg;
                _tabBtns[i].Draw(sb, _pixel, _font, 0.78f);
            }

            // Sub tabs
            string[] names = _subNames[_tab];
            for (int i = 0; i < _subTabs.Count; i++)
            {
                _subTabs[i].NormalColor = i == _subTab ? UIHelper.Blue * 0.2f : UIHelper.CardBg;
                _subTabs[i].Draw(sb, _pixel, _font, 0.72f);
            }

            // Items
            int iw = 220, ih = 90, gap = 14, cols = 4;
            int startX = W / 2 - (cols * (iw + gap)) / 2;
            int startY = 158;

            List<(string name, string icon, int price, bool owned, Rarity rar, int level, int maxLvl, bool canUpg)> items = new();

            if (_tab == 2) // FRUITS
            {
                FruitType filter = _subTab switch { 1 => FruitType.Élémentaire, 2 => FruitType.Bête, _ => FruitType.Naturel };
                foreach (var f in Catalog.Fruits)
                    if (f.Type == filter)
                        items.Add((f.IsEquipped ? $"[ÉQUIPÉ] {f.Name}" : f.Name, f.Icon, f.BuyPrice, f.IsOwned, f.Rarity, 0, 0, false));
            }
            else if (_tab == 1)
            {
                if (_subTab == 0) foreach (var w in Catalog.Weapons)
                    items.Add((w.Name, w.Icon, w.BuyPrice, w.IsOwned, w.Rarity, w.Level, w.MaxLevel, false));
                else if (_subTab == 1) foreach (var c in Catalog.Characters)
                    items.Add((c.Name, c.Icon, c.BuyPrice, c.IsOwned, c.Rarity, c.Level, c.MaxLevel, false));
                else foreach (var v in Catalog.Vehicles)
                    items.Add((v.Name, v.Icon, v.BuyPrice, v.IsOwned, v.Rarity, v.Level, v.MaxLevel, false));
            }
            else
            {
                if (_subTab == 0) foreach (var w in Catalog.Weapons)
                    items.Add((w.Name, w.Icon, 0, w.IsOwned, w.Rarity, w.Level, w.MaxLevel, w.IsOwned && w.Level < w.MaxLevel));
                else if (_subTab == 1) foreach (var c in Catalog.Characters)
                    items.Add((c.Name, c.Icon, 0, c.IsOwned, c.Rarity, c.Level, c.MaxLevel, c.IsOwned && c.Level < c.MaxLevel));
                else if (_subTab == 2) foreach (var v in Catalog.Vehicles)
                    items.Add((v.Name, v.Icon, 0, v.IsOwned, v.Rarity, v.Level, v.MaxLevel, v.IsOwned && v.Level < v.MaxLevel));
                else foreach (var ab in Catalog.Abilities)
                    items.Add((ab.Name, ab.Icon, ab.BuyPrice, ab.IsOwned, Rarity.Epic, 1, 1, false));
            }

            for (int i = 0; i < items.Count && i < _itemBtns.Count; i++)
            {
                var item = items[i];
                var btn  = _itemBtns[i];
                Color rarCol = UIHelper.RarityColors[(int)item.rar];

                // Pour les fruits, le btn.Bounds est le BOUTON d'action (bas de carte)
                // On dessine la carte complète au-dessus
                int cardX = btn.Bounds.X - 4;
                int cardY = btn.Bounds.Y - (ih - 30) - 4;  // reconstruit la carte complète
                int cardW = iw, cardH = ih;
                if (_tab == 2)
                {
                    // Fond carte
                    UIHelper.DrawBox(sb, _pixel, new Rectangle(cardX, cardY, cardW, cardH),
                        UIHelper.CardBg, rarCol * 0.6f, 2);
                    // Icône
                    UIHelper.DrawCenteredText(sb, _bigFont, item.icon,
                        new Rectangle(cardX + 4, cardY + 4, 50, 50), Color.White, 0.7f);
                    // Nom
                    sb.DrawString(_font, item.name,
                        new Vector2(cardX + 60, cardY + 8), UIHelper.TextMain);
                    // Rareté
                    sb.DrawString(_font, UIHelper.RarityNames[(int)item.rar],
                        new Vector2(cardX + 60, cardY + 26), rarCol);
                    // Type fruit
                    string fruitType = _subTab == 1 ? "Élémentaire" : _subTab == 2 ? "Bête" : "Naturel";
                    sb.DrawString(_font, fruitType,
                        new Vector2(cardX + 60, cardY + 40), UIHelper.TextDim * 0.7f);
                    // Bouton action (centré, bien visible)
                    btn.Draw(sb, _pixel, _font, 0.75f);
                    continue;
                }

                UIHelper.DrawBox(sb, _pixel, btn.Bounds, UIHelper.CardBg, rarCol * 0.5f, 2);

                // Sprite personnage si disponible (tab 1, sous-onglet personnages/skins)
                if (_tab == 1 && _subTab == 1 || _tab == 0 && _subTab == 1)
                {
                    var charSprite = TravelTour.Core.SpriteLoader.Character(item.name);
                    if (charSprite != null)
                    {
                        Color tint = item.owned ? Color.White : Color.White * 0.55f;
                        sb.Draw(charSprite,
                            new Rectangle(btn.Bounds.X + 2, btn.Bounds.Y + 2, 56, 56),
                            null, tint, 0f, Vector2.Zero, SpriteEffects.None, 0f);
                        goto skipEmoji;
                    }
                }
                // Sprite véhicule ou capacité si disponible
                {
                    var vSprite = TravelTour.Core.SpriteLoader.Vehicle(item.name);
                    var aSprite = TravelTour.Core.SpriteLoader.Ability(item.name);
                    var anySprite = vSprite ?? aSprite;
                    if (anySprite != null)
                        sb.Draw(anySprite, new Rectangle(btn.Bounds.X + 4, btn.Bounds.Y + 4, 50, 50), Color.White);
                    else
                        UIHelper.DrawCenteredText(sb, _bigFont, item.icon,
                            new Rectangle(btn.Bounds.X + 4, btn.Bounds.Y + 4, 50, 50), Color.White, 0.65f);
                }
                skipEmoji:;

                // Name
                sb.DrawString(_font, item.name,
                    new Vector2(btn.Bounds.X + 60, btn.Bounds.Y + 10),
                    UIHelper.TextMain);

                // Rarity
                sb.DrawString(_font, UIHelper.RarityNames[(int)item.rar],
                    new Vector2(btn.Bounds.X + 60, btn.Bounds.Y + 28),
                    rarCol);

                // Level or price
                if (item.canUpg)
                {
                    UIHelper.DrawProgressBar(sb, _pixel,
                        new Rectangle(btn.Bounds.X + 8, btn.Bounds.Y + 68, btn.Bounds.Width - 16, 8),
                        (float)item.level / item.maxLvl, UIHelper.Blue, new Color(20, 22, 40));
                    sb.DrawString(_font, $"Niv. {item.level} / {item.maxLvl}",
                        new Vector2(btn.Bounds.X + 60, btn.Bounds.Y + 50),
                        UIHelper.Blue);
                }
                else if (!item.owned)
                {
                    string priceLabel = item.price == 0 ? "🏆 Drop Boss" : $"{item.price:N0} or";
                    Color  priceColor = item.price == 0 ? Color.Orange : UIHelper.Gold;
                    sb.DrawString(_font, priceLabel,
                        new Vector2(btn.Bounds.X + 60, btn.Bounds.Y + 50),
                        priceColor);
                }
                else if (item.name.StartsWith("[ÉQUIPÉ]"))
                {
                    sb.DrawString(_font, "⚡ Équipé — Clic: retirer",
                        new Vector2(btn.Bounds.X + 60, btn.Bounds.Y + 50),
                        new Color(255, 160, 0));
                }
                else
                {
                    sb.DrawString(_font, "✔ Possédé — Clic: équiper",
                        new Vector2(btn.Bounds.X + 60, btn.Bounds.Y + 50),
                        Color.Green);
                }
            }

            // Onglet vente de matériaux
            if (_tab == 3)
                DrawSellTab(sb, W, H);

            // Materials summary (seulement sur les autres onglets)
            if (_tab != 3)
            {
                int mx = W - 200, my = 160;
                UIHelper.DrawBox(sb, _pixel, new Rectangle(mx, my, 180, 200), UIHelper.Dark2, UIHelper.TextDim * 0.3f, 1);
                sb.DrawString(_font, "MATÉRIAUX", new Vector2(mx + 10, my + 8), UIHelper.TextDim);
                int mi = 0;
                foreach (var kv in PlayerSave.Materials)
                {
                    sb.DrawString(_font, $"{MaterialInfo.GetIcon(kv.Key)} {kv.Value}",
                        new Vector2(mx + 10, my + 28 + mi * 18), UIHelper.TextMain);
                    mi++;
                }
            }

            DrawToast(sb, W, H);

            // ── TUTO FRUITS ──────────────────────────────────────────
            if (_fruitTutoShown)
                DrawFruitTuto(sb, W, H);
        }

        void DrawFruitTuto(SpriteBatch sb, int W, int H)
        {
            // Fond assombri
            sb.Draw(_pixel, new Rectangle(0, 0, W, H), Color.Black * 0.72f);

            int bw = 700, bh = 460;
            int bx = W / 2 - bw / 2, by = H / 2 - bh / 2;
            UIHelper.DrawBox(sb, _pixel, new Rectangle(bx, by, bw, bh),
                new Color(10, 8, 22), new Color(255, 140, 0) * 0.8f, 2);

            // Titre
            UIHelper.DrawCenteredText(sb, _bigFont, "🍎  FRUITS DU DÉMON",
                new Rectangle(bx, by + 14, bw, 40), new Color(255, 140, 0), 0.72f);
            sb.Draw(_pixel, new Rectangle(bx + 20, by + 54, bw - 40, 1), new Color(255, 140, 0) * 0.4f);

            int stepX = bx + 30, stepY = by + 68;

            void Step(string num, Color numCol, string title, string desc, string tip)
            {
                // Numéro dans un cercle
                UIHelper.DrawBox(sb, _pixel, new Rectangle(stepX, stepY, 36, 36),
                    numCol * 0.2f, numCol * 0.7f, 2);
                UIHelper.DrawCenteredText(sb, _bigFont, num,
                    new Rectangle(stepX, stepY, 36, 36), numCol, 0.6f);
                // Titre
                sb.DrawString(_font, title, new Vector2(stepX + 48, stepY + 2), Color.White);
                // Description
                sb.DrawString(_font, desc, new Vector2(stepX + 48, stepY + 20), UIHelper.TextDim);
                // Tip
                if (tip != "")
                {
                    UIHelper.DrawBox(sb, _pixel, new Rectangle(stepX + 48, stepY + 38, bw - 80, 22),
                        new Color(255, 140, 0) * 0.08f, new Color(255, 140, 0) * 0.25f, 1);
                    sb.DrawString(_font, "  " + tip, new Vector2(stepX + 52, stepY + 40),
                        new Color(255, 200, 80));
                }
                stepY += 76;
                sb.Draw(_pixel, new Rectangle(stepX, stepY - 8, bw - 60, 1),
                    UIHelper.TextDim * 0.15f);
            }

            Step("1", new Color(80, 200, 120),
                "Choisis un type de fruit",
                "Clique sur 🌿 Naturel, ⚡ Élémentaire ou 🐉 Bête pour filtrer la liste.",
                "💡 Fruit du Golem 🪨 est déjà possédé — c'est ton fruit de départ !");

            Step("2", new Color(255, 140, 0),
                "Achète un fruit",
                "Clique sur un fruit non possédé pour l'acheter avec ton or.",
                "💡 Exemple : Fruit de la Fleur 🌸 coûte 800 or  ·  Fruit de l'Éclair ⚡ = 9 000 or");

            Step("3", new Color(200, 80, 255),
                "Équipe le fruit",
                "Clique sur un fruit déjà possédé pour l'équiper (un seul à la fois).",
                "💡 Le fruit équipé ajoute ses 4 moves dans le HUD de combat");

            Step("4", new Color(0, 200, 255),
                "Utilise les moves en combat",
                "Chaque fruit a 4 attaques spéciales activées par :",
                "🎮  R = Move 1   ·   T = Move 2   ·   F = Move 3   ·   G = Ultime");

            // Bouton
            _fruitTutoBtn.Draw(sb, _pixel, _font, 0.85f);
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

        // ── Onglet Vente de Matériaux ──────────────────────────────
        void DrawSellTab(SpriteBatch sb, int W, int H)
        {
            UIHelper.DrawCenteredText(sb, _font,
                "Vends tes matériaux contre de l'or",
                new Rectangle(0, 152, W, 22), UIHelper.TextDim, 0.82f);

            var mats = MaterialInfo.Data;
            int cols = 4, cw = 260, ch = 100, gap = 12;
            int startX = W / 2 - (cols * (cw + gap)) / 2;
            int startY = 180;
            int idx = 0;

            foreach (var kv in mats)
            {
                string key   = kv.Key;
                var    info  = kv.Value;
                int qty = PlayerSave.Materials.TryGetValue(key, out int q) ? q : 0;
                int sellPrice = MaterialInfo.SellPrice(key);

                int col = idx % cols, row = idx / cols;
                int x = startX + col * (cw + gap);
                int y = startY + row * (ch + gap);

                Color rc = UIHelper.RarityColors[(int)info.Rarity];

                // Fond carte
                UIHelper.DrawBox(sb, _pixel, new Rectangle(x, y, cw, ch), UIHelper.CardBg, rc * 0.5f, 2);

                // Icône + nom
                UIHelper.DrawCenteredText(sb, _bigFont, info.Icon,
                    new Rectangle(x + 4, y + 8, 52, 52), Color.White, 0.75f);
                sb.DrawString(_font, info.Label,
                    new Vector2(x + 62, y + 10), UIHelper.TextMain);
                sb.DrawString(_font, $"Rareté : {UIHelper.RarityNames[(int)info.Rarity]}",
                    new Vector2(x + 62, y + 28), rc * 0.8f);
                sb.DrawString(_font, $"Stock : {qty}",
                    new Vector2(x + 62, y + 44), qty > 0 ? UIHelper.Gold : UIHelper.TextDim);
                sb.DrawString(_font, $"{sellPrice} 💰 / unité",
                    new Vector2(x + 62, y + 60), UIHelper.Gold * 0.8f);

                // Bouton Vendre 1
                int bx1 = x + 4, by = y + ch - 26, bw1 = (cw - 16) / 2;
                var r1 = new Rectangle(bx1, by, bw1, 22);
                bool hov1 = r1.Contains(_curMs.Position) && qty > 0;
                bool click1 = hov1 && _curMs.LeftButton == ButtonState.Released
                              && _prevMs.LeftButton == ButtonState.Pressed;
                sb.Draw(_pixel, r1, hov1 && qty > 0 ? new Color(40,80,20) : new Color(20,40,10));
                UIHelper.DrawBox(sb, _pixel, r1, Color.Transparent,
                    qty > 0 ? new Color(80,180,40) * 0.7f : UIHelper.TextDim * 0.3f, 1);
                UIHelper.DrawCenteredText(sb, _font, "Vendre 1", r1,
                    qty > 0 ? new Color(120,230,60) : UIHelper.TextDim * 0.4f, 0.72f);
                if (click1 && qty > 0)
                {
                    PlayerSave.ConsumeMaterial(key, 1);
                    PlayerSave.AddGold(sellPrice);
                    ShowToast($"+{sellPrice} 💰  (vendu 1 {info.Icon} {info.Label})", UIHelper.Gold);
                }

                // Bouton Vendre Tout
                int bx2 = bx1 + bw1 + 8;
                var r2 = new Rectangle(bx2, by, bw1, 22);
                bool hov2 = r2.Contains(_curMs.Position) && qty > 0;
                bool click2 = hov2 && _curMs.LeftButton == ButtonState.Released
                              && _prevMs.LeftButton == ButtonState.Pressed;
                sb.Draw(_pixel, r2, hov2 && qty > 0 ? new Color(80,40,10) : new Color(40,20,5));
                UIHelper.DrawBox(sb, _pixel, r2, Color.Transparent,
                    qty > 0 ? UIHelper.Gold * 0.7f : UIHelper.TextDim * 0.3f, 1);
                UIHelper.DrawCenteredText(sb, _font, $"Vendre {qty}", r2,
                    qty > 0 ? UIHelper.Gold : UIHelper.TextDim * 0.4f, 0.72f);
                if (click2 && qty > 0)
                {
                    int total = sellPrice * qty;
                    PlayerSave.ConsumeMaterial(key, qty);
                    PlayerSave.AddGold(total);
                    ShowToast($"+{total} 💰  (vendu {qty}× {info.Icon} {info.Label})", UIHelper.Gold);
                }

                idx++;
            }
        }

        void ShowToast(string m, Color c) { _toast = m; _toastColor = c; _toastTimer = 2.5f; }
        public void Dispose() { }
    }
}
