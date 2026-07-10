using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using FontStashSharp;
using System;

namespace TravelTour.UI
{
    public class UIButton
    {
        public Rectangle Bounds;
        public string    Label;
        public Color     NormalColor;
        public Color     HoverColor;
        public Color     TextColor;
        public Action    OnClick;
        public bool      Enabled = true;

        bool _wasPressed;
        bool _hovered;

        public UIButton(Rectangle bounds, string label, Action onClick,
            Color? normal = null, Color? hover = null)
        {
            Bounds      = bounds;
            Label       = label;
            OnClick     = onClick;
            NormalColor = normal ?? UIHelper.CardBg;
            HoverColor  = hover  ?? new Color(30, 35, 65);
            TextColor   = UIHelper.TextMain;
        }

        public void Update(MouseState ms)
        {
            if (!Enabled) return;
            _hovered = Bounds.Contains(ms.Position);
            bool pressed = ms.LeftButton == ButtonState.Pressed;
            if (_hovered && !pressed && _wasPressed) OnClick?.Invoke();
            _wasPressed = pressed;
        }

        public void Draw(SpriteBatch sb, Texture2D pixel, SpriteFontBase font, float scale = 1f)
        {
            Color fill   = !Enabled ? new Color(25, 27, 45) : _hovered ? HoverColor : NormalColor;
            Color border = !Enabled ? new Color(40, 45, 70) : _hovered ? UIHelper.Blue : new Color(50, 55, 90);
            UIHelper.DrawBox(sb, pixel, Bounds, fill, border, 2);
            Color tc = Enabled ? TextColor : UIHelper.TextDim;
            UIHelper.DrawCenteredText(sb, font, Label, Bounds, tc, scale);
        }
    }
}
