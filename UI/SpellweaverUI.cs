using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using ReLogic.Graphics;

namespace Stataria.UI
{
    public static class SpellweaverUI
    {
        public static void Draw(SpriteBatch spriteBatch)
        {
            if (Main.dedServ)
                return;

            Player player = Main.LocalPlayer;
            if (player == null || !player.active || player.dead)
                return;

            var spellPlayer = player.GetModPlayer<SpellweaverPlayer>();
            if (!spellPlayer.IsSpellweaverActive)
                return;

            var clientConfig = ModContent.GetInstance<StatariaClientConfig>();
            
            Vector2 basePosition = StatariaResourceDisplaySet.GetClampedResourceBarPosition();
            float screenPosX = basePosition.X;
            float screenPosY = basePosition.Y;

            int levelBoxWidth = 45;
            int barPadding = clientConfig.BarPadding;
            int barHeight = clientConfig.BarHeight;
            int barWidth = clientConfig.BarWidth;

            float chargeUIX = screenPosX + levelBoxWidth + barPadding;
            float chargeUIY;

            if (clientConfig.StretchXPBarToBottom)
            {
                chargeUIY = screenPosY + (barHeight + barPadding) * 2;
            }
            else
            {
                chargeUIY = screenPosY + (barHeight + barPadding) * 2 + (barHeight / 2) + barPadding;
            }

            int trackerHeight = 16;
            float maxCharge = spellPlayer.MaxElementalCharge;

            // Background of the entire container
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Rectangle containerRect = new Rectangle((int)chargeUIX, (int)chargeUIY, barWidth, trackerHeight);
            
            // Draw dark blue container background
            spriteBatch.Draw(pixel, containerRect, new Color(15, 30, 45, 180));
            DrawBorder(spriteBatch, containerRect, new Color(50, 100, 150, 200));

            // Draw progress bar
            float currentCharge = spellPlayer.ElementalCharge;
            float pct = maxCharge > 0 ? currentCharge / maxCharge : 0f;
            int fillWidth = (int)(barWidth * pct);

            Rectangle fillRect = new Rectangle((int)chargeUIX, (int)chargeUIY, fillWidth, trackerHeight);
            
            // Vibrant cyan/electric blue bar
            Color barColor = new Color(0, 200, 230);
            spriteBatch.Draw(pixel, fillRect, barColor);

            // Draw Text centered on the bar
            string chargeText = $"Elemental Charge: {(int)currentCharge}/{(int)maxCharge}";
            DynamicSpriteFont font = FontAssets.ItemStack.Value;
            float textScale = 0.75f;
            Vector2 textSize = font.MeasureString(chargeText) * textScale;
            Vector2 textPosition = new Vector2(
                chargeUIX + barWidth / 2 - textSize.X / 2,
                chargeUIY + trackerHeight / 2 - textSize.Y / 2);

            // Shadow
            spriteBatch.DrawString(font, chargeText, textPosition + new Vector2(1, 1) * textScale, Color.Black * 0.7f, 0f, Vector2.Zero, textScale, SpriteEffects.None, 0f);
            // Text
            spriteBatch.DrawString(font, chargeText, textPosition, Color.White, 0f, Vector2.Zero, textScale, SpriteEffects.None, 0f);
        }

        private static void DrawBorder(SpriteBatch spriteBatch, Rectangle rect, Color color)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            int thickness = 1;
            // Top
            spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
            // Bottom
            spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y + rect.Height - thickness, rect.Width, thickness), color);
            // Left
            spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
            // Right
            spriteBatch.Draw(pixel, new Rectangle(rect.X + rect.Width - thickness, rect.Y, thickness, rect.Height), color);
        }
    }
}
