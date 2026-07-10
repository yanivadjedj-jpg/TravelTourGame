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
    public class WalletState : IGameState
    {
        readonly TravelTourGame _game;
        Texture2D _pixel = null!;
        SpriteFontBase _font = null!, _bigFont = null!;

        UIButton _backBtn = null!;
        float _time;
        List<UIButton> _statBtns = new();

        // Popup toasts
        float _popupAlpha;
        string _popupText = "";
        float _popupTimer;

        public WalletState(TravelTourGame game) => _game = game;

        public void Load(Texture2D pixel, SpriteFontBase font, SpriteFontBase bigFont)
        {
            _pixel = pixel; _font = font; _bigFont = bigFont;
            int W = _game.GraphicsDevice.Viewport.Width;
            _backBtn = new UIButton(new Rectangle(16,16,110,36),"← Menu",
                () => _game.ChangeState(GameState.MainMenu));
            RebuildStatBtns(W);
        }

        void RebuildStatBtns(int W)
        {
            _statBtns.Clear();
            // Positions calculées dynamiquement dans Draw() via _statBtns[i].Bounds = ...
            // On crée juste les boutons avec une position temporaire
            System.Action[] actions = {
                () => { PlayerSave.AllocMelee();   RebuildStatBtns(W); },
                () => { PlayerSave.AllocDefense(); RebuildStatBtns(W); },
                () => { PlayerSave.AllocSword();   RebuildStatBtns(W); },
                () => { PlayerSave.AllocFruit();   RebuildStatBtns(W); },
                () => { PlayerSave.AllocSpeed();   RebuildStatBtns(W); },
            };
            for (int i = 0; i < 5; i++)
                _statBtns.Add(new UIButton(new Rectangle(0, 0, 28, 22), "+", actions[i],
                    new Color(255, 140, 0) * 0.5f, new Color(255, 140, 0)));
        }

        public void Update(GameTime gt)
        {
            _time += (float)gt.ElapsedGameTime.TotalSeconds;
            _popupTimer -= (float)gt.ElapsedGameTime.TotalSeconds;
            var ms = Mouse.GetState();
            _backBtn.Update(ms);
            foreach (var b in _statBtns.ToList()) b.Update(ms);
            if (Keyboard.GetState().IsKeyDown(Keys.Escape)) _game.ChangeState(GameState.MainMenu);
        }

        public void Draw(SpriteBatch sb)
        {
            int W = _game.GraphicsDevice.Viewport.Width;
            int H = _game.GraphicsDevice.Viewport.Height;

            sb.Draw(_pixel, new Rectangle(0,0,W,H), UIHelper.Dark);

            // Animated background
            for (int i=0;i<12;i++)
            {
                float off = (_time*15f+i*80)%W;
                sb.Draw(_pixel, new Rectangle((int)off,0,1,H), UIHelper.Gold*0.03f);
            }

            _backBtn.Draw(sb,_pixel,_font,0.85f);
            UIHelper.DrawCenteredText(sb,_bigFont,"📊  STATS  &  PORTEFEUILLE",
                new Rectangle(0,14,W,50), new Color(255, 140, 0), 0.68f);

            // ── NIVEAU + OR PANEL ─────────────────────────────────
            int gx=60, gy=72, gw=W-120, gh=140;
            UIHelper.DrawBox(sb,_pixel,new Rectangle(gx,gy,gw,gh), UIHelper.Dark2, UIHelper.Gold*0.5f, 2);

            // Niveau central (grande taille)
            int lvl = PlayerSave.PlayerLevel;
            string lvlBig = $"NIVEAU  {lvl}";
            Vector2 lvlSz = _bigFont.MeasureString(lvlBig);
            sb.DrawString(_bigFont, lvlBig,
                new Vector2(gx + gw/2f - lvlSz.X/2f, gy+10), UIHelper.Gold);

            // Barre XP
            int xpPx = gx+20, xpPy = gy+52, xpW = gw-40;
            UIHelper.DrawBox(sb,_pixel,new Rectangle(xpPx,xpPy,xpW,16), new Color(10,8,25), UIHelper.Purple*0.4f, 1);
            UIHelper.DrawProgressBar(sb,_pixel,new Rectangle(xpPx+1,xpPy+1,xpW-2,14),
                PlayerSave.LevelProgressPct(), UIHelper.Purple, new Color(10,8,25));
            string xpStr = $"{PlayerSave.LevelXp} / {PlayerSave.XpToNextLevel()} XP  →  Niveau {lvl+1}";
            UIHelper.DrawCenteredText(sb,_font,xpStr,new Rectangle(xpPx,xpPy,xpW,16),UIHelper.TextMain,0.75f);

            // Rang + bonus de stats
            string rankStr = $"Rang {PlayerSave.GetRank()}";
            sb.DrawString(_font, rankStr, new Vector2(gx+20, gy+78), UIHelper.Purple);
            sb.DrawString(_font,
                $"+{(int)PlayerSave.LevelHpBonus()} HP   +{(int)PlayerSave.LevelAtkBonus()} ATK   +{(int)PlayerSave.LevelDefBonus()} DEF  (bonus niveau)",
                new Vector2(gx+120, gy+78), UIHelper.TextDim);

            // Or
            string goldStr = $"💰 {PlayerSave.Gold:N0} pièces d'or";
            sb.DrawString(_font, goldStr, new Vector2(gx+20, gy+104), UIHelper.Gold);

            // ══════════════════════════════════════════════════════
            // ── GRILLE DE STATS (style Blox Fruit) ────────────────
            // ══════════════════════════════════════════════════════
            int sgY = gy + gh + 14;
            int rowH = 38;
            int sgH  = 52 + 5 * rowH;   // header + 5 lignes

            // Fond + bordure orange
            UIHelper.DrawBox(sb, _pixel, new Rectangle(gx, sgY, gw, sgH),
                new Color(12, 10, 22), new Color(255, 140, 0) * 0.7f, 2);

            // ── Header ─────────────────────────────────────────────
            // Titre
            sb.DrawString(_font, "STATS", new Vector2(gx + 16, sgY + 10), new Color(255, 140, 0));

            // Points disponibles (coin droit, encadré)
            string ptsStr = PlayerSave.FreeStatPoints > 0
                ? $"⚡ {PlayerSave.FreeStatPoints} POINTS DISPONIBLES"
                : $"Total : {PlayerSave.TotalStats()} pts";
            Color ptsCol = PlayerSave.FreeStatPoints > 0 ? new Color(255, 220, 0) : UIHelper.TextDim;
            Vector2 ptsSz = _font.MeasureString(ptsStr);
            if (PlayerSave.FreeStatPoints > 0)
                UIHelper.DrawBox(sb, _pixel,
                    new Rectangle(gx + gw - (int)ptsSz.X - 28, sgY + 7, (int)ptsSz.X + 16, 22),
                    new Color(40, 30, 5), new Color(255, 200, 0) * 0.6f, 1);
            sb.DrawString(_font, ptsStr,
                new Vector2(gx + gw - (int)ptsSz.X - 20, sgY + 10), ptsCol);

            // Séparateur sous le header
            sb.Draw(_pixel, new Rectangle(gx + 10, sgY + 34, gw - 20, 1), new Color(255, 140, 0) * 0.35f);

            // ── Lignes stats ────────────────────────────────────────
            string[] sLabels  = { "Corps-à-Corps", "Défense", "Épée", "Fruit", "Vitesse" };
            string[] sIcons   = { "👊", "🛡️", "⚔️", "🍎", "💨" };
            int[]    sVals    = { PlayerSave.StatMelee, PlayerSave.StatDefense,
                                  PlayerSave.StatSword, PlayerSave.StatFruit, PlayerSave.StatSpeed };
            float[]  sBonuses = {
                (PlayerSave.MeleeDmgBonus()  - 1f) * 100f,
                (PlayerSave.DefenseBonus()   - 1f) * 100f,
                (PlayerSave.SwordDmgBonus()  - 1f) * 100f,
                (PlayerSave.FruitDmgBonus()  - 1f) * 100f,
                (PlayerSave.SpeedBonus()     - 1f) * 100f,
            };
            Color[] sCols = {
                new Color(255, 120, 60),  // Melee  — orange
                new Color(80, 180, 255),  // Defense — bleu
                new Color(255, 210, 50),  // Sword  — or
                new Color(200, 80, 255),  // Fruit  — violet
                new Color(80, 230, 180),  // Speed  — cyan
            };

            int nameCol  = gx + 16;
            int barStart = gx + 180;
            int barLen   = gw - 180 - 100;  // barre longue
            int valCol   = gx + gw - 95;
            int btnCol   = gx + gw - 40;

            for (int i = 0; i < 5; i++)
            {
                int ry = sgY + 40 + i * rowH;
                float pct = System.Math.Clamp((float)sVals[i] / PlayerSave.MAX_STAT, 0f, 1f);
                Color c = sCols[i];

                // Fond alterné
                if (i % 2 == 0)
                    sb.Draw(_pixel, new Rectangle(gx + 2, ry - 2, gw - 4, rowH - 1),
                        new Color(255, 255, 255) * 0.025f);

                // Icône + nom
                sb.DrawString(_font, $"{sIcons[i]}  {sLabels[i]}",
                    new Vector2(nameCol, ry + 8), UIHelper.TextMain);

                // Barre de progression (fond + remplissage)
                sb.Draw(_pixel, new Rectangle(barStart, ry + 10, barLen, 12), new Color(20, 20, 35));
                sb.Draw(_pixel, new Rectangle(barStart, ry + 10, barLen, 12), new Color(40, 40, 65));
                if (pct > 0)
                {
                    sb.Draw(_pixel, new Rectangle(barStart, ry + 10, (int)(barLen * pct), 12), c * 0.75f);
                    // Reflet brillant au sommet de la barre
                    sb.Draw(_pixel, new Rectangle(barStart, ry + 10, (int)(barLen * pct), 4), c * 0.4f);
                }
                // Séparateurs de segments (tous les 20%)
                for (int seg = 1; seg <= 4; seg++)
                    sb.Draw(_pixel, new Rectangle(barStart + barLen * seg / 5, ry + 10, 1, 12),
                        new Color(0, 0, 0) * 0.5f);

                // Valeur / max + bonus
                sb.DrawString(_font, $"{sVals[i]}",
                    new Vector2(valCol, ry + 8), c);
                sb.DrawString(_font, $"+{sBonuses[i]:F0}%",
                    new Vector2(valCol + 28, ry + 8), c * 0.65f);

                // Bouton [+]
                if (i < _statBtns.Count)
                {
                    _statBtns[i].Bounds = new Rectangle(btnCol, ry + 6, 28, 22);
                    if (PlayerSave.FreeStatPoints > 0)
                        _statBtns[i].Draw(sb, _pixel, _font, 0.8f);
                    else
                    {
                        // Bouton grisé
                        UIHelper.DrawBox(sb, _pixel, _statBtns[i].Bounds,
                            new Color(20, 20, 30), new Color(50, 50, 70), 1);
                        UIHelper.DrawCenteredText(sb, _font, "+",
                            _statBtns[i].Bounds, UIHelper.TextDim * 0.4f, 0.8f);
                    }
                }
            }

            // ── MATERIALS GRID ──────────────────────────────────────
            int my = sgY + sgH + 12;
            sb.DrawString(_font, "MATÉRIAUX", new Vector2(gx,my), UIHelper.TextDim);
            sb.Draw(_pixel, new Rectangle(gx, my+20, gw, 1), UIHelper.TextDim*0.3f);

            int cols=4, cardW=(gw-cols*12)/cols, cardH=100;
            int ci=0;
            foreach (var kv in PlayerSave.Materials)
            {
                int col = ci%cols, row = ci/cols;
                int cx = gx + col*(cardW+12);
                int cy = my+28 + row*(cardH+10);

                bool hasMat = kv.Value > 0;
                MaterialInfo.Data.TryGetValue(kv.Key, out var d); string matIcon=d.Item1??"❓"; string matLabel=d.Item2??kv.Key; int matRar=(int)(d.Item4);
                Color rarCol = UIHelper.RarityColors[matRar];

                UIHelper.DrawBox(sb,_pixel,new Rectangle(cx,cy,cardW,cardH),
                    hasMat ? UIHelper.CardBg : new Color(12,13,24),
                    hasMat ? rarCol*0.6f : new Color(30,32,50), 1);

                // Icon
                UIHelper.DrawCenteredText(sb,_bigFont, matIcon,
                    new Rectangle(cx,cy+6,cardW,40), Color.White, 0.7f);

                // Count (big)
                string cnt = kv.Value.ToString();
                var csz = _bigFont.MeasureString(cnt);
                Color cntCol = hasMat ? rarCol : UIHelper.TextDim*0.4f;
                UIHelper.DrawCenteredText(sb,_bigFont, cnt,
                    new Rectangle(cx,cy+46,cardW,28), cntCol, 0.55f);

                // Label
                UIHelper.DrawCenteredText(sb,_font, matLabel,
                    new Rectangle(cx,cy+74,cardW,20), UIHelper.TextDim*0.8f, 0.65f);

                // Rarity dot
                sb.Draw(_pixel, new Rectangle(cx+cardW-12, cy+6, 8, 8), rarCol);

                ci++;
            }

            // ── HOW TO EARN ──────────────────────────────────────────
            int ey = my+28 + ((ci-1)/cols+1)*(cardH+10) + 16;
            if (ey < H-80)
            {
                sb.Draw(_pixel, new Rectangle(gx,ey,gw,1), UIHelper.TextDim*0.2f);
                sb.DrawString(_font, "COMMENT GAGNER DES RESSOURCES",
                    new Vector2(gx, ey+10), UIHelper.TextDim);

                string[] tips = {
                    "💰 Or          →  Battre des ennemis, terminer des donjons, score Crosspark",
                    "🔴 Communs     →  Donjons faciles (Rang E-C)",
                    "🌙 Rares       →  Donjons moyens (Rang B-A)",
                    "💎 Épiques     →  Donjons difficiles (Rang S)",
                    "💀 Légendaires →  Donjons Boss et Chapitres Histoire",
                };
                for (int i=0;i<tips.Length;i++)
                    sb.DrawString(_font, tips[i], new Vector2(gx+16, ey+34+i*22),
                        i==0 ? UIHelper.Gold : UIHelper.TextMain);
            }
        }

        public void Dispose(){}
    }
}
