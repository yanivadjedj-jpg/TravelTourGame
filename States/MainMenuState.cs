using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FontStashSharp;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using TravelTour.Core;
using TravelTour.UI;

namespace TravelTour.States
{
    public class MainMenuState : IGameState
    {
        readonly TravelTourGame _game;
        Texture2D _pixel = null!;
        SpriteFontBase _font = null!, _bigFont = null!;

        List<UIButton> _buttons = new();
        MouseState _prevMs;

        // ── Star field ──────────────────────────────────────────────────────────
        readonly record struct Star(float X, float Y, float R, float Phase, float Speed);
        List<Star> _stars = new();

        // ── Shooting stars ───────────────────────────────────────────────────────
        struct ShootingStar
        {
            public float X, Y, VX, VY, Life, MaxLife;
            public bool Active;
        }
        ShootingStar _shootingStar;
        float _shootingTimer;
        readonly Random _rng = new();

        // ── Interactive particles ────────────────────────────────────────────────
        struct Particle
        {
            public float X, Y, VX, VY, Life, MaxLife, R;
            public Color Col;
        }
        List<Particle> _particles = new();
        float _particleSpawn;

        // ── Title animation ──────────────────────────────────────────────────────
        float _titleTime;
        float _introTimer;          // each char appears every INTRO_CHAR_DT seconds
        int   _introCharsVisible;
        const string TitleText   = "TRAVEL TOUR";
        const float  IntroChrDt  = 0.08f;   // delay per letter

        // ── Nebulae ──────────────────────────────────────────────────────────────
        struct Nebula { public float CX, CY, BaseR; public Color Col; public float Phase; }
        Nebula[] _nebulae = null!;

        // ── Grid perspective ─────────────────────────────────────────────────────
        float _gridScroll;

        // ── Hover border rotation ────────────────────────────────────────────────
        float _borderAngle;

        // ── Card data ────────────────────────────────────────────────────────────
        readonly record struct CardDef(string Icon, string Label, string Desc, GameState State, Color Col);
        CardDef[] _cardDefs = null!;
        bool[] _hovered = null!;

        // ── Toast ────────────────────────────────────────────────────────────────
        string _toast = "";
        float  _toastTimer;
        Color  _toastColor;

        UIButton _resetBtn      = null!;
        UIButton _fullscreenBtn = null!;

        public MainMenuState(TravelTourGame game) => _game = game;

        // ═════════════════════════════════════════════════════════════════════════
        public void Load(Texture2D pixel, SpriteFontBase font, SpriteFontBase bigFont)
        {
            _pixel   = pixel;
            _font    = font;
            _bigFont = bigFont;

            int W = _game.GraphicsDevice.Viewport.Width;
            int H = _game.GraphicsDevice.Viewport.Height;

            // Star field
            for (int i = 0; i < 220; i++)
                _stars.Add(new(
                    _rng.Next(0, W), _rng.Next(0, H),
                    _rng.Next(1, 4),
                    (float)(_rng.NextDouble() * Math.PI * 2),
                    (float)(_rng.NextDouble() * 0.8f + 0.3f)));

            // Nebulae  (blue, violet, gold)
            _nebulae = new Nebula[]
            {
                new() { CX = W * 0.22f, CY = H * 0.38f, BaseR = 320, Col = new Color(40, 60, 220, 18),  Phase = 0f   },
                new() { CX = W * 0.78f, CY = H * 0.55f, BaseR = 280, Col = new Color(140, 30, 200, 18), Phase = 1.3f },
                new() { CX = W * 0.50f, CY = H * 0.80f, BaseR = 240, Col = new Color(220, 160, 20, 14), Phase = 2.7f },
            };

            // Card definitions  (icon, short label, description, state, accent)
            _cardDefs = new CardDef[]
            {
                new("📊", "STATS",        "Grille de stats style Blox Fruits", GameState.Wallet,     new Color(255, 140, 0)),
                new("🎒", "INVENTAIRE",   "Tous tes objets possédés",           GameState.Inventory,   new Color(80, 200, 120)),
                new("🏍️", "CROSSPARK",    "Défi de course & acrobaties",  GameState.Crosspark,  UIHelper.Gold),
                // MY TEAM retiré
                new("🏪", "BOUTIQUE",     "Achète équipement & potions",   GameState.Boutique,   UIHelper.Purple),
                new("🏰", "ENTRAÎNEMENT", "Améliore tes compétences",      GameState.Training,   new Color(240, 80,  96)),
                new("📖", "HISTOIRE",     "Suis la trame narrative",       GameState.Story,      new Color(64, 224, 160)),
                new("🍎", "FRUITS",       "Mange un fruit du démon",       GameState.Fruits,     new Color(255, 80, 180)),
                new("📋", "QUÊTES",       "Missions & récompenses",         GameState.Quest,      new Color(255, 200, 60)),
                new("🏺", "ARTEFACTS",    "Chapeaux & équipements passifs", GameState.Artifact,   new Color(255, 165, 0)),
                new("🌌", "ARRIÈRE PLAN", "Personnalise ton univers",      GameState.Background, new Color(0, 224, 204)),
            };
            _hovered = new bool[_cardDefs.Length];

            BuildButtons(W, H);

            // Shooting star initial
            _shootingTimer = 0f;

            // Title intro
            _introCharsVisible = 0;
            _introTimer = 0f;
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Disposition en losange : lignes avec décalage central
        static readonly int[] DiamondRowCounts = { 2, 3, 3, 2, 2 };

        void BuildButtons(int W, int H)
        {
            int cardW = 240, cardH = 78, gap = 10;
            int rowH = cardH + gap;
            int totalRows = DiamondRowCounts.Length;
            int totalH = totalRows * rowH - gap;
            int startY = H / 2 - totalH / 2 + 50;

            int idx = 0;
            for (int r = 0; r < DiamondRowCounts.Length && idx < _cardDefs.Length; r++)
            {
                int cnt = System.Math.Min(DiamondRowCounts[r], _cardDefs.Length - idx);
                int rowW = cnt * (cardW + gap) - gap;
                int rowX = W / 2 - rowW / 2;
                int y    = startY + r * rowH;

                for (int c = 0; c < cnt && idx < _cardDefs.Length; c++)
                {
                    int x   = rowX + c * (cardW + gap);
                    var d   = _cardDefs[idx];

                    _buttons.Add(new UIButton(
                        new Rectangle(x, y, cardW, cardH),
                        "",
                        () => _game.ChangeState(d.State),
                        UIHelper.CardBg,
                        new Color(d.Col.R / 6, d.Col.G / 6, d.Col.B / 6)
                    ) { TextColor = d.Col });
                    idx++;
                }
            }

            // Bouton Plein écran (coin bas gauche)
            _fullscreenBtn = new UIButton(
                new Rectangle(10, H - 44, 160, 34),
                "⛶ Plein écran [F11]",
                () => (_game as TravelTourGame)?.ToggleFullscreen(),
                new Color(10, 20, 40), new Color(20, 40, 80)
            ) { TextColor = new Color(100, 160, 255) };

            // Bouton Recommencer (coin bas droite)
            _resetBtn = new UIButton(
                new Rectangle(W - 180, H - 44, 170, 34),
                "🔄 Recommencer",
                () =>
                {
                    PlayerSave.Gold = 500;
                    PlayerSave.PlayerLevel = 1;
                    PlayerSave.LevelXp = 0;
                    PlayerSave.StatMelee = PlayerSave.StatDefense = PlayerSave.StatSword =
                    PlayerSave.StatFruit = PlayerSave.StatSpeed = PlayerSave.FreeStatPoints = 0;
                    foreach (var f in Catalog.Fruits) f.IsOwned = f.Name == "Fruit du Golem";
                    foreach (var c in Catalog.Characters) c.IsOwned = c.BuyPrice == 0;
                    PlayerSave.EquippedFruitName = null;
                    SaveSystem.Save();
                    _toast = "Partie réinitialisée !";
                    _toastTimer = 2.5f;
                    _toastColor = Color.OrangeRed;
                },
                new Color(40, 10, 10), new Color(80, 20, 20)
            ) { TextColor = Color.OrangeRed };
        }

        // ═════════════════════════════════════════════════════════════════════════
        public void Update(GameTime gt)
        {
            float dt = (float)gt.ElapsedGameTime.TotalSeconds;
            float t  = (float)gt.TotalGameTime.TotalSeconds;

            _titleTime  += dt;
            _toastTimer -= dt;
            _gridScroll += dt * 24f;
            _borderAngle += dt * 1.2f;

            // Title letter-by-letter intro
            if (_introCharsVisible < TitleText.Length)
            {
                _introTimer += dt;
                while (_introTimer >= IntroChrDt && _introCharsVisible < TitleText.Length)
                {
                    _introTimer -= IntroChrDt;
                    _introCharsVisible++;
                }
            }

            // Mouse
            var ms = Mouse.GetState();
            for (int i = 0; i < _buttons.Count; i++)
            {
                _buttons[i].Update(ms);
                _hovered[i] = _buttons[i].Bounds.Contains(ms.Position);
            }
            _resetBtn?.Update(ms);
            _fullscreenBtn?.Update(ms);
            _prevMs = ms;

            // ── Shooting star ────────────────────────────────────────────────────
            _shootingTimer -= dt;
            if (_shootingTimer <= 0f)
            {
                int W = _game.GraphicsDevice.Viewport.Width;
                int H = _game.GraphicsDevice.Viewport.Height;
                _shootingStar = new ShootingStar
                {
                    X       = (float)(_rng.NextDouble() * W * 0.5),
                    Y       = (float)(_rng.NextDouble() * H * 0.3),
                    VX      = (float)(_rng.NextDouble() * 400 + 300),
                    VY      = (float)(_rng.NextDouble() * 180 + 80),
                    MaxLife = (float)(_rng.NextDouble() * 0.5 + 0.6),
                    Life    = 0f,
                    Active  = true
                };
                _shootingTimer = (float)(_rng.NextDouble() * 2 + 2.5); // 2.5–4.5 s
            }
            if (_shootingStar.Active)
            {
                _shootingStar.X    += _shootingStar.VX * dt;
                _shootingStar.Y    += _shootingStar.VY * dt;
                _shootingStar.Life += dt;
                if (_shootingStar.Life >= _shootingStar.MaxLife)
                    _shootingStar.Active = false;
            }

            // ── Interactive particles ────────────────────────────────────────────
            _particleSpawn -= dt;
            if (_particleSpawn <= 0f)
            {
                _particleSpawn = 0.04f;   // spawn ~25/s
                int W = _game.GraphicsDevice.Viewport.Width;
                int H = _game.GraphicsDevice.Viewport.Height;
                var c = _rng.Next(3) switch
                {
                    0 => UIHelper.Blue   * 0.7f,
                    1 => UIHelper.Purple * 0.7f,
                    _ => UIHelper.Gold   * 0.6f,
                };
                _particles.Add(new Particle
                {
                    X      = (float)(_rng.NextDouble() * W),
                    Y      = (float)(_rng.NextDouble() * H),
                    VX     = (float)((_rng.NextDouble() - 0.5) * 40),
                    VY     = (float)((_rng.NextDouble() - 0.5) * 40),
                    MaxLife= (float)(_rng.NextDouble() * 4 + 3),
                    Life   = 0f,
                    R      = (float)(_rng.NextDouble() * 2.5 + 1),
                    Col    = c,
                });
            }

            // Update + mouse repulsion for particles
            Vector2 mousePos = new Vector2(ms.X, ms.Y);
            for (int i = _particles.Count - 1; i >= 0; i--)
            {
                var p = _particles[i];
                p.Life += dt;
                if (p.Life >= p.MaxLife) { _particles.RemoveAt(i); continue; }

                // Mouse repulsion within 100px
                Vector2 diff = new Vector2(p.X, p.Y) - mousePos;
                float dist   = diff.Length();
                if (dist < 100f && dist > 0.1f)
                {
                    Vector2 push = Vector2.Normalize(diff) * (100f - dist) * 60f * dt;
                    p.VX += push.X;
                    p.VY += push.Y;
                }

                // Dampen
                p.VX *= 1f - dt * 1.5f;
                p.VY *= 1f - dt * 1.5f;

                p.X += p.VX * dt;
                p.Y += p.VY * dt;
                _particles[i] = p;
            }
        }

        // ═════════════════════════════════════════════════════════════════════════
        public void Draw(SpriteBatch sb)
        {
            int W = _game.GraphicsDevice.Viewport.Width;
            int H = _game.GraphicsDevice.Viewport.Height;
            float t = _titleTime;

            // ── Background FC Mobile style ───────────────────────────────────────
            var menuBg = TravelTour.Core.SpriteLoader.BgCity();
            if (menuBg != null)
            {
                // Image de fond centrée avec légère animation
                float bgOff = (float)Math.Sin(t * 0.15) * 20f;
                sb.Draw(menuBg, new Rectangle(-(int)bgOff, -20, W + (int)(bgOff * 2) + 40, H + 40), Color.White * 0.5f);
                // Overlay sombre pour lisibilité
                sb.Draw(_pixel, new Rectangle(0, 0, W, H), new Color(5, 5, 20) * 0.6f);
            }
            else
                sb.Draw(_pixel, new Rectangle(0, 0, W, H), UIHelper.Dark);

            // ── Nebulae (pulsating) ──────────────────────────────────────────────
            foreach (var nb in _nebulae)
            {
                float pulse = (float)(Math.Sin(t * 0.6 + nb.Phase) * 0.18 + 1.0);
                int r = (int)(nb.BaseR * pulse);
                DrawGlow(sb, nb.CX, nb.CY, r, nb.Col);
            }

            // ── Perspective grid ─────────────────────────────────────────────────
            DrawPerspectiveGrid(sb, W, H);

            // ── Star field ───────────────────────────────────────────────────────
            foreach (var s in _stars)
            {
                float a = (float)(Math.Sin(s.Phase + t * s.Speed) * 0.45f + 0.55f);
                var c = new Color(200, 220, 255) * a;
                sb.Draw(_pixel, new Rectangle((int)s.X, (int)s.Y, (int)s.R, (int)s.R), c);
            }

            // ── Shooting star ────────────────────────────────────────────────────
            if (_shootingStar.Active)
                DrawShootingStar(sb);

            // ── Particles ────────────────────────────────────────────────────────
            foreach (var p in _particles)
            {
                float life01 = 1f - p.Life / p.MaxLife;
                float alpha  = life01 < 0.2f ? life01 / 0.2f : life01;
                int   sz     = Math.Max(1, (int)p.R);
                sb.Draw(_pixel,
                    new Rectangle((int)(p.X - sz / 2f), (int)(p.Y - sz / 2f), sz, sz),
                    p.Col * alpha);
            }

            // ── Animated title ───────────────────────────────────────────────────
            DrawAnimatedTitle(sb, W, t);

            // ── Subtitle ─────────────────────────────────────────────────────────
            string sub  = "Action  ·  Adventure  ·  Combat 2D";
            Vector2 sSize = _font.MeasureString(sub);
            float subY  = 68f + _bigFont.MeasureString("A").Y + 10f;
            sb.DrawString(_font, sub, new Vector2(W / 2f - sSize.X / 2f, subY), UIHelper.TextDim);

            // ── Badges ───────────────────────────────────────────────────────────
            float badgeY = subY + 36f;
            DrawBadge(sb, W / 2 - 220, (int)badgeY, "ACTION RPG",    UIHelper.Blue);
            DrawBadge(sb, W / 2 - 60,  (int)badgeY, "COMBAT 2D",     UIHelper.Gold);
            DrawBadge(sb, W / 2 + 100, (int)badgeY, "SOLO LEVELING", UIHelper.Purple);

            // ── HUD Niveau / Or / Rang ──────────────────────────────────────────
            int px = W - 290;

            // Niveau + rang
            string lvlStr = $"Niv.{PlayerSave.PlayerLevel}  Rang {PlayerSave.GetRank()}";
            sb.DrawString(_font, lvlStr, new Vector2(px, 12), UIHelper.Gold);

            // Barre XP
            UIHelper.DrawProgressBar(sb, _pixel,
                new Rectangle(px, 32, 200, 7),
                PlayerSave.LevelProgressPct(),
                UIHelper.Purple, new Color(15, 10, 30));
            sb.DrawString(_font, $"{PlayerSave.LevelXp}/{PlayerSave.XpToNextLevel()} XP  →  Niv.{PlayerSave.PlayerLevel+1}",
                new Vector2(px, 41), UIHelper.TextDim);

            // Or — encadré doré bien visible
            string goldStr = $"💰 {PlayerSave.Gold:N0} or";
            Vector2 gSz = _font.MeasureString(goldStr);
            UIHelper.DrawBox(sb, _pixel,
                new Rectangle(px - 6, 54, (int)gSz.X + 14, 24),
                new Color(30, 22, 5), UIHelper.Gold * 0.6f, 1);
            sb.DrawString(_font, goldStr, new Vector2(px, 58), UIHelper.Gold);

            // ── Event cards ──────────────────────────────────────────────────────
            for (int i = 0; i < _buttons.Count; i++)
                DrawCard(sb, i, t);

            // ── Bouton Recommencer ────────────────────────────────────────────────
            _resetBtn?.Draw(sb, _pixel, _font, 0.78f);
            _fullscreenBtn?.Draw(sb, _pixel, _font, 0.78f);

            // ── Toast ────────────────────────────────────────────────────────────
            if (_toastTimer > 0)
            {
                string to = _toast;
                Vector2 ts = _font.MeasureString(to);
                int tx = (int)(W / 2f - ts.X / 2f - 16);
                int ty = H - 80;
                UIHelper.DrawBox(sb, _pixel, new Rectangle(tx, ty, (int)ts.X + 32, 36),
                    UIHelper.Dark2, UIHelper.Blue, 1);
                sb.DrawString(_font, to, new Vector2(tx + 16, ty + 8), _toastColor);
            }

            // ── Hint ─────────────────────────────────────────────────────────────
            sb.DrawString(_font, "ESC: Quitter", new Vector2(16, H - 30), UIHelper.TextDim * 0.5f);
        }

        // ─────────────────────────────────────────────────────────────────────────
        void DrawAnimatedTitle(SpriteBatch sb, int W, float t)
        {
            // Measure full title to center it
            Vector2 fullSize = _bigFont.MeasureString(TitleText);
            float startX     = W / 2f - fullSize.X / 2f;
            float baseY      = 62f;

            float curX = startX;
            for (int i = 0; i < _introCharsVisible && i < TitleText.Length; i++)
            {
                char ch = TitleText[i];
                if (ch == ' ') { curX += _bigFont.MeasureString(" ").X; continue; }

                // Oscillation: each letter has a unique phase
                float wave  = (float)(Math.Sin(t * 2.2 + i * 0.45) * 5.0);
                // Intro pop-in scale (grows from 0 to 1 quickly)
                float age   = (_introCharsVisible - i) * IntroChrDt;  // how long since appeared
                float scale = Math.Clamp(age / 0.15f, 0f, 1f);

                // Color gradient along letters
                float hue01 = (float)i / TitleText.Length;
                Color col   = Color.Lerp(UIHelper.Blue, UIHelper.Purple, hue01);
                // Add glow pulse
                float glow  = (float)(Math.Sin(t * 1.8 + i * 0.6) * 0.3 + 0.7);
                col = new Color(
                    (int)(col.R * glow),
                    (int)(col.G * glow),
                    (int)(col.B * glow),
                    255);

                string letter = ch.ToString();
                Vector2 lSize = _bigFont.MeasureString(letter);

                if (scale >= 1f)
                {
                    sb.DrawString(_bigFont, letter,
                        new Vector2(curX, baseY + wave), col);
                }
                else
                {
                    // Draw with scaled origin (simulate pop-in via offset)
                    float off = lSize.Y * (1f - scale) * 0.5f;
                    sb.DrawString(_bigFont, letter,
                        new Vector2(curX, baseY + wave + off),
                        col * scale);
                }

                curX += lSize.X;
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        void DrawPerspectiveGrid(SpriteBatch sb, int W, int H)
        {
            Color lineCol = new Color(30, 50, 120, 22);
            int horizonY  = H / 2;
            int gridRows  = 14;
            int gridCols  = 12;

            // Horizontal lines converging toward center
            for (int row = 0; row <= gridRows; row++)
            {
                float t01   = (float)row / gridRows;
                // Scroll effect: shift rows downward slowly
                float scrollT = ((t01 + _gridScroll / (H * 0.6f)) % 1f);
                int y = horizonY + (int)((H - horizonY) * (scrollT * scrollT));
                if (y > H) continue;
                float alpha  = scrollT * 0.6f;
                Color lc = lineCol * alpha;
                sb.Draw(_pixel, new Rectangle(0, y, W, 1), lc);
            }

            // Vertical lines that fan out from vanishing point
            int vanishX = W / 2;
            for (int col = 0; col <= gridCols; col++)
            {
                float t01   = (float)col / gridCols;           // 0..1
                float spread = (t01 - 0.5f) * 2f;             // -1..1
                int bottomX = (int)(W / 2f + spread * W * 0.6f);
                float alpha = 0.4f - Math.Abs(spread) * 0.25f;
                Color lc = lineCol * alpha;
                DrawLine(sb, vanishX, horizonY, bottomX, H, lc);
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        void DrawLine(SpriteBatch sb, int x0, int y0, int x1, int y1, Color col)
        {
            int dx = x1 - x0, dy = y1 - y0;
            int steps = Math.Max(Math.Abs(dx), Math.Abs(dy));
            if (steps == 0) return;
            float sx = (float)dx / steps, sy = (float)dy / steps;
            // Draw every 3rd pixel to keep it thin
            for (int i = 0; i < steps; i += 3)
                sb.Draw(_pixel, new Rectangle((int)(x0 + sx * i), (int)(y0 + sy * i), 1, 1), col);
        }

        // ─────────────────────────────────────────────────────────────────────────
        void DrawShootingStar(SpriteBatch sb)
        {
            float life01  = _shootingStar.Life / _shootingStar.MaxLife;
            float alpha   = life01 < 0.1f ? life01 / 0.1f : (1f - life01);
            int   trailLen = 60;

            for (int i = trailLen; i >= 0; i -= 2)
            {
                float t01  = (float)i / trailLen;
                float px   = _shootingStar.X - _shootingStar.VX * t01 * 0.08f;
                float py   = _shootingStar.Y - _shootingStar.VY * t01 * 0.08f;
                float a    = (1f - t01) * alpha;
                int   sz   = Math.Max(1, (int)((1f - t01) * 3));
                sb.Draw(_pixel,
                    new Rectangle((int)px, (int)py, sz, sz),
                    new Color(220, 240, 255) * a);
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        void DrawCard(SpriteBatch sb, int idx, float t)
        {
            var btn  = _buttons[idx];
            var def  = _cardDefs[idx];
            bool hov = _hovered[idx];

            Rectangle r = btn.Bounds;

            // Background fill FC Mobile style — dégradé sombre + reflet
            Color baseCol = hov
                ? new Color(def.Col.R / 4, def.Col.G / 4, def.Col.B / 4)
                : new Color(14, 16, 30);
            sb.Draw(_pixel, r, baseCol);
            // Reflet haut (simule surface brillante)
            sb.Draw(_pixel, new Rectangle(r.X + 2, r.Y + 2, r.Width - 4, r.Height / 3),
                new Color(255, 255, 255) * (hov ? 0.06f : 0.03f));
            // Barre couleur accent en bas
            sb.Draw(_pixel, new Rectangle(r.X, r.Y + r.Height - 4, r.Width, 4), def.Col * 0.8f);
            // Lueur de fond couleur
            if (hov)
                sb.Draw(_pixel, new Rectangle(r.X - 4, r.Y - 4, r.Width + 8, r.Height + 8),
                    def.Col * 0.08f);

            // ── Static border ──
            Color baseBorder = hov ? def.Col : new Color(50, 55, 90);
            UIHelper.DrawBox(sb, _pixel, r, Color.Transparent, baseBorder, 2);

            // ── Animated rotating border on hover ──
            if (hov)
            {
                int bLen = (r.Width + r.Height) * 2;
                int seg  = bLen / 4;            // length of each glowing segment
                int pos  = (int)(_borderAngle * (bLen / (float)(Math.PI * 2)) * 60f) % bLen;

                for (int s = 0; s < 2; s++)     // 2 symmetrical segments
                {
                    int segStart = (pos + s * (bLen / 2)) % bLen;
                    DrawBorderSegment(sb, r, segStart, seg, def.Col);
                }
            }

            // ── Barre couleur gauche (remplace le badge) ──
            sb.Draw(_pixel, new Rectangle(r.X, r.Y, 3, r.Height), def.Col * 0.8f);

            // ── Image de carte (droite, réduite) ──
            string stateKey = def.State.ToString();
            var cardImg = TravelTour.Core.SpriteLoader.MenuCard(stateKey);
            int imgW = 80, imgH = r.Height - 6;
            int imgX = r.Right - imgW - 3;
            int imgY = r.Y + 3;
            if (cardImg != null)
            {
                sb.Draw(cardImg, new Rectangle(imgX, imgY, imgW, imgH),
                    Color.White * (hov ? 0.9f : 0.65f));
                for (int fx = 0; fx < 16; fx++)
                    sb.Draw(_pixel, new Rectangle(imgX + fx, imgY, 1, imgH),
                        new Color(14, 16, 30) * (1f - fx / 16f));
            }

            // ── Emoji (à droite, sur l'image) ──
            int iconX = imgX + imgW / 2;
            int iconY = r.Y + r.Height / 2 - 8;
            Vector2 iconSz = _bigFont.MeasureString(def.Icon);
            sb.DrawString(_bigFont, def.Icon,
                new Vector2(iconX - iconSz.X / 2f, iconY - iconSz.Y / 2f),
                Color.White * (hov ? 1f : 0.75f));

            // ── Titre (gauche) ──
            sb.DrawString(_font, def.Label,
                new Vector2(r.X + 26, r.Y + 10),
                def.Col);

            // ── Description courte (gauche, une ligne) ──
            string desc = def.Desc;
            float descScale = 0.72f;
            sb.DrawString(_font, desc,
                new Vector2(r.X + 26, r.Y + 28),
                UIHelper.TextDim * (hov ? 0.9f : 0.65f));
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Draws one glowing segment that travels around the border perimeter.
        void DrawBorderSegment(SpriteBatch sb, Rectangle r, int start, int length, Color col)
        {
            // Perimeter order: top, right, bottom (reversed), left (reversed)
            int[] perimX = new int[0]; // we iterate manually
            int W = r.Width, H = r.Height;
            int perim = (W + H) * 2;

            for (int i = 0; i < length; i += 2)
            {
                int pos    = (start + i) % perim;
                float t01  = (float)i / length;
                float a    = (float)(Math.Sin(t01 * Math.PI)) * 0.9f + 0.1f;
                Color c    = col * a;

                int px, py;
                if (pos < W)            { px = r.X + pos;           py = r.Y;              }  // top
                else if (pos < W + H)   { px = r.Right - 1;         py = r.Y + pos - W;    }  // right
                else if (pos < 2*W + H) { px = r.Right - (pos - W - H) - 1; py = r.Bottom - 1; } // bottom
                else                    { px = r.X;                  py = r.Bottom - (pos - 2*W - H) - 1; } // left

                sb.Draw(_pixel, new Rectangle(px, py, 2, 2), c);
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        void DrawCircle(SpriteBatch sb, int cx, int cy, int r, Color fill, Color border)
        {
            // Filled circle approximation with pixel squares
            for (int dy = -r; dy <= r; dy++)
            for (int dx = -r; dx <= r; dx++)
            {
                if (dx * dx + dy * dy <= r * r)
                    sb.Draw(_pixel, new Rectangle(cx + dx, cy + dy, 1, 1), fill);
            }
            // Border ring
            for (int dy = -r; dy <= r; dy++)
            for (int dx = -r; dx <= r; dx++)
            {
                int d2 = dx * dx + dy * dy;
                if (d2 <= r * r && d2 >= (r - 2) * (r - 2))
                    sb.Draw(_pixel, new Rectangle(cx + dx, cy + dy, 1, 1), border);
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        void DrawGlow(SpriteBatch sb, float cx, float cy, int r, Color c)
        {
            for (int i = r; i > 0; i -= 18)
            {
                float a = (float)(r - i) / r * c.A / 255f;
                sb.Draw(_pixel,
                    new Rectangle((int)(cx - i), (int)(cy - i), i * 2, i * 2),
                    new Color(c.R, c.G, c.B) * a);
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        void DrawBadge(SpriteBatch sb, int x, int y, string text, Color col)
        {
            Vector2 sz   = _font.MeasureString(text);
            var     rect = new Rectangle(x, y, (int)sz.X + 24, 28);
            UIHelper.DrawBox(sb, _pixel, rect, col * 0.12f, col, 1);
            sb.DrawString(_font, text, new Vector2(x + 12, y + 4), col);
        }

        // ─────────────────────────────────────────────────────────────────────────
        public void ShowToast(string msg, Color col)
        { _toast = msg; _toastColor = col; _toastTimer = 2.5f; }

        public void Dispose() { }
    }
}
