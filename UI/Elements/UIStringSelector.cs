using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Reflection;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.Config.UI;
using Terraria.UI;

namespace Stataria.UI.Elements
{
    public class UIStringSelector : UIElement
    {
        private UIText label;
        private UITextPanel<string> selectionButton;
        
        private string name;
        private PropertyFieldWrapper fieldInfo;
        private object configInstance;
        private ModConfig parentConfig;
        
        private string[] options;
        private int currentIndex = 0;

        private Action<string, bool> onHover;
        private string tooltipText;
        private bool reloadRequired;

        public UIStringSelector(string name, PropertyFieldWrapper fieldInfo, object configInstance, string[] options, ModConfig parentConfig, string tooltipText = "", bool reloadRequired = false, Action<string, bool> onHover = null)
        {
            this.name = name;
            this.fieldInfo = fieldInfo;
            this.configInstance = configInstance;
            this.options = options;
            this.parentConfig = parentConfig;
            this.tooltipText = tooltipText;
            this.reloadRequired = reloadRequired;
            this.onHover = onHover;

            Height.Set(40f, 0f);
            Width.Set(0f, 1f);

            label = new UIText(name, 0.9f);
            label.VAlign = 0.5f;
            label.Left.Set(10f, 0f);
            Append(label);

            string currentValue = (string)fieldInfo.GetValue(configInstance);
            currentIndex = Math.Max(0, Array.IndexOf(options, currentValue));

            selectionButton = new UITextPanel<string>(options[currentIndex], 0.8f);
            selectionButton.Width.Set(150f, 0f);
            selectionButton.Height.Set(30f, 0f);
            selectionButton.HAlign = 1f;
            selectionButton.VAlign = 0.5f;
            selectionButton.BackgroundColor = new Color(50, 30, 80);
            
            selectionButton.OnMouseOver += (evt, el) => {
                if (CustomConfigUIState.DialogOpen) return;
                selectionButton.BackgroundColor = new Color(100, 50, 150);
                if (this.onHover != null) this.onHover(this.tooltipText, this.reloadRequired);
            };
            selectionButton.OnMouseOut += (evt, el) => {
                if (CustomConfigUIState.DialogOpen) return;
                selectionButton.BackgroundColor = new Color(50, 30, 80);
                if (this.onHover != null) this.onHover("", false);
            };
            
            selectionButton.OnLeftClick += (evt, el) => {
                if (CustomConfigUIState.DialogOpen) return;
                SoundEngine.PlaySound(SoundID.MenuTick);
                currentIndex = (currentIndex + 1) % options.Length;
                string newValue = options[currentIndex];
                selectionButton.SetText(newValue);
                fieldInfo.SetValue(configInstance, newValue);
            };
            
            Append(selectionButton);
        }
        
        public override void Draw(SpriteBatch spriteBatch)
        {
            base.Draw(spriteBatch);
            if (!CustomConfigUIState.DialogOpen && IsMouseHovering && onHover != null)
            {
                onHover(tooltipText, reloadRequired);
            }
        }
    }
}
