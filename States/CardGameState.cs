using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using FontStashSharp;
using System;
using System.Collections.Generic;
using TravelTour.Core;
using TravelTour.UI;

namespace TravelTour.States
{
    public class CardGameState : IGameState
    {
        readonly TravelTourGame _game;
        Texture2D      _pixel  = null!;
        SpriteFontBase _font   = null!, _bigFont = null!;

        // ── Types ─────────────────────────────────────────────
        enum CardType { Story, Character, Event, Boss }

        class StoryCard
        {
            public string   Title   = "";
            public string   Text    = "";
            public string   Icon    = "";
            public CardType Type;
            public Color    Accent;
            public int      ActIndex;   // 0-3
            public bool     IsUnlocked = true;
        }

        // ── Données ───────────────────────────────────────────
        List<StoryCard> _cards = new();
        int  _selectedIdx   = 0;
        int  _handStart     = 0;
        const int HAND_VISIBLE = 5;

        // ── Animation ─────────────────────────────────────────
        float _time;
        float _flipAnim    = 1f;    // 0 = retournée, 1 = face
        int   _flipTarget  = 0;
        bool  _isFlipping  = false;

        // Particules flottantes
        struct Particle { public float X, Y, VX, VY, Life; public Color Col; public float R; }
        List<Particle> _particles = new();
        readonly Random _rng = new();

        // ── Toast ─────────────────────────────────────────────
        string _toast = ""; float _toastTimer; Color _toastColor;

        // ── Boutons ───────────────────────────────────────────
        UIButton _backBtn = null!;
        UIButton _prevBtn = null!, _nextBtn = null!;

        MouseState _prevMs;

        static readonly Color[] ActColors =
        {
            new Color(64, 200, 255),    // Acte I  — bleu clair
            new Color(255, 160, 60),    // Acte II — orange
            new Color(180, 80, 255),    // Acte III — violet
            new Color(240, 60, 60),     // Acte IV — rouge
        };

        public CardGameState(TravelTourGame game) => _game = game;

        // ═════════════════════════════════════════════════════
        public void Load(Texture2D pixel, SpriteFontBase font, SpriteFontBase bigFont)
        {
            _pixel = pixel; _font = font; _bigFont = bigFont;
            BuildCards();

            int W = _game.GraphicsDevice.Viewport.Width;
            int H = _game.GraphicsDevice.Viewport.Height;

            _backBtn = new UIButton(new Rectangle(16, 16, 100, 36), "← Menu",
                () => _game.ChangeState(GameState.MainMenu));
            _prevBtn = new UIButton(new Rectangle(W / 2 - 340, H / 2 - 20, 44, 44), "◀",
                () => Navigate(-1));
            _nextBtn = new UIButton(new Rectangle(W / 2 + 296, H / 2 - 20, 44, 44), "▶",
                () => Navigate(1));
        }

        void BuildCards()
        {
            _cards.Clear();

            // ── ACTE I ────────────────────────────────────────
            _cards.Add(new StoryCard {
                Title = "L'Éveil du Dernier Chasseur", Icon = "😎", Type = CardType.Story, ActIndex = 0,
                Accent = ActColors[0],
                Text = "Kai Shadowstep est le chasseur le plus faible du monde — rang E dans un système qui classe les humains selon leur capacité à combattre les monstres surgissant de portails dimensionnels."
            });
            _cards.Add(new StoryCard {
                Title = "Le Double Portail", Icon = "🌀", Type = CardType.Event, ActIndex = 0,
                Accent = ActColors[0],
                Text = "Lors d'une mission qui devait être simple, Kai est abandonné dans un donjon double-portail par ses coéquipiers. Il faillit mourir dans l'obscurité."
            });
            _cards.Add(new StoryCard {
                Title = "Le Système", Icon = "💻", Type = CardType.Character, ActIndex = 0,
                Accent = ActColors[0],
                Text = "Au lieu de périr, quelque chose s'éveille en lui : le Système — une interface invisible que lui seul peut voir.\n\n\"Quête activée : Devenez le plus fort.\""
            });
            _cards.Add(new StoryCard {
                Title = "Adaptation Infinie", Icon = "⚡", Type = CardType.Boss, ActIndex = 0,
                Accent = ActColors[0],
                Text = "Son pouvoir unique : l'Adaptation Infinie. Il absorbe la force de chaque ennemi vaincu et ne cesse jamais de croître. Chaque défaite le rend plus dangereux."
            });

            // ── ACTE II ───────────────────────────────────────
            _cards.Add(new StoryCard {
                Title = "Le Fruit du Monde", Icon = "🌀", Type = CardType.Event, ActIndex = 1,
                Accent = ActColors[1],
                Text = "En explorant les ruines d'un portail ancien, Kai découvre un fruit étrange — le Sekai Sekai no Mi, Fruit du Monde. Il lui confère le pouvoir d'ouvrir des portails entre dimensions."
            });
            _cards.Add(new StoryCard {
                Title = "Jimmy", Icon = "😎", Type = CardType.Character, ActIndex = 1,
                Accent = ActColors[1],
                Text = "Jimmy — navigateur excentrique au grand cœur. Il pilote le Tommy Mayo, véhicule légendaire capable de traverser les portails. \"Un voyage sans ami, c'est juste une fuite.\""
            });
            _cards.Add(new StoryCard {
                Title = "Sakura Storm", Icon = "🌸", Type = CardType.Character, ActIndex = 1,
                Accent = ActColors[1],
                Text = "Sakura Storm — combattante au souffle de cyclone. Ses poings génèrent des tornades de chakra. Elle rejoint l'équipage après un duel spectaculaire contre Kai."
            });
            _cards.Add(new StoryCard {
                Title = "L'Équipage du Grand Tour", Icon = "🚀", Type = CardType.Story, ActIndex = 1,
                Accent = ActColors[1],
                Text = "Ensemble, Kai, Jimmy, Sakura et Ryo Thunder forment l'équipage du Grand Tour — naviguant à bord du Tommy Mayo à travers les dimensions pour écrire leur propre légende."
            });

            // ── ACTE III ──────────────────────────────────────
            _cards.Add(new StoryCard {
                Title = "Le Royaume des Chakras", Icon = "💫", Type = CardType.Event, ActIndex = 2,
                Accent = ActColors[2],
                Text = "Le portail les propulse dans le Royaume des Chakras — un monde où la force intérieure s'exprime à travers des techniques anciennes transmises de maître à élève."
            });
            _cards.Add(new StoryCard {
                Title = "Rasengan Dimensionnel", Icon = "🌀", Type = CardType.Character, ActIndex = 2,
                Accent = ActColors[2],
                Text = "Kai marie son Adaptation Infinie avec le chakra : naît alors le Rasengan Dimensionnel — une attaque qui déchire l'espace-temps et ouvre des portails offensifs."
            });
            _cards.Add(new StoryCard {
                Title = "Le Syndicat du Néant", Icon = "👹", Type = CardType.Boss, ActIndex = 2,
                Accent = ActColors[2],
                Text = "Douze individus masqués — Le Syndicat du Néant — cherche à capturer tous les utilisateurs de portails pour fusionner les dimensions en un seul chaos absolu."
            });
            _cards.Add(new StoryCard {
                Title = "Alliance Dimensionnelle", Icon = "🤝", Type = CardType.Story, ActIndex = 2,
                Accent = ActColors[2],
                Text = "Face au Syndicat, Kai doit forger des alliances avec les gardiens de chaque dimension. Chaque monde a ses propres règles, ses propres puissances et ses propres sacrifices."
            });

            // ── ACTE IV ───────────────────────────────────────
            _cards.Add(new StoryCard {
                Title = "L'Absolu", Icon = "👑", Type = CardType.Boss, ActIndex = 3,
                Accent = ActColors[3],
                Text = "Au cœur de toutes les dimensions se dresse L'Absolu — un être qui a transcendé tous les systèmes de puissance. Il vainc n'importe quel ennemi d'un seul geste. Il s'ennuie. Il attend."
            });
            _cards.Add(new StoryCard {
                Title = "Dimension Zéro", Icon = "🌌", Type = CardType.Event, ActIndex = 3,
                Accent = ActColors[3],
                Text = "Kai et son équipage affrontent L'Absolu dans la dimension zéro — un espace entre les espaces, où les lois de la physique et du chakra n'existent plus."
            });
            _cards.Add(new StoryCard {
                Title = "Le Vrai But", Icon = "🌅", Type = CardType.Story, ActIndex = 3,
                Accent = ActColors[3],
                Text = "\"Je ne cherche pas à être le plus fort. Je cherche à protéger ce voyage pour tous.\"\n\nL'Absolu est le gardien du Grand Tour. Chaque épreuve n'était qu'un test pour trouver un successeur."
            });
            _cards.Add(new StoryCard {
                Title = "Le Grand Tour", Icon = "🌊", Type = CardType.Character, ActIndex = 3,
                Accent = ActColors[3],
                Text = "L'Absolu sourit pour la première fois depuis un millénaire. Le Grand Tour continue — non plus comme une quête de puissance, mais comme un voyage de découverte et de protection."
            });
        }

        // ── Navigation ────────────────────────────────────────
        void Navigate(int dir)
        {
            int next = Math.Clamp(_selectedIdx + dir, 0, _cards.Count - 1);
            if (next == _selectedIdx) return;
            StartFlip(next);
        }

        void StartFlip(int target)
        {
            _flipTarget  = target;
            _isFlipping  = true;
            _flipAnim    = 1f;
        }

        // ═════════════════════════════════════════════════════
        public void Update(GameTime gt)
        {
            float dt = (float)gt.ElapsedGameTime.TotalSeconds;
            _time        += dt;
            _toastTimer  -= dt;

            // Flip animation
            if (_isFlipping)
            {
                _flipAnim -= dt * 5f;
                if (_flipAnim <= 0f)
                {
                    _selectedIdx = _flipTarget;
                    _flipAnim    = 0f;
                    _isFlipping  = false;
                    SpawnParticles();
                    _flipAnim = 1f;
                }
            }

            // Ajuster le hand scroll
            if (_selectedIdx < _handStart) _handStart = _selectedIdx;
            if (_selectedIdx >= _handStart + HAND_VISIBLE) _handStart = _selectedIdx - HAND_VISIBLE + 1;
            _handStart = Math.Clamp(_handStart, 0, Math.Max(0, _cards.Count - HAND_VISIBLE));

            // Particules
            UpdateParticles(dt);
            if (_rng.NextDouble() < 0.15f) SpawnFloatParticle();

            var kb = Keyboard.GetState();
            if (kb.IsKeyDown(Keys.Left)  && !_prevMs.Equals(Mouse.GetState())) {}
            if (kb.IsKeyDown(Keys.Escape)) { _game.ChangeState(GameState.MainMenu); return; }

            // Keyboard navigation
            bool leftKey  = kb.IsKeyDown(Keys.Left)  || kb.IsKeyDown(Keys.A);
            bool rightKey = kb.IsKeyDown(Keys.Right) || kb.IsKeyDown(Keys.D);

            var ms = Mouse.GetState();

            // Click sur une carte dans la main
            int W = _game.GraphicsDevice.Viewport.Width;
            int H = _game.GraphicsDevice.Viewport.Height;
            int handY = H - 160;
            int cw = 110, ch = 140, hgap = 12;
            int totalHandW = HAND_VISIBLE * (cw + hgap) - hgap;
            int handX = W / 2 - totalHandW / 2;

            for (int i = 0; i < HAND_VISIBLE; i++)
            {
                int cardIdx = _handStart + i;
                if (cardIdx >= _cards.Count) break;
                var r = new Rectangle(handX + i * (cw + hgap), handY, cw, ch);
                if (r.Contains(ms.Position) && ms.LeftButton == ButtonState.Pressed
                    && _prevMs.LeftButton == ButtonState.Released)
                {
                    if (cardIdx != _selectedIdx) StartFlip(cardIdx);
                }
            }

            _prevMs = ms;
            _backBtn.Update(ms);
            _prevBtn.Update(ms);
            _nextBtn.Update(ms);
        }

        void SpawnParticles()
        {
            if (_selectedIdx >= _cards.Count) return;
            var c = _cards[_selectedIdx];
            int W = _game.GraphicsDevice.Viewport.Width;
            int H = _game.GraphicsDevice.Viewport.Height;
            for (int i = 0; i < 20; i++)
            {
                float angle = (float)(_rng.NextDouble() * Math.PI * 2);
                float speed = (float)(_rng.NextDouble() * 120 + 40);
                _particles.Add(new Particle {
                    X = W / 2f, Y = H / 2f - 80,
                    VX = (float)Math.Cos(angle) * speed,
                    VY = (float)Math.Sin(angle) * speed - 60,
                    Life = (float)(_rng.NextDouble() * 1f + 0.5f),
                    Col = c.Accent,
                    R = (float)(_rng.NextDouble() * 4 + 2)
                });
            }
        }

        void SpawnFloatParticle()
        {
            int W = _game.GraphicsDevice.Viewport.Width;
            int H = _game.GraphicsDevice.Viewport.Height;
            if (_selectedIdx >= _cards.Count) return;
            var c = _cards[_selectedIdx];
            _particles.Add(new Particle {
                X = (float)(_rng.NextDouble() * W),
                Y = H + 10,
                VX = (float)((_rng.NextDouble() - 0.5) * 20),
                VY = -(float)(_rng.NextDouble() * 40 + 20),
                Life = (float)(_rng.NextDouble() * 3 + 2),
                Col = c.Accent * 0.5f,
                R = (float)(_rng.NextDouble() * 3 + 1)
            });
        }

        void UpdateParticles(float dt)
        {
            for (int i = _particles.Count - 1; i >= 0; i--)
            {
                var p = _particles[i];
                p.X    += p.VX * dt;
                p.Y    += p.VY * dt;
                p.VY   += 60f * dt;
                p.Life -= dt;
                if (p.Life <= 0) { _particles.RemoveAt(i); continue; }
                _particles[i] = p;
            }
        }

        // ═════════════════════════════════════════════════════
        public void Draw(SpriteBatch sb)
        {
            int W = _game.GraphicsDevice.Viewport.Width;
            int H = _game.GraphicsDevice.Viewport.Height;

            // Fond — grille lumineuse
            DrawBg(sb, W, H);

            // Particules
            foreach (var p in _particles)
            {
                float a = Math.Clamp(p.Life, 0, 1);
                int r = (int)p.R;
                sb.Draw(_pixel, new Rectangle((int)p.X - r, (int)p.Y - r, r * 2, r * 2), p.Col * a);
            }

            // ── Carte centrale ────────────────────────────────
            if (_selectedIdx < _cards.Count)
                DrawMainCard(sb, W, H, _cards[_selectedIdx]);

            // ── Main (rangée de cartes en bas) ────────────────
            DrawHand(sb, W, H);

            // ── UI ────────────────────────────────────────────
            _backBtn.Draw(sb, _pixel, _font, 0.85f);
            _prevBtn.Draw(sb, _pixel, _font, 0.9f);
            _nextBtn.Draw(sb, _pixel, _font, 0.9f);

            // Compteur
            UIHelper.DrawCenteredText(sb, _font, $"{_selectedIdx + 1} / {_cards.Count}",
                new Rectangle(W / 2 - 50, H - 175, 100, 20), UIHelper.TextDim, 0.8f);

            // Indicateur d'acte
            if (_selectedIdx < _cards.Count)
            {
                var card = _cards[_selectedIdx];
                string actLabel = $"ACTE {card.ActIndex + 1}";
                UIHelper.DrawCenteredText(sb, _font, actLabel,
                    new Rectangle(W / 2 - 100, 22, 200, 24), card.Accent, 0.78f);
            }

            DrawToast(sb, W, H);
        }

        void DrawBg(SpriteBatch sb, int W, int H)
        {
            // Dégradé sombre déjà appliqué par le jeu — on ajoute juste une grille subtile
            float gridAlpha = 0.04f;
            int gridSize = 60;
            for (int x = 0; x < W; x += gridSize)
                sb.Draw(_pixel, new Rectangle(x, 0, 1, H), new Color(60, 80, 180) * gridAlpha);
            for (int y = 0; y < H; y += gridSize)
                sb.Draw(_pixel, new Rectangle(0, y, W, 1), new Color(60, 80, 180) * gridAlpha);
        }

        void DrawMainCard(SpriteBatch sb, int W, int H, StoryCard card)
        {
            int cw = 560, ch = 400;
            int cx = W / 2 - cw / 2;
            int cy = H / 2 - ch / 2 - 50;

            // Effet flip (compression horizontale)
            float scaleX = _isFlipping ? Math.Abs(2 * _flipAnim - 1) : 1f;
            int drawW = (int)(cw * scaleX);
            int drawX = W / 2 - drawW / 2;

            if (drawW < 4) return;

            // Fond de la carte
            Color bg = new Color(8, 10, 22);
            sb.Draw(_pixel, new Rectangle(drawX, cy, drawW, ch), bg);

            // Bordure colorée avec glow
            float pulse = (float)(Math.Sin(_time * 2.5) * 0.3 + 0.7);
            DrawGlowBorder(sb, drawX, cy, drawW, ch, card.Accent, pulse);

            // Bande de type en haut
            string typeLabel = card.Type switch {
                CardType.Story     => "📖 HISTOIRE",
                CardType.Character => "👤 PERSONNAGE",
                CardType.Event     => "⚡ ÉVÉNEMENT",
                CardType.Boss      => "💀 BOSS",
                _                  => "?"
            };
            sb.Draw(_pixel, new Rectangle(drawX, cy, drawW, 32), card.Accent * 0.2f);
            UIHelper.DrawCenteredText(sb, _font, typeLabel,
                new Rectangle(drawX, cy + 4, drawW, 24), card.Accent, 0.78f);

            // ── Zone art (icône géant) ───────────────────────
            int artH = 130;
            sb.Draw(_pixel, new Rectangle(drawX, cy + 32, drawW, artH), card.Accent * 0.06f);

            // Icône de fond décoratif (grand, transparent)
            UIHelper.DrawCenteredText(sb, _bigFont, card.Icon,
                new Rectangle(drawX, cy + 38, drawW, artH - 12),
                Color.White * 0.15f, 2.5f);

            // Icône principal
            float iconPulse = 1f + (float)(Math.Sin(_time * 3) * 0.05);
            UIHelper.DrawCenteredText(sb, _bigFont, card.Icon,
                new Rectangle(drawX, cy + 38, drawW, artH - 12),
                Color.White, iconPulse);

            // ── Titre ────────────────────────────────────────
            int titleY = cy + 32 + artH + 10;
            sb.Draw(_pixel, new Rectangle(drawX + 20, titleY - 2, drawW - 40, 1), card.Accent * 0.5f);
            UIHelper.DrawCenteredText(sb, _bigFont, card.Title,
                new Rectangle(drawX, titleY + 4, drawW, 30), card.Accent, 0.62f);

            // ── Texte ─────────────────────────────────────────
            int textY = titleY + 40;
            DrawWrappedText(sb, card.Text,
                drawX + 24, textY, drawW - 48, H - textY - 180, _font, UIHelper.TextMain * 0.9f, 0.82f);

            // ── Badges d'acte ────────────────────────────────
            Color actBadge = ActColors[card.ActIndex];
            string actStr = $"ACTE {card.ActIndex + 1}";
            sb.Draw(_pixel, new Rectangle(drawX + 8, cy + ch - 28, 60, 20), actBadge * 0.18f);
            sb.DrawString(_font, actStr, new Vector2(drawX + 11, cy + ch - 26), actBadge * 0.8f);

            // Numéro de carte (coin bas droit)
            string numStr = $"#{_selectedIdx + 1:D2}";
            sb.DrawString(_font, numStr,
                new Vector2(drawX + drawW - 36, cy + ch - 26), UIHelper.TextDim * 0.6f);
        }

        void DrawHand(SpriteBatch sb, int W, int H)
        {
            int cw = 110, ch = 140, hgap = 12;
            int totalHandW = HAND_VISIBLE * (cw + hgap) - hgap;
            int handX = W / 2 - totalHandW / 2;
            int handY = H - 160;

            var ms = Mouse.GetState();

            for (int i = 0; i < HAND_VISIBLE; i++)
            {
                int cardIdx = _handStart + i;
                if (cardIdx >= _cards.Count) break;

                var card    = _cards[cardIdx];
                bool active = (cardIdx == _selectedIdx);
                bool hov    = new Rectangle(handX + i * (cw + hgap), handY, cw, ch).Contains(ms.Position);

                int drawY = hov || active ? handY - 10 : handY;
                float scale = active ? 1f : 0.9f;
                int dw = (int)(cw * scale), dh = (int)(ch * scale);
                int dx = handX + i * (cw + hgap) + (cw - dw) / 2;

                // Fond
                sb.Draw(_pixel, new Rectangle(dx, drawY, dw, dh), new Color(8, 10, 22));

                // Bordure
                float bPulse = active ? (float)(Math.Sin(_time * 3) * 0.3 + 0.7) : 0.4f;
                DrawGlowBorder(sb, dx, drawY, dw, dh, active ? card.Accent : UIHelper.TextDim, bPulse);

                // Type strip
                sb.Draw(_pixel, new Rectangle(dx, drawY, dw, 16), card.Accent * 0.25f);

                // Icône
                UIHelper.DrawCenteredText(sb, _bigFont, card.Icon,
                    new Rectangle(dx, drawY + 16, dw, 54), Color.White, 0.55f);

                // Titre tronqué
                string shortTitle = card.Title.Length > 14 ? card.Title.Substring(0, 13) + "…" : card.Title;
                UIHelper.DrawCenteredText(sb, _font, shortTitle,
                    new Rectangle(dx, drawY + 70, dw, 30), active ? card.Accent : UIHelper.TextDim, 0.58f);

                // Badge acte
                sb.Draw(_pixel, new Rectangle(dx + 4, drawY + dh - 20, 28, 14), ActColors[card.ActIndex] * 0.2f);
                sb.DrawString(_font, $"A{card.ActIndex + 1}",
                    new Vector2(dx + 7, drawY + dh - 19), ActColors[card.ActIndex] * 0.8f);

                // Sélection highlight
                if (active)
                    sb.Draw(_pixel, new Rectangle(dx, drawY + dh - 3, dw, 3), card.Accent);
            }

            // Flèches de scroll de la main si plus de cartes
            if (_handStart > 0)
                sb.DrawString(_font, "◀", new Vector2(handX - 22, handY + 50), UIHelper.TextDim);
            if (_handStart + HAND_VISIBLE < _cards.Count)
                sb.DrawString(_font, "▶", new Vector2(handX + totalHandW + 6, handY + 50), UIHelper.TextDim);
        }

        void DrawWrappedText(SpriteBatch sb, string text, int x, int y, int maxW, int maxH, SpriteFontBase font, Color col, float scale)
        {
            var words = text.Split(' ');
            string line = "";
            int lineH = (int)(font.MeasureString("Ag").Y) + 4;
            int curY = y;

            foreach (var word in words)
            {
                string test = line.Length == 0 ? word : line + " " + word;
                if (test.Contains('\n'))
                {
                    var parts = test.Split('\n');
                    sb.DrawString(font, parts[0], new Vector2(x, curY), col);
                    curY += lineH;
                    line = parts.Length > 1 ? parts[1] : "";
                    continue;
                }
                float w = font.MeasureString(test).X;
                if (w > maxW && line.Length > 0)
                {
                    if (curY + lineH > y + maxH) { sb.DrawString(font, line + "…", new Vector2(x, curY), col * 0.6f); return; }
                    sb.DrawString(font, line, new Vector2(x, curY), col);
                    curY += lineH;
                    line = word;
                }
                else { line = test; }
            }
            if (line.Length > 0 && curY + lineH <= y + maxH)
                sb.DrawString(font, line, new Vector2(x, curY), col);
        }

        void DrawGlowBorder(SpriteBatch sb, int x, int y, int w, int h, Color col, float alpha)
        {
            sb.Draw(_pixel, new Rectangle(x,       y,       w, 2), col * alpha);
            sb.Draw(_pixel, new Rectangle(x,       y+h-2,   w, 2), col * alpha);
            sb.Draw(_pixel, new Rectangle(x,       y,       2, h), col * alpha);
            sb.Draw(_pixel, new Rectangle(x+w-2,   y,       2, h), col * alpha);
            sb.Draw(_pixel, new Rectangle(x+1,     y+1,     w-2, 1), col * alpha * 0.4f);
            sb.Draw(_pixel, new Rectangle(x+1,     y+h-3,   w-2, 1), col * alpha * 0.4f);
        }

        void DrawToast(SpriteBatch sb, int W, int H)
        {
            if (_toastTimer <= 0) return;
            var ts = _font.MeasureString(_toast);
            UIHelper.DrawBox(sb, _pixel,
                new Rectangle((int)(W/2f - ts.X/2f - 16), H - 60, (int)ts.X + 32, 36),
                UIHelper.Dark2, _toastColor, 1);
            sb.DrawString(_font, _toast, new Vector2(W/2f - ts.X/2f, H - 52), _toastColor);
        }

        public void Dispose() { }
    }
}
