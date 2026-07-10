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
    public class FruitState : IGameState
    {
        readonly TravelTourGame _game;
        Texture2D _pixel = null!;
        SpriteFontBase _font = null!, _bigFont = null!;

        UIButton _backBtn = null!;
        float _time;
        int _hovered = -1;
        int _scrollY = 0;
        int _prevScroll;
        MouseState _prevMs;

        // Filter tabs
        int _filterTab = 0; // 0=All, 1=Naturel, 2=Élémentaire, 3=Bête
        List<UIButton> _filterBtns = new();

        // Detail panel
        FruitData? _selectedFruit = null;

        string _toast = ""; float _toastTimer; Color _toastColor;

        // Card layout
        const int CardW = 200, CardH = 260, CardGap = 16, Cols = 3;

        static readonly Color[] RarityColors = {
            new Color(160,100,50),   // Common
            new Color(50,160,255),   // Rare
            new Color(168,85,247),   // Epic
            new Color(255,195,50),   // Legendary
            new Color(255,80,180),   // Mythical — rose vif
        };
        static readonly Color[] RarityBg = {
            new Color(30,20,10),
            new Color(10,20,35),
            new Color(20,8,35),
            new Color(35,28,5),
            new Color(35,5,20),
        };

        public FruitState(TravelTourGame game) => _game = game;

        public void Load(Texture2D pixel, SpriteFontBase font, SpriteFontBase bigFont)
        {
            _pixel = pixel; _font = font; _bigFont = bigFont;
            int W = _game.GraphicsDevice.Viewport.Width;
            _backBtn = new UIButton(new Rectangle(16,16,110,36),"← Menu",
                ()=>_game.ChangeState(GameState.MainMenu));

            string[] tabs = {"Tous","Naturel","Élémentaire","Bête"};
            for(int i=0;i<tabs.Length;i++){
                int idx=i;
                _filterBtns.Add(new UIButton(
                    new Rectangle(W/2-360+idx*185,65,175,30),tabs[i],
                    ()=>{ _filterTab=idx; _scrollY=0; _selectedFruit=null; }));
            }
        }

        List<FruitData> FilteredFruits()
        {
            if (_filterTab==0) return Catalog.Fruits;
            var type=(FruitType)(_filterTab-1);
            return Catalog.Fruits.FindAll(f=>f.Type==type);
        }

        public void Update(GameTime gt)
        {
            _time+=(float)gt.ElapsedGameTime.TotalSeconds;
            _toastTimer-=(float)gt.ElapsedGameTime.TotalSeconds;
            int W=_game.GraphicsDevice.Viewport.Width;
            int H=_game.GraphicsDevice.Viewport.Height;
            var ms=Mouse.GetState();
            var kb=Keyboard.GetState();

            _backBtn.Update(ms);
            foreach(var b in _filterBtns) b.Update(ms);

            // Scroll
            if(ms.ScrollWheelValue!=_prevScroll){
                _scrollY=Math.Clamp(_scrollY-(ms.ScrollWheelValue-_prevScroll)/120*30,0,MaxScroll(H));
                _prevScroll=ms.ScrollWheelValue;
            }
            _prevScroll=ms.ScrollWheelValue;
            if(kb.IsKeyDown(Keys.Down)) _scrollY=Math.Clamp(_scrollY+3,0,MaxScroll(H));
            if(kb.IsKeyDown(Keys.Up))   _scrollY=Math.Clamp(_scrollY-3,0,MaxScroll(H));

            // Hover
            _hovered=-1;
            var fruits=FilteredFruits();
            int gridY=GridStartY()-_scrollY;
            for(int i=0;i<fruits.Count;i++){
                var r=CardRect(i,gridY,W);
                if(r.Contains(ms.Position)){_hovered=i;break;}
            }

            bool clicked=ms.LeftButton==ButtonState.Pressed&&_prevMs.LeftButton==ButtonState.Released;
            if(clicked&&_hovered>=0){
                _selectedFruit=fruits[_hovered];
            }

            // Boutons action du panneau détail (clic direct)
            if(clicked && _selectedFruit != null)
            {
                int panY = H - 250;
                var f = _selectedFruit;
                var acc = RarityColors[(int)f.Rarity];
                if(!f.IsOwned)
                {
                    var buyRect = new Rectangle(20, panY + 162, 180, 36);
                    if(buyRect.Contains(ms.Position))
                    {
                        if(!PlayerSave.SpendGold(f.BuyPrice)) ShowToast("Or insuffisant!", Color.Red);
                        else { f.IsOwned = true; if(!PlayerSave.OwnedFruits.Contains(f.Name)) PlayerSave.OwnedFruits.Add(f.Name); ShowToast($"🍎 {f.Name} acheté!", acc); }
                    }
                }
                else if(!f.IsEquipped)
                {
                    var eqRect = new Rectangle(20, panY + 162, 160, 36);
                    if(eqRect.Contains(ms.Position))
                    {
                        PlayerSave.EquipFruit(f.Name);
                        ShowToast($"🍎 {f.Name} équipé!", acc);
                    }
                }
                else
                {
                    var uneqRect = new Rectangle(20, panY + 162, 160, 36);
                    if(uneqRect.Contains(ms.Position))
                    {
                        PlayerSave.UnequipFruit();
                        ShowToast("Fruit retiré.", UIHelper.TextDim);
                    }
                }
            }

            if(kb.IsKeyDown(Keys.Escape)&&!_prevMs.Equals(default)){
                if(_selectedFruit!=null) _selectedFruit=null;
                else _game.ChangeState(GameState.MainMenu);
            }
            _prevMs=ms;
        }

        int GridStartY() => 105;
        int MaxScroll(int H)
        {
            var fruits=FilteredFruits();
            int rows=(fruits.Count+Cols-1)/Cols;
            int totalH=rows*(CardH+CardGap);
            int visible=H-GridStartY()-(_selectedFruit!=null?260:0);
            return Math.Max(0,totalH-visible);
        }

        Rectangle CardRect(int idx,int gridY,int W)
        {
            var fruits=FilteredFruits();
            int startX=W/2-(Cols*(CardW+CardGap))/2;
            int col=idx%Cols, row=idx/Cols;
            return new Rectangle(startX+col*(CardW+CardGap), gridY+row*(CardH+CardGap), CardW,CardH);
        }

        public void Draw(SpriteBatch sb)
        {
            int W=_game.GraphicsDevice.Viewport.Width;
            int H=_game.GraphicsDevice.Viewport.Height;

            // BG
            sb.Draw(_pixel,new Rectangle(0,0,W,H),UIHelper.Dark);
            for(int i=0;i<8;i++){
                float off=(_time*20f+i*100)%W;
                sb.Draw(_pixel,new Rectangle((int)off,0,1,H),new Color(255,80,180)*0.03f);
            }

            // Header
            UIHelper.DrawCenteredText(sb,_bigFont,"🍎  FRUITS DU DÉMON",
                new Rectangle(0,10,W,50),new Color(255,80,180),0.72f);

            // Equipped fruit indicator
            var eq=PlayerSave.GetEquippedFruit();
            if(eq!=null){
                string eqStr=$"Équipé : {eq.Icon} {eq.Name}  |  Maîtrise {eq.Mastery}/600";
                UIHelper.DrawCenteredText(sb,_font,eqStr,new Rectangle(0,52,W,18),
                    RarityColors[(int)eq.Rarity],0.82f);
            } else {
                UIHelper.DrawCenteredText(sb,_font,"Aucun fruit équipé — Sélectionne-en un !",
                    new Rectangle(0,52,W,18),UIHelper.TextDim,0.8f);
            }

            // Filter tabs
            for(int i=0;i<_filterBtns.Count;i++){
                _filterBtns[i].NormalColor=i==_filterTab?new Color(255,80,180)*0.2f:UIHelper.CardBg;
                _filterBtns[i].TextColor=i==_filterTab?new Color(255,80,180):UIHelper.TextDim;
                _filterBtns[i].Draw(sb,_pixel,_font,0.78f);
            }

            // Grid
            var fruits=FilteredFruits();
            int gridY=GridStartY()-_scrollY;
            int minY=GridStartY()-5, maxY=_selectedFruit!=null?H-265:H;
            for(int i=0;i<fruits.Count;i++){
                var r=CardRect(i,gridY,W);
                if(r.Bottom<minY||r.Y>maxY) continue;
                DrawFruitCard(sb,fruits[i],r,i==_hovered);
            }

            // Detail panel
            if(_selectedFruit!=null) DrawDetailPanel(sb,W,H,_selectedFruit);

            _backBtn.Draw(sb,_pixel,_font,0.85f);

            // Toast
            if(_toastTimer>0){
                float a=Math.Min(1f,_toastTimer/0.4f);
                var ts=_font.MeasureString(_toast);
                int tx=(int)(W/2f-ts.X/2f-14);
                sb.Draw(_pixel,new Rectangle(tx,H-70,(int)ts.X+28,34),UIHelper.Dark2*(a*0.95f));
                sb.Draw(_pixel,new Rectangle(tx,H-70,(int)ts.X+28,2),_toastColor*a);
                sb.DrawString(_font,_toast,new Vector2(tx+14,H-62),_toastColor*a);
            }
        }

        void DrawFruitCard(SpriteBatch sb,FruitData f,Rectangle r,bool hover)
        {
            int ri=(int)f.Rarity;
            Color acc=RarityColors[ri];
            Color bg=RarityBg[ri];
            float pulse=hover?(float)(Math.Sin(_time*4)*0.2f+0.8f):0.6f;
            bool eq=f.IsEquipped;

            // BG gradient
            sb.Draw(_pixel,new Rectangle(r.X,r.Y,r.Width,r.Height/2),Color.Lerp(bg,acc*0.3f,0.4f));
            sb.Draw(_pixel,new Rectangle(r.X,r.Y+r.Height/2,r.Width,r.Height/2),Color.Lerp(bg,Color.Black,0.2f));

            // Border
            Color border=eq?UIHelper.Gold:f.IsOwned?acc*pulse:UIHelper.TextDim*0.3f;
            int bw=eq||hover?3:1;
            sb.Draw(_pixel,new Rectangle(r.X,r.Y,r.Width,bw),border);
            sb.Draw(_pixel,new Rectangle(r.X,r.Bottom-bw,r.Width,bw),border);
            sb.Draw(_pixel,new Rectangle(r.X,r.Y,bw,r.Height),border);
            sb.Draw(_pixel,new Rectangle(r.Right-bw,r.Y,bw,r.Height),border);

            // Rarity strip
            sb.Draw(_pixel,new Rectangle(r.X,r.Y,r.Width,22),acc*(f.IsOwned?0.25f:0.1f));
            UIHelper.DrawCenteredText(sb,_font,f.GetRarityLabel(),new Rectangle(r.X,r.Y+3,r.Width,16),acc*(f.IsOwned?1f:0.4f),0.65f);

            // Type badge
            string typeStr=f.Type switch { FruitType.Naturel=>"NAT",FruitType.Élémentaire=>"ÉLÉ",_=>"BÊTE"};
            sb.DrawString(_font,typeStr,new Vector2(r.X+6,r.Y+5),acc*(f.IsOwned?0.7f:0.3f));

            // Not owned overlay
            if(!f.IsOwned) sb.Draw(_pixel,new Rectangle(r.X,r.Y,r.Width,r.Height),Color.Black*0.55f);

            // Big icon
            UIHelper.DrawCenteredText(sb,_bigFont,f.Icon,
                new Rectangle(r.X,r.Y+22,r.Width,80),f.IsOwned?Color.White:Color.White*0.35f,0.88f);

            // Name
            UIHelper.DrawCenteredText(sb,_font,f.Name,
                new Rectangle(r.X,r.Y+106,r.Width,18),f.IsOwned?Color.White:UIHelper.TextDim*0.5f,0.76f);

            // Mastery bar
            if(f.IsOwned){
                int my=r.Y+130, mw=r.Width-24;
                sb.Draw(_pixel,new Rectangle(r.X+12,my,mw,8),new Color(10,8,20));
                sb.Draw(_pixel,new Rectangle(r.X+12,my,(int)(mw*f.MasteryPct()),8),acc*0.85f);
                sb.DrawString(_font,$"Maîtrise {f.Mastery}/600",new Vector2(r.X+12,my+10),UIHelper.TextDim);

                // Moves preview
                for(int m=0;m<Math.Min(4,f.Moves.Length);m++){
                    var mv=f.Moves[m];
                    bool unlocked=f.Mastery>=mv.MasteryReq;
                    Color mc=unlocked?acc:UIHelper.TextDim*0.3f;
                    sb.Draw(_pixel,new Rectangle(r.X+10+m*46,r.Y+158,38,38),mc*0.15f);
                    UIHelper.DrawCenteredText(sb,_font,mv.Icon,new Rectangle(r.X+10+m*46,r.Y+160,38,30),unlocked?Color.White:Color.White*0.2f,0.7f);
                    UIHelper.DrawCenteredText(sb,_font,mv.Key,new Rectangle(r.X+10+m*46,r.Y+190,38,16),mc,0.6f);
                }
            } else {
                // Lock + price
                UIHelper.DrawCenteredText(sb,_bigFont,"🔒",new Rectangle(r.X,r.Y+124,r.Width,40),Color.White*0.5f,0.55f);
                UIHelper.DrawCenteredText(sb,_font,$"{f.BuyPrice:N0} or",new Rectangle(r.X,r.Y+168,r.Width,18),UIHelper.Gold*0.7f,0.7f);
            }

            // Equipped badge
            if(eq){
                sb.Draw(_pixel,new Rectangle(r.X,r.Bottom-22,r.Width,22),UIHelper.Gold*0.3f);
                UIHelper.DrawCenteredText(sb,_font,"✔ ÉQUIPÉ",new Rectangle(r.X,r.Bottom-20,r.Width,18),UIHelper.Gold,0.72f);
            }

            // Hover shimmer
            if(hover&&f.IsOwned){
                float sx=(float)((Math.Sin(_time*5)+1)/2)*r.Width;
                sb.Draw(_pixel,new Rectangle(r.X+(int)sx-10,r.Y,18,r.Height),Color.White*0.05f);
            }
        }

        void DrawDetailPanel(SpriteBatch sb,int W,int H,FruitData f)
        {
            int panY=H-255, panH=255;
            int ri=(int)f.Rarity;
            Color acc=RarityColors[ri];

            // Panel bg
            UIHelper.DrawBox(sb,_pixel,new Rectangle(0,panY,W,panH),
                new Color(8,6,15),acc*0.4f,2);

            // Close hint
            sb.DrawString(_font,"ESC pour fermer",new Vector2(W-120,panY+6),UIHelper.TextDim*0.5f);

            // Fruit name + icon
            sb.DrawString(_bigFont,$"{f.Icon}  {f.Name}",new Vector2(20,panY+8),acc);
            sb.DrawString(_font,f.Description,new Vector2(20,panY+46),UIHelper.TextDim);

            // Moves
            int col=0;
            foreach(var mv in f.Moves){
                bool unlocked=f.Mastery>=mv.MasteryReq;
                Color mc=unlocked?acc:UIHelper.TextDim*0.3f;
                int mx=20+col*(W/4);
                int my=panY+72;

                sb.Draw(_pixel,new Rectangle(mx,my,W/4-12,80),mc*0.1f);
                sb.Draw(_pixel,new Rectangle(mx,my,W/4-12,2),mc*0.5f);

                sb.DrawString(_font,$"[{mv.Key}] {mv.Icon} {mv.Name}",new Vector2(mx+6,my+4),unlocked?Color.White:UIHelper.TextDim*0.3f);
                sb.DrawString(_font,mv.Desc,new Vector2(mx+6,my+22),UIHelper.TextDim*(unlocked?0.8f:0.3f));
                sb.DrawString(_font,$"DMG:{mv.Damage}  CD:{mv.Cooldown}s",new Vector2(mx+6,my+40),mc);
                if(mv.MasteryReq>0)
                    sb.DrawString(_font,$"Maîtrise {mv.MasteryReq} requis",new Vector2(mx+6,my+58),
                        unlocked?new Color(64,224,160):Color.Red*0.7f);
                col++;
            }

            // Action buttons
            int bx=20;
            if(!f.IsOwned){
                // Buy button
                UIButton buyBtn=new UIButton(new Rectangle(bx,panY+162,180,36),$"Acheter — {f.BuyPrice:N0} or",
                    ()=>{
                        if(!PlayerSave.SpendGold(f.BuyPrice)){ ShowToast("Or insuffisant!",Color.Red); return; }
                        f.IsOwned=true;
                        PlayerSave.OwnedWeapons.Add(f.Name); // reuse list for fruits for now
                        ShowToast($"🍎 {f.Name} acheté!",acc);
                    },new Color(60,35,5),new Color(90,55,10));
                buyBtn.TextColor=UIHelper.Gold;
                buyBtn.Draw(sb,_pixel,_font,0.82f);
            } else if(!f.IsEquipped){
                UIButton eqBtn=new UIButton(new Rectangle(bx,panY+162,160,36),"Équiper",
                    ()=>{ PlayerSave.EquipFruit(f.Name); ShowToast($"🍎 {f.Name} équipé!",acc); },
                    new Color(20,40,10),new Color(30,60,15));
                eqBtn.TextColor=new Color(64,224,160);
                eqBtn.Draw(sb,_pixel,_font,0.82f);
            } else {
                UIButton uneqBtn=new UIButton(new Rectangle(bx,panY+162,160,36),"Retirer",
                    ()=>{ PlayerSave.UnequipFruit(); ShowToast("Fruit retiré.",UIHelper.TextDim); },
                    new Color(40,10,10),new Color(60,15,15));
                uneqBtn.TextColor=Color.OrangeRed;
                uneqBtn.Draw(sb,_pixel,_font,0.82f);
            }
        }

        void ShowToast(string m,Color c){_toast=m;_toastColor=c;_toastTimer=2.5f;}
        public void Dispose(){}
    }
}
