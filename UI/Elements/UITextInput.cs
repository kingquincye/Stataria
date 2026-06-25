using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.UI;

namespace Stataria.UI.Elements
{
    public class UITextInput : UIElement
    {
        private Terraria.Localization.LocalizedText _placeholder;
        private string _text = "";
        private bool _focused;
        private Action<string> _onTextChanged;
        private int _maxLength;

        public string Text
        {
            get => _text;
            set
            {
                if (_text != value)
                {
                    _text = value ?? "";
                    _onTextChanged?.Invoke(_text);
                }
            }
        }

        public bool Focused => _focused;

        public UITextInput(Terraria.Localization.LocalizedText placeholder, string initialText = "", Action<string> onTextChanged = null, int maxLength = 30)
        {
            _placeholder = placeholder;
            _text = initialText;
            _onTextChanged = onTextChanged;
            _maxLength = maxLength;

            Width.Set(0, 1f);
            Height.Set(30, 0f);
        }

        public override void LeftClick(UIMouseEvent evt)
        {
            if (CustomConfigUIState.DialogOpen) return;
            base.LeftClick(evt);
            _focused = true;
            Main.clrInput();
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (CustomConfigUIState.DialogOpen)
            {
                if (_focused)
                {
                    _focused = false;
                    if (Main.CurrentInputTextTakerOverride == this)
                        Main.CurrentInputTextTakerOverride = null;
                }
                return;
            }

            if (IsMouseHovering || _focused)
            {
                Main.LocalPlayer.mouseInterface = true;
            }

            if (_focused && Main.mouseLeft && !IsMouseHovering)
            {
                _focused = false;
                if (Main.CurrentInputTextTakerOverride == this)
                {
                    Main.CurrentInputTextTakerOverride = null;
                }
            }
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            CalculatedStyle dimensions = GetDimensions();

            if (_focused)
            {
                Main.CurrentInputTextTakerOverride = this;
                Terraria.GameInput.PlayerInput.WritingText = true;
                Main.instance.HandleIME();

                string newText = Main.GetInputText(_text);
                if (newText != _text)
                {
                    if (newText.Length <= _maxLength)
                    {
                        _text = newText;
                        _onTextChanged?.Invoke(_text);
                    }
                }

                if (Main.inputTextEscape || Main.inputTextEnter)
                {
                    _focused = false;
                    Main.CurrentInputTextTakerOverride = null;
                }
            }
            else if (Main.CurrentInputTextTakerOverride == this)
            {
                Main.CurrentInputTextTakerOverride = null;
            }

            // Draw background and border
            Color bgColor = _focused ? new Color(60, 40, 90) : new Color(30, 20, 50);
            Color borderColor = _focused ? new Color(180, 80, 220) : new Color(60, 40, 90);

            spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle((int)dimensions.X, (int)dimensions.Y, (int)dimensions.Width, (int)dimensions.Height), borderColor);
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle((int)dimensions.X + 2, (int)dimensions.Y + 2, (int)dimensions.Width - 4, (int)dimensions.Height - 4), bgColor);

            // Draw text or placeholder
            string drawText = _text;
            Color textColor = Color.White;

            if (string.IsNullOrEmpty(drawText))
            {
                drawText = _placeholder?.Value ?? "";
                textColor = Color.Gray;
            }
            else if (_focused && Main.GameUpdateCount % 40 < 20)
            {
                drawText += "|";
            }

            Vector2 textSize = FontAssets.MouseText.Value.MeasureString(drawText) * 0.85f;
            Vector2 textPos = new Vector2(dimensions.X + 8, dimensions.Y + (dimensions.Height - textSize.Y) / 2f + 2f);
            Utils.DrawBorderString(spriteBatch, drawText, textPos, textColor, 0.85f);
        }
    }
}
