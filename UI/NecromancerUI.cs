using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using ReLogic.Graphics;

namespace Stataria.UI
{
    public static class NecromancerUI
    {
        public static void Draw(SpriteBatch spriteBatch)
        {
            if (Main.dedServ)
                return;

            Player player = Main.LocalPlayer;
            if (player == null || !player.active || player.dead)
                return;

            var necPlayer = player.GetModPlayer<NecromancerPlayer>();
            if (!necPlayer.IsNecromancerActive)
                return;

            var clientConfig = ModContent.GetInstance<StatariaClientConfig>();
            
            Vector2 basePosition = StatariaResourceDisplaySet.GetClampedResourceBarPosition();
            float screenPosX = basePosition.X;
            float screenPosY = basePosition.Y;

            int levelBoxWidth = 45;
            int barPadding = clientConfig.BarPadding;
            int barHeight = clientConfig.BarHeight;
            int barWidth = clientConfig.BarWidth;

            float soulUIX = screenPosX + levelBoxWidth + barPadding;
            float soulUIY;

            if (clientConfig.StretchXPBarToBottom)
            {
                soulUIY = screenPosY + (barHeight + barPadding) * 2;
            }
            else
            {
                soulUIY = screenPosY + (barHeight + barPadding) * 2 + (barHeight / 2) + barPadding;
            }

            int trackerHeight = 16;
            int maxCapacity = necPlayer.GetMaxSoulCapacity();
            float maxDuration = necPlayer.GetMaxSoulDuration();

            // Background of the entire container
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Rectangle containerRect = new Rectangle((int)soulUIX, (int)soulUIY, barWidth, trackerHeight);
            
            // Draw dark purple container background
            spriteBatch.Draw(pixel, containerRect, new Color(30, 15, 45, 180));
            DrawBorder(spriteBatch, containerRect, new Color(100, 50, 150, 200));

            // Draw progress bar
            int currentSouls = necPlayer.SoulReserveLifetimes.Count;
            float pct = maxCapacity > 0 ? (float)currentSouls / maxCapacity : 0f;
            int fillWidth = (int)(barWidth * pct);

            Rectangle fillRect = new Rectangle((int)soulUIX, (int)soulUIY, fillWidth, trackerHeight);
            
            // Solid deep purple color for the reserve bar
            Color barColor = new Color(130, 30, 200);
            spriteBatch.Draw(pixel, fillRect, barColor);

            // Draw Text centered on the bar
            string soulText = $"Soul Reserve: {currentSouls}/{maxCapacity}";
            DynamicSpriteFont font = FontAssets.ItemStack.Value;
            float textScale = 0.75f;
            Vector2 textSize = font.MeasureString(soulText) * textScale;
            Vector2 textPosition = new Vector2(
                soulUIX + barWidth / 2 - textSize.X / 2,
                soulUIY + trackerHeight / 2 - textSize.Y / 2);

            // Shadow
            spriteBatch.DrawString(font, soulText, textPosition + new Vector2(1, 1) * textScale, Color.Black * 0.7f, 0f, Vector2.Zero, textScale, SpriteEffects.None, 0f);
            // Text
            spriteBatch.DrawString(font, soulText, textPosition, Color.White, 0f, Vector2.Zero, textScale, SpriteEffects.None, 0f);
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
