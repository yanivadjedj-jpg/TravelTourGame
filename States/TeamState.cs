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
    public class TeamState : IGameState
    {
        readonly TravelTourGame _game;
        Texture2D _pixel = null!;
        SpriteFontBase _font = null!, _bigFont = null!;

        UIButton _backBtn = null!;
        bool _needRebuild;
        float _time;
        int _hovered = -1;
        int _scrollOffset = 0;

        // Toast
        string _toast = ""; float _toastTimer; Color _toastColor;

        // Card dimensions
        const int CardW = 168, CardH = 220, CardGap = 14;
        const int Cols = 4;

        // Slot dimensions
        const int SlotW = 180, SlotH = 230;

        // ── OVR calculation ──────────────────────────────────────
        static int CalcOvr(CharacterData c)
        {
            float score = c.ScaledHP() * 0.15f + c.ScaledAtk() * 4f + c.ScaledDef() * 2.5f + c.BaseSpeed * 3f;
            return Math.Clamp((int)(score / 8f), 1, 99);
        }

        // ── Rarity colors ─────────────────────────────────────────
        static readonly Color[] RarBg = {
            new Color(35, 25, 15),   // Common  — bronze
            new Color(18, 28, 42),   // Rare    — blue
            new Color(28, 12, 42),   // Epic    — purple
            new Color(42, 32, 6),    // Legendary — gold
        };
        static readonly Color[] RarAccent = {
            new Color(160, 100, 50),  // Common
            new Color(50,  160, 255), // Rare
            new Color(168, 85,  247), // Epic
            new Color(255, 195, 50),  // Legendary
        };
        static readonly string[] RarLabel = { "COMMUN","RARE","ÉPIQUE","LÉGENDAIRE" };

        public TeamState(TravelTourGame game) => _game = game;

        public void Load(Texture2D pixel, SpriteFontBase font, SpriteFontBase bigFont)
        {
            _pixel = pixel; _font = font; _bigFont = bigFont;
            _backBtn = new UIButton(new Rectangle(16,16,110,36),"← Menu",
                ()=>_game.ChangeState(GameState.MainMenu));
        }

        public void Update(GameTime gt)
        {
            _time += (float)gt.ElapsedGameTime.TotalSeconds;
            _toastTimer -= (float)gt.ElapsedGameTime.TotalSeconds;

            if (_needRebuild) { _needRebuild = false; }

            int W = _game.GraphicsDevice.Viewport.Width;
            int H = _game.GraphicsDevice.Viewport.Height;
            var ms = Mouse.GetState();
            var kb = Keyboard.GetState();

            _backBtn.Update(ms);

            // Scroll wheel
            if (ms.ScrollWheelValue != _prevScroll)
            {
                _scrollOffset = Math.Clamp(_scrollOffset - (ms.ScrollWheelValue - _prevScroll)/120*30, 0, MaxScroll(H));
                _prevScroll = ms.ScrollWheelValue;
            }
            _prevScroll = ms.ScrollWheelValue;

            // Arrow key scroll
            if (kb.IsKeyDown(Keys.Down)) _scrollOffset = Math.Clamp(_scrollOffset + 3, 0, MaxScroll(H));
            if (kb.IsKeyDown(Keys.Up))   _scrollOffset = Math.Clamp(_scrollOffset - 3, 0, MaxScroll(H));

            // Detect card hover & click
            _hovered = -1;
            int gridY = SlotAreaBottom(H) + 50 - _scrollOffset;
            for (int i = 0; i < Catalog.Characters.Count; i++)
            {
                var r = CardRect(i, gridY, W);
                if (r.Contains(ms.Position)) { _hovered = i; break; }
            }

            bool clicked = ms.LeftButton == ButtonState.Pressed && _prevMs.LeftButton == ButtonState.Released;
            if (clicked && _hovered >= 0)
            {
                var c = Catalog.Characters[_hovered];
                if (!c.IsOwned) ShowToast($"Non possédé — 🔒 {c.BuyPrice:N0} or", Color.Red);
                else if (TeamManager.IsInTeam(c.Name))
                { TeamManager.Remove(c.Name); ShowToast($"{c.Name} retiré de l'équipe", UIHelper.TextDim); }
                else if (!TeamManager.Add(c.Name))
                { ShowToast("Équipe complète ! Retirez un membre.", Color.Yellow); }
                else ShowToast($"{c.Name} ajouté à l'équipe !", RarAccent[(int)c.Rarity]);
            }

            // ESC
            if (kb.IsKeyDown(Keys.Escape)) _game.ChangeState(GameState.MainMenu);

            _prevMs = ms;
        }

        int SlotAreaBottom(int H) => H / 3 + 20;
        int MaxScroll(int H)
        {
            int rows = (Catalog.Characters.Count + Cols - 1) / Cols;
            int totalH = rows * (CardH + CardGap);
            int visible = H - SlotAreaBottom(H) - 60;
            return Math.Max(0, totalH - visible);
        }

        Rectangle CardRect(int idx, int gridY, int W)
        {
            int startX = W/2 - (Cols*(CardW+CardGap))/2;
            int col = idx % Cols, row = idx / Cols;
            int x = startX + col*(CardW+CardGap);
            int y = gridY  + row*(CardH+CardGap);
            return new Rectangle(x, y, CardW, CardH);
        }

        MouseState _prevMs;
        int _prevScroll;

        // ── DRAW ─────────────────────────────────────────────────
        public void Draw(SpriteBatch sb)
        {
            int W = _game.GraphicsDevice.Viewport.Width;
            int H = _game.GraphicsDevice.Viewport.Height;

            // Background
            sb.Draw(_pixel, new Rectangle(0,0,W,H), UIHelper.Dark);

            // Animated scan lines
            for (int i=0;i<10;i++)
            {
                float off=(_time*18f+i*90)%H;
                sb.Draw(_pixel,new Rectangle(0,(int)off,W,1),UIHelper.Gold*0.025f);
            }

            DrawHeader(sb, W);
            DrawTeamSlots(sb, W, H);
            DrawTeamOvr(sb, W, H);
            DrawCardGrid(sb, W, H);
            DrawScrollIndicator(sb, W, H);

            _backBtn.Draw(sb, _pixel, _font, 0.85f);

            DrawToast(sb, W, H);
        }

        // ── HEADER ────────────────────────────────────────────────
        void DrawHeader(SpriteBatch sb, int W)
        {
            UIHelper.DrawCenteredText(sb, _bigFont, "⚔️  MY TEAM",
                new Rectangle(0, 14, W, 50), UIHelper.Gold, 0.72f);
            UIHelper.DrawCenteredText(sb, _font, "Sélectionne 3 combattants pour ton équipe",
                new Rectangle(0, 56, W, 22), UIHelper.TextDim, 0.82f);
        }

        // ── TEAM SLOTS ────────────────────────────────────────────
        void DrawTeamSlots(SpriteBatch sb, int W, int H)
        {
            int slotAreaH = SlotAreaBottom(H) - 84;
            int totalW = 3*(SlotW+20)-20;
            int startX = W/2 - totalW/2;
            int startY = 84;

            for (int i=0; i<3; i++)
            {
                int sx = startX + i*(SlotW+20);
                string member = PlayerSave.CurrentTeam[i] ?? "";
                bool filled = !string.IsNullOrEmpty(member);

                if (filled)
                {
                    var ch = Catalog.Characters.Find(c=>c.Name==member);
                    if (ch != null) DrawFilledSlot(sb, sx, startY, ch, i);
                }
                else
                {
                    DrawEmptySlot(sb, sx, startY, i);
                }
            }
        }

        void DrawFilledSlot(SpriteBatch sb, int x, int y, CharacterData c, int slot)
        {
            int ri = (int)c.Rarity;
            Color acc = RarAccent[ri];
            Color bg  = RarBg[ri];
            float pulse = (float)Math.Sin(_time * 2f + slot) * 0.15f + 0.85f;

            // Card background with gradient
            DrawCardGradient(sb, x, y, SlotW, SlotH, bg, acc);

            // Glow border
            DrawGlowBorder(sb, x, y, SlotW, SlotH, acc, pulse);

            // Rarity label strip at top
            sb.Draw(_pixel, new Rectangle(x, y, SlotW, 22), acc * 0.3f);
            UIHelper.DrawCenteredText(sb, _font, RarLabel[ri],
                new Rectangle(x, y+2, SlotW, 18), acc, 0.7f);

            // Big emoji character
            UIHelper.DrawCenteredText(sb, _bigFont, c.Icon,
                new Rectangle(x, y+22, SlotW, 80), Color.White, 0.85f);

            // OVR rating
            int ovr = CalcOvr(c);
            sb.Draw(_pixel, new Rectangle(x+8, y+100, 46, 46), acc*0.2f);
            UIHelper.DrawCenteredText(sb, _bigFont, ovr.ToString(),
                new Rectangle(x+8, y+102, 46, 42), acc, 0.55f);
            UIHelper.DrawCenteredText(sb, _font, "OVR",
                new Rectangle(x+8, y+143, 46, 14), acc*0.7f, 0.6f);

            // Name
            UIHelper.DrawCenteredText(sb, _font, c.Name,
                new Rectangle(x, y+158, SlotW, 20), Color.White, 0.78f);

            // Stats mini
            DrawMiniStat(sb, x+8,  y+182, "HP",  (int)c.ScaledHP(),  new Color(64,224,160));
            DrawMiniStat(sb, x+62, y+182, "ATK", (int)c.ScaledAtk(), Color.OrangeRed);
            DrawMiniStat(sb, x+116,y+182, "DEF", (int)c.ScaledDef(), UIHelper.Blue);
            DrawMiniStat(sb, x+8,  y+200, "SPD", (int)(c.BaseSpeed*10), UIHelper.Gold);
            DrawMiniStat(sb, x+62, y+200, "NV",  c.Level, UIHelper.Purple);

            // Remove button (small X)
            int rx = x+SlotW-22, ry = y+4;
            sb.Draw(_pixel, new Rectangle(rx,ry,18,18), new Color(80,10,10)*0.8f);
            UIHelper.DrawCenteredText(sb, _font, "✕",
                new Rectangle(rx,ry+1,18,16), new Color(255,80,80), 0.75f);
            // Detect remove click
            if (Mouse.GetState().LeftButton == ButtonState.Pressed)
            {
                var ms = Mouse.GetState();
                if (new Rectangle(rx,ry,18,18).Contains(ms.Position)&&
                    _prevMs.LeftButton==ButtonState.Released)
                { TeamManager.Remove(Catalog.Characters.Find(ch=>ch.Name==PlayerSave.CurrentTeam[slot])?.Name??""); ShowToast("Personnage retiré",UIHelper.TextDim); }
            }
        }

        void DrawEmptySlot(SpriteBatch sb, int x, int y, int slot)
        {
            float alpha = 0.4f + (float)Math.Sin(_time * 1.5f + slot) * 0.15f;
            sb.Draw(_pixel, new Rectangle(x,y,SlotW,SlotH), new Color(15,18,35));
            DrawDashedBorder(sb, x, y, SlotW, SlotH, UIHelper.TextDim * alpha);
            UIHelper.DrawCenteredText(sb, _bigFont, "+",
                new Rectangle(x, y+70, SlotW, 60), UIHelper.TextDim * alpha, 0.9f);
            UIHelper.DrawCenteredText(sb, _font, $"Slot {slot+1}",
                new Rectangle(x, y+148, SlotW, 24), UIHelper.TextDim * alpha * 0.7f, 0.8f);
        }

        // ── TEAM OVR ─────────────────────────────────────────────
        void DrawTeamOvr(SpriteBatch sb, int W, int H)
        {
            int bottom = SlotAreaBottom(H);
            int filled = 0; float totalOvr = 0;
            foreach (var m in PlayerSave.CurrentTeam)
                if (!string.IsNullOrEmpty(m))
                {
                    var c = Catalog.Characters.Find(ch=>ch.Name==m);
                    if (c != null) { totalOvr += CalcOvr(c); filled++; }
                }

            string ovrStr = filled > 0 ? $"OVR ÉQUIPE : {(int)(totalOvr/filled)}" : "Équipe vide";
            Color ovrCol = filled == 3 ? UIHelper.Gold : UIHelper.TextDim;

            sb.Draw(_pixel, new Rectangle(0, bottom, W, 1), UIHelper.Gold * 0.2f);
            UIHelper.DrawCenteredText(sb, _font, ovrStr,
                new Rectangle(0, bottom+6, W, 22), ovrCol, 0.88f);

            if (filled > 0)
            {
                string hint = filled==3?"✔ Équipe complète — prête au combat !":$"{filled}/3 membres";
                UIHelper.DrawCenteredText(sb, _font, hint,
                    new Rectangle(0, bottom+24, W, 18), filled==3?new Color(64,224,160):UIHelper.TextDim, 0.75f);
            }
        }

        // ── CARD GRID ─────────────────────────────────────────────
        void DrawCardGrid(SpriteBatch sb, int W, int H)
        {
            int gridY = SlotAreaBottom(H) + 50 - _scrollOffset;

            // Clip to grid area
            // (MonoGame doesn't clip by default — we just skip off-screen cards)
            int minY = SlotAreaBottom(H) + 48;
            int maxY = H;

            for (int i=0; i<Catalog.Characters.Count; i++)
            {
                var r = CardRect(i, gridY, W);
                if (r.Bottom < minY || r.Y > maxY) continue;
                DrawCharCard(sb, Catalog.Characters[i], r, i == _hovered);
            }

            // Grid label
            int labelY = SlotAreaBottom(H) + 48 - 22;
            sb.Draw(_pixel, new Rectangle(0, labelY, W, 1), UIHelper.TextDim * 0.2f);
            int startX = W/2 - (Cols*(CardW+CardGap))/2;
            sb.DrawString(_font, "PERSONNAGES DISPONIBLES",
                new Vector2(startX, labelY + 4), UIHelper.TextDim);
            sb.DrawString(_font, $"  {Catalog.Characters.Count} cartes  |  Défilez ↓",
                new Vector2(startX + 230, labelY + 4), UIHelper.TextDim * 0.5f);
        }

        void DrawCharCard(SpriteBatch sb, CharacterData c, Rectangle r, bool hover)
        {
            int ri = (int)c.Rarity;
            Color acc = RarAccent[ri];
            Color bg  = RarBg[ri];
            bool inTeam = TeamManager.IsInTeam(c.Name);
            bool owned  = c.IsOwned;
            float pulse = hover ? (float)(Math.Sin(_time * 4f) * 0.2f + 0.8f) : 0.6f;

            // Card background
            DrawCardGradient(sb, r.X, r.Y, r.Width, r.Height, bg, acc);

            // Glow border — brighter on hover/selected
            Color borderCol = inTeam ? UIHelper.Gold : owned ? acc * pulse : UIHelper.TextDim * 0.4f;
            int bw = inTeam || hover ? 3 : 1;
            DrawGlowBorder(sb, r.X, r.Y, r.Width, r.Height, borderCol, 1f);

            // Rarity strip top
            sb.Draw(_pixel, new Rectangle(r.X, r.Y, r.Width, 20), acc * (owned?0.25f:0.1f));
            UIHelper.DrawCenteredText(sb, _font, RarLabel[ri],
                new Rectangle(r.X, r.Y+2, r.Width, 16), acc*(owned?1f:0.4f), 0.62f);

            // Lock overlay if not owned
            if (!owned) sb.Draw(_pixel, new Rectangle(r.X, r.Y, r.Width, r.Height), Color.Black*0.55f);

            // Character icon
            float iconScale = hover ? 0.82f : 0.72f;
            UIHelper.DrawCenteredText(sb, _bigFont, c.Icon,
                new Rectangle(r.X, r.Y+20, r.Width, 72), owned?Color.White:Color.White*0.4f, iconScale);

            // OVR badge
            int ovr = CalcOvr(c);
            int bx = r.X+6, by = r.Y+90;
            sb.Draw(_pixel, new Rectangle(bx, by, 40, 40), acc*(owned?0.18f:0.06f));
            UIHelper.DrawCenteredText(sb, _bigFont, ovr.ToString(),
                new Rectangle(bx, by+2, 40, 36), owned?acc:UIHelper.TextDim*0.4f, 0.48f);
            sb.DrawString(_font, "OVR", new Vector2(bx+6, by+35), owned?acc*0.7f:UIHelper.TextDim*0.3f);

            // Name
            UIHelper.DrawCenteredText(sb, _font, c.Name,
                new Rectangle(r.X, r.Y+132, r.Width, 18), owned?Color.White:UIHelper.TextDim*0.5f, 0.72f);

            // Stats bars
            DrawStatBar(sb, r.X+8, r.Y+154, r.Width-16, "HP",  c.ScaledHP(),  220f, new Color(64,224,160), owned);
            DrawStatBar(sb, r.X+8, r.Y+168, r.Width-16, "ATK", c.ScaledAtk(),  50f, Color.OrangeRed,       owned);
            DrawStatBar(sb, r.X+8, r.Y+182, r.Width-16, "DEF", c.ScaledDef(),  30f, UIHelper.Blue,         owned);
            DrawStatBar(sb, r.X+8, r.Y+196, r.Width-16, "SPD", c.BaseSpeed*10, 120f,UIHelper.Gold,          owned);

            // Lock icon
            if (!owned)
            {
                UIHelper.DrawCenteredText(sb, _bigFont, "🔒",
                    new Rectangle(r.X, r.Y+60, r.Width, 40), Color.White*0.6f, 0.5f);
                UIHelper.DrawCenteredText(sb, _font, $"{c.BuyPrice:N0} or",
                    new Rectangle(r.X, r.Y+98, r.Width, 18), UIHelper.Gold*0.7f, 0.68f);
            }

            // IN TEAM badge
            if (inTeam)
            {
                sb.Draw(_pixel, new Rectangle(r.Right-42, r.Y+22, 38, 20), UIHelper.Gold*0.25f);
                UIHelper.DrawCenteredText(sb, _font, "✔ TEAM",
                    new Rectangle(r.Right-42, r.Y+24, 38, 16), UIHelper.Gold, 0.6f);
            }

            // Hover shimmer
            if (hover && owned)
            {
                float shimmerX = (float)((Math.Sin(_time * 5f) + 1) / 2) * r.Width;
                sb.Draw(_pixel, new Rectangle(r.X+(int)shimmerX-8, r.Y, 16, r.Height), Color.White*0.06f);
            }
        }

        // ── DRAWING HELPERS ──────────────────────────────────────
        void DrawCardGradient(SpriteBatch sb, int x, int y, int w, int h, Color top, Color acc)
        {
            // Simple 3-band gradient
            int band = h / 3;
            sb.Draw(_pixel, new Rectangle(x, y,        w, band),   Color.Lerp(top, acc*0.3f, 0.5f));
            sb.Draw(_pixel, new Rectangle(x, y+band,   w, band),   top);
            sb.Draw(_pixel, new Rectangle(x, y+band*2, w, h-band*2), Color.Lerp(top, Color.Black, 0.3f));
        }

        void DrawGlowBorder(SpriteBatch sb, int x, int y, int w, int h, Color col, float alpha)
        {
            // Outer glow (2px semi-transparent)
            sb.Draw(_pixel, new Rectangle(x-2,y-2,w+4,2), col*(alpha*0.3f));
            sb.Draw(_pixel, new Rectangle(x-2,y+h,w+4,2), col*(alpha*0.3f));
            sb.Draw(_pixel, new Rectangle(x-2,y-2,2,h+4), col*(alpha*0.3f));
            sb.Draw(_pixel, new Rectangle(x+w,y-2,2,h+4), col*(alpha*0.3f));
            // Border
            sb.Draw(_pixel, new Rectangle(x,y,w,2), col*alpha);
            sb.Draw(_pixel, new Rectangle(x,y+h-2,w,2), col*alpha);
            sb.Draw(_pixel, new Rectangle(x,y,2,h), col*alpha);
            sb.Draw(_pixel, new Rectangle(x+w-2,y,2,h), col*alpha);
        }

        void DrawDashedBorder(SpriteBatch sb, int x, int y, int w, int h, Color col)
        {
            for (int i=x;i<x+w;i+=12) { sb.Draw(_pixel,new Rectangle(i,y,6,2),col); sb.Draw(_pixel,new Rectangle(i,y+h-2,6,2),col); }
            for (int i=y;i<y+h;i+=12) { sb.Draw(_pixel,new Rectangle(x,i,2,6),col); sb.Draw(_pixel,new Rectangle(x+w-2,i,2,6),col); }
        }

        void DrawMiniStat(SpriteBatch sb, int x, int y, string label, int val, Color col)
        {
            sb.DrawString(_font, $"{label}:{val}", new Vector2(x, y), col);
        }

        void DrawStatBar(SpriteBatch sb, int x, int y, int w, string label, float val, float max, Color col, bool owned)
        {
            int lw = 26;
            sb.DrawString(_font, label, new Vector2(x, y), UIHelper.TextDim*(owned?0.7f:0.3f));
            int barX = x+lw, barW = w-lw-30;
            sb.Draw(_pixel, new Rectangle(barX,y+2,barW,8), new Color(10,12,20));
            if (owned)
            {
                float pct = Math.Clamp(val/max, 0f, 1f);
                sb.Draw(_pixel, new Rectangle(barX,y+2,(int)(barW*pct),8), col*0.85f);
            }
            sb.DrawString(_font, ((int)val).ToString(), new Vector2(barX+barW+4,y), owned?col:UIHelper.TextDim*0.3f);
        }

        void DrawScrollIndicator(SpriteBatch sb, int W, int H)
        {
            int maxS = MaxScroll(H);
            if (maxS <= 0) return;
            float pct = (float)_scrollOffset / maxS;
            int trackH = 120, trackX = W-16, trackY = SlotAreaBottom(H)+60;
            sb.Draw(_pixel, new Rectangle(trackX,trackY,4,trackH), new Color(20,22,40));
            int thumbH = Math.Max(20, trackH-80);
            int thumbY = trackY + (int)((trackH-thumbH)*pct);
            sb.Draw(_pixel, new Rectangle(trackX,thumbY,4,thumbH), UIHelper.Purple*0.7f);
        }

        void DrawToast(SpriteBatch sb, int W, int H)
        {
            if (_toastTimer <= 0) return;
            float a = Math.Min(1f, _toastTimer/0.4f);
            var ts = _font.MeasureString(_toast);
            int tx=(int)(W/2f-ts.X/2f-16), ty=H-70;
            sb.Draw(_pixel,new Rectangle(tx,ty,(int)ts.X+32,36),UIHelper.Dark2*(a*0.95f));
            sb.Draw(_pixel,new Rectangle(tx,ty,(int)ts.X+32,2),_toastColor*a);
            sb.DrawString(_font,_toast,new Vector2(tx+16,ty+10),_toastColor*a);
        }

        void ShowToast(string m,Color c){_toast=m;_toastColor=c;_toastTimer=2.5f;}
        public void Dispose(){}
    }
}

// ── Static TeamManager ─────────────────────────────────────────
public static class TeamManager
{
    public static bool Add(string name)
    {
        for (int i = 0; i < 3; i++)
            if (string.IsNullOrEmpty(TravelTour.Core.PlayerSave.CurrentTeam[i]))
            { TravelTour.Core.PlayerSave.CurrentTeam[i] = name; return true; }
        return false;
    }
    public static void Remove(string name)
    {
        for (int i = 0; i < 3; i++)
            if (TravelTour.Core.PlayerSave.CurrentTeam[i] == name)
                TravelTour.Core.PlayerSave.CurrentTeam[i] = null!;
    }
    public static bool IsInTeam(string name)
    {
        foreach (var m in TravelTour.Core.PlayerSave.CurrentTeam)
            if (m == name) return true;
        return false;
    }
    public static TravelTour.Core.CharacterData? GetLeader()
    {
        if (!string.IsNullOrEmpty(TravelTour.Core.PlayerSave.CurrentTeam[0]))
            return TravelTour.Core.Catalog.Characters.Find(c => c.Name == TravelTour.Core.PlayerSave.CurrentTeam[0]);
        return TravelTour.Core.Catalog.Characters.Find(c => c.IsOwned);
    }
}
