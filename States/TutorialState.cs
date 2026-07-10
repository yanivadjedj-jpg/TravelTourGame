using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using FontStashSharp;
using System.Collections.Generic;
using TravelTour.Core;
using TravelTour.UI;

namespace TravelTour.States
{
    public class TutorialState : IGameState
    {
        readonly TravelTourGame _game;
        Texture2D _pixel = null!;
        SpriteFontBase _font = null!, _bigFont = null!;

        int   _page;
        float _time;
        float _demoTimer;
        UIButton _nextBtn = null!, _skipBtn = null!, _prevBtn = null!;
        bool _prevNext, _prevSkip, _prevPrev;

        // Mini demo player animation
        float _demoX = 200f, _demoY = 340f, _demoVelY;
        bool  _demoGrounded = true;
        float _demoAtk;
        bool  _demoDash;
        float _demoDashTimer;

        static readonly TutorialPage[] Pages = {
            new(){
                Title    = "BIENVENUE DANS TRAVEL TOUR",
                Subtitle = "Un tutoriel rapide pour apprendre à jouer",
                Icon     = "🎮",
                Lines    = new[]{"Travel Tour est un jeu d'action-aventure 2D.",
                                 "Tu vas explorer des donjons, combattre des ennemis",
                                 "et devenir le chasseur le plus puissant du multivers.",
                                 "","Appuie sur SUIVANT pour commencer le tutoriel."},
                DemoType = DemoAnim.Idle
            },
            new(){
                Title    = "SE DÉPLACER",
                Subtitle = "Contrôles de mouvement",
                Icon     = "🏃",
                Lines    = new[]{
                    "  A  ou  ←   →  D  ou  →    Déplacer à gauche / droite",
                    "",
                    "  W  ou  ↑  ou  ESPACE        Sauter",
                    "",
                    "  Tu peux sauter 2 fois de suite (double saut) !",
                    "",
                    "  SHIFT gauche                 Dash (esquive rapide)"},
                DemoType = DemoAnim.Move
            },
            new(){
                Title    = "ATTAQUER",
                Subtitle = "Système de combat",
                Icon     = "⚔️",
                Lines    = new[]{
                    "  Z  ou  Clic Gauche    Attaque légère  (+combo si enchaîné)",
                    "",
                    "  X  ou  Clic Droit     Attaque lourde  (×2 dégâts)",
                    "",
                    "  Q                     Capacité spéciale 1",
                    "  E                     Capacité spéciale 2",
                    "",
                    "  Enchaîne 3 attaques légères pour un COMBO ×1.5 !"},
                DemoType = DemoAnim.Attack
            },
            new(){
                Title    = "LES DONJONS",
                Subtitle = "Entraînement et Histoire",
                Icon     = "🏰",
                Lines    = new[]{
                    "  Clique sur ENTRAÎNEMENT pour choisir un donjon.",
                    "",
                    "  Clique sur HISTOIRE pour jouer les chapitres de l'aventure.",
                    "",
                    "  Dans un donjon :",
                    "    • Bat toutes les vagues d'ennemis",
                    "    • Affronte le Boss final",
                    "    • Récupère des matériaux pour améliorer ton équipement"},
                DemoType = DemoAnim.Idle
            },
            new(){
                Title    = "AUTRES FONCTIONNALITÉS",
                Subtitle = "Personnalisation et équipe",
                Icon     = "✨",
                Lines    = new[]{
                    "  MY TEAM       →  Choisir 3 personnages pour ton équipe",
                    "  BOUTIQUE      →  Acheter et améliorer armes, skins, véhicules",
                    "  CROSSPARK     →  Faire des figures à moto pour gagner de l'or",
                    "  ARRIÈRE PLAN  →  Choisir le fond de ta page d'accueil",
                    "",
                    "  Tue des ennemis pour gagner de l'or et des matériaux.",
                    "  Utilise-les à la Boutique pour devenir plus fort !"},
                DemoType = DemoAnim.Idle
            },
            new(){
                Title    = "PRÊT À JOUER !",
                Subtitle = "L'aventure commence maintenant",
                Icon     = "🚀",
                Lines    = new[]{
                    "  Tu connais maintenant les bases de Travel Tour.",
                    "",
                    "  Conseil : commence par HISTOIRE → Acte I",
                    "  pour apprendre les mécaniques en jouant.",
                    "",
                    "  Bonne chance, Chasseur !"},
                DemoType = DemoAnim.Idle
            },
        };

        public TutorialState(TravelTourGame game) => _game = game;

        public void Load(Texture2D pixel, SpriteFontBase font, SpriteFontBase bigFont)
        {
            _pixel = pixel; _font = font; _bigFont = bigFont;
            RebuildBtns();
        }

        void RebuildBtns()
        {
            int W = _game.GraphicsDevice.Viewport.Width;
            int H = _game.GraphicsDevice.Viewport.Height;
            bool last = _page == Pages.Length - 1;

            _prevBtn = new UIButton(new Rectangle(W/2-280, H-66, 120, 46), "◀ Préc.",
                () => { if (_page > 0) { _page--; RebuildBtns(); } });
            _nextBtn = new UIButton(new Rectangle(W/2-60,  H-66, 160, 46),
                last ? "🎮 JOUER !" : "Suivant ▶",
                () => {
                    if (last) _game.ChangeState(GameState.MainMenu);
                    else { _page++; RebuildBtns(); }
                }, last ? new Color(60,20,0) : UIHelper.CardBg);
            _skipBtn = new UIButton(new Rectangle(W/2+120, H-66, 120, 46), "Passer",
                () => _game.ChangeState(GameState.MainMenu));

            _prevBtn.Enabled = _page > 0;
            if (last) _nextBtn.TextColor = UIHelper.Gold;
        }

        public void Update(GameTime gt)
        {
            _time += (float)gt.ElapsedGameTime.TotalSeconds;
            _demoTimer += (float)gt.ElapsedGameTime.TotalSeconds;
            float dt = (float)gt.ElapsedGameTime.TotalSeconds;

            var ms = Mouse.GetState();
            _prevBtn.Update(ms); _nextBtn.Update(ms); _skipBtn.Update(ms);

            // Demo animation
            UpdateDemo(dt);

            var kb = Keyboard.GetState();
            if (kb.IsKeyDown(Keys.Escape)) _game.ChangeState(GameState.MainMenu);

            // Arrow key navigation
            bool nl = kb.IsKeyDown(Keys.Right);
            bool pr = kb.IsKeyDown(Keys.Left);
            if (nl && !_prevNext) { if (_page < Pages.Length-1) { _page++; RebuildBtns(); } else _game.ChangeState(GameState.MainMenu); }
            if (pr && !_prevPrev && _page > 0) { _page--; RebuildBtns(); }
            _prevNext = nl; _prevPrev = pr;
        }

        void UpdateDemo(float dt)
        {
            var demo = Pages[_page].DemoType;
            switch (demo)
            {
                case DemoAnim.Move:
                    _demoX += (float)System.Math.Sin(_demoTimer * 1.5f) * 80f * dt;
                    _demoX = System.Math.Clamp(_demoX, 120, 400);
                    break;
                case DemoAnim.Attack:
                    _demoAtk = (_demoAtk + dt * 1.5f) % (float)(System.Math.PI * 2);
                    break;
                case DemoAnim.Dash:
                    _demoDashTimer += dt;
                    if (_demoDashTimer > 1.5f) { _demoDashTimer = 0; _demoDash = !_demoDash; }
                    break;
            }
            // Gravity
            if (!_demoGrounded) _demoVelY += 600f * dt;
            _demoY += _demoVelY * dt;
            if (_demoY >= 340f) { _demoY = 340f; _demoVelY = 0; _demoGrounded = true; }
            // Auto-jump for Move demo
            if (demo == DemoAnim.Move && _demoGrounded && _demoTimer % 2f < 0.05f)
            { _demoVelY = -300f; _demoGrounded = false; }
        }

        public void Draw(SpriteBatch sb)
        {
            int W = _game.GraphicsDevice.Viewport.Width;
            int H = _game.GraphicsDevice.Viewport.Height;
            var pg = Pages[_page];

            // Background
            sb.Draw(_pixel, new Rectangle(0,0,W,H), UIHelper.Dark);

            // Animated grid
            for (int i=0; i<20; i++)
            {
                float off = (_time * 20f + i * 60) % W;
                sb.Draw(_pixel, new Rectangle((int)off, 0, 1, H), UIHelper.Blue * 0.04f);
                sb.Draw(_pixel, new Rectangle(0, i * 40, W, 1), UIHelper.Purple * 0.03f);
            }

            // Top bar
            UIHelper.DrawBox(sb, _pixel, new Rectangle(0,0,W,60), UIHelper.Dark2, UIHelper.Blue*0.3f, 1);
            UIHelper.DrawCenteredText(sb, _bigFont, "🎮  TUTORIEL",
                new Rectangle(0,8,W,44), UIHelper.Blue, 0.65f);

            // Progress dots
            for (int i=0; i<Pages.Length; i++)
            {
                int dotX = W/2 - Pages.Length*16 + i*32;
                Color dc = i == _page ? UIHelper.Gold : UIHelper.TextDim * 0.4f;
                int ds = i == _page ? 10 : 6;
                sb.Draw(_pixel, new Rectangle(dotX-ds/2, 54-ds/2, ds, ds), dc);
            }

            // Main card
            int cx=60, cy=70, cw=W-120, cH=H-150;
            UIHelper.DrawBox(sb, _pixel, new Rectangle(cx,cy,cw,cH), UIHelper.Dark2, UIHelper.Blue*0.3f, 1);

            // Icon + title
            UIHelper.DrawCenteredText(sb,_bigFont,pg.Icon,new Rectangle(cx,cy+12,cw,50),Color.White,0.85f);
            UIHelper.DrawCenteredText(sb,_bigFont,pg.Title,new Rectangle(cx,cy+60,cw,44),UIHelper.Gold,0.62f);
            UIHelper.DrawCenteredText(sb,_font,pg.Subtitle,new Rectangle(cx,cy+104,cw,26),UIHelper.TextDim,0.88f);

            sb.Draw(_pixel, new Rectangle(cx+30,cy+133,cw-60,1), UIHelper.Blue*0.3f);

            // Demo area (right half)
            int demoX = cx + cw/2 + 20, demoY = cy + 145, demoW = cw/2 - 40, demoH = 200;
            sb.Draw(_pixel, new Rectangle(demoX, demoY, demoW, demoH), UIHelper.Dark * 0.5f);
            sb.Draw(_pixel, new Rectangle(demoX, demoY + demoH - 20, demoW, 20), new Color(30,35,60));
            DrawDemo(sb, demoX, demoY, demoW, demoH, pg.DemoType);

            // Lines (left half)
            int lineX = cx + 24, lineY = cy + 148;
            for (int i=0; i<pg.Lines.Length; i++)
            {
                Color lc = pg.Lines[i].StartsWith("  ") ? UIHelper.TextMain : UIHelper.TextDim;
                if (pg.Lines[i].Contains("COMBO") || pg.Lines[i].Contains("double") || pg.Lines[i].Contains("!"))
                    lc = UIHelper.Gold;
                sb.DrawString(_font, pg.Lines[i], new Vector2(lineX, lineY + i * 26), lc);
            }

            // Page indicator
            sb.DrawString(_font, $"{_page+1} / {Pages.Length}",
                new Vector2(W - 80, H - 56), UIHelper.TextDim);

            _prevBtn.Draw(sb,_pixel,_font,0.85f);
            _nextBtn.Draw(sb,_pixel,_font,0.88f);
            _skipBtn.Draw(sb,_pixel,_font,0.8f);
        }

        void DrawDemo(SpriteBatch sb, int ox, int oy, int w, int h, DemoAnim anim)
        {
            int gY = oy + h - 20;
            int cx = ox + w/2;

            switch (anim)
            {
                case DemoAnim.Idle:
                    // Simple idle bob
                    float bob = (float)System.Math.Sin(_time * 2f) * 3f;
                    DrawMiniPlayer(sb, cx - 16, gY - 48 + (int)bob, UIHelper.Blue);
                    sb.DrawString(_font, "IDLE", new Vector2(cx - 14, oy + 8), UIHelper.TextDim);
                    break;

                case DemoAnim.Move:
                    float mx = ox + 20 + (_demoTimer * 60f) % (w - 40);
                    DrawMiniPlayer(sb, (int)mx, (int)_demoY - 30 + oy, UIHelper.Blue);
                    // Arrow indicator
                    sb.DrawString(_font, "→ D", new Vector2(ox+8, oy+8), UIHelper.Gold);
                    sb.DrawString(_font, "↑ W", new Vector2(ox+8, oy+26), new Color(0,200,255));
                    break;

                case DemoAnim.Attack:
                    DrawMiniPlayer(sb, cx - 16, gY - 48, UIHelper.Blue);
                    // Sword slash
                    float slashX = (float)System.Math.Sin(_demoAtk) * 30f;
                    float slashY = (float)System.Math.Cos(_demoAtk * 0.7f) * 20f;
                    sb.Draw(_pixel, new Rectangle(cx+8+(int)slashX, gY-40+(int)slashY, 3, 30), UIHelper.Gold);
                    sb.Draw(_pixel, new Rectangle(cx+8+(int)slashX-10, gY-30+(int)slashY, 25, 3), UIHelper.Gold);
                    // Combo text
                    if ((int)(_demoAtk * 3) % 5 == 0)
                        sb.DrawString(_font, "COMBO!", new Vector2(cx, oy+10), UIHelper.Gold);
                    break;
            }
        }

        void DrawMiniPlayer(SpriteBatch sb, int x, int y, Color color)
        {
            // Body
            sb.Draw(_pixel, new Rectangle(x, y, 28, 36), color);
            // Head
            sb.Draw(_pixel, new Rectangle(x+4, y-16, 20, 16), new Color(220,180,140));
            // Eyes
            sb.Draw(_pixel, new Rectangle(x+6, y-12, 4, 4), Color.Black);
            sb.Draw(_pixel, new Rectangle(x+14, y-12, 4, 4), Color.Black);
        }

        public void Dispose() { }
    }

    public class TutorialPage
    {
        public string Title="", Subtitle="", Icon="";
        public string[] Lines = System.Array.Empty<string>();
        public DemoAnim DemoType;
    }

    public enum DemoAnim { Idle, Move, Attack, Dash }
}
