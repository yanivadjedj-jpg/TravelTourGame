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
    public class CrossparkState : IGameState
    {
        readonly TravelTourGame _game;
        Texture2D _pixel = null!; SpriteFontBase _font = null!, _bigFont = null!;

        UIButton _backBtn = null!;

        // Moto
        Vector2  _motoPos;
        Vector2  _motoVel;
        bool     _grounded;
        float    _rotation;
        float    _rotSpeed;
        bool     _isTricking;
        string   _currentTrick = "";
        float    _trickTimer;

        // Score
        int   _score;
        int   _displayedScore;
        int   _combo;
        float _comboMult = 1f;
        float _sessionTimer = 90f;
        bool  _sessionActive  = false;  // starts false — need to press JOUER
        bool  _waitingToStart = true;
        UIButton _startBtn = null!;

        // Track
        List<Rectangle> _ramps   = new();
        List<Rectangle> _ground  = new();

        // Particles / trail
        record struct Particle(Vector2 Pos, Vector2 Vel, float Life, Color Col, int Size);
        List<Particle> _trail  = new();
        List<Particle> _smoke  = new();   // exhaust smoke
        List<Particle> _stars  = new();   // trick stars

        // Stars rotation angle
        float _starAngle;

        // Foule silhouette (pre-generated)
        record struct CrowdPerson(int X, int H, Color Col);
        List<CrowdPerson> _crowd = new();

        // Toast
        string _toast = ""; float _toastTimer; Color _toastColor;

        // Trick banner (slides in from top)
        string _trickBanner = "";
        float  _trickBannerTimer;
        float  _trickBannerY;          // animated Y

        // Input
        bool _prevJump, _prevLeft, _prevRight;

        // Random (shared, not per-frame)
        readonly Random _rng = new Random(42);

        const float GRAVITY    = 900f;
        const float MAX_SPEED  = 380f;
        const float ACCEL      = 200f;
        const float JUMP_FORCE = 500f;

        public CrossparkState(TravelTourGame game) => _game = game;

        public void Load(Texture2D pixel, SpriteFontBase font, SpriteFontBase bigFont)
        {
            _pixel = pixel; _font = font; _bigFont = bigFont;
            int W = _game.GraphicsDevice.Viewport.Width;
            int H = _game.GraphicsDevice.Viewport.Height;

            _backBtn = new UIButton(new Rectangle(16, 16, 100, 36), "← Menu",
                () => _game.ChangeState(GameState.MainMenu));

            _startBtn = new UIButton(
                new Rectangle(W/2-140, H/2+60, 280, 60),
                "🏍️  JOUER",
                () => { _waitingToStart = false; _sessionActive = true; },
                new Color(60, 30, 0), new Color(100, 50, 0)
            ) { TextColor = UIHelper.Gold };

            _motoPos = new Vector2(200, H - 120);
            _motoVel = Vector2.Zero;

            // ── Parcours style Stunt Bike Extreme ────────────────────────────────
            // Zone 1 : Départ (flat)
            _ground.Add(new Rectangle(0,    H - 60,  500, 60));
            // Rampe de lancement #1
            _ramps.Add(new Rectangle(500,   H - 130, 90,  70));
            // Gap #1 (vide : pas de sol de 590 à 750)
            _ground.Add(new Rectangle(590,  H - 60,  160, 60));
            // Rampe de réception
            _ramps.Add(new Rectangle(750,   H - 110, 80,  50));

            // Zone 2 : Double ramp
            _ground.Add(new Rectangle(830,  H - 60,  200, 60));
            _ramps.Add(new Rectangle(1030,  H - 170, 100, 110));   // grande rampe
            // Big air gap #2 (1130 → 1350)
            _ground.Add(new Rectangle(1350, H - 60,  150, 60));
            _ramps.Add(new Rectangle(1500,  H - 200, 110, 140));   // rampe haute

            // Zone 3 : Half-pipe (simulé par 2 rampes face-à-face)
            _ground.Add(new Rectangle(1610, H - 60,  80,  60));    // fond du pipe
            _ramps.Add(new Rectangle(1690,  H - 220, 120, 160));   // rampe droite pipe

            // Zone 4 : Série de bosses rapides
            _ground.Add(new Rectangle(1810, H - 60,  120, 60));
            _ramps.Add(new Rectangle(1930,  H - 130, 70,  70));
            _ground.Add(new Rectangle(2000, H - 60,  100, 60));
            _ramps.Add(new Rectangle(2100,  H - 150, 80,  90));
            _ground.Add(new Rectangle(2180, H - 60,  100, 60));
            _ramps.Add(new Rectangle(2280,  H - 170, 90,  110));

            // Zone 5 : Méga rampe finale
            _ground.Add(new Rectangle(2370, H - 60,  200, 60));
            _ramps.Add(new Rectangle(2570,  H - 260, 130, 200));   // rampe géante
            // Réception longue + fin
            _ground.Add(new Rectangle(2700, H - 60,  W,   60));

            // Pre-generate crowd
            Color[] crowdColors = {
                new Color(200, 80,  80),
                new Color(80,  200, 120),
                new Color(80,  120, 220),
                new Color(220, 200, 80),
                new Color(180, 80,  200),
                new Color(80,  200, 200),
            };
            for (int x = 30; x < W * 3; x += 24 + _rng.Next(0, 16))
            {
                int ph = 28 + _rng.Next(0, 14);
                _crowd.Add(new CrowdPerson(x, ph, crowdColors[_rng.Next(crowdColors.Length)]));
            }
        }

        public void Update(GameTime gt)
        {
            float dt = (float)gt.ElapsedGameTime.TotalSeconds;
            var kb = Keyboard.GetState();
            var ms = Mouse.GetState();

            _backBtn.Update(ms);
            _toastTimer       -= dt;
            _trickBannerTimer -= dt;

            // Waiting to start — show intro screen
            if (_waitingToStart)
            {
                _startBtn.Update(ms);
                if (kb.IsKeyDown(Keys.Escape)) _game.ChangeState(GameState.MainMenu);
                return;
            }

            // Animate score display
            if (_displayedScore < _score)
                _displayedScore = Math.Min(_displayedScore + Math.Max(1, (_score - _displayedScore) / 8 + 2), _score);

            // Animate banner slide-in
            float bannerTargetY = _trickBannerTimer > 0 ? 60f : -80f;
            _trickBannerY += (bannerTargetY - _trickBannerY) * Math.Min(1f, dt * 10f);

            // Exhaust smoke (always emit while grounded+moving or in air)
            _starAngle += dt * 180f;
            EmitExhaust(dt);

            if (!_sessionActive)
            {
                if (kb.IsKeyDown(Keys.Escape)) _game.ChangeState(GameState.MainMenu);
                UpdateParticles(dt);
                return;
            }

            _sessionTimer -= dt;
            if (_sessionTimer <= 0) EndSession();

            HandleMoto(kb, dt);
            UpdateTrick(kb, dt);
            UpdateParticles(dt);

            if (kb.IsKeyDown(Keys.Escape)) _game.ChangeState(GameState.MainMenu);
        }

        void EmitExhaust(float dt)
        {
            // Exhaust pipe is at rear-bottom of moto
            if (_rng.NextSingle() < 0.35f)
            {
                var smokePos = _motoPos + new Vector2(2, 32);
                _smoke.Add(new Particle(
                    smokePos,
                    new Vector2(_rng.NextSingle() * 10 - 20, -40 - _rng.NextSingle() * 30),
                    0.9f + _rng.NextSingle() * 0.5f,
                    new Color(160, 160, 160),
                    6 + _rng.Next(0, 6)));
            }
        }

        void HandleMoto(KeyboardState kb, float dt)
        {
            int W3 = _game.GraphicsDevice.Viewport.Width * 3;
            int H  = _game.GraphicsDevice.Viewport.Height;

            // Horizontal
            if (kb.IsKeyDown(Keys.D) || kb.IsKeyDown(Keys.Right))
                _motoVel.X = Math.Min(_motoVel.X + ACCEL * dt, MAX_SPEED);
            else if (kb.IsKeyDown(Keys.A) || kb.IsKeyDown(Keys.Left))
                _motoVel.X = Math.Max(_motoVel.X - ACCEL * dt, -MAX_SPEED * 0.5f);
            else
                _motoVel.X *= 0.92f;

            // Gravity
            if (!_grounded) _motoVel.Y += GRAVITY * dt;

            // Jump
            bool jumpNow = kb.IsKeyDown(Keys.W) || kb.IsKeyDown(Keys.Space) || kb.IsKeyDown(Keys.Up);
            if (jumpNow && !_prevJump && _grounded)
            {
                _motoVel.Y = -JUMP_FORCE;
                _grounded  = false;
            }
            _prevJump = jumpNow;

            _motoPos += _motoVel * dt;

            // Clamp horizontal
            _motoPos.X = Math.Clamp(_motoPos.X, 0, W3 - 80);

            // Ground / ramp collisions
            _grounded = false;
            var motoRect = new Rectangle((int)_motoPos.X, (int)_motoPos.Y, 80, 40);

            foreach (var g in _ground)
            {
                if (motoRect.Intersects(g) && _motoVel.Y >= 0)
                {
                    _motoPos.Y = g.Y - 40;
                    _motoVel.Y = 0;
                    _grounded  = true;
                    if (_isTricking) LandTrick();
                }
            }
            foreach (var r in _ramps)
            {
                if (motoRect.Intersects(r) && _motoVel.Y >= 0)
                {
                    _motoPos.Y = r.Y - 40;
                    _motoVel.Y = -JUMP_FORCE * 0.7f;
                    _grounded  = false;
                }
            }

            // World floor
            if (_motoPos.Y + 40 >= H - 60)
            {
                _motoPos.Y = H - 100;
                _motoVel.Y = 0;
                _grounded  = true;
                if (_isTricking) LandTrick();
            }

            // Wheel spin / dust particles when grounded
            if (_grounded && Math.Abs(_motoVel.X) > 50)
            {
                _trail.Add(new Particle(
                    _motoPos + new Vector2(10, 36),
                    new Vector2(-_motoVel.X * 0.1f + _rng.NextSingle() * 20 - 10, -20),
                    0.4f,
                    new Color(100, 80, 60),
                    8));
            }

            // Trick trail particles while in the air tricking
            if (_isTricking)
            {
                Color trailCol = _currentTrick switch
                {
                    "Backflip"    => new Color(40,  120, 255),
                    "Superman"    => new Color(220, 180, 0),
                    "Coffin Flip" => new Color(220, 40,  40),
                    _             => new Color(180, 180, 180),
                };
                _trail.Add(new Particle(
                    _motoPos + new Vector2(40, 20) + new Vector2(_rng.NextSingle() * 20 - 10, _rng.NextSingle() * 10 - 5),
                    new Vector2(-_motoVel.X * 0.3f + _rng.NextSingle() * 30 - 15, _rng.NextSingle() * 20 - 10),
                    0.55f,
                    trailCol,
                    10 + _rng.Next(0, 8)));

                // Spawn orbiting stars
                if (_rng.NextSingle() < 0.3f)
                {
                    float ang = (float)(_rng.NextSingle() * Math.PI * 2);
                    _stars.Add(new Particle(
                        _motoPos + new Vector2(40, 10),
                        new Vector2((float)Math.Cos(ang) * 60, (float)Math.Sin(ang) * 60),
                        0.7f + _rng.NextSingle() * 0.3f,
                        trailCol,
                        6 + _rng.Next(0, 5)));
                }
            }
        }

        void UpdateTrick(KeyboardState kb, float dt)
        {
            if (_grounded) { _rotation = 0; return; }

            // NOUVEAUX CONTRÔLES NATURELS :
            // S/↓       = Backflip         (rotation arrière — facile)
            // ← (en vol)= No Hands         (pencher en arrière — naturel)
            // ↑/W (vol) = Superman          (s'allonger — intuitif)
            // Space (vol)= Coffin Flip      (pouce sur espace — accessible)

            if (kb.IsKeyDown(Keys.S) || kb.IsKeyDown(Keys.Down))
            {
                _isTricking   = true;
                _currentTrick = "Backflip";
                _rotSpeed     = 350f;
            }
            else if (kb.IsKeyDown(Keys.Space) && _motoVel.Y < 0)
            {
                // Space en l'air = Coffin Flip (pouce accessible)
                if (!_isTricking) { _isTricking = true; _currentTrick = "Coffin Flip"; _rotSpeed = 500f; }
            }
            else if (kb.IsKeyDown(Keys.A) || kb.IsKeyDown(Keys.Left))
            {
                // Gauche en l'air = No Hands (déjà sous le pouce/index)
                if (!_isTricking) { _isTricking = true; _currentTrick = "No Hands"; _rotSpeed = 0f; }
            }
            else if ((kb.IsKeyDown(Keys.W) || kb.IsKeyDown(Keys.Up)) && _motoVel.Y < -50)
            {
                // Haut en l'air = Superman (intuitif : s'envoler)
                if (!_isTricking) { _isTricking = true; _currentTrick = "Superman"; _trickTimer = 0; }
            }

            if (_isTricking)
            {
                _trickTimer += dt;
                _rotation   += _rotSpeed * dt;
            }
        }

        void LandTrick()
        {
            if (!_isTricking) return;

            bool clean = Math.Abs(_rotation % 360 - 0) < 40 ||
                         Math.Abs(_rotation % 360 - 360) < 40 ||
                         _currentTrick == "No Hands" || _currentTrick == "Superman";

            if (clean)
            {
                _combo++;
                _comboMult = 1f + _combo * 0.3f;
                var trick = Catalog.Tricks.Find(t => t.Name == _currentTrick) ?? new TrickData { Name = _currentTrick, BaseScore = 200 };
                int pts = (int)(trick.BaseScore * _comboMult);
                _score += pts;
                _trickBanner = $"{_currentTrick}  +{pts} pts";
                _trickBannerTimer = 1.8f;
                _trickBannerY = -80f; // reset for slide animation
                if (_combo > 1) ShowToast($"x{_combo} COMBO!", UIHelper.Gold);
            }
            else
            {
                _combo = 0; _comboMult = 1f;
                ShowToast("CRASH!", Color.Red);
            }

            _isTricking   = false;
            _currentTrick = "";
            _rotation     = 0;
            _rotSpeed     = 0;
        }

        void UpdateParticles(float dt)
        {
            _trail.RemoveAll(p => p.Life <= 0);
            for (int i = 0; i < _trail.Count; i++)
            {
                var p = _trail[i];
                _trail[i] = p with { Pos = p.Pos + p.Vel * dt, Life = p.Life - dt };
            }

            _smoke.RemoveAll(p => p.Life <= 0);
            for (int i = 0; i < _smoke.Count; i++)
            {
                var p = _smoke[i];
                // smoke rises and expands
                _smoke[i] = p with {
                    Pos  = p.Pos + p.Vel * dt,
                    Vel  = p.Vel with { Y = p.Vel.Y * 0.98f },
                    Life = p.Life - dt,
                    Size = p.Size + (int)(dt * 8)
                };
            }

            _stars.RemoveAll(p => p.Life <= 0);
            for (int i = 0; i < _stars.Count; i++)
            {
                var p = _stars[i];
                // stars spiral outward
                float ang = _starAngle * MathHelper.Pi / 180f + i * 0.7f;
                float radius = (1f - p.Life / 1f) * 45f;
                _stars[i] = p with {
                    Pos  = _motoPos + new Vector2(40, 10) + new Vector2((float)Math.Cos(ang + i) * radius, (float)Math.Sin(ang + i) * radius),
                    Life = p.Life - dt
                };
            }
        }

        void EndSession()
        {
            _sessionActive = false;
            int gold = _score / 10;
            PlayerSave.AddGold(gold);
            ShowToast($"Session terminée! Score: {_score:N0}  +{gold}", UIHelper.Gold);
        }

        public void Draw(SpriteBatch sb)
        {
            int W = _game.GraphicsDevice.Viewport.Width;
            int H = _game.GraphicsDevice.Viewport.Height;

            // ── INTRO SCREEN ─────────────────────────────────────────────────────
            if (_waitingToStart)
            {
                sb.Draw(_pixel, new Rectangle(0,0,W,H), UIHelper.Dark);
                for (int i=0;i<8;i++)
                {
                    float off=((float)_starAngle*0.5f+i*110)%W;
                    sb.Draw(_pixel,new Rectangle((int)off,0,1,H),UIHelper.Gold*0.04f);
                }
                UIHelper.DrawCenteredText(sb,_bigFont,"🏍️  CROSSPARK",
                    new Rectangle(0,H/2-200,W,60),UIHelper.Gold,0.85f);
                UIHelper.DrawCenteredText(sb,_font,"Fais des figures pour scorer un maximum de points !",
                    new Rectangle(0,H/2-130,W,30),UIHelper.TextDim,0.88f);

                // Controls table
                int tx=W/2-200, ty=H/2-80;
                UIHelper.DrawBox(sb,_pixel,new Rectangle(tx,ty,400,160),UIHelper.Dark2,UIHelper.Gold*0.4f,1);
                string[] ctrl={
                    "D / →              Accélérer",
                    "A / ←              Freiner / reculer",
                    "W / ↑ / Espace     Sauter sur une rampe",
                    "S / ↓   (en vol)   Backflip  (+350 pts)",
                    "Espace  (en vol)   Coffin Flip  (+800 pts)",
                    "A / ←   (en vol)   No Hands  (+200 pts)",
                    "W / ↑   (en vol)   Superman  (+500 pts)",
                    "ÉCHAP              Menu",
                };
                for(int i=0;i<ctrl.Length;i++)
                    sb.DrawString(_font,ctrl[i],new Vector2(tx+16,ty+10+i*23),UIHelper.TextMain);

                _startBtn.Draw(sb,_pixel,_bigFont,0.75f);
                _backBtn.Draw(sb,_pixel,_font,0.85f);
                return;
            }
            // W and H already declared in intro block above — use the existing ones below

            // Sky gradient (dark blue top, lighter bottom)
            sb.Draw(_pixel, new Rectangle(0, 0, W, H / 2),      new Color(8, 10, 25));
            sb.Draw(_pixel, new Rectangle(0, H / 2, W, H / 2),  new Color(12, 16, 40));

            // Camera offset
            float camX = Math.Clamp(_motoPos.X - W / 3f, 0, W * 3 - W);

            sb.End();
            sb.Begin(transformMatrix: Matrix.CreateTranslation(-(int)camX, 0, 0));

            // --- Far background: buildings silhouette ---
            for (int x = 0; x < W * 3; x += 120)
            {
                int bh = 80 + (x / 100 % 3) * 40;
                // building body
                sb.Draw(_pixel, new Rectangle(x + 2, H - 60 - bh, 96, bh), new Color(15, 18, 40));
                // window grid (small bright squares)
                for (int wy = 0; wy < bh - 10; wy += 14)
                for (int wx = 0; wx < 80; wx += 14)
                {
                    bool lit = ((x / 120 + wx / 14 + wy / 14) % 3) != 0;
                    if (lit)
                        sb.Draw(_pixel, new Rectangle(x + 6 + wx, H - 56 - bh + wy, 8, 8), new Color(60, 80, 120) * 0.6f);
                }
            }

            // --- Crowd silhouettes (behind ground) ---
            foreach (var person in _crowd)
            {
                int groundY = H - 60;
                // body
                sb.Draw(_pixel, new Rectangle(person.X,     groundY - person.H,      12, person.H),      person.Col * 0.55f);
                // head
                sb.Draw(_pixel, new Rectangle(person.X + 2, groundY - person.H - 10, 8,  8),             person.Col * 0.6f);
            }

            // --- Ground with dashed center line ---
            foreach (var g in _ground)
            {
                UIHelper.DrawBox(sb, _pixel, g, new Color(25, 30, 55), UIHelper.Blue * 0.4f, 2);
                // dashed white center line
                int lineY = g.Y + g.Height / 2 - 2;
                for (int dx = g.X; dx < g.Right; dx += 36)
                    sb.Draw(_pixel, new Rectangle(dx, lineY, 20, 3), Color.White * 0.35f);
            }

            // --- Ramps with directional arrows and flags ---
            foreach (var r in _ramps)
            {
                UIHelper.DrawBox(sb, _pixel, r, new Color(30, 40, 70), UIHelper.Gold * 0.5f, 2);

                // Arrow painted on the ramp face (simple right-facing arrow using rectangles)
                int ax = r.X + r.Width / 2 - 18;
                int ay = r.Y + r.Height / 2 - 4;
                sb.Draw(_pixel, new Rectangle(ax,      ay,     28, 7),  UIHelper.Gold * 0.7f); // shaft
                sb.Draw(_pixel, new Rectangle(ax + 22, ay - 5, 12, 17), UIHelper.Gold * 0.7f); // head

                // Flag pole at top
                int flagX = r.X + r.Width / 2;
                int flagY = r.Y - 24;
                sb.Draw(_pixel, new Rectangle(flagX,     flagY, 2,  24), UIHelper.TextDim * 0.8f); // pole
                // flag rectangle: alternate color per ramp index
                Color flagColor = UIHelper.Gold;
                if (r.Width == 120) flagColor = new Color(220, 60, 60);
                if (r.Height == 140) flagColor = new Color(60, 200, 120);
                sb.Draw(_pixel, new Rectangle(flagX + 2, flagY, 16, 10), flagColor * 0.9f);
            }

            // --- Smoke particles ---
            foreach (var p in _smoke)
            {
                float a = (p.Life / 1.4f) * 0.45f;
                sb.Draw(_pixel, new Rectangle((int)p.Pos.X - p.Size/2, (int)p.Pos.Y - p.Size/2, p.Size, p.Size),
                    p.Col * a);
            }

            // --- Tire mark / trick trail particles ---
            foreach (var p in _trail)
            {
                float a = p.Life / 0.55f;
                sb.Draw(_pixel, new Rectangle((int)p.Pos.X, (int)p.Pos.Y, p.Size, p.Size), p.Col * a);
            }

            // --- Star particles ---
            foreach (var p in _stars)
            {
                float a = p.Life * 1.2f;
                int s = p.Size;
                int px = (int)p.Pos.X;
                int py = (int)p.Pos.Y;
                // draw a simple 4-point star with two crossed rectangles
                sb.Draw(_pixel, new Rectangle(px - s/2, py - 1,   s,     3), p.Col * a);
                sb.Draw(_pixel, new Rectangle(px - 1,   py - s/2, 3,     s), p.Col * a);
            }

            // --- Moto ---
            float rot = _isTricking ? MathHelper.ToRadians(_rotation) : 0;
            Vector2 motoCenter = _motoPos + new Vector2(40, 20);

            // Draw all moto parts with rotation applied manually via transform
            sb.End();
            sb.Begin(transformMatrix:
                Matrix.CreateTranslation(-(int)camX, 0, 0) *
                Matrix.CreateTranslation(-motoCenter.X, -motoCenter.Y, 0) *
                Matrix.CreateRotationZ(rot) *
                Matrix.CreateTranslation(motoCenter.X, motoCenter.Y, 0));

            DrawMoto(sb);

            sb.End();
            sb.Begin(transformMatrix: Matrix.CreateTranslation(-(int)camX, 0, 0));

            // (restore world transform for any remaining world-space draws)
            sb.End();
            sb.Begin();

            // ================================================================
            // HUD (screen space, no camera)
            // ================================================================
            DrawHUD(sb, W, H);

            DrawToast(sb, W, H);
        }

        void DrawMoto(SpriteBatch sb)
        {
            int mx = (int)_motoPos.X;
            int my = (int)_motoPos.Y;

            // ── Sprite moto ────────────────────────────────
            var motoSprite = TravelTour.Core.SpriteLoader.Moto();
            if (motoSprite != null)
            {
                bool goingLeft = _motoVel.X < -10f;
                var flip = goingLeft ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
                sb.Draw(motoSprite, new Rectangle(mx - 12, my - 40, 104, 80), null, Color.White, 0f, Vector2.Zero, flip, 0f);
                return;
            }
            // Fallback rectangles ci-dessous ──────────────

            // --- Wheels: outer (dark) + inner rim (lighter) ---
            // Rear wheel
            sb.Draw(_pixel, new Rectangle(mx + 3,  my + 26, 24, 24), new Color(30, 30, 50));
            sb.Draw(_pixel, new Rectangle(mx + 7,  my + 30, 16, 16), new Color(70, 70, 100));
            sb.Draw(_pixel, new Rectangle(mx + 12, my + 35, 6,  6),  new Color(140, 140, 180));  // hub
            // Front wheel
            sb.Draw(_pixel, new Rectangle(mx + 52, my + 26, 24, 24), new Color(30, 30, 50));
            sb.Draw(_pixel, new Rectangle(mx + 56, my + 30, 16, 16), new Color(70, 70, 100));
            sb.Draw(_pixel, new Rectangle(mx + 61, my + 35, 6,  6),  new Color(140, 140, 180));

            // --- Main body / frame (dark) ---
            sb.Draw(_pixel, new Rectangle(mx + 12, my + 10, 54, 20), new Color(25, 35, 70));

            // --- Fairing / carénage avant (triangle approximated with two overlapping rects) ---
            sb.Draw(_pixel, new Rectangle(mx + 54, my + 6,  22, 14), new Color(60, 120, 220));  // upper fairing
            sb.Draw(_pixel, new Rectangle(mx + 60, my + 14, 16, 12), new Color(40, 90,  180));  // lower fairing
            // headlight glint
            sb.Draw(_pixel, new Rectangle(mx + 70, my + 8,  8,  4),  new Color(200, 230, 255) * 0.9f);

            // --- Tank / réservoir (rectangle légèrement plus clair) ---
            sb.Draw(_pixel, new Rectangle(mx + 22, my + 4,  32, 14), new Color(70, 130, 240));
            // tank highlight (simulate rounded top)
            sb.Draw(_pixel, new Rectangle(mx + 24, my + 4,  28, 4),  new Color(100, 160, 255) * 0.6f);

            // --- Exhaust pipe (bottom right) ---
            sb.Draw(_pixel, new Rectangle(mx + 10, my + 28, 20, 5), new Color(80, 80, 80));

            // --- Handlebar / guidon (thin horizontal bar near front) ---
            sb.Draw(_pixel, new Rectangle(mx + 50, my + 2, 18, 4), new Color(160, 160, 180));

            // --- Rider / pilote ---
            // Body (leaning forward)
            sb.Draw(_pixel, new Rectangle(mx + 28, my - 22, 20, 20), new Color(40, 60, 120));   // jacket
            // Arms reaching forward (2 thin rectangles)
            sb.Draw(_pixel, new Rectangle(mx + 42, my - 12, 18, 5),  new Color(40, 60, 120));   // left arm
            sb.Draw(_pixel, new Rectangle(mx + 42, my - 7,  18, 5),  new Color(40, 60, 120));   // right arm
            // Hands on handlebar
            sb.Draw(_pixel, new Rectangle(mx + 58, my - 13, 6, 11), new Color(200, 160, 100));

            // Head + helmet
            sb.Draw(_pixel, new Rectangle(mx + 30, my - 36, 16, 16), new Color(30, 30, 30));    // helmet outer
            sb.Draw(_pixel, new Rectangle(mx + 32, my - 34, 12, 8),  new Color(60, 120, 220));  // helmet color band
            sb.Draw(_pixel, new Rectangle(mx + 32, my - 28, 12, 5),  new Color(180, 210, 255) * 0.5f); // visor
        }

        void DrawHUD(SpriteBatch sb, int W, int H)
        {
            // --- Chrono frame ---
            int timerSeconds = (int)Math.Max(0, _sessionTimer);
            bool urgentTime  = timerSeconds <= 10 && _sessionActive;
            Color chronoColor = urgentTime ? Color.Red : UIHelper.Gold;

            // Outer frame
            UIHelper.DrawBox(sb, _pixel,
                new Rectangle(W / 2 - 66, 8, 132, 44),
                UIHelper.Dark2, chronoColor * 0.8f, 2);
            // Inner fill bar showing time remaining
            float timePct = Math.Clamp(_sessionTimer / 90f, 0f, 1f);
            sb.Draw(_pixel, new Rectangle(W / 2 - 64, 10, (int)(128 * timePct), 40),
                chronoColor * 0.12f);

            string timeStr = $"{timerSeconds / 60:D2}:{timerSeconds % 60:D2}";
            Vector2 timeSize = _bigFont.MeasureString(timeStr);
            sb.DrawString(_bigFont, timeStr,
                new Vector2(W / 2f - timeSize.X / 2f, 14), chronoColor);

            // --- Or du joueur (coin haut gauche) ---
            UIHelper.DrawBox(sb, _pixel,
                new Rectangle(10, 8, 180, 36),
                UIHelper.Dark2, UIHelper.Gold * 0.7f, 2);
            sb.DrawString(_font, $"💰 {PlayerSave.Gold:N0} or",
                new Vector2(18, 16), UIHelper.Gold);

            // --- Or gagnable cette session ---
            int goldPreview = _score / 10;
            if (goldPreview > 0)
                sb.DrawString(_font, $"+{goldPreview} en cours",
                    new Vector2(18, 34), UIHelper.Gold * 0.6f);

            // --- Score frame ---
            UIHelper.DrawBox(sb, _pixel,
                new Rectangle(W - 210, 8, 200, 42),
                UIHelper.Dark2, UIHelper.Blue * 0.7f, 2);
            string scoreLabel = "SCORE";
            sb.DrawString(_font, scoreLabel, new Vector2(W - 205, 12), UIHelper.Blue * 0.6f);
            string scoreStr = $"{_displayedScore:N0}";
            Vector2 scoreSize = _font.MeasureString(scoreStr);
            sb.DrawString(_font, scoreStr,
                new Vector2(W - 14 - scoreSize.X, 26), UIHelper.TextMain);

            // --- Combo display ---
            if (_combo > 1)
            {
                UIHelper.DrawBox(sb, _pixel,
                    new Rectangle(W - 210, 56, 200, 34),
                    UIHelper.Dark2, UIHelper.Gold * 0.5f, 1);
                sb.DrawString(_font,
                    $"x{_combo} COMBO  x{_comboMult:F1}",
                    new Vector2(W - 205, 62), UIHelper.Gold);
            }

            // --- Trick banner (slides in from top) ---
            if (_trickBannerTimer > 0 || _trickBannerY > -70f)
            {
                float bannerAlpha = _trickBannerTimer > 0.4f ? 1f :
                                    _trickBannerTimer > 0f   ? _trickBannerTimer / 0.4f : 0f;
                if (bannerAlpha > 0.01f)
                {
                    int bannerW = 420;
                    int bannerX = (W - bannerW) / 2;
                    int bannerY = (int)_trickBannerY;

                    // Outer glow
                    UIHelper.DrawBox(sb, _pixel,
                        new Rectangle(bannerX - 4, bannerY - 4, bannerW + 8, 52),
                        UIHelper.Gold * 0.15f * bannerAlpha, UIHelper.Gold * bannerAlpha, 2);
                    // Inner panel
                    UIHelper.DrawBox(sb, _pixel,
                        new Rectangle(bannerX, bannerY, bannerW, 44),
                        UIHelper.Dark2 * 0.95f * bannerAlpha, null);

                    UIHelper.DrawCenteredText(sb, _bigFont, _trickBanner,
                        new Rectangle(bannerX, bannerY, bannerW, 44),
                        UIHelper.Gold * bannerAlpha, 0.65f);
                }
            }

            // --- Controls hint ---
            sb.DrawString(_font, "D=Accel  W/Espace=Saut  |  En vol :  S=Backflip  Espace=CoffinFlip  A=NoHands  W=Superman",
                new Vector2(16, H - 28), UIHelper.TextDim * 0.6f);

            // --- Session end overlay ---
            if (!_sessionActive)
            {
                sb.Draw(_pixel, new Rectangle(0, 0, W, H), Color.Black * 0.72f);

                UIHelper.DrawBox(sb, _pixel,
                    new Rectangle(W / 2 - 240, H / 2 - 90, 480, 180),
                    UIHelper.Dark2, UIHelper.Gold * 0.9f, 3);

                UIHelper.DrawCenteredText(sb, _bigFont, "SESSION TERMINEE!",
                    new Rectangle(0, H / 2 - 80, W, 60), UIHelper.Gold, 0.8f);
                UIHelper.DrawCenteredText(sb, _font,
                    $"Score final : {_score:N0}",
                    new Rectangle(0, H / 2 - 10, W, 40), UIHelper.TextMain, 0.9f);
                UIHelper.DrawCenteredText(sb, _font, "ECHAP pour revenir",
                    new Rectangle(0, H / 2 + 40, W, 30), UIHelper.TextDim, 0.8f);
            }

            // Back button drawn last (on top)
            _backBtn.Draw(sb, _pixel, _font, 0.85f);
        }

        void DrawToast(SpriteBatch sb, int W, int H)
        {
            if (_toastTimer <= 0) return;
            float alpha = _toastTimer < 0.5f ? _toastTimer / 0.5f : 1f;
            Vector2 ts = _font.MeasureString(_toast);
            UIHelper.DrawBox(sb, _pixel,
                new Rectangle((int)(W / 2f - ts.X / 2f - 16), H - 64, (int)ts.X + 32, 36),
                UIHelper.Dark2 * alpha, _toastColor * alpha, 1);
            sb.DrawString(_font, _toast, new Vector2(W / 2f - ts.X / 2f, H - 56), _toastColor * alpha);
        }

        void ShowToast(string m, Color c) { _toast = m; _toastColor = c; _toastTimer = 2.5f; }
        public void Dispose() { }
    }
}
