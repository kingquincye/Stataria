using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.UI;
using Terraria.GameContent;
using System.Text.RegularExpressions;

namespace Stataria.UI.Elements
{
    public class UIHeader : UIElement
    {
        private string _text;

        public UIHeader(string text)
        {
            // Fallback formatting: If translation isn't provided, format camel case and underscores
            _text = FormatText(text);
            Width.Set(0, 1f);
            Height.Set(50, 0f);
        }

        private string FormatText(string str)
        {
            if (string.IsNullOrEmpty(str)) return str;
            
            // Try treating as translation key if it's not looking like a plain string
            string localized = Terraria.Localization.Language.GetTextValue(str);
            if (localized != str) return localized;

            string result = str.Replace("_", " ");
            result = Regex.Replace(result, "([a-z])([A-Z])", "$1 $2");
            return char.ToUpper(result[0]) + result.Substring(1);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            CalculatedStyle dimensions = GetDimensions();
            
            // Draw a subtle line separator above
            float lineY = dimensions.Y + 12f;
            Utils.DrawLine(spriteBatch, new Vector2(dimensions.X + 30, lineY), new Vector2(dimensions.X + dimensions.Width - 30, lineY), new Color(130, 60, 180) * 0.6f, new Color(130, 60, 180) * 0.6f, 2f);

            // Draw Centered Text
            // Using DeathText for a prominent header look, scaled down nicely
            Vector2 textSize = FontAssets.DeathText.Value.MeasureString(_text) * 0.45f;
            Vector2 textPos = new Vector2(dimensions.X + (dimensions.Width / 2f) - (textSize.X / 2f), dimensions.Y + 15f);
            
            Utils.DrawBorderStringFourWay(spriteBatch, FontAssets.DeathText.Value, _text, textPos.X, textPos.Y, Color.LightGray, Color.Black, Vector2.Zero, 0.45f);
            
            // Draw a subtle line separator below
            float bottomLineY = dimensions.Y + 45f;
            Utils.DrawLine(spriteBatch, new Vector2(dimensions.X + 30, bottomLineY), new Vector2(dimensions.X + dimensions.Width - 30, bottomLineY), new Color(130, 60, 180) * 0.6f, new Color(130, 60, 180) * 0.6f, 2f);
        }
    }
}
