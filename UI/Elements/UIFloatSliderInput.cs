using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Reflection;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.Config.UI;
using Stataria;

namespace Stataria.UI.Elements
{
    public class UIFloatSliderInput : UIElement
    {
        private PropertyFieldWrapper _property;
        private object _configInstance;
        private string _label;

        private float _min;
        private float _max;
        private float _step;

        private bool _dragging;
        private bool _typing;
        private string _typedText = "";
        private Terraria.ModLoader.Config.ModConfig _rootConfig;
        private string _tooltip;
        private bool _reloadRequired;
        private Action<string, bool> _onHover;

        public UIFloatSliderInput(string label, PropertyFieldWrapper property, object configInstance, float min, float max, float step = 0.01f, Terraria.ModLoader.Config.ModConfig rootConfig = null, string tooltip = "", bool reloadRequired = false, Action<string, bool> onHover = null)
        {
            _label = label;
            _property = property;
            _configInstance = configInstance;
            _min = min;
            _max = max;
            _step = step;
            _rootConfig = rootConfig;
            _tooltip = tooltip;
            _reloadRequired = reloadRequired;
            _onHover = onHover;

            Width.Set(0, 1f);
            Height.Set(40, 0f);
        }

        private float Value
        {
            get => (float)_property.GetValue(_configInstance);
            set 
            {
                _property.SetValue(_configInstance, MathHelper.Clamp(value, _min, _max));
                if (_rootConfig != null) 
                {
                    var saveMethod = typeof(Terraria.ModLoader.Config.ConfigManager).GetMethod("Save", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                    if (saveMethod != null) saveMethod.Invoke(null, new object[] { _rootConfig });
                }
            }
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            CalculatedStyle dimensions = GetDimensions();

            // Draw Label (scaled down slightly)
            Vector2 textPos = new Vector2(dimensions.X + 10, dimensions.Y + 12);
            Utils.DrawBorderString(spriteBatch, _label, textPos, Color.White, 0.9f);

            // Calculate layout for slider and textbox
            float textInputWidth = 80f;
            float padding = 10f;
            float sliderWidth = dimensions.Width - (_label.Length * 8) - textInputWidth - (padding * 4) - 50; 
            float sliderX = dimensions.X + (_label.Length * 8) + 50 + padding;
            
            Rectangle sliderRect = new Rectangle((int)sliderX, (int)(dimensions.Y + 15), (int)sliderWidth, 10);
            Rectangle textInputRect = new Rectangle((int)(sliderRect.Right + padding), (int)(dimensions.Y + 8), (int)textInputWidth, 24);

            // Draw Slider Background
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, sliderRect, new Color(40, 20, 50));

            // Draw Slider Fill
            float fillPercent = Utils.Clamp((Value - _min) / (_max - _min), 0f, 1f);
            Rectangle fillRect = new Rectangle(sliderRect.X, sliderRect.Y, (int)(sliderRect.Width * fillPercent), sliderRect.Height);
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, fillRect, new Color(170, 50, 200)); // Bright purple

            // Draw TextInput Background
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, textInputRect, _typing ? new Color(80, 40, 100) : new Color(30, 15, 40));

            // Blinking cursor logic
            string displayValue = Value.ToString("0.00");
            if (_typing)
            {
                displayValue = _typedText;
                if (Main.GameUpdateCount % 40 < 20)
                {
                    displayValue += "|";
                }
            }

            // Draw TextInput Value (scaled down slightly to fit better)
            Vector2 valuePos = new Vector2(textInputRect.X + 5, textInputRect.Y + 5);
            Utils.DrawBorderString(spriteBatch, displayValue, valuePos, Color.White, 0.9f);

            if (!CustomConfigUIState.DialogOpen && IsMouseHovering)
            {
                Main.LocalPlayer.mouseInterface = true;
                _onHover?.Invoke(_tooltip, _reloadRequired);
            }

            // Handle Interactions
            HandleMouseInput(sliderRect, textInputRect);
        }

        private void HandleMouseInput(Rectangle sliderRect, Rectangle textInputRect)
        {
            // Block all input while a file dialog is open; also cancel any in-progress drag/type
            if (CustomConfigUIState.DialogOpen)
            {
                _dragging = false;
                if (_typing)
                {
                    _typing = false;
                    if (Main.CurrentInputTextTakerOverride == this)
                        Main.CurrentInputTextTakerOverride = null;
                }
                return;
            }

            bool justPressed = Main.mouseLeft && Main.mouseLeftRelease;

            if (justPressed && sliderRect.Contains(Main.MouseScreen.ToPoint()))
            {
                _dragging = true;
                _typing = false;
            }
            else if (justPressed && textInputRect.Contains(Main.MouseScreen.ToPoint()))
            {
                _typing = true;
                _typedText = Value.ToString("0.00");
                Main.hasFocus = true;
                Main.clrInput(); // Reset chat input
            }
            else if (justPressed && !textInputRect.Contains(Main.MouseScreen.ToPoint()))
            {
                if (_typing) ApplyTypedText();
                _typing = false;
            }

            if (!Main.mouseLeft)
            {
                _dragging = false;
            }

            if (_dragging)
            {
                float percent = Utils.Clamp((Main.mouseX - sliderRect.X) / (float)sliderRect.Width, 0f, 1f);
                float val = _min + percent * (_max - _min);
                if (_step > 0)
                {
                    val = (float)Math.Round(val / _step) * _step;
                }
                Value = val;
            }

            if (_typing)
            {
                Main.CurrentInputTextTakerOverride = this;
                Terraria.GameInput.PlayerInput.WritingText = true;
                Main.instance.HandleIME();

                string newText = Main.GetInputText(_typedText);
                
                if (Main.inputTextEscape || Main.inputTextEnter)
                {
                    ApplyTypedText();
                    _typing = false;
                    Main.CurrentInputTextTakerOverride = null;
                }
                else
                {
                    _typedText = "";
                    foreach (char c in newText)
                    {
                        if (char.IsDigit(c) || c == '.' || c == '-')
                            _typedText += c;
                    }
                }
            }
            else if (Main.CurrentInputTextTakerOverride == this)
            {
               Main.CurrentInputTextTakerOverride = null;
            }
        }

        private void ApplyTypedText()
        {
            if (float.TryParse(_typedText, out float parsed))
            {
                Value = parsed;
            }
        }
    }
}
