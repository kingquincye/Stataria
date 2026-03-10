using Terraria;
using Terraria.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria.GameContent.UI.Elements;
using Terraria.Audio;
using Terraria.ID;

namespace Stataria
{
    public class CheckBoxElement : UIElement
    {
        private bool isChecked;
        private UIText checkText;
        private readonly Color checkedColor = new Color(80, 220, 80);
        private readonly Color uncheckedColor = new Color(190, 190, 190);
        private string label;

        public bool IsChecked
        {
            get => isChecked;
            set
            {
                isChecked = value;
                UpdateCheckText();
            }
        }

        public event Action<bool> OnCheckChanged;

        public CheckBoxElement(string label = "")
        {
            this.label = label;
            Width.Set(24f, 0f);
            Height.Set(24f, 0f);

            checkText = new UIText(isChecked ? "✓" : "□", 1f);
            checkText.HAlign = 0.5f;
            checkText.VAlign = 0.5f;
            checkText.TextColor = isChecked ? checkedColor : uncheckedColor;
            Append(checkText);
        }

        private void UpdateCheckText()
        {
            checkText.SetText(isChecked ? "✓" : "□");
            checkText.TextColor = isChecked ? checkedColor : uncheckedColor;
        }

        public override void LeftClick(UIMouseEvent evt)
        {
            base.LeftClick(evt);

            isChecked = !isChecked;
            UpdateCheckText();
            SoundEngine.PlaySound(SoundID.MenuTick);

            OnCheckChanged?.Invoke(isChecked);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            base.Draw(spriteBatch);

            if (!string.IsNullOrEmpty(label))
            {
                Vector2 position = GetDimensions().Position();
                Vector2 textPosition = new Vector2(position.X + Width.Pixels + 5, position.Y + Height.Pixels / 2 - 10);
                Utils.DrawBorderString(spriteBatch, label, textPosition, Color.White, 0.8f);
            }
        }
    }
}