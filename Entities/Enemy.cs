using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FontStashSharp;
using System;
using System.Collections.Generic;
using TravelTour.Core;
using TravelTour.UI;

namespace TravelTour.Entities
{
    public enum EnemyState { Patrol, Chase, Attack, Stunned, Dead }

    public class Enemy
    {
        public Vector2   Position;
        public Rectangle Bounds => new((int)Position.X, (int)Position.Y, 44, 60);
        public float     MaxHP, CurrentHP;
        public float     AttackDamage;
        public float     MoveSpeed;
        public bool      IsAlive => CurrentHP > 0 && State != EnemyState.Dead;
        public EnemyState State = EnemyState.Patrol;

        // Attack hitbox
        public Rectangle AttackBox;
        public bool      AttackActive;

        // Drops
        public int           GoldDrop;
        public MaterialReward[] Drops = Array.Empty<MaterialReward>();
        public string?       FruitDrop;          // nom du fruit droppé (boss only)
        public float         FruitDropChance = 0.25f;  // 25% de chance de drop

        // Ennemi à distance (mage)
        public bool IsRanged = false;

        // Ultime longue distance + burst de vitesse (boss)
        public bool  HasUltimate       = false;
        public float UltimateDamageMult = 1.8f;
        float _ultimateTimer;
        float _speedBurstTimer;
        const float ULTIMATE_CD          = 5f;
        const float SPEED_BURST_DURATION = 0.8f;
        const float SPEED_BURST_MULT     = 1.9f;

        // Events
        public Action<int, Vector2>?  OnGoldDrop;
        public Action<string, int>?   OnMaterialDrop;
        public Action<string>?        OnFruitDrop;
        public Action<Vector2, Vector2, float>? OnRangedAttack;  // (position départ, direction, dégâts) — mages
        public Action<Vector2, Vector2, float>? OnUltimateAttack; // (position départ, direction, dégâts) — boss

        Vector2  _velocity;
        Vector2  _patrolOrigin;
        float    _patrolDir = 1f;
        float    _attackTimer;
        float    _stunTimer;
        float    _flashTimer;
        bool     _grounded;
        float    _deathTimer;          // counts up after death for blink/fade animation
        float    _stateTime;           // total time spent in current state (for animations)
        EnemyState _prevState;

        const float GRAVITY    = 1200f;
        const float PATROL_W   = 120f;
        const float DETECT_R   = 380f;
        const float ATTACK_R   = 65f;
        const float ATTACK_CD  = 1.1f;
        const float RANGED_ATTACK_R  = 320f;
        const float RANGED_ATTACK_CD = 1.6f;

        public void Init(Vector2 pos, float hp = 120f, float atk = 20f, float spd = 80f, int gold = 50)
        {
            Position     = pos;
            _patrolOrigin = pos;
            MaxHP = CurrentHP = hp;
            AttackDamage = atk;
            MoveSpeed    = spd;
            GoldDrop     = gold;
        }

        public void Update(GameTime gt, Vector2 playerPos, List<Rectangle> platforms, Rectangle world)
        {
            float dt = (float)gt.ElapsedGameTime.TotalSeconds;

            // Death animation: keep ticking even when dead
            if (State == EnemyState.Dead)
            {
                _deathTimer += dt;
                return;
            }

            if (!IsAlive) return;

            _attackTimer -= dt;
            _flashTimer  -= dt;
            AttackActive  = false;
            if (_speedBurstTimer > 0) _speedBurstTimer -= dt;

            if (State == EnemyState.Stunned)
            {
                _stunTimer -= dt;
                if (_stunTimer <= 0) State = EnemyState.Patrol;
                ApplyGravity(dt, platforms, world);
                return;
            }

            // Ultime longue distance : indépendant de l'état (mêlée + distance)
            if (HasUltimate)
            {
                _ultimateTimer -= dt;
                if (_ultimateTimer <= 0f)
                {
                    _ultimateTimer = ULTIMATE_CD;
                    Vector2 origin = Position + new Vector2(22, 20);
                    Vector2 dir    = playerPos - origin;
                    dir = dir.LengthSquared() > 0.01f ? Vector2.Normalize(dir) : new Vector2(_patrolDir, 0);
                    OnUltimateAttack?.Invoke(origin, dir, AttackDamage * UltimateDamageMult);
                    _speedBurstTimer = SPEED_BURST_DURATION;
                }
            }

            float dist = Vector2.Distance(Position, playerPos);
            float attackRange = IsRanged ? RANGED_ATTACK_R : ATTACK_R;
            var newState = dist < attackRange ? EnemyState.Attack
                         : dist < DETECT_R    ? EnemyState.Chase
                         : EnemyState.Patrol;
            if (newState != _prevState) { _stateTime = 0f; _prevState = newState; }
            State = newState;
            _stateTime += dt;

            switch (State)
            {
                case EnemyState.Patrol: DoPatrol(dt); break;
                case EnemyState.Chase:  DoChase(dt, playerPos); break;
                case EnemyState.Attack: DoAttack(dt, playerPos); break;
            }

            ApplyGravity(dt, platforms, world);
        }

        void DoPatrol(float dt)
        {
            _velocity.X = _patrolDir * MoveSpeed * 0.5f;
            if (Math.Abs(Position.X - _patrolOrigin.X) >= PATROL_W)
                _patrolDir *= -1;
        }

        void DoChase(float dt, Vector2 target)
        {
            _patrolDir  = target.X > Position.X ? 1f : -1f;
            float mult  = _speedBurstTimer > 0 ? SPEED_BURST_MULT : 1f;
            _velocity.X = _patrolDir * MoveSpeed * mult;
        }

        void DoAttack(float dt, Vector2 target)
        {
            _velocity.X = 0;
            if (_attackTimer > 0) return;
            AttackActive = true;

            if (IsRanged)
            {
                _attackTimer = RANGED_ATTACK_CD;
                Vector2 origin = Position + new Vector2(22, 30);
                Vector2 dir    = target - origin;
                dir = dir.LengthSquared() > 0.01f ? Vector2.Normalize(dir) : new Vector2(_patrolDir, 0);
                OnRangedAttack?.Invoke(origin, dir, AttackDamage);
                return;
            }

            _attackTimer = ATTACK_CD;
            AttackBox = new Rectangle(
                (int)Position.X - 20, (int)Position.Y + 10,
                44 + 40, 40);
        }

        void ApplyGravity(float dt, List<Rectangle> platforms, Rectangle world)
        {
            _velocity.Y += GRAVITY * dt;
            Position    += _velocity * dt;
            _grounded    = false;

            if (Position.Y + 60 >= world.Bottom)
            {
                Position.Y  = world.Bottom - 60;
                _velocity.Y = 0;
                _grounded   = true;
            }
            foreach (var p in platforms)
            {
                if (!Bounds.Intersects(p)) continue;
                if (_velocity.Y > 0 && Position.Y + 60 - _velocity.Y * 0.016f <= p.Y + 4)
                {
                    Position.Y  = p.Y - 60;
                    _velocity.Y = 0;
                    _grounded   = true;
                }
            }

            if (Position.X < world.X) { Position.X = world.X; _patrolDir *= -1; }
            if (Position.X + 44 > world.Right) { Position.X = world.Right - 44; _patrolDir *= -1; }
        }

        public void TakeDamage(float dmg)
        {
            if (!IsAlive) return;
            CurrentHP -= dmg;
            _flashTimer = 0.12f;
            if (CurrentHP <= 0) Die();
        }

        public void Stun(float dur) { State = EnemyState.Stunned; _stunTimer = dur; _velocity = Vector2.Zero; }

        void Die()
        {
            State = EnemyState.Dead;
            OnGoldDrop?.Invoke(GoldDrop, Position);
            var rng = new Random();
            foreach (var d in Drops)
            {
                int qty = rng.Next(d.Min, d.Max + 1);
                if (qty > 0) OnMaterialDrop?.Invoke(d.Material, qty);
            }
            if (FruitDrop != null)
            {
                TravelTour.Core.PlayerSave.IncrementBossKill(FruitDrop);
                int kills = TravelTour.Core.PlayerSave.GetBossKills(FruitDrop);
                if (kills >= TravelTour.Core.PlayerSave.BossKillsRequired
                    && rng.NextDouble() < FruitDropChance)
                    OnFruitDrop?.Invoke(FruitDrop);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Draw — enhanced enemy graphics
        // ─────────────────────────────────────────────────────────────────────
        public bool      IsBoss      = false;
        public string    WeaponName  = "";
        public string    WeaponIcon  = "";
        public WeaponType WeaponKind = WeaponType.Sword;
        public float     WeaponDmg  = 0f;
        public string    SpriteKey   = "";  // clé sprite spécifique (ex: "enemy_ghost")

        public void Draw(SpriteBatch sb, Texture2D pixel)
        {
            // ── Death animation ────────────────────────────────────────────────
            if (State == EnemyState.Dead)
            {
                const float DEATH_DURATION = 1.0f;
                if (_deathTimer >= DEATH_DURATION) return;
                float blinkRate = 8f + _deathTimer * 20f;
                bool visible = (int)(_deathTimer * blinkRate) % 2 == 0;
                if (!visible) return;
                byte alpha = (byte)(255 * (1f - _deathTimer / DEATH_DURATION));
                // Utilise le sprite pour la mort aussi
                var deadSprite = !string.IsNullOrEmpty(SpriteKey)
                    ? TravelTour.Core.SpriteLoader.Get(SpriteKey)
                    : TravelTour.Core.SpriteLoader.Enemy(IsBoss);
                if (deadSprite != null)
                {
                    Color dc = new Color((byte)200, (byte)80, (byte)80, alpha);
                    int dw = IsBoss ? 80 : 56; int dh = IsBoss ? 100 : 76;
                    sb.Draw(deadSprite, new Rectangle((int)Position.X - (dw-44)/2, (int)Position.Y - (dh-60)/2, dw, dh), null, dc, 0f, Vector2.Zero, SpriteEffects.None, 0f);
                }
                else
                {
                    Color deadColor = new Color((byte)160, (byte)40, (byte)40, alpha);
                    sb.Draw(pixel, Bounds, deadColor);
                }
                return;
            }

            if (!IsAlive) return;

            // ── Sprite ────────────────────────────────────────────────────────
            var sprite = !string.IsNullOrEmpty(SpriteKey)
                ? TravelTour.Core.SpriteLoader.Get(SpriteKey)
                : TravelTour.Core.SpriteLoader.Enemy(IsBoss);
            if (sprite != null)
            {
                int sw = IsBoss ? 96 : 60; int sh = IsBoss ? 120 : 80;
                int sx = (int)Position.X - (sw - 44) / 2;
                int sy = (int)Position.Y - (sh - 60) / 2;

                Color tint = _flashTimer > 0 ? new Color(255, 80, 80) :
                             IsRanged && State == EnemyState.Attack ? new Color(190, 130, 255) :
                             IsRanged ? new Color(150, 110, 230) :
                             State == EnemyState.Attack ? new Color(255, 200, 200) :
                             Color.White;

                // Flip selon direction de déplacement
                bool facingLeft = _velocity.X < -0.5f || (_velocity.X == 0 && _patrolDir < 0);
                var flip = facingLeft ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

                // Ombre
                sb.Draw(pixel, new Rectangle(sx + 8, sy + sh - 4, sw - 16, 8), Color.Black * 0.3f);

                sb.Draw(sprite, new Rectangle(sx, sy, sw, sh), null, tint, 0f, Vector2.Zero, flip, 0f);

                // Arme du boss
                if (IsBoss && WeaponName != "")
                    DrawBossWeapon(sb, pixel, sx, sy, sw, sh, facingLeft);

                // Orbe du mage
                if (IsRanged && !IsBoss)
                    DrawMageOrb(sb, pixel, sx, sy, sw, facingLeft);

                // Barre HP au-dessus
                DrawHealthBar(sb, pixel, sx, sy - 10, sw);
                return;
            }

            int  bx = (int)Position.X;
            int  by = (int)Position.Y;
            int  bw = Bounds.Width;   // 44
            int  bh = Bounds.Height;  // 60

            // ── 1. DROP SHADOW ────────────────────────────────────────────────
            Color shadowColor = new Color(0, 0, 0, 60);
            sb.Draw(pixel, new Rectangle(bx + 4, by + bh + 2, bw, 6), shadowColor);

            // Aura d'attaque supprimée (contours retirés)

            // ── 3. BODY — state-dependent color & scale ───────────────────────
            Color  bodyColor;
            int    drawX = bx, drawY = by, drawW = bw, drawH = bh;

            switch (State)
            {
                case EnemyState.Patrol:
                    bodyColor = _flashTimer > 0 ? Color.White : new Color(200, 60, 60);
                    break;

                case EnemyState.Chase:
                    bodyColor = _flashTimer > 0 ? Color.White : new Color(220, 40, 40);
                    break;

                case EnemyState.Attack:
                {
                    // Pulse body size ×1.1 in sync with the aura
                    float pulse = (float)Math.Sin(_stateTime * 8.0) * 0.5f + 0.5f;
                    int   extra = (int)(pulse * bw * 0.10f);  // up to +10%
                    drawX = bx - extra / 2;
                    drawY = by - extra / 2;
                    drawW = bw + extra;
                    drawH = bh + extra;
                    bodyColor = _flashTimer > 0 ? Color.White : new Color(255, 20, 20);
                    break;
                }

                case EnemyState.Stunned:
                    bodyColor = _flashTimer > 0 ? Color.White : new Color(160, 80, 160);
                    break;

                default:
                    bodyColor = new Color(200, 60, 60);
                    break;
            }

            sb.Draw(pixel, new Rectangle(drawX, drawY, drawW, drawH), bodyColor);

            // ── 4. EYES — expressive per state ────────────────────────────────
            {
                int eyeSize, eyeY;
                Color eyeColor;

                if (State == EnemyState.Attack)
                {
                    // Large red eyes in Attack
                    eyeSize  = 10;
                    eyeY     = drawY + 10;
                    eyeColor = new Color(255, 40, 40);
                }
                else if (State == EnemyState.Chase)
                {
                    // Slightly enlarged eyes in Chase
                    eyeSize  = 9;
                    eyeY     = drawY + 9;
                    eyeColor = Color.White;
                }
                else if (State == EnemyState.Stunned)
                {
                    // X-shaped eyes (two pixels) when stunned — drawn as white dots
                    eyeSize  = 6;
                    eyeY     = drawY + 10;
                    eyeColor = new Color(240, 240, 100);
                }
                else
                {
                    eyeSize  = 7;
                    eyeY     = drawY + 10;
                    eyeColor = Color.White;
                }

                // Left eye
                sb.Draw(pixel, new Rectangle(drawX + 7,       eyeY, eyeSize, eyeSize), eyeColor);
                // Right eye
                sb.Draw(pixel, new Rectangle(drawX + drawW - 7 - eyeSize, eyeY, eyeSize, eyeSize), eyeColor);

                // In Attack: add red pupils inside the white eyes
                if (State == EnemyState.Attack)
                {
                    int pupilSize = 4;
                    int pupilOff  = (eyeSize - pupilSize) / 2;
                    Color pupilColor = new Color(140, 0, 0);
                    sb.Draw(pixel, new Rectangle(drawX + 7 + pupilOff, eyeY + pupilOff, pupilSize, pupilSize), pupilColor);
                    sb.Draw(pixel, new Rectangle(drawX + drawW - 7 - eyeSize + pupilOff, eyeY + pupilOff, pupilSize, pupilSize), pupilColor);
                }
            }

            // ── 5. STUNNED STARS — animated yellow squares orbiting above ─────
            if (State == EnemyState.Stunned)
            {
                const int STAR_COUNT  = 3;
                const int ORBIT_R     = 12;
                const int STAR_SIZE   = 6;
                float     angle0      = _stunTimer * 4f; // rotation driven by remaining stun time

                for (int i = 0; i < STAR_COUNT; i++)
                {
                    float a = angle0 + i * (MathHelper.TwoPi / STAR_COUNT);
                    int   sx = bx + bw / 2 + (int)(Math.Cos(a) * ORBIT_R) - STAR_SIZE / 2;
                    int   sy = by - 14    + (int)(Math.Sin(a) * 5);          // slight vertical bob
                    sb.Draw(pixel, new Rectangle(sx, sy, STAR_SIZE, STAR_SIZE), Color.Yellow);
                }
            }

            // ── 6. STATE INDICATOR ICON above the enemy ───────────────────────
            // We simulate a text icon using coloured pixel rectangles (no font needed)
            if (State == EnemyState.Chase || State == EnemyState.Attack)
            {
                // "!" for Chase, "★" approximated for Attack — drawn as a small pixel badge
                int iconX = bx + bw / 2 - 4;
                int iconY = by - 24;

                if (State == EnemyState.Chase)
                {
                    // Exclamation mark: thin column + dot
                    sb.Draw(pixel, new Rectangle(iconX, iconY,      4, 10), Color.Yellow);
                    sb.Draw(pixel, new Rectangle(iconX, iconY + 12, 4, 4),  Color.Yellow);
                }
                else // Attack
                {
                    // Star shape: cross + diagonal squares
                    Color starCol = new Color(255, 80, 80);
                    sb.Draw(pixel, new Rectangle(iconX - 3, iconY + 3,  10, 4),  starCol); // horizontal bar
                    sb.Draw(pixel, new Rectangle(iconX + 1, iconY,       4, 10), starCol); // vertical bar
                    // diagonal corners
                    sb.Draw(pixel, new Rectangle(iconX - 4, iconY,      3, 3),   starCol);
                    sb.Draw(pixel, new Rectangle(iconX + 5, iconY,      3, 3),   starCol);
                    sb.Draw(pixel, new Rectangle(iconX - 4, iconY + 7,  3, 3),   starCol);
                    sb.Draw(pixel, new Rectangle(iconX + 5, iconY + 7,  3, 3),   starCol);
                }
            }

            // ── HP bar ────────────────────────────────────────────────────────
            UIHelper.DrawProgressBar(sb, pixel,
                new Rectangle(bx, by - 10, 44, 6),
                CurrentHP / MaxHP, Color.LimeGreen, new Color(40, 0, 0));
        }

        void DrawBossWeapon(SpriteBatch sb, Texture2D pixel, int sx, int sy, int sw, int sh, bool facingLeft)
        {
            // Position : côté droit du boss (ou gauche si retourné)
            int wx = facingLeft ? sx - 28 : sx + sw + 4;
            int wy = sy + sh / 4;

            // Couleur selon le type d'arme
            Color metalColor  = new Color(180, 190, 210);
            Color bladeColor  = new Color(210, 220, 240);
            Color handleColor = new Color(100, 60, 30);
            Color glowColor   = new Color(180, 30, 30);

            // Lueur d'attaque pulsante
            if (State == EnemyState.Attack)
            {
                float p = (float)System.Math.Abs(System.Math.Sin(_stateTime * 12.0));
                sb.Draw(pixel, new Rectangle(wx - 6, wy - 6, 36, 80), glowColor * (0.4f * p));
            }

            switch (WeaponKind)
            {
                case WeaponType.Sword:
                case WeaponType.Scythe:
                    // Lame : longue bande verticale
                    sb.Draw(pixel, new Rectangle(wx + 10, wy - 30, 10, 70), bladeColor);
                    sb.Draw(pixel, new Rectangle(wx + 11, wy - 32, 8, 4),   new Color(255, 240, 180)); // pointe
                    // Garde (crossguard)
                    sb.Draw(pixel, new Rectangle(wx + 2,  wy + 20, 26, 8),  metalColor);
                    // Manche
                    sb.Draw(pixel, new Rectangle(wx + 12, wy + 28, 6, 20), handleColor);
                    // Reflet tranchant
                    sb.Draw(pixel, new Rectangle(wx + 10, wy - 28, 3, 40), Color.White * 0.5f);
                    if (WeaponKind == WeaponType.Scythe)
                    {
                        // Lame courbe supplémentaire
                        sb.Draw(pixel, new Rectangle(wx - 10, wy - 32, 22, 10), bladeColor);
                        sb.Draw(pixel, new Rectangle(wx - 14, wy - 28, 10, 8),  bladeColor);
                    }
                    break;

                case WeaponType.Staff:
                    // Bâton long
                    sb.Draw(pixel, new Rectangle(wx + 11, wy - 40, 6, 90), handleColor);
                    // Orbe en haut
                    sb.Draw(pixel, new Rectangle(wx + 4,  wy - 54, 20, 20), new Color(120, 40, 180));
                    sb.Draw(pixel, new Rectangle(wx + 7,  wy - 57, 14, 14), new Color(160, 80, 220));
                    sb.Draw(pixel, new Rectangle(wx + 10, wy - 60, 8,  8),  new Color(200, 140, 255));
                    // Lueur orbe
                    float glow = (float)System.Math.Abs(System.Math.Sin(_stateTime * 3.0));
                    sb.Draw(pixel, new Rectangle(wx + 2, wy - 58, 24, 24), new Color(180, 60, 255) * (0.3f + glow * 0.3f));
                    break;

                case WeaponType.Bow:
                    // Arc : deux branches avec une corde
                    sb.Draw(pixel, new Rectangle(wx + 12, wy - 30, 6, 20), handleColor);  // branche haute
                    sb.Draw(pixel, new Rectangle(wx + 12, wy + 10, 6, 20), handleColor);  // branche basse
                    sb.Draw(pixel, new Rectangle(wx + 14, wy - 10, 4, 20), handleColor);  // centre
                    // Courbure (simulée)
                    sb.Draw(pixel, new Rectangle(wx + 4,  wy - 26, 8, 6),  metalColor);
                    sb.Draw(pixel, new Rectangle(wx + 4,  wy + 20, 8, 6),  metalColor);
                    // Corde
                    sb.Draw(pixel, new Rectangle(wx + 6,  wy - 22, 2, 44), new Color(220, 200, 150));
                    break;

                case WeaponType.Gauntlet:
                    // Gantelet : boîte métallique avec pointes
                    sb.Draw(pixel, new Rectangle(wx + 4, wy,     22, 28), metalColor);
                    sb.Draw(pixel, new Rectangle(wx + 6, wy - 2, 18, 4),  bladeColor); // bord supérieur
                    // Pointes sur les jointures
                    for (int i = 0; i < 3; i++)
                        sb.Draw(pixel, new Rectangle(wx + 6 + i * 6, wy - 6, 4, 6), bladeColor);
                    // Reflets
                    sb.Draw(pixel, new Rectangle(wx + 4, wy + 4, 4, 16), Color.White * 0.3f);
                    break;
            }
        }

        void DrawMageOrb(SpriteBatch sb, Texture2D pixel, int sx, int sy, int sw, bool facingLeft)
        {
            int ox = facingLeft ? sx - 10 : sx + sw + 2;
            int oy = sy + 14;
            float pulse = (float)Math.Abs(Math.Sin(_stateTime * 5.0));
            Color glow = new Color(180, 100, 255);

            if (State == EnemyState.Attack)
                sb.Draw(pixel, new Rectangle(ox - 6, oy - 6, 24, 24), glow * (0.25f + pulse * 0.25f));

            sb.Draw(pixel, new Rectangle(ox, oy, 12, 12), glow);
            sb.Draw(pixel, new Rectangle(ox + 3, oy + 3, 6, 6), new Color(230, 200, 255));
        }

        void DrawHealthBar(SpriteBatch sb, Texture2D pixel, int x, int y, int w)
        {
            UIHelper.DrawProgressBar(sb, pixel,
                new Rectangle(x, y, w, 7),
                CurrentHP / MaxHP,
                IsBoss ? Color.OrangeRed : Color.LimeGreen,
                new Color(40, 0, 0));
        }
    }
}
