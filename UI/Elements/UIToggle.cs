using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.UI;
using Terraria.ModLoader.Config;
using System;
using Terraria.ModLoader.Config.UI;
using Stataria;

namespace Stataria.UI.Elements
{
    public class UIToggle : UIElement
    {
        private PropertyFieldWrapper _property;
        private object _configInstance;
        private string _label;
        private Terraria.ModLoader.Config.ModConfig _rootConfig;
        private string _tooltip;
        private bool _reloadRequired;
        private Action<string, bool> _onHover;

        public UIToggle(string label, PropertyFieldWrapper property, object configInstance, Terraria.ModLoader.Config.ModConfig rootConfig, string tooltip, bool reloadRequired, Action<string, bool> onHover)
        {
            _label = label;
            _property = property;
            _configInstance = configInstance;
            _rootConfig = rootConfig;
            _tooltip = tooltip;
            _reloadRequired = reloadRequired;
            _onHover = onHover;

            Width.Set(0, 1f);
            Height.Set(30, 0f);
        }

        private bool Value
        {
            get => (bool)_property.GetValue(_configInstance);
            set 
            {
                _property.SetValue(_configInstance, value);
                if (_rootConfig != null) 
                {
                    var saveMethod = typeof(Terraria.ModLoader.Config.ConfigManager).GetMethod("Save", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                    if (saveMethod != null) saveMethod.Invoke(null, new object[] { _rootConfig });
                }
            }
        }

        private bool _hoveringPill;
        private bool _wasMouseDown;

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            CalculatedStyle dimensions = GetDimensions();
            
            // Draw Label
            Vector2 textPos = new Vector2(dimensions.X + 10, dimensions.Y + 7);
            Utils.DrawBorderString(spriteBatch, _label, textPos, Color.White, 0.9f);

            // Draw Toggle Pill
            float pillWidth = 40f;
            float pillHeight = 20f;
            Vector2 pillPos = new Vector2(dimensions.X + dimensions.Width - pillWidth - 10, dimensions.Y + 5);
            Rectangle pillRect = new Rectangle((int)pillPos.X, (int)pillPos.Y, (int)pillWidth, (int)pillHeight);

            bool isOn = Value;
            Color pillColor = isOn ? new Color(170, 50, 200) : new Color(40, 20, 50);

            // Simple pill drawing (A real mod would use a custom texture, we use a scaled magic pixel for a box)
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, pillRect, pillColor);

            // Draw Nub
            float nubSize = 16f;
            float nubX = isOn ? pillRect.Right - nubSize - 2 : pillRect.Left + 2;
            Rectangle nubRect = new Rectangle((int)nubX, pillRect.Y + 2, (int)nubSize, (int)nubSize);
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, nubRect, Color.White);
            
            _hoveringPill = pillRect.Contains(Main.MouseScreen.ToPoint());
            
            if (!CustomConfigUIState.DialogOpen && (IsMouseHovering || _hoveringPill))
            {
                Main.LocalPlayer.mouseInterface = true;
                _onHover?.Invoke(_tooltip, _reloadRequired);
            }

            // Click Logic — skip if a file dialog is open
            bool isMouseDown = Main.mouseLeft;
            if (!CustomConfigUIState.DialogOpen && isMouseDown && !_wasMouseDown && _hoveringPill)
            {
                Value = !Value;
                SoundEngine.PlaySound(SoundID.MenuTick);
            }
            _wasMouseDown = isMouseDown;
        }
    }
}
