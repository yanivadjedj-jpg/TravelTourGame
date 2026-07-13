using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FontStashSharp;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using TravelTour.Core;
using TravelTour.UI;

namespace TravelTour.Entities
{
    public class Player
    {
        // ── Stats ──────────────────────────────────────────
        public float MaxHP, CurrentHP;
        public float MaxChakra, CurrentChakra;
        public float BaseAtk, BaseDef, Speed;
        public int   ComboCount;

        // ── Physics ────────────────────────────────────────
        public Vector2 Position;
        public Vector2 Velocity;
        public Rectangle Bounds => new((int)Position.X, (int)Position.Y, 48, 72);

        bool  _isGrounded;
        bool  _facingRight = true;
        int   _jumpsLeft;
        bool  _isDashing;
        float _dashTimer;
        float _dashCooldown;
        float _attackTimer;
        float _comboTimer;
        bool  _prevJump, _prevDash, _prevLight, _prevHeavy;
        bool  _prevQ, _prevE, _prevMastery, _prevTransform;
        float _masteryCd;

        public CharacterData? Character;

        // ── Effet visuel des attaques spéciales (orbe d'énergie) ──
        public string ActiveEffectKey = "";
        float _effectTime;

        // ── Transformation de fruit (sprite + puissance) ──
        bool  _transformActive;
        float _transformTimer;
        float _transformCd;
        const float TRANSFORM_DURATION = 12f;
        const float TRANSFORM_CD       = 20f;

        float TransformMult() => _transformActive
            ? (TravelTour.Core.PlayerSave.GetEquippedFruit()?.TransformAtkMult ?? 1f) : 1f;

        // ── Abilities ──────────────────────────────────────
        public List<AbilityData> Abilities = new();
        float[] _abilityCd = new float[5];

        // ── Fruit moves (R, T, F, G) ──────────────────────
        public float[] FruitMoveCd  = new float[4];
        static readonly Keys[] FruitKeys = { Keys.R, Keys.T, Keys.F, Keys.G };
        bool[] _prevFruitKey = new bool[4];

        // ── Hitbox ────────────────────────────────────────
        public Rectangle AttackBox;
        public float      AttackDamage;
        public bool       AttackActive;
        float _attackBoxTimer;

        // ── Visual ────────────────────────────────────────
        Color _flashColor = Color.White;
        float _flashTimer;
        float _walkTimer;                     // accumulates to animate legs
        int   _particleFrame;                 // golden particle pulse counter

        // ── Toast (UI callback) ───────────────────────────
        public Action<string, Color>? ShowToast;
        public Action<string, Color>? OnAbilityUsed;  // (nom capacité, couleur)

        const float GRAVITY       = 1400f;
        const float JUMP_FORCE    = 560f;
        const float DASH_FORCE    = 700f;
        const float DASH_DURATION = 0.15f;
        const float DASH_CD       = 0.8f;
        const float ATK_CD        = 0.30f;
        const float PLAYER_DMG    = 0.22f;  // multiplicateur dégâts attaques normales
        const float ABILITY_DMG   = 0.20f;  // multiplicateur dégâts capacités/fruits

        // ── Init ──────────────────────────────────────────
        public void Init(CharacterData c)
        {
            Character = c;
            MaxHP = MaxChakra = 0;
            MaxHP     = (c.ScaledHP()  + TravelTour.Core.PlayerSave.LevelHpBonus())
                        * TravelTour.Core.PlayerSave.DefenseBonus()
                        * TravelTour.Core.PlayerSave.ArtifactMult(TravelTour.Core.ArtifactEffect.HpBoost);
            CurrentHP = MaxHP;
            MaxChakra = c.MaxChakra;
            CurrentChakra = MaxChakra;
            BaseAtk = (c.ScaledAtk() + TravelTour.Core.PlayerSave.LevelAtkBonus())
                      * TravelTour.Core.PlayerSave.MeleeDmgBonus()
                      * TravelTour.Core.PlayerSave.ArtifactMult(TravelTour.Core.ArtifactEffect.AtkBoost)
                      * c.MasteryAtkMult();
            BaseDef = (c.ScaledDef() + TravelTour.Core.PlayerSave.LevelDefBonus())
                      * TravelTour.Core.PlayerSave.DefenseBonus()
                      * TravelTour.Core.PlayerSave.ArtifactMult(TravelTour.Core.ArtifactEffect.DefBoost);
            Speed   = c.BaseSpeed * 60f
                      * TravelTour.Core.PlayerSave.SpeedBonus()
                      * TravelTour.Core.PlayerSave.ArtifactMult(TravelTour.Core.ArtifactEffect.SpeedBoost);
        }

        // ── Update ────────────────────────────────────────
        public void Update(GameTime gt, KeyboardState kb, MouseState ms,
            Rectangle worldBounds, List<Rectangle> platforms)
        {
            float dt = (float)gt.ElapsedGameTime.TotalSeconds;

            _dashTimer    -= dt;
            _dashCooldown -= dt;
            _attackTimer  -= dt;
            _comboTimer   -= dt;
            _flashTimer   -= dt;
            _masteryCd    -= dt;
            _effectTime   += dt;
            if (_transformActive)
            {
                _transformTimer -= dt;
                if (_transformTimer <= 0f) { _transformActive = false; _transformCd = TRANSFORM_CD; }
            }
            else if (_transformCd > 0) _transformCd -= dt;
            _attackBoxTimer -= dt;
            _walkTimer    += dt;
            _particleFrame = (int)(_walkTimer * 10f) % 4;
            if (_attackBoxTimer <= 0) AttackActive = false;
            if (!AttackActive) ActiveEffectKey = "";
            if (_comboTimer <= 0) ComboCount = 0;

            for (int i = 0; i < _abilityCd.Length; i++)
                if (_abilityCd[i] > 0) _abilityCd[i] -= dt;
            for (int i = 0; i < FruitMoveCd.Length; i++)
                if (FruitMoveCd[i] > 0) FruitMoveCd[i] -= dt;

            // Chakra regen
            CurrentChakra = Math.Min(MaxChakra, CurrentChakra + 6f * dt);

            HandleMovement(kb, dt);
            ApplyGravity(dt);
            ResolveCollisions(platforms, worldBounds);
            HandleAttack(kb, ms);
        }

        void HandleMovement(KeyboardState kb, float dt)
        {
            if (_isDashing) return;

            float dx = 0;
            if (kb.IsKeyDown(Keys.A) || kb.IsKeyDown(Keys.Left))  dx = -1;
            if (kb.IsKeyDown(Keys.D) || kb.IsKeyDown(Keys.Right)) dx =  1;

            float spdMult = _transformActive
                ? (TravelTour.Core.PlayerSave.GetEquippedFruit()?.TransformSpeedMult ?? 1f) : 1f;
            Velocity.X = dx * Speed * spdMult;
            if (dx > 0) _facingRight = true;
            if (dx < 0) _facingRight = false;

            // Jump (W or Up or Space)
            bool jumpNow = kb.IsKeyDown(Keys.W) || kb.IsKeyDown(Keys.Up) || kb.IsKeyDown(Keys.Space);
            if (jumpNow && !_prevJump && _jumpsLeft > 0)
            {
                Velocity.Y = -JUMP_FORCE;
                _jumpsLeft--;
                _isGrounded = false;
            }
            _prevJump = jumpNow;

            // Dash (LeftShift)
            bool dashNow = kb.IsKeyDown(Keys.LeftShift);
            if (dashNow && !_prevDash && _dashCooldown <= 0 && !_isDashing)
                StartDash();
            _prevDash = dashNow;
        }

        void StartDash()
        {
            _isDashing    = true;
            _dashTimer    = DASH_DURATION;
            _dashCooldown = DASH_CD;
            Velocity.X    = (_facingRight ? 1 : -1) * DASH_FORCE;
            Velocity.Y    = 0;
            ShowToast?.Invoke("DASH!", UIHelper.Blue);
        }

        void ApplyGravity(float dt)
        {
            if (_isDashing && _dashTimer > 0)
            {
                Velocity.Y = 0;
                if (_dashTimer <= 0) _isDashing = false;
            }
            else
            {
                Velocity.Y += GRAVITY * dt;
            }
            Position += Velocity * dt;
        }

        void ResolveCollisions(List<Rectangle> platforms, Rectangle world)
        {
            _isGrounded = false;

            // World bounds
            if (Position.X < world.X) { Position.X = world.X; Velocity.X = 0; }
            if (Position.X + 48 > world.Right) { Position.X = world.Right - 48; Velocity.X = 0; }
            if (Position.Y < world.Y) { Position.Y = world.Y; Velocity.Y = 0; }
            if (Position.Y + 72 > world.Bottom)
            {
                Position.Y = world.Bottom - 72;
                Velocity.Y = 0;
                Land();
            }

            foreach (var p in platforms)
            {
                if (!Bounds.Intersects(p)) continue;
                // only land from above
                if (Velocity.Y > 0 && Position.Y + 72 - Velocity.Y * 0.016f <= p.Y + 4)
                {
                    Position.Y = p.Y - 72;
                    Velocity.Y = 0;
                    Land();
                }
            }
        }

        void Land()
        {
            if (!_isGrounded)
            {
                _isGrounded = true;
                _jumpsLeft  = 2;
                _isDashing  = false;
            }
        }

        void HandleAttack(KeyboardState kb, MouseState ms)
        {
            if (_attackTimer > 0) return;

            // Light attack: Z or Left click
            bool la = kb.IsKeyDown(Keys.Z) || ms.LeftButton == ButtonState.Pressed;
            if (la && !_prevLight)
            {
                ComboCount++;
                _comboTimer = 1.2f;
                float dmg = BaseAtk * (ComboCount >= 3 ? 1.3f : 1f) * TravelTour.Core.PlayerSave.MeleeDmgBonus() * PLAYER_DMG * TransformMult();
                TriggerAttack(dmg, 60, ATK_CD);
                if (ComboCount >= 3) ShowToast?.Invoke($"x{ComboCount} COMBO!", UIHelper.Gold);
                if (ComboCount > TravelTour.Core.PlayerSave.MaxComboReached)
                    TravelTour.Core.PlayerSave.MaxComboReached = ComboCount;
            }
            _prevLight = la;

            // Heavy attack: X or Right click
            bool ha = kb.IsKeyDown(Keys.X) || ms.RightButton == ButtonState.Pressed;
            if (ha && !_prevHeavy)
            {
                float dmg = BaseAtk * 1.6f * TravelTour.Core.PlayerSave.SwordDmgBonus() * PLAYER_DMG * TransformMult();
                TriggerAttack(dmg, 80, ATK_CD * 1.8f);
                ComboCount = 0;
            }
            _prevHeavy = ha;

            // Ability 1: Q
            bool q1 = kb.IsKeyDown(Keys.Q);
            if (q1 && !_prevQ) UseAbility(0);
            _prevQ = q1;

            // Ability 2: E
            bool q2 = kb.IsKeyDown(Keys.E);
            if (q2 && !_prevE) UseAbility(1);
            _prevE = q2;

            // Fruit moves: R, T, F, G
            UseFruitMoves(kb);

            // Ultime de maîtrise : C (débloqué à la maîtrise Platine du perso actif)
            bool mc = kb.IsKeyDown(Keys.C);
            if (mc && !_prevMastery) UseMasteryUltimate();
            _prevMastery = mc;

            // Transformation de fruit : V (nécessite maîtrise 600 du fruit équipé)
            bool tv = kb.IsKeyDown(Keys.V);
            if (tv && !_prevTransform) ToggleTransform();
            _prevTransform = tv;
        }

        void ToggleTransform()
        {
            if (_transformActive) return;
            var fruit = TravelTour.Core.PlayerSave.GetEquippedFruit();
            if (fruit == null || !fruit.CanTransform)
            {
                ShowToast?.Invoke("Aucun fruit transformable équipé", Color.Gray);
                return;
            }
            if (fruit.Mastery < 600)
            {
                ShowToast?.Invoke($"🔒 Maîtrise 600 requise pour {fruit.Name}", Color.Gray);
                return;
            }
            if (_transformCd > 0)
            {
                ShowToast?.Invoke($"Transformation en recharge ({_transformCd:F0}s)", Color.Yellow);
                return;
            }
            _transformActive = true;
            _transformTimer  = TRANSFORM_DURATION;
            ShowToast?.Invoke($"🔥 TRANSFORMATION : {fruit.Name} !", new Color(255, 120, 40));
            OnAbilityUsed?.Invoke($"🔥 Transformation {fruit.Name}", new Color(255, 120, 40));
        }

        void UseMasteryUltimate()
        {
            if (Character == null || !Character.MasteryUltimateUnlocked) return;
            if (_masteryCd > 0)
            {
                ShowToast?.Invoke($"{Character.MasteryUltimateName} en recharge ({_masteryCd:F0}s)", Color.Yellow);
                return;
            }
            _masteryCd = 8f;
            AttackDamage = BaseAtk * 2.2f * TravelTour.Core.PlayerSave.MeleeDmgBonus() * PLAYER_DMG * TransformMult();
            AttackActive = true;
            _attackBoxTimer = 0.4f;
            AttackBox = new Rectangle((int)Position.X - 130, (int)Position.Y - 130, 300, 300);
            ActiveEffectKey = "gold";
            _effectTime = 0f;
            Flash(new Color(255, 215, 0));
            ShowToast?.Invoke($"⚡ {Character.MasteryUltimateName}!", new Color(255, 215, 0));
            OnAbilityUsed?.Invoke($"⚡ {Character.MasteryUltimateName}", new Color(255, 215, 0));
        }

        void UseFruitMoves(KeyboardState kb)
        {
            var fruit = TravelTour.Core.PlayerSave.GetEquippedFruit();
            if (fruit == null) return;

            for (int i = 0; i < 4 && i < fruit.Moves.Length; i++)
            {
                bool pressed = kb.IsKeyDown(FruitKeys[i]);
                if (!pressed || _prevFruitKey[i]) { _prevFruitKey[i] = pressed; continue; }
                _prevFruitKey[i] = true;

                var move = fruit.Moves[i];

                if (FruitMoveCd[i] > 0)
                {
                    ShowToast?.Invoke($"{move.Icon} {move.Name} ({FruitMoveCd[i]:F0}s)", new Color(200, 200, 80));
                    continue;
                }
                if (move.MasteryReq > fruit.Mastery)
                {
                    ShowToast?.Invoke($"🔒 Maîtrise {move.MasteryReq} requise", Color.Gray);
                    continue;
                }
                if (CurrentChakra < move.ChakraCost)
                {
                    ShowToast?.Invoke("Chakra insuffisant !", Color.Red);
                    continue;
                }

                CurrentChakra -= move.ChakraCost;
                FruitMoveCd[i] = move.Cooldown;

                // Appliquer l'effet du move
                float dmg = move.Damage * TravelTour.Core.PlayerSave.FruitDmgBonus() * ABILITY_DMG * TransformMult();
                if (dmg > 0)
                {
                    AttackDamage  = dmg;
                    AttackActive  = true;
                    _attackBoxTimer = 0.4f;
                    AttackBox = new Rectangle((int)Position.X - 120, (int)Position.Y - 120, 290, 290);
                    Flash(new Color(255, 140, 0));
                    ActiveEffectKey = "orange";
                    _effectTime = 0f;
                }
                ShowToast?.Invoke($"{fruit.Icon} {move.Icon} {move.Name}!", new Color(255, 140, 0));
                // Pas de gros plan caméra sur R (move de base, trop fréquent)
                if (i > 0) OnAbilityUsed?.Invoke($"{fruit.Icon} {move.Name}", new Color(255, 140, 0));
                TravelTour.Core.PlayerSave.AddFruitMastery(fruit.Name, 2);
            }
        }

        void TriggerAttack(float dmg, int reach, float cd)
        {
            AttackDamage = dmg;
            AttackActive = true;
            _attackTimer = cd;
            _attackBoxTimer = 0.15f;
            int dir = _facingRight ? 1 : -1;
            AttackBox = new Rectangle(
                (int)Position.X + (dir > 0 ? 48 : -reach),
                (int)Position.Y + 20,
                reach, 40);
            Flash(Color.White);
        }

        void UseAbility(int idx)
        {
            if (idx >= Abilities.Count) return;
            var ab = Abilities[idx];
            if (_abilityCd[idx] > 0)
            {
                ShowToast?.Invoke($"{ab.Name} en recharge ({_abilityCd[idx]:F0}s)...", Color.Yellow);
                return;
            }
            if (CurrentChakra < ab.ChakraCost)
            {
                ShowToast?.Invoke("Chakra insuffisant!", Color.Red);
                return;
            }
            CurrentChakra -= ab.ChakraCost;
            _abilityCd[idx] = ab.Cooldown;
            AttackDamage = ab.Damage * TravelTour.Core.PlayerSave.FruitDmgBonus() * ABILITY_DMG * TransformMult();
            AttackActive = true;
            _attackBoxTimer = ab.Duration > 0 ? Math.Min(ab.Duration, 1f) : 0.3f;
            AttackBox = new Rectangle((int)Position.X - 150, (int)Position.Y - 150, 300 + 48, 300 + 72);
            ActiveEffectKey = "purple";
            _effectTime = 0f;
            ShowToast?.Invoke($"⚡ {ab.Name}!", UIHelper.Purple);
            OnAbilityUsed?.Invoke($"{ab.Icon} {ab.Name}", UIHelper.Purple);
        }

        public void DrawEffect(SpriteBatch sb)
        {
            if (!AttackActive || ActiveEffectKey == "") return;
            var tex = TravelTour.Core.SpriteLoader.Effect(ActiveEffectKey);
            if (tex == null) return;

            float pulse = 1f + (float)Math.Sin(_effectTime * 14.0) * 0.08f;
            float fade  = Math.Clamp(_attackBoxTimer / 0.2f, 0f, 1f);
            int size = (int)(Math.Min(AttackBox.Width, AttackBox.Height) * 0.7f * pulse);
            var dest = new Rectangle(
                AttackBox.Center.X - size / 2,
                AttackBox.Center.Y - size / 2,
                size, size);
            sb.Draw(tex, dest, Color.White * fade);
        }

        public void TakeDamage(float amount)
        {
            float reduced = Math.Max(1f, amount - BaseDef * 0.4f);
            CurrentHP = Math.Max(0f, CurrentHP - reduced);
            Flash(Color.Red);
        }

        public bool IsAlive => CurrentHP > 0;

        void Flash(Color c) { _flashColor = c; _flashTimer = 0.1f; }

        // ── Draw ──────────────────────────────────────────
        public void Draw(SpriteBatch sb, Texture2D pixel)
        {
            int px = (int)Position.X;
            int py = (int)Position.Y;
            int dir = _facingRight ? 1 : -1;

            bool isAttacking = _flashTimer > 0 && _flashColor == Color.White;
            bool isDamaged   = _flashTimer > 0 && _flashColor == Color.Red;

            // ── Aura de transformation (derrière le sprite) ──
            if (_transformActive)
            {
                var fruit = TravelTour.Core.PlayerSave.GetEquippedFruit();
                var aura  = fruit != null ? TravelTour.Core.SpriteLoader.Get(fruit.TransformAuraKey) : null;
                if (aura != null)
                {
                    float apulse = 1f + (float)Math.Sin(_effectTime * 6.0) * 0.06f;
                    int asize = (int)(96 * apulse);
                    sb.Draw(aura, new Rectangle(px + 24 - asize / 2, py + 30 - asize / 2, asize, asize),
                        Color.White * 0.85f);
                }
            }

            // ── Sprite ────────────────────────────────────
            var sprite = TravelTour.Core.SpriteLoader.Player(isAttacking);
            if (sprite != null)
            {
                Color tint = isDamaged      ? new Color(255, 80, 80)   :
                             _transformActive ? new Color(255, 190, 120) :
                             isAttacking    ? new Color(255, 255, 180)  :
                             Color.White;
                var flip = _facingRight ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
                sb.Draw(sprite, new Rectangle(px - 8, py - 8, 64, 88), null, tint, 0f, Vector2.Zero, flip, 0f);
                // Ombre
                sb.Draw(pixel, new Rectangle(px + 4, py + 76, 40, 8), Color.Black * 0.3f);
                return;
            }

            // ── 1. Ombre portée (fallback) ────────────────
            sb.Draw(pixel,
                new Rectangle(px + 4, py + 68, 40, 8),
                Color.Black * 0.35f);

            // ── 2. Aura DASH (anneaux concentriques bleus) ─
            if (_isDashing)
            {
                for (int i = 4; i >= 1; i--)
                {
                    float alpha = 0.08f * i;
                    int pad = i * 5;
                    sb.Draw(pixel,
                        new Rectangle(px - pad, py - pad, 48 + pad * 2, 72 + pad * 2),
                        new Color(60, 160, 255) * alpha);
                }
            }

            // ── 3. Aura ATTAQUE (flash blanc + particules dorées) ──
            if (isAttacking)
            {
                sb.Draw(pixel,
                    new Rectangle(px - 6, py - 6, 60, 84),
                    Color.White * 0.25f);

                // Particules dorées : 4 coins en rotation simulée
                int[] pxOff = { -8, 56, -10, 56 };
                int[] pyOff = { -8, -8,  74, 74 };
                int[] pSize = { 6, 5, 5, 6 };
                for (int i = 0; i < 4; i++)
                {
                    int pf = (_particleFrame + i) % 4;
                    int ox = pxOff[pf];
                    int oy = pyOff[pf];
                    sb.Draw(pixel,
                        new Rectangle(px + ox, py + oy, pSize[i], pSize[i]),
                        UIHelper.Gold * 0.8f);
                }
            }

            // ── 4. Corps principal ─────────────────────────

            // Couleur de base selon l'état
            Color skinColor  = new Color(220, 185, 145);
            Color bodyColor  = isDamaged ? Color.Red :
                               isAttacking ? Color.White :
                               new Color(80, 130, 220);
            Color shirtColor = isDamaged ? new Color(180, 50, 50) :
                               isAttacking ? new Color(255, 230, 100) :
                               new Color(55, 100, 190);
            Color hairColor  = new Color(40, 25, 15);
            Color pantsColor = new Color(50, 50, 120);

            // -- JAMBES animées (2 rectangles qui alternent) --
            // Oscillation en fonction de _walkTimer et de la vitesse
            bool moving = Math.Abs(Velocity.X) > 5f && _isGrounded;
            float legSwing = moving ? (float)Math.Sin(_walkTimer * 10.0) * 6f : 0f;

            // Jambe gauche
            sb.Draw(pixel,
                new Rectangle(px + 4,  py + 42 + (int)legSwing,  16, 28),
                pantsColor);
            // Jambe droite
            sb.Draw(pixel,
                new Rectangle(px + 28, py + 42 - (int)legSwing,  16, 28),
                pantsColor);

            // Chaussures (pied de chaque jambe)
            sb.Draw(pixel,
                new Rectangle(px + 2,  py + 66 + (int)legSwing,  18, 6),
                new Color(40, 30, 20));
            sb.Draw(pixel,
                new Rectangle(px + 26, py + 66 - (int)legSwing,  18, 6),
                new Color(40, 30, 20));

            // -- TORSE / corps (plus étroit que la hauteur totale) --
            sb.Draw(pixel,
                new Rectangle(px + 6, py + 22, 36, 22),
                shirtColor);

            // Détail col / ligne centrale chemise
            sb.Draw(pixel,
                new Rectangle(px + 21, py + 22, 4, 22),
                bodyColor * 0.5f);

            // Bras gauche
            sb.Draw(pixel,
                new Rectangle(px + 0, py + 22, 8, 18),
                skinColor);
            // Bras droit
            sb.Draw(pixel,
                new Rectangle(px + 40, py + 22, 8, 18),
                skinColor);

            // -- TÊTE (carré légèrement arrondi simulé) --
            // Fond de la tête (peau)
            sb.Draw(pixel,
                new Rectangle(px + 10, py + 2, 28, 22),
                skinColor);
            // Coins arrondis simulés : masquer 2px aux coins avec fond transparent
            // (on dessine par-dessus avec la couleur du fond — pas nécessaire en SpriteBatch)

            // -- CHEVEUX --
            // Masse principale des cheveux (dessus de la tête)
            sb.Draw(pixel,
                new Rectangle(px + 10, py + 0, 28, 8),
                hairColor);
            // Frange côté direction
            if (_facingRight)
                sb.Draw(pixel, new Rectangle(px + 30, py + 6, 10, 6), hairColor);
            else
                sb.Draw(pixel, new Rectangle(px + 8,  py + 6, 10, 6), hairColor);
            // Nuque
            sb.Draw(pixel,
                new Rectangle(px + 10, py + 18, 6, 6),
                hairColor);

            // -- YEUX --
            int eyeLX = _facingRight ? px + 14 : px + 18;
            int eyeRX = _facingRight ? px + 21 : px + 25;
            int eyeY  = py + 9;

            // Blancs
            sb.Draw(pixel, new Rectangle(eyeLX,     eyeY, 5, 5), Color.White);
            sb.Draw(pixel, new Rectangle(eyeRX + 2, eyeY, 5, 5), Color.White);
            // Pupilles (décalées vers la direction du regard)
            int pupilOff = _facingRight ? 2 : 1;
            sb.Draw(pixel, new Rectangle(eyeLX     + pupilOff, eyeY + 1, 3, 3), new Color(30, 20, 10));
            sb.Draw(pixel, new Rectangle(eyeRX + 2 + pupilOff, eyeY + 1, 3, 3), new Color(30, 20, 10));

            // ── 5. Indicateur de direction (flèche devant) ──
            int arrowX = _facingRight ? px + 50 : px - 8;
            // Corps de la flèche
            sb.Draw(pixel,
                new Rectangle(arrowX, py + 32, 6, 4),
                UIHelper.Gold * 0.9f);
            // Pointe (triangle simulé : 3 pixels décroissants)
            int tip = _facingRight ? arrowX + 6 : arrowX - 3;
            sb.Draw(pixel, new Rectangle(tip,     py + 31, 3, 6), UIHelper.Gold);
            sb.Draw(pixel, new Rectangle(tip + (_facingRight ? 3 : -2), py + 32, 2, 4), UIHelper.Gold);

            // ── 6. Dash trail (traîne fantôme) ────────────
            if (_isDashing)
            {
                for (int i = 1; i <= 4; i++)
                {
                    float alpha = 0.18f - i * 0.04f;
                    int ox = -dir * i * 14;
                    sb.Draw(pixel,
                        new Rectangle(px + ox + 6, py + 22, 36, 22),
                        new Color(60, 160, 255) * alpha);
                }
            }

            // ── 7. Barre HP flottante au-dessus du joueur ──
            int barW   = 44;
            int barH   = 5;
            int barX   = px + 2;
            int barY   = py - 14;
            float hpPct = MaxHP > 0 ? CurrentHP / MaxHP : 1f;
            int   fillW = (int)(barW * hpPct);

            // Fond noir
            sb.Draw(pixel, new Rectangle(barX - 1, barY - 1, barW + 2, barH + 2), Color.Black * 0.7f);
            // Partie rouge (manque de vie)
            sb.Draw(pixel, new Rectangle(barX, barY, barW, barH), new Color(160, 30, 30));
            // Partie verte (vie restante)
            if (fillW > 0)
            {
                Color hpColor = hpPct > 0.5f ? new Color(60, 200, 60) :
                                hpPct > 0.25f ? new Color(220, 180, 0) :
                                new Color(220, 50, 50);
                sb.Draw(pixel, new Rectangle(barX, barY, fillW, barH), hpColor);
            }
            // Reflet brillant (ligne claire en haut de la barre)
            sb.Draw(pixel, new Rectangle(barX, barY, fillW, 1), Color.White * 0.4f);

            // ── 8. Hitbox d'attaque (debug visuel) ─────────
            if (AttackActive)
                sb.Draw(pixel, AttackBox, UIHelper.Gold * 0.35f);
        }

        public string GetAbilityCdText(int idx)
        {
            if (idx >= Abilities.Count) return "";
            return _abilityCd[idx] > 0 ? $"{_abilityCd[idx]:F0}s" : "PRÊT";
        }
        public float GetAbilityCdPct(int idx)
        {
            if (idx >= Abilities.Count) return 0f;
            return _abilityCd[idx] / Math.Max(Abilities[idx].Cooldown, 1f);
        }
    }
}
