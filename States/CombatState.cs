using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FontStashSharp;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using TravelTour.Core;
using TravelTour.Entities;
using TravelTour.UI;

namespace TravelTour.States
{
    public class CombatState : IGameState
    {
        readonly TravelTourGame _game;
        Texture2D  _pixel = null!;
        SpriteFontBase _font = null!, _bigFont = null!;

        // World
        Rectangle _world;
        List<Rectangle> _platforms = new();

        // Entities
        Player _player = new();
        List<Enemy> _enemies = new();
        DungeonData _dungeon = null!;

        // State
        int   _wave, _totalWaves = 3;
        bool  _bossWave, _victory, _defeat;
        float _waveDelay;
        int   _enemiesLeft;

        // Tutoriel combat
        bool  _tutoDismissed = false;
        float _tutoTimer     = 0f;

        // Sélection de classe (donjon spécial)
        bool _classSelectShown = false;
        int  _classSelectHover = -1;
        List<UIButton> _classSelectBtns = new();

        // Particles
        record struct Particle(Vector2 Pos, Vector2 Vel, Color Col, float Life, float MaxLife, float Size);
        List<Particle> _particles = new();

        // Floating damage numbers
        record struct DamageNumber(Vector2 Pos, Vector2 Vel, string Text, Color Col, float Life, float MaxLife, float Scale);
        List<DamageNumber> _damageNumbers = new();

        // Toast
        string _toast = ""; float _toastTimer; Color _toastColor;

        // UI buttons
        UIButton _backBtn = null!;

        // Scroll
        float _scrollX;

        // Boss flash effect
        float _bossFlashTimer;
        float _bossPulseTimer;

        // ── GROS PLAN capacité spéciale ───────────────────────────
        float _zoomTimer  = 0f;
        float _zoomTotal  = 0f;   // durée totale de l'effet
        float _zoomPeak   = 1f;   // facteur de zoom max
        float _zoomFlash  = 0f;   // flash blanc alpha
        string _zoomLabel = "";   // nom de la capacité affichée
        Color  _zoomColor = Color.White;
        bool   _zoomActive => _zoomTimer > 0f;
        bool  _bossActive;


        public CombatState(TravelTourGame game) => _game = game;

        public void SetDungeon(DungeonData d) => _dungeon = d;

        public void Load(Texture2D pixel, SpriteFontBase font, SpriteFontBase bigFont)
        {
            _pixel = pixel; _font = font; _bigFont = bigFont;
            int W = _game.GraphicsDevice.Viewport.Width;
            int H = _game.GraphicsDevice.Viewport.Height;

            _world = new Rectangle(0, 0, W * 3, H);
            _backBtn = new UIButton(new Rectangle(16, 16, 100, 36), "← Menu",
                () => _game.ChangeState(GameState.MainMenu));

            // Platforms
            _platforms.Add(new Rectangle(0, H - 60, W * 3, 60));           // ground
            _platforms.Add(new Rectangle(300, H - 170, 200, 20));
            _platforms.Add(new Rectangle(700, H - 260, 180, 20));
            _platforms.Add(new Rectangle(1100, H - 180, 220, 20));
            _platforms.Add(new Rectangle(1600, H - 270, 200, 20));
            _platforms.Add(new Rectangle(2000, H - 160, 240, 20));

            // Player
            var leader = TeamManager.GetLeader();
            if (leader != null) _player.Init(leader);
            else _player.Init(Catalog.Characters[0]);
            _player.Position = new Vector2(120, H - 200);
            _player.ShowToast     = (m, c) => ShowToast(m, c);
            _player.OnAbilityUsed = (name, col) => TriggerAbilityZoom(name, col, 1.9f, 0.75f);

            // Add owned abilities
            foreach (var ab in Catalog.Abilities)
                if (ab.IsOwned) _player.Abilities.Add(ab);

            if (_dungeon != null && _dungeon.BossGauntlet)
            {
                _bossWave    = true;
                _totalWaves  = 4;
                ShowToast("⚔ GAUNTLET — 4 BOSS À VAINCRE !", Color.Red);
                SpawnBoss();
            }
            else SpawnWave();
        }

        static readonly string[][] _enemySpritePools =
        {
            new[]{ "enemy_basic",   "enemy_soldier" },   // diff 0 Easy
            new[]{ "enemy_soldier", "enemy_ninja"   },   // diff 1 Medium
            new[]{ "enemy_ninja",   "enemy_demon"   },   // diff 2 Hard
            new[]{ "enemy_demon",   "enemy_ghost"   },   // diff 3 Boss
            new[]{ "enemy_ghost",   "enemy_demon"   },   // diff 4 Legendary
        };

        void SpawnWave()
        {
            _waveDelay = 0;
            int count = _dungeon != null ? _dungeon.EnemyCount / _totalWaves : 3;
            count = Math.Max(2, count);

            _enemies.Clear();
            var rng = new Random();
            int H = _game.GraphicsDevice.Viewport.Height;
            float baseX = _player.Position.X + 250f;

            int diff = _dungeon != null ? (int)_dungeon.Difficulty : 0;
            var spritePool = _enemySpritePools[Math.Clamp(diff, 0, _enemySpritePools.Length - 1)];

            for (int i = 0; i < count; i++)
            {
                var e = new Enemy();
                e.SpriteKey = spritePool[i % spritePool.Length];
                float x = baseX + i * 140f + rng.Next(-30, 30);
                x = Math.Clamp(x, 100, _world.Width - 100);
                // Multiplicateur d'acte : Acte 1=×1.0  Acte 2=×1.4  Acte 3=×1.9  Acte 4=×2.5  Acte 5=×3.2
                float actMult = _dungeon != null && _dungeon.StoryActIndex >= 0
                    ? 1f + _dungeon.StoryActIndex * 0.55f
                    : 1f;
                e.Init(new Vector2(x, H - 200),
                    hp:  (int)((100 + diff * 30 + _wave * 35) * actMult),
                    atk: (int)((12  + diff * 5  + _wave * 5)  * actMult),
                    spd: (int)((60  + diff * 12 + _wave * 8)  * actMult),
                    gold: 3 + diff * 2 + _wave * 2);
                int xpGain = 5 + (_dungeon != null ? (int)_dungeon.Difficulty * 3 : 0) + _wave * 2;
                e.OnGoldDrop = (g, pos) =>
                {
                    int reduced = System.Math.Max(1, g / 2); // or réduit de moitié
                    PlayerSave.AddGold(reduced);
                    PlayerSave.TotalGoldEarned += reduced;
                    bool rankedUp = PlayerSave.AddXp(xpGain);
                    SpawnHitBurst(pos, UIHelper.Gold);
                    ShowToast($"+{reduced}💰  +{xpGain}XP", UIHelper.Gold);
                    if (rankedUp) ShowToast($"⬆ RANG {PlayerSave.GetRank()} !", new Color(255,200,0));
                    PlayerSave.EnemiesKilled++;
                    foreach (var q in Catalog.Quests) q.CheckCompleted();
                };
                e.OnMaterialDrop = (m, q) => PlayerSave.AddMaterial(m, q);
                if (_dungeon != null)
                {
                    var drops = new MaterialReward[_dungeon.Rewards.Count];
                    for (int j = 0; j < _dungeon.Rewards.Count; j++)
                    {
                        var r = _dungeon.Rewards[j];
                        drops[j] = new MaterialReward { Material = r.Material, Min = 0, Max = Math.Max(1, r.Max / 2) };
                    }
                    e.Drops = drops;
                }
                else e.Drops = Array.Empty<MaterialReward>();
                _enemies.Add(e);
            }
            _enemiesLeft = count;
            ShowToast($"Vague {_wave + 1} — {count} ennemis !", UIHelper.Blue);
        }

        static readonly string[] _bossFruits =
        {
            "Fruit de la Fleur",   // diff 0 / wave 0
            "Fruit du Phénix",     // diff 1 / wave 1
            "Fruit Glace",         // diff 1b / wave 2
            "Fruit de l'Éclair",   // diff 2
            "Fruit du Sphinx",     // diff 2b
            "Fruit des Ombres",    // diff 3
            "Fruit du Magma",      // diff 3b
            "Fruit de la Lumière", // diff 4
            "Sekai Sekai no Mi",   // wave 8+
        };

        // Armes assignées aux boss selon la difficulté
        static readonly (string Name, string Icon, WeaponType Kind, float DmgBonus)[] _bossWeapons =
        {
            ("Épée Six Seven",      "⚔️",  WeaponType.Sword,    1.20f),  // diff 0
            ("Arc Éternel",         "🪃",  WeaponType.Bow,      1.30f),  // diff 1
            ("Sceptre des Ombres",  "🔱",  WeaponType.Staff,    1.45f),  // diff 2
            ("Lame du Chaos",       "🗡️",  WeaponType.Sword,    1.60f),  // diff 3
            ("Faux du Néant",       "☠️",  WeaponType.Scythe,   1.80f),  // diff 4
        };

        void SpawnBoss()
        {
            _enemies.Clear();
            var boss = new Enemy();
            int H = _game.GraphicsDevice.Viewport.Height;
            int diff = _dungeon != null ? (int)_dungeon.Difficulty : 0;
            int fruitIdx = Math.Clamp(diff * 2 + (_wave % 2), 0, _bossFruits.Length - 1);
            string fruitName = _bossFruits[fruitIdx];

            // Multiplicateur d'acte pour le boss
            float actMultBoss = _dungeon != null && _dungeon.StoryActIndex >= 0
                ? 1f + _dungeon.StoryActIndex * 0.65f   // boss scale encore plus fort
                : 1f;

            // Arme du boss
            var weapon = _bossWeapons[Math.Clamp(diff, 0, _bossWeapons.Length - 1)];
            float baseAtk = (32 + _wave * 4) * actMultBoss;

            boss.Init(new Vector2(_player.Position.X + 400f, H - 200),
                hp:  (int)(((60 + diff * 80) + _wave * 60) * actMultBoss),
                atk: (10 + diff * 6)  * weapon.DmgBonus,
                spd: 60 + diff * 7,
                gold: 300);
            boss.FruitDrop  = fruitName;
            boss.IsBoss     = true;
            boss.WeaponName = weapon.Name;
            boss.WeaponIcon = weapon.Icon;
            boss.WeaponKind = weapon.Kind;
            boss.WeaponDmg  = weapon.DmgBonus;
            boss.OnGoldDrop = (g, pos) => {
                int reduced = System.Math.Max(1, g / 2);
                PlayerSave.AddGold(reduced);
                PlayerSave.TotalGoldEarned += reduced;
                PlayerSave.BossesDefeated++;
                foreach (var q in Catalog.Quests) q.CheckCompleted();
                ShowToast($"+{reduced} 💰 BOSS!", UIHelper.Gold);
            };
            boss.OnFruitDrop = (fn) =>
            {
                var f = Catalog.Fruits.Find(fr => fr.Name == fn);
                if (f != null && !f.IsOwned)
                {
                    f.IsOwned = true;
                    ShowToast($"🍎 FRUIT OBTENU : {f.Icon} {f.Name}!", new Color(255, 160, 0));
                }
                else
                {
                    ShowToast($"🍎 {fn} (déjà possédé)", UIHelper.Gold);
                }
            };
            _enemies.Add(boss);
            _enemiesLeft = 1;
            _bossActive = true;
            _bossFlashTimer = 1.2f;
            _bossPulseTimer = 0f;
            ShowToast($"⚠️ BOSS {weapon.Icon} {weapon.Name} — 🍎 {fruitName}!", Color.Red);
        }

        public void Update(GameTime gt)
        {
            float dt = (float)gt.ElapsedGameTime.TotalSeconds;
            var kb = Keyboard.GetState();
            var ms = Mouse.GetState();

            _backBtn.Update(ms);
            _toastTimer -= dt;

            // Tutoriel — dismiss au premier clic ou touche d'action
            if (!_tutoDismissed)
            {
                _tutoTimer += dt;
                bool anyAction = kb.IsKeyDown(Keys.Z) || kb.IsKeyDown(Keys.X) ||
                                 kb.IsKeyDown(Keys.R) || kb.IsKeyDown(Keys.T) ||
                                 kb.IsKeyDown(Keys.F) || kb.IsKeyDown(Keys.G) ||
                                 ms.LeftButton == ButtonState.Pressed ||
                                 _tutoTimer >= 8f;
                if (anyAction) _tutoDismissed = true;
            }

            // Story return timer
            if (_returnToStoryTimer > 0)
            {
                _returnToStoryTimer -= dt;
                if (_returnToStoryTimer <= 0) _game.NotifyStoryVictory();
            }

            // Boss timers
            if (_bossFlashTimer > 0) _bossFlashTimer -= dt;
            if (_zoomTimer      > 0) { _zoomTimer -= dt; _zoomFlash = Math.Max(0f, _zoomFlash - dt * 3f); }
            if (_bossActive) _bossPulseTimer += dt;

            if (_victory || _defeat)
            {
                // Sélection de classe active — mettre à jour ses boutons
                if (_classSelectShown)
                    foreach (var b in _classSelectBtns.ToList()) b.Update(ms);
                return;
            }

            // Pause combat pendant le tutoriel
            if (!_tutoDismissed) return;

            _player.Update(gt, kb, ms, _world, _platforms);

            // Camera scroll
            int W = _game.GraphicsDevice.Viewport.Width;
            _scrollX = Math.Clamp(_player.Position.X - W / 3f, 0, _world.Width - W);

            // Update enemies
            foreach (var e in _enemies)
            {
                e.Update(gt, _player.Position, _platforms, _world);

                // Enemy hits player
                if (e.AttackActive && e.AttackBox.Intersects(_player.Bounds))
                    _player.TakeDamage(e.AttackDamage);
            }

            // Player hits enemies
            if (_player.AttackActive)
            {
                foreach (var e in _enemies)
                {
                    if (!e.IsAlive) continue;
                    if (_player.AttackBox.Intersects(e.Bounds))
                    {
                        int dmg = (int)_player.AttackDamage;
                        e.TakeDamage(dmg);
                        Vector2 hitPos = e.Position + new Vector2(22, 20);
                        SpawnHitBurst(hitPos, Color.OrangeRed);
                        SpawnDamageNumber(hitPos, $"-{dmg}", Color.Red, 1.2f);
                    }
                }
            }

            // Count alive enemies
            int alive = 0;
            for (int i = 0; i < _enemies.Count; i++)
                if (_enemies[i].IsAlive) alive++;
            if (alive == 0 && _enemiesLeft > 0 && _waveDelay <= 0)
                _waveDelay = 2f;
            _enemiesLeft = alive;

            // Wave progression
            if (_waveDelay > 0)
            {
                _waveDelay -= dt;
                if (_waveDelay <= 0)
                {
                    _wave++;
                    if (_wave < _totalWaves)
                    {
                        // Heal 50% max HP between waves
                        float heal = _player.MaxHP * 0.50f;
                        _player.CurrentHP = Math.Min(_player.MaxHP, _player.CurrentHP + heal);
                        ShowToast($"Vague suivante — +{(int)heal} HP récupérés !", new Color(64,224,160));
                        SpawnWave();
                    }
                    else if (!_bossWave) { _bossWave = true; SpawnBoss(); }
                    else if (_bossWave && _dungeon != null && _dungeon.BossGauntlet && _wave < _totalWaves)
                    {
                        float heal = _player.MaxHP * 0.40f;
                        _player.CurrentHP = Math.Min(_player.MaxHP, _player.CurrentHP + heal);
                        ShowToast($"BOSS {_wave + 1}/4 — +{(int)heal} HP récupérés !", new Color(255, 80, 80));
                        SpawnBoss();
                    }
                    else Victory();
                }
            }

            // Death
            if (!_player.IsAlive) Defeat();

            // Particles
            _particles.RemoveAll(p => p.Life <= 0);
            for (int i = 0; i < _particles.Count; i++)
            {
                var p = _particles[i];
                _particles[i] = p with
                {
                    Pos  = p.Pos + p.Vel * dt,
                    Vel  = p.Vel + new Vector2(0, 350) * dt,
                    Life = p.Life - dt
                };
            }

            // Damage numbers
            _damageNumbers.RemoveAll(d => d.Life <= 0);
            for (int i = 0; i < _damageNumbers.Count; i++)
            {
                var d = _damageNumbers[i];
                _damageNumbers[i] = d with
                {
                    Pos  = d.Pos + d.Vel * dt,
                    Life = d.Life - dt
                };
            }

            // ESC
            if (kb.IsKeyDown(Keys.Escape)) _game.ChangeState(GameState.MainMenu);
        }

        // Set this to return to Story after a story dungeon win
        public bool IsStoryDungeon;

        void Victory()
        {
            _victory = true;
            if (_dungeon != null)
            {
                int reward = _dungeon.GoldReward / 2; // or réduit de moitié
                PlayerSave.AddGold(reward);
                PlayerSave.TotalGoldEarned += reward;
                int bonusXp = reward / 5;
                PlayerSave.AddXp(bonusXp);
                PlayerSave.DungeonsCompleted++;
                foreach (var q in Catalog.Quests) q.CheckCompleted();
            }
            ShowToast("VICTOIRE! 🏆", UIHelper.Gold);
            // Donjon de classe → sélection de classe
            if (_dungeon != null && _dungeon.IsClassDungeon && !PlayerSave.ClassDungeonDone)
            {
                _classSelectShown = true;
                BuildClassSelectButtons();
            }
            // If story dungeon, notify game after delay
            if (IsStoryDungeon)
                _returnToStoryTimer = 3f;
        }

        void BuildClassSelectButtons()
        {
            _classSelectBtns.Clear();
            int W = _game.GraphicsDevice.Viewport.Width;
            int H = _game.GraphicsDevice.Viewport.Height;
            int bw = 200, bh = 50, gap = 12;
            int total = Catalog.PlayerClasses.Length;
            int startX = W / 2 - (total * (bw + gap)) / 2;
            int by2 = H / 2 + 80;
            for (int i = 0; i < total; i++)
            {
                int idx = i;
                var cls = Catalog.PlayerClasses[i];
                _classSelectBtns.Add(new UIButton(
                    new Rectangle(startX + i * (bw + gap), by2, bw, bh),
                    $"{cls.Icon} {cls.Name}",
                    () => ChooseClass(idx),
                    new Color(20, 8, 35), new Color(50, 20, 80)
                ) { TextColor = new Color(200, 140, 255) });
            }
        }

        void ChooseClass(int idx)
        {
            var cls = Catalog.PlayerClasses[idx];
            PlayerSave.PlayerClassName   = cls.Name;
            PlayerSave.PlayerClassIcon   = cls.Icon;
            PlayerSave.ClassDungeonDone  = true;
            // Donner les capacités de classe comme AbilityData
            foreach (var abilityName in cls.Abilities)
            {
                var existing = Catalog.Abilities.Find(a => a.Name == abilityName);
                if (existing != null) existing.IsOwned = true;
            }
            SaveSystem.Save();
            _classSelectShown = false;
            ShowToast($"{cls.Icon} Classe choisie : {cls.Name} !", new Color(200, 140, 255));
        }

        float _returnToStoryTimer = -1f;

        void Defeat()
        {
            _defeat = true;
            ShowToast("DÉFAITE... 💀", Color.Red);
        }

        // 8-direction burst on hit
        void SpawnHitBurst(Vector2 pos, Color col)
        {
            var rng = new Random();
            for (int i = 0; i < 8; i++)
            {
                float angle = i * (MathF.PI * 2f / 8f) + (float)rng.NextDouble() * 0.3f;
                float speed = 120f + (float)rng.NextDouble() * 140f;
                Vector2 vel = new Vector2(MathF.Cos(angle) * speed, MathF.Sin(angle) * speed);
                float size = 4f + (float)rng.NextDouble() * 5f;
                _particles.Add(new Particle(pos, vel, col, 0.55f, 0.55f, size));
            }
            // Extra bright center sparks
            for (int i = 0; i < 4; i++)
            {
                float angle = (float)rng.NextDouble() * MathF.PI * 2f;
                float speed = 60f + (float)rng.NextDouble() * 80f;
                _particles.Add(new Particle(pos,
                    new Vector2(MathF.Cos(angle) * speed, MathF.Sin(angle) * speed),
                    Color.White, 0.3f, 0.3f, 3f));
            }
        }

        void SpawnParticles(Vector2 pos, Color col, int count)
        {
            var rng = new Random();
            for (int i = 0; i < count; i++)
            {
                _particles.Add(new Particle(
                    pos,
                    new Vector2(rng.NextSingle() * 200 - 100, rng.NextSingle() * -300 - 50),
                    col, 0.6f, 0.6f, 6f));
            }
        }

        void SpawnDamageNumber(Vector2 pos, string text, Color col, float scale)
        {
            var rng = new Random();
            float vx = rng.NextSingle() * 40f - 20f;
            _damageNumbers.Add(new DamageNumber(pos, new Vector2(vx, -80f), text, col, 1.1f, 1.1f, scale));
        }

        public void Draw(SpriteBatch sb)
        {
            int W = _game.GraphicsDevice.Viewport.Width;
            int H = _game.GraphicsDevice.Viewport.Height;

            // ─── ARRIÈRE-PLAN FC Mobile style ─────────────────────────────────
            var bgTex = TravelTour.Core.SpriteLoader.BgDungeon() ?? TravelTour.Core.SpriteLoader.BgCity();
            if (bgTex != null)
            {
                float bgScroll = _scrollX * 0.12f;
                int bgW = (int)(W * 1.5f);
                sb.Draw(bgTex, new Rectangle(-(int)bgScroll, 0, bgW, H), Color.White);
            }
            else
            {
                DrawVerticalGradient(sb, W, H);
                DrawMoon(sb, W, H);
                DrawMountains(sb, W, H);
                DrawBuildings(sb, W, H);
            }

            // Overlay sombre pour profondeur (FC Mobile style)
            sb.Draw(_pixel, new Rectangle(0, 0, W, H), new Color(0, 0, 10) * 0.25f);

            // Sol lumineux dynamique — rayon de lumière centré sur le joueur
            int lightX = (int)(_player.Position.X - _scrollX);
            for (int i = 5; i >= 1; i--)
            {
                float alpha = 0.018f * i;
                int lw = 120 * i; int lh = 40 * i;
                sb.Draw(_pixel, new Rectangle(lightX - lw/2 + 24, H - 65 - lh/4, lw, lh/2),
                    new Color(120, 160, 255) * alpha);
            }

            // Brume au sol
            sb.Draw(_pixel, new Rectangle(0, H - 70, W, 70), new Color(10, 15, 40) * 0.55f);
            sb.Draw(_pixel, new Rectangle(0, H - 45, W, 45), new Color(20, 25, 60) * 0.45f);

            // ─── WORLD TRANSFORM ───
            sb.End();

            // ── Calcul transform avec gros plan si capacité active ──
            Matrix worldTransform;
            if (_zoomActive)
            {
                // Progression 0→1→0 (zoom in puis out)
                float t = 1f - (_zoomTimer / _zoomTotal);
                float zoomCurve = t < 0.25f
                    ? t / 0.25f                       // montée rapide
                    : 1f - (t - 0.25f) / 0.75f;      // descente progressive
                float zoom = 1f + (_zoomPeak - 1f) * Math.Clamp(zoomCurve, 0f, 1f);

                int W2 = _game.GraphicsDevice.Viewport.Width;
                int H2 = _game.GraphicsDevice.Viewport.Height;
                float px = _player.Position.X;
                float py = _player.Position.Y;
                worldTransform =
                    Matrix.CreateScale(zoom, zoom, 1f) *
                    Matrix.CreateTranslation(
                        W2 / 2f - px * zoom,
                        H2 / 2f - py * zoom,
                        0f);
            }
            else
            {
                worldTransform = Matrix.CreateTranslation(-(int)_scrollX, 0, 0);
            }

            sb.Begin(transformMatrix: worldTransform);

            // Platforms améliorées
            DrawPlatforms(sb);

            // Enemies
            foreach (var e in _enemies) e.Draw(sb, _pixel);

            // Player
            _player.Draw(sb, _pixel);

            // Particles
            foreach (var p in _particles)
            {
                float a = p.Life / p.MaxLife;
                int s = (int)p.Size;
                // Glow : 3 couches (grande → petite) style FC Mobile
                sb.Draw(_pixel, new Rectangle((int)p.Pos.X - s, (int)p.Pos.Y - s, s*2, s*2), p.Col * (a * 0.25f));
                sb.Draw(_pixel, new Rectangle((int)p.Pos.X - s/2, (int)p.Pos.Y - s/2, s, s), p.Col * (a * 0.6f));
                sb.Draw(_pixel, new Rectangle((int)p.Pos.X - 2, (int)p.Pos.Y - 2, 4, 4), Color.White * (a * 0.9f));
            }

            // Floating damage numbers (world-space)
            foreach (var d in _damageNumbers)
            {
                float a = d.Life / d.MaxLife;
                float yOff = (1f - a) * -20f;
                Vector2 size = _bigFont.MeasureString(d.Text);
                // Shadow
                sb.DrawString(_bigFont, d.Text,
                    new Vector2(d.Pos.X - size.X / 2f + 2, d.Pos.Y + yOff + 2),
                    Color.Black * a * 0.7f);
                // Main
                sb.DrawString(_bigFont, d.Text,
                    new Vector2(d.Pos.X - size.X / 2f, d.Pos.Y + yOff),
                    d.Col * a);
            }

            sb.End();
            sb.Begin();

            // ─── EFFETS GROS PLAN capacité ─────────────────────────────────────
            if (_zoomActive)
            {
                int W3 = _game.GraphicsDevice.Viewport.Width;
                int H3 = _game.GraphicsDevice.Viewport.Height;

                // Flash blanc
                if (_zoomFlash > 0f)
                    sb.Draw(_pixel, new Rectangle(0, 0, W3, H3), Color.White * _zoomFlash);

                // Vignette zoom — bords sombres pour dramatiser
                float zPct = 1f - (_zoomTimer / _zoomTotal);
                float vigA = 0.6f * Math.Clamp(1f - Math.Abs(zPct - 0.5f) * 2f, 0f, 1f);
                for (int vi = 0; vi < 5; vi++)
                {
                    int vp = vi * 28;
                    sb.Draw(_pixel, new Rectangle(0,  0,  vp, H3), Color.Black * vigA);
                    sb.Draw(_pixel, new Rectangle(W3 - vp, 0, vp, H3), Color.Black * vigA);
                    sb.Draw(_pixel, new Rectangle(0,  0,  W3, vp), Color.Black * vigA * 0.6f);
                    sb.Draw(_pixel, new Rectangle(0,  H3 - vp, W3, vp), Color.Black * vigA * 0.6f);
                }

                // Nom de la capacité au centre en grand
                float nameAlpha = Math.Clamp(1f - (_zoomTimer / _zoomTotal) * 1.5f + 0.3f, 0f, 1f);
                if (nameAlpha > 0f)
                {
                    Vector2 labelSz = _bigFont.MeasureString(_zoomLabel);
                    float lx = W3 / 2f - labelSz.X / 2f;
                    float ly = H3 * 0.35f;
                    // Ombre
                    sb.DrawString(_bigFont, _zoomLabel, new Vector2(lx + 3, ly + 3), Color.Black * nameAlpha * 0.8f);
                    // Texte principal
                    sb.DrawString(_bigFont, _zoomLabel, new Vector2(lx, ly), _zoomColor * nameAlpha);
                    // Ligne décorative sous le titre
                    int lineW = (int)(labelSz.X * nameAlpha);
                    sb.Draw(_pixel, new Rectangle((int)(W3 / 2f - lineW / 2f), (int)(ly + labelSz.Y + 4), lineW, 3),
                        _zoomColor * nameAlpha * 0.8f);
                }
            }

            // ─── VIGNETTE FC Mobile style ─────────────────────────────────────
            int vSize = 120;
            // Bords gauche/droite
            for (int i = 0; i < 6; i++)
            {
                float a = 0.06f * (6 - i);
                int w2 = vSize - i * 18;
                sb.Draw(_pixel, new Rectangle(0, 0, w2, H), Color.Black * a);
                sb.Draw(_pixel, new Rectangle(W - w2, 0, w2, H), Color.Black * a);
            }
            // Haut/bas
            for (int i = 0; i < 4; i++)
            {
                float a = 0.07f * (4 - i);
                int h2 = 80 - i * 18;
                sb.Draw(_pixel, new Rectangle(0, 0, W, h2), Color.Black * a);
                sb.Draw(_pixel, new Rectangle(0, H - h2, W, h2), Color.Black * a);
            }

            // ─── HUD ───
            DrawHUD(sb, W, H);

            // ─── BOSS FLASH ───
            if (_bossFlashTimer > 0)
            {
                float flashA = Math.Clamp(_bossFlashTimer / 1.2f, 0f, 1f);
                sb.Draw(_pixel, new Rectangle(0, 0, W, H), new Color(200, 0, 0) * flashA * 0.4f);
                DrawBossText(sb, W, H, flashA);
            }
            else if (_bossActive && _enemiesLeft > 0)
            {
                float pulse = 0.5f + 0.5f * MathF.Sin(_bossPulseTimer * 3f);
                DrawBossText(sb, W, H, pulse * 0.6f);
            }

            // Victory / Defeat overlay
            if (_victory || _defeat)
            {
                sb.Draw(_pixel, new Rectangle(0, 0, W, H), Color.Black * 0.65f);
                string msg = _victory ? "VICTOIRE!" : "DÉFAITE...";
                Color mc   = _victory ? UIHelper.Gold : Color.Red;
                Vector2 ms2 = _bigFont.MeasureString(msg);
                // Shadow
                sb.DrawString(_bigFont, msg,
                    new Vector2(W / 2f - ms2.X / 2f + 3, H / 2f - ms2.Y / 2f + 3),
                    Color.Black * 0.8f);
                sb.DrawString(_bigFont, msg,
                    new Vector2(W / 2f - ms2.X / 2f, H / 2f - ms2.Y / 2f), mc);
                sb.DrawString(_font, "Appuyez sur ÉCHAP pour revenir au menu",
                    new Vector2(W / 2f - 180, H / 2f + ms2.Y),
                    UIHelper.TextDim);
            }

            // Toast
            DrawToast(sb, W, H);

            // ─── SÉLECTION DE CLASSE ─────────────────────────────────────────────
            if (_classSelectShown)
                DrawClassSelect(sb, W, H);

            // ─── TUTO COMBAT (première ouverture) ───────────────────────────────
            if (!_tutoDismissed)
                DrawCombatTuto(sb, W, H);
        }

        void DrawClassSelect(SpriteBatch sb, int W, int H)
        {
            // Fond dramatique
            sb.Draw(_pixel, new Rectangle(0, 0, W, H), new Color(5, 0, 20) * 0.88f);

            // Titre
            UIHelper.DrawCenteredText(sb, _bigFont, "✨  CHOISISSEZ VOTRE CLASSE  ✨",
                new Rectangle(0, H / 2 - 200, W, 50), new Color(220, 160, 255), 0.75f);
            UIHelper.DrawCenteredText(sb, _font, "Cette décision est définitive — choisissez avec soin.",
                new Rectangle(0, H / 2 - 155, W, 26), UIHelper.TextDim, 0.8f);

            // Cartes de classes
            var classes = Catalog.PlayerClasses;
            int cw = 190, ch = 130, gap = 10;
            int startX = W / 2 - (classes.Length * (cw + gap)) / 2;
            int cy = H / 2 - 80;

            for (int i = 0; i < classes.Length && i < _classSelectBtns.Count; i++)
            {
                var cls = classes[i];
                var btn = _classSelectBtns[i];
                bool hov = btn.Bounds.Contains(Mouse.GetState().Position);

                Color accent = new Color(180, 80, 255);
                Color bg = hov ? new Color(40, 15, 70) : new Color(20, 8, 35);
                UIHelper.DrawBox(sb, _pixel, new Rectangle(startX + i*(cw+gap), cy, cw, ch),
                    bg, accent * (hov ? 1f : 0.4f), hov ? 2 : 1);

                // Icône
                UIHelper.DrawCenteredText(sb, _bigFont, cls.Icon,
                    new Rectangle(startX + i*(cw+gap), cy + 8, cw, 40), Color.White, 0.85f);

                // Nom
                UIHelper.DrawCenteredText(sb, _font, cls.Name,
                    new Rectangle(startX + i*(cw+gap), cy + 52, cw, 20),
                    hov ? new Color(220,160,255) : UIHelper.TextMain, 0.75f);

                // Capacités
                for (int j = 0; j < cls.Abilities.Length && j < 3; j++)
                {
                    string aShort = cls.Abilities[j].Length > 18 ? cls.Abilities[j][..18] + "…" : cls.Abilities[j];
                    UIHelper.DrawCenteredText(sb, _font, $"• {aShort}",
                        new Rectangle(startX + i*(cw+gap), cy + 72 + j * 18, cw, 16),
                        UIHelper.TextDim * 0.8f, 0.65f);
                }

                btn.Draw(sb, _pixel, _font, 0.0f); // invisible — just for click area
            }

            // Boutons sélection visibles en bas
            foreach (var b in _classSelectBtns)
                b.Draw(sb, _pixel, _font, 0.78f);
        }

        void DrawCombatTuto(SpriteBatch sb, int W, int H)
        {
            // Fond semi-transparent
            sb.Draw(_pixel, new Rectangle(0, 0, W, H), Color.Black * 0.55f);

            int bx = W / 2 - 340, by = H / 2 - 220, bw = 680, bh = 440;
            UIHelper.DrawBox(sb, _pixel, new Rectangle(bx, by, bw, bh), new Color(10, 12, 30), UIHelper.Gold * 0.8f, 2);

            // Titre
            UIHelper.DrawCenteredText(sb, _bigFont, "⚔️  CONTRÔLES DE COMBAT",
                new Rectangle(bx, by + 12, bw, 40), UIHelper.Gold, 0.7f);

            // Séparateur
            sb.Draw(_pixel, new Rectangle(bx + 20, by + 52, bw - 40, 1), UIHelper.Gold * 0.4f);

            var (f, bf) = (_font, _bigFont);
            int col1 = bx + 28, col2 = bx + 360, ry = by + 64;

            void Row(int x, int y, string key, string action, Color keyCol)
            {
                UIHelper.DrawBox(sb, _pixel, new Rectangle(x, y, 80, 28), new Color(20, 25, 50), keyCol * 0.7f, 1);
                UIHelper.DrawCenteredText(sb, f, key, new Rectangle(x, y, 80, 28), keyCol, 0.75f);
                sb.DrawString(f, action, new Vector2(x + 90, y + 6), UIHelper.TextMain);
            }

            // Colonne gauche — déplacement & attaques
            sb.DrawString(f, "DÉPLACEMENT & ATTAQUES", new Vector2(col1, ry), UIHelper.Blue);
            ry += 28;
            Row(col1, ry, "← →", "Se déplacer", UIHelper.Blue);            ry += 36;
            Row(col1, ry, "W / Espace", "Sauter", UIHelper.Blue);          ry += 36;
            Row(col1, ry, "Z / Clic G", "Attaque légère", UIHelper.Gold);  ry += 36;
            Row(col1, ry, "X / Clic D", "Attaque lourde", UIHelper.Gold);  ry += 36;
            Row(col1, ry, "Z×3", "Combo × 1.3", new Color(255,200,0));     ry += 36;
            Row(col1, ry, "Shift / C", "Dash", UIHelper.Blue);             ry += 36;
            Row(col1, ry, "Q / E", "Capacités 1 & 2", UIHelper.Purple);

            // Colonne droite — fruits
            ry = by + 64 + 28;
            sb.DrawString(f, "FRUITS DU DÉMON", new Vector2(col2, by + 64), new Color(255, 140, 0));
            Row(col2, ry, "R", "Move 1 du fruit", new Color(255,140,0));   ry += 36;
            Row(col2, ry, "T", "Move 2 du fruit", new Color(255,140,0));   ry += 36;
            Row(col2, ry, "F", "Move 3 du fruit", new Color(255,140,0));   ry += 36;
            Row(col2, ry, "G", "Move 4 (Ultime)", new Color(255,80,0));    ry += 36;

            sb.DrawString(f, "→ Équipe un fruit dans la Boutique", new Vector2(col2, ry + 4), UIHelper.TextDim);

            // Dismiss
            float alpha = _tutoTimer > 6f ? 0.5f + 0.5f * MathF.Sin(_tutoTimer * 4f) : 1f;
            UIHelper.DrawCenteredText(sb, f, "Appuyez sur Z ou cliquez pour commencer",
                new Rectangle(bx, by + bh - 32, bw, 24), UIHelper.Gold * alpha, 0.8f);
        }

        // ─── BACKGROUND HELPERS ───

        void DrawVerticalGradient(SpriteBatch sb, int W, int H)
        {
            // Simulate gradient with horizontal bands
            int bands = 32;
            int bandH = H / bands;
            for (int i = 0; i < bands; i++)
            {
                float t = i / (float)(bands - 1);
                // Top: near-black (5,5,20) → Bottom: dark purple (35,10,65)
                Color top    = new Color(5, 5, 20);
                Color bottom = new Color(35, 10, 65);
                byte r = (byte)(top.R + (bottom.R - top.R) * t);
                byte g = (byte)(top.G + (bottom.G - top.G) * t);
                byte b = (byte)(top.B + (bottom.B - top.B) * t);
                sb.Draw(_pixel, new Rectangle(0, i * bandH, W, bandH + 1), new Color(r, g, b));
            }
        }

        void DrawMoon(SpriteBatch sb, int W, int H)
        {
            // Moon: large circle drawn as concentric squares (no Circle primitive)
            int moonX = (int)(W * 0.78f);
            int moonY = (int)(H * 0.18f);
            int moonR = 55;
            // Outer glow
            for (int r = moonR + 20; r > moonR; r -= 2)
            {
                float a = (r - moonR) / 20f;
                sb.Draw(_pixel,
                    new Rectangle(moonX - r, moonY - r, r * 2, r * 2),
                    new Color(200, 200, 255) * (0.04f * (1f - a)));
            }
            // Moon body (approximate circle with layered rectangles)
            for (int r = moonR; r > 0; r -= 1)
            {
                float t = 1f - (float)r / moonR;
                // Blend from pale yellow-white center to cool white edge
                Color inner = new Color(255, 250, 200);
                Color outer = new Color(220, 225, 255);
                byte rv = (byte)(outer.R + (inner.R - outer.R) * t);
                byte gv = (byte)(outer.G + (inner.G - outer.G) * t);
                byte bv = (byte)(outer.B + (inner.B - outer.B) * t);
                float alpha = 0.18f + t * 0.08f;
                sb.Draw(_pixel,
                    new Rectangle(moonX - r, moonY - r, r * 2, r * 2),
                    new Color(rv, gv, bv) * alpha);
            }
        }

        void DrawMountains(SpriteBatch sb, int W, int H)
        {
            // Parallaxe très lente (20%)
            float parallax = 0.18f;
            int ox = (int)(_scrollX * parallax);
            // Mountain silhouettes: series of triangular peaks via tall rectangles tapering
            int[] peakX    = { 0,  120, 260, 400, 530, 700, 860, 1020, 1180, 1350 };
            int[] peakH    = { 120, 180, 140, 200, 160, 220, 130, 190,  150,  210 };
            int   baseY    = H - 60;
            int   worldW   = W + 400;

            for (int p = 0; p < peakX.Length; p++)
            {
                int px = ((peakX[p] - ox % worldW + worldW) % worldW) - 100;
                int ph = peakH[p];
                int pw = 160 + ph / 3;
                // Draw triangle approximation: stack of horizontal lines narrowing toward top
                for (int y = 0; y < ph; y++)
                {
                    float t = (float)y / ph;
                    int lineW = (int)(pw * t);
                    int lineX = px + (pw - lineW) / 2;
                    sb.Draw(_pixel,
                        new Rectangle(lineX, baseY - ph + y, lineW, 1),
                        new Color(12, 8, 25) * 0.92f);
                }
            }
        }

        void DrawBuildings(SpriteBatch sb, int W, int H)
        {
            // Parallaxe moyenne (35%)
            float parallax = 0.35f;
            int ox = (int)(_scrollX * parallax);
            int baseY = H - 60;

            // Building definitions: (x, width, height)
            (int bx, int bw, int bh)[] buildings = {
                (50,   55, 95),  (120,  40, 130), (185, 65, 80),
                (270,  50, 110), (340,  35, 155), (400, 60, 90),
                (480,  45, 120), (550,  55, 100), (630, 40, 140),
                (690,  70, 75),  (780,  50, 115), (850, 45, 130),
                (920,  60, 95),  (1000, 35, 160), (1060,55, 85),
            };
            int worldW = W + 600;
            var rng    = new Random(42); // fixed seed for stable windows

            foreach (var (bx, bw, bh) in buildings)
            {
                int px = ((bx - ox % worldW + worldW) % worldW) - 80;
                int py = baseY - bh;

                // Building body
                sb.Draw(_pixel, new Rectangle(px, py, bw, bh), new Color(18, 12, 38) * 0.88f);
                // Rooftop line
                sb.Draw(_pixel, new Rectangle(px, py, bw, 2), new Color(60, 40, 100) * 0.7f);

                // Windows (yellow dots)
                for (int wy = py + 8; wy < baseY - 10; wy += 14)
                {
                    for (int wx = px + 6; wx < px + bw - 6; wx += 10)
                    {
                        bool lit = (rng.Next(0, 3) != 0);
                        if (lit)
                            sb.Draw(_pixel, new Rectangle(wx, wy, 5, 5),
                                new Color(255, 220, 80) * 0.55f);
                        else
                            sb.Draw(_pixel, new Rectangle(wx, wy, 5, 5),
                                new Color(20, 18, 40) * 0.5f);
                    }
                }
            }
        }

        // ─── PLATFORMS AMÉLIORÉES ───

        void DrawPlatforms(SpriteBatch sb)
        {
            foreach (var p in _platforms)
            {
                // Shadow portée en dessous
                sb.Draw(_pixel,
                    new Rectangle(p.X + 4, p.Y + p.Height, p.Width - 4, 6),
                    Color.Black * 0.35f);

                // Corps principal
                UIHelper.DrawBox(sb, _pixel, p, new Color(25, 30, 55), new Color(45, 55, 100), 2);

                // Texture interne : lignes verticales fines semi-transparentes
                int step = 18;
                for (int x = p.X + step; x < p.X + p.Width - 4; x += step)
                    sb.Draw(_pixel, new Rectangle(x, p.Y + 2, 1, p.Height - 4),
                        new Color(80, 100, 160) * 0.12f);

                // Bord supérieur brillant (highlight)
                sb.Draw(_pixel, new Rectangle(p.X + 1, p.Y, p.Width - 2, 2),
                    new Color(120, 160, 255) * 0.55f);
                sb.Draw(_pixel, new Rectangle(p.X + 1, p.Y + 1, p.Width - 2, 1),
                    new Color(200, 220, 255) * 0.22f);
            }
        }

        // ─── BOSS TEXT ───

        void DrawBossText(SpriteBatch sb, int W, int H, float alpha)
        {
            string bossText = "BOSS";
            Vector2 bossSize = _bigFont.MeasureString(bossText);
            float bx = W / 2f - bossSize.X / 2f;
            float by = H * 0.28f;
            // Glow shadow layers
            for (int s = 6; s >= 1; s--)
            {
                sb.DrawString(_bigFont, bossText,
                    new Vector2(bx, by + s),
                    new Color(180, 0, 0) * alpha * (0.25f / s));
            }
            sb.DrawString(_bigFont, bossText,
                new Vector2(bx + 2, by + 2),
                Color.Black * alpha * 0.8f);
            sb.DrawString(_bigFont, bossText,
                new Vector2(bx, by),
                new Color(255, 60, 60) * alpha);
        }

        // ─── HUD ───

        void DrawHUD(SpriteBatch sb, int W, int H)
        {
            _backBtn.Draw(sb, _pixel, _font, 0.85f);

            // ── HP bar avec dégradé ──
            int barX = W / 2 - 150;
            int barW = 300;

            // Portrait FC Mobile style
            int portraitSize = 44;
            int portraitX    = barX - portraitSize - 8;
            int portraitY    = 10;
            // Fond dégradé doré
            sb.Draw(_pixel, new Rectangle(portraitX, portraitY, portraitSize, portraitSize), new Color(20, 15, 40));
            sb.Draw(_pixel, new Rectangle(portraitX, portraitY, portraitSize, 3), UIHelper.Gold);
            sb.Draw(_pixel, new Rectangle(portraitX, portraitY + portraitSize - 3, portraitSize, 3), UIHelper.Gold);
            sb.Draw(_pixel, new Rectangle(portraitX, portraitY, 3, portraitSize), UIHelper.Gold);
            sb.Draw(_pixel, new Rectangle(portraitX + portraitSize - 3, portraitY, 3, portraitSize), UIHelper.Gold);
            // Sprite joueur dans le portrait
            var playerPortrait = TravelTour.Core.SpriteLoader.Player(false);
            if (playerPortrait != null)
                sb.Draw(playerPortrait, new Rectangle(portraitX + 2, portraitY + 2, portraitSize - 4, portraitSize - 4), Color.White);
            else
            {
                sb.Draw(_pixel, new Rectangle(portraitX + 16, portraitY + 4, 12, 12), UIHelper.Blue);
                sb.Draw(_pixel, new Rectangle(portraitX + 12, portraitY + 16, 20, 24), new Color(80, 130, 220));
            }
            // Lueur dorée (glow border)
            sb.Draw(_pixel, new Rectangle(portraitX - 2, portraitY - 2, portraitSize + 4, 2), UIHelper.Gold * 0.5f);
            sb.Draw(_pixel, new Rectangle(portraitX - 2, portraitY + portraitSize, portraitSize + 4, 2), UIHelper.Gold * 0.5f);

            // HP bar background + border
            UIHelper.DrawBox(sb, _pixel, new Rectangle(barX, 14, barW, 20), UIHelper.Dark2, UIHelper.TextDim, 1);

            // HP gradient fill (vert → jaune → rouge selon le %HP)
            float hpPct = _player.CurrentHP / _player.MaxHP;
            int hpFilled = (int)(296 * Math.Clamp(hpPct, 0f, 1f));
            if (hpFilled > 0)
            {
                // Dégradé simulé avec 3 bandes
                Color hpLow  = new Color(220, 60, 40);
                Color hpMid  = new Color(220, 180, 40);
                Color hpHigh = new Color(60, 200, 80);
                Color hpCol  = hpPct > 0.6f ? hpHigh : (hpPct > 0.3f ? hpMid : hpLow);
                // Dark BG
                sb.Draw(_pixel, new Rectangle(barX + 2, 15, 296, 18), new Color(20, 40, 20));
                // Fill with lighter top strip
                sb.Draw(_pixel, new Rectangle(barX + 2, 15, hpFilled, 18), hpCol * 0.9f);
                sb.Draw(_pixel, new Rectangle(barX + 2, 15, hpFilled, 5), hpCol * 1.3f);
            }
            sb.DrawString(_font, $"HP  {(int)_player.CurrentHP}/{(int)_player.MaxHP}",
                new Vector2(barX + 4, 17), UIHelper.TextMain);

            // ── Chakra bar avec dégradé ──
            UIHelper.DrawBox(sb, _pixel, new Rectangle(barX, 38, barW, 12), UIHelper.Dark2, UIHelper.TextDim, 1);
            float cpPct   = _player.CurrentChakra / _player.MaxChakra;
            int   cpFilled = (int)(296 * Math.Clamp(cpPct, 0f, 1f));
            if (cpFilled > 0)
            {
                sb.Draw(_pixel, new Rectangle(barX + 2, 39, 296, 10), new Color(20, 10, 40));
                sb.Draw(_pixel, new Rectangle(barX + 2, 39, cpFilled, 10), UIHelper.Purple * 0.9f);
                sb.Draw(_pixel, new Rectangle(barX + 2, 39, cpFilled, 3),  new Color(220, 160, 255) * 0.8f);
            }

            // ── Abilities avec frame dorée / grisée ──
            for (int i = 0; i < Math.Min(2, _player.Abilities.Count); i++)
            {
                int ax  = 16 + i * 76;
                int ay  = H - 84;
                float pct = _player.GetAbilityCdPct(i);
                bool ready = pct <= 0f;

                // Frame couleur selon état
                Color frameBorder = ready ? UIHelper.Gold : new Color(70, 70, 90);
                Color frameInner  = ready ? new Color(40, 30, 10) : new Color(20, 20, 30);

                UIHelper.DrawBox(sb, _pixel, new Rectangle(ax, ay, 68, 68), frameInner, frameBorder, 2);

                // Bord supplémentaire doré si prêt
                if (ready)
                {
                    sb.Draw(_pixel, new Rectangle(ax + 2, ay + 2, 64, 1), UIHelper.Gold * 0.5f);
                    sb.Draw(_pixel, new Rectangle(ax + 2, ay + 2, 1, 64), UIHelper.Gold * 0.5f);
                }

                // Cooldown overlay
                if (pct > 0)
                    sb.Draw(_pixel, new Rectangle(ax + 2, ay + 2, 64, (int)(64 * pct)),
                        Color.Black * 0.65f);

                string cd = _player.GetAbilityCdText(i);
                Color iconColor = ready ? UIHelper.Gold : UIHelper.TextDim;
                UIHelper.DrawCenteredText(sb, _font,
                    _player.Abilities[i].Icon + " " + cd,
                    new Rectangle(ax, ay, 68, 68),
                    iconColor, 0.7f);

                sb.DrawString(_font, $"[{(i == 0 ? "Q" : "E")}]",
                    new Vector2(ax + 26, ay + 70), UIHelper.TextDim * 0.6f);
            }

            // ── Barre Fruit du Démon ───────────────────────────────────────────
            var equippedFruit = PlayerSave.GetEquippedFruit();
            if (equippedFruit != null)
            {
                string[] fruitKeyLabels = { "R", "T", "F", "G" };
                int fSlotSize = 62;
                int fStartX   = W / 2 - (equippedFruit.Moves.Length * (fSlotSize + 6)) / 2;
                int fY        = H - 130;

                // Label fruit
                sb.DrawString(_font, $"{equippedFruit.Icon} {equippedFruit.Name}",
                    new Vector2(fStartX, fY - 18), new Color(255, 140, 0));

                for (int i = 0; i < equippedFruit.Moves.Length && i < 4; i++)
                {
                    var move = equippedFruit.Moves[i];
                    float cd   = _player.FruitMoveCd[i];
                    float maxCd = move.Cooldown;
                    float pct  = maxCd > 0 ? Math.Clamp(cd / maxCd, 0f, 1f) : 0f;
                    bool ready  = cd <= 0 && move.MasteryReq <= equippedFruit.Mastery;
                    bool locked = move.MasteryReq > equippedFruit.Mastery;

                    int fx = fStartX + i * (fSlotSize + 6);

                    // Fond
                    Color fBorder = locked ? new Color(60,60,60) : ready ? new Color(255,140,0) : new Color(80,60,20);
                    Color fInner  = locked ? new Color(20,20,20) : new Color(30,20,5);
                    UIHelper.DrawBox(sb, _pixel, new Rectangle(fx, fY, fSlotSize, fSlotSize), fInner, fBorder, 2);

                    // Cooldown overlay
                    if (pct > 0)
                        sb.Draw(_pixel, new Rectangle(fx+2, fY+2, fSlotSize-4, (int)((fSlotSize-4)*pct)), Color.Black * 0.7f);

                    // Icone move
                    UIHelper.DrawCenteredText(sb, _bigFont, locked ? "🔒" : move.Icon,
                        new Rectangle(fx, fY+4, fSlotSize, fSlotSize-16), ready ? Color.White : Color.Gray, 0.6f);

                    // Nom court (2 mots max)
                    string shortName = move.Name.Split(' ')[0];
                    UIHelper.DrawCenteredText(sb, _font, shortName,
                        new Rectangle(fx, fY + fSlotSize - 14, fSlotSize, 14), locked ? Color.Gray : new Color(255,200,100), 0.6f);

                    // Touche
                    sb.DrawString(_font, $"[{fruitKeyLabels[i]}]",
                        new Vector2(fx + fSlotSize/2 - 10, fY + fSlotSize + 2), new Color(255,140,0) * 0.8f);

                    // CD texte
                    if (cd > 0)
                        UIHelper.DrawCenteredText(sb, _font, $"{cd:F0}s",
                            new Rectangle(fx, fY+20, fSlotSize, 20), Color.White, 0.75f);
                }
            }

            // ── Niveau du joueur (coin haut droit) ──
            int lvl = PlayerSave.PlayerLevel;
            string lvlStr = $"Niv.{lvl}  Rang {PlayerSave.GetRank()}";
            sb.DrawString(_font, lvlStr, new Vector2(W - 180, 16), UIHelper.Gold);
            // Barre XP mini sous le niveau
            UIHelper.DrawProgressBar(sb, _pixel,
                new Rectangle(W - 180, 34, 160, 6),
                PlayerSave.LevelProgressPct(),
                new Color(168, 85, 247), new Color(20, 15, 35));
            sb.DrawString(_font, $"{PlayerSave.LevelXp}/{PlayerSave.XpToNextLevel()} XP",
                new Vector2(W - 180, 42), UIHelper.TextDim);

            // ── Ennemis / Vague ──
            bool isGauntlet = _dungeon != null && _dungeon.BossGauntlet;
            string waveLabel = isGauntlet
                ? $"⚔ BOSS {Math.Min(_wave + 1, _totalWaves)}/{_totalWaves}  HP restants : {_enemiesLeft}"
                : $"Ennemis : {_enemiesLeft}  Vague : {_wave + 1}/{_totalWaves + 1}";
            sb.DrawString(_font, waveLabel,
                new Vector2(W - 310, 64), isGauntlet ? Color.Red : UIHelper.TextMain);

            // ── Combo ──
            if (_player.ComboCount > 1)
            {
                string comboText = $"x{_player.ComboCount} COMBO";
                Vector2 comboSize = _bigFont.MeasureString(comboText);
                float cx = W / 2f - comboSize.X / 2f;
                float cy = H / 2f - 130;
                // Glow
                sb.DrawString(_bigFont, comboText, new Vector2(cx + 2, cy + 2), Color.Black * 0.7f);
                sb.DrawString(_bigFont, comboText, new Vector2(cx, cy), UIHelper.Gold);
            }

            // Controls
            sb.DrawString(_font, "A/D=Déplacement  W=Saut  Shift=Dash  Z=Attaque  X=Lourd  Q/E=Capacités",
                new Vector2(16, H - 24), UIHelper.TextDim * 0.6f);
        }

        void DrawToast(SpriteBatch sb, int W, int H)
        {
            if (_toastTimer <= 0) return;
            Vector2 ts = _font.MeasureString(_toast);
            int tx = (int)(W / 2f - ts.X / 2f - 16);
            UIHelper.DrawBox(sb, _pixel, new Rectangle(tx, H - 60, (int)ts.X + 32, 36),
                UIHelper.Dark2, _toastColor, 1);
            sb.DrawString(_font, _toast, new Vector2(tx + 16, H - 52), _toastColor);
        }

        void TriggerAbilityZoom(string label, Color col, float zoomPeak, float duration)
        {
            _zoomTimer = duration;
            _zoomTotal = duration;
            _zoomPeak  = zoomPeak;
            _zoomFlash = 0.85f;
            _zoomLabel = label;
            _zoomColor = col;
        }

        void ShowToast(string msg, Color col)
        { _toast = msg; _toastColor = col; _toastTimer = 2.5f; }

        public void Dispose() { }
    }

    // Simple LINQ-style helper to avoid System.Linq dependency issues
    static class EnumHelper
    {
        public static int Count<T>(this List<T> list, Func<T, bool> pred)
        {
            int c = 0;
            foreach (var item in list) if (pred(item)) c++;
            return c;
        }
        public static IEnumerable<U> Select<T, U>(this IEnumerable<T> src, Func<T, U> f)
        {
            foreach (var i in src) yield return f(i);
        }
    }
}
