using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using ReLogic.Graphics;
using Terraria.GameContent.UI.ResourceSets;
using System;

namespace Stataria
{
    public class StatariaResourceDisplaySet : ModResourceDisplaySet
    {
        private Texture2D pixelTexture;
        private Vector2 basePosition = Vector2.Zero;
        private int barWidth;
        private int barHeight;
        private int barPadding;

        private Color healthColor = Color.Red;
        private Color healthBgColor = Color.DarkRed * 0.7f;

        private Color manaColor = Color.Blue;
        private Color manaBgColor = Color.DarkBlue * 0.7f;

        private Color xpColor = Color.Gold;
        private Color xpBgColor = Color.Goldenrod * 0.7f;

        private Color borderColor = Color.White * 0.8f;
        private Color textColor = Color.White;
        private Color textShadowColor = Color.Black * 0.7f;

        private Color levelBoxColor = new Color(160, 75, 220, 200);
        private Color rebirthTextColorDarkPurple = new Color(75, 0, 130);

        private int levelBoxWidth = 45;
        private int levelBoxHeight = 56;
        private float textScale = 0.8f;

        public override void PreDrawResources(PlayerStatsSnapshot snapshot)
        {
            pixelTexture = TextureAssets.MagicPixel.Value;
            var config = ModContent.GetInstance<StatariaConfig>();

            float screenPosX = Main.screenWidth * config.resourceBars.PositionXPercent;
            float screenPosY = Main.screenHeight * config.resourceBars.PositionYPercent;

            basePosition = new Vector2(screenPosX, screenPosY);
            barWidth = config.resourceBars.BarWidth;
            barHeight = config.resourceBars.BarHeight;
            barPadding = config.resourceBars.BarPadding;
        }

        public override void DrawLife(SpriteBatch spriteBatch)
        {
            Player player = Main.LocalPlayer;
            RPGPlayer rpgPlayer = player.GetModPlayer<RPGPlayer>();

            DrawLevelBox(spriteBatch, rpgPlayer);

            Vector2 healthBarPosition = new Vector2(
                basePosition.X + levelBoxWidth + barPadding,
                basePosition.Y);

            spriteBatch.Draw(
                pixelTexture,
                new Rectangle((int)healthBarPosition.X, (int)healthBarPosition.Y, barWidth, barHeight),
                new Rectangle(0, 0, 1, 1),
                healthBgColor);

            float healthPercent = player.statLifeMax2 > 0 ? MathHelper.Clamp((float)player.statLife / player.statLifeMax2, 0f, 1f) : 0;
            int healthFillWidth = (int)(barWidth * healthPercent);
            spriteBatch.Draw(
                pixelTexture,
                new Rectangle((int)healthBarPosition.X, (int)healthBarPosition.Y, healthFillWidth, barHeight),
                new Rectangle(0, 0, 1, 1),
                healthColor);

            DrawBarBorders(spriteBatch, healthBarPosition, barWidth, barHeight);

            int displayHealth = Math.Min(player.statLife, player.statLifeMax2);
            string healthText = $"{displayHealth}/{player.statLifeMax2}";
            DynamicSpriteFont font = FontAssets.ItemStack.Value;
            Vector2 textSize = font.MeasureString(healthText) * textScale;
            Vector2 textPosition = new Vector2(
                healthBarPosition.X + barWidth / 2 - textSize.X / 2,
                healthBarPosition.Y + barHeight / 2 - textSize.Y / 2);

            spriteBatch.DrawString(font, healthText, textPosition + new Vector2(1, 1) * textScale, textShadowColor, 0f, Vector2.Zero, textScale, SpriteEffects.None, 0f);
            spriteBatch.DrawString(font, healthText, textPosition, textColor, 0f, Vector2.Zero, textScale, SpriteEffects.None, 0f);
        }

        public override void DrawMana(SpriteBatch spriteBatch)
        {
            Player player = Main.LocalPlayer;
            RPGPlayer rpgPlayer = player.GetModPlayer<RPGPlayer>();

            Vector2 manaBarPosition = new Vector2(
                basePosition.X + levelBoxWidth + barPadding,
                basePosition.Y + barHeight + barPadding);

            spriteBatch.Draw(
                pixelTexture,
                new Rectangle((int)manaBarPosition.X, (int)manaBarPosition.Y, barWidth, barHeight),
                new Rectangle(0, 0, 1, 1),
                manaBgColor);

            float manaPercent = player.statManaMax2 == 0 ? 0 : MathHelper.Clamp((float)player.statMana / player.statManaMax2, 0f, 1f);
            int manaFillWidth = (int)(barWidth * manaPercent);
            spriteBatch.Draw(
                pixelTexture,
                new Rectangle((int)manaBarPosition.X, (int)manaBarPosition.Y, manaFillWidth, barHeight),
                new Rectangle(0, 0, 1, 1),
                manaColor);

            DrawBarBorders(spriteBatch, manaBarPosition, barWidth, barHeight);

            int displayMana = Math.Min(player.statMana, player.statManaMax2);
            string manaText = $"{displayMana}/{player.statManaMax2}";
            DynamicSpriteFont font = FontAssets.ItemStack.Value;
            Vector2 textSize = font.MeasureString(manaText) * textScale;
            Vector2 textPosition = new Vector2(
                manaBarPosition.X + barWidth / 2 - textSize.X / 2,
                manaBarPosition.Y + barHeight / 2 - textSize.Y / 2);

            spriteBatch.DrawString(font, manaText, textPosition + new Vector2(1, 1) * textScale, textShadowColor, 0f, Vector2.Zero, textScale, SpriteEffects.None, 0f);
            spriteBatch.DrawString(font, manaText, textPosition, textColor, 0f, Vector2.Zero, textScale, SpriteEffects.None, 0f);

            DrawXpBar(spriteBatch, rpgPlayer);
        }

        private void DrawBarBorders(SpriteBatch spriteBatch, Vector2 position, int width, int height)
        {
            int borderThickness = 1;
            spriteBatch.Draw(pixelTexture, new Rectangle((int)position.X, (int)position.Y, width, borderThickness), borderColor);
            spriteBatch.Draw(pixelTexture, new Rectangle((int)position.X, (int)position.Y + height - borderThickness, width, borderThickness), borderColor);
            spriteBatch.Draw(pixelTexture, new Rectangle((int)position.X, (int)position.Y, borderThickness, height), borderColor);
            spriteBatch.Draw(pixelTexture, new Rectangle((int)position.X + width - borderThickness, (int)position.Y, borderThickness, height), borderColor);
        }

        private void DrawLevelBox(SpriteBatch spriteBatch, RPGPlayer rpgPlayer)
        {
            spriteBatch.Draw(
                pixelTexture,
                new Rectangle((int)basePosition.X, (int)basePosition.Y, levelBoxWidth, levelBoxHeight),
                new Rectangle(0, 0, 1, 1),
                levelBoxColor);

            DrawBarBorders(spriteBatch, basePosition, levelBoxWidth, levelBoxHeight);

            DynamicSpriteFont font = FontAssets.ItemStack.Value;
            float currentTextScale = 0.9f;

            string levelLabelText = "Lv.";
            Vector2 levelLabelTextSize = font.MeasureString(levelLabelText) * currentTextScale;
            Vector2 levelLabelTextPosition = new Vector2(
                basePosition.X + levelBoxWidth / 2 - levelLabelTextSize.X / 2,
                basePosition.Y + levelBoxHeight * 0.20f - levelLabelTextSize.Y /2 );

            spriteBatch.DrawString(font, levelLabelText, levelLabelTextPosition + new Vector2(1,1)*currentTextScale, textShadowColor,0f, Vector2.Zero, currentTextScale, SpriteEffects.None, 0f);
            spriteBatch.DrawString(font, levelLabelText, levelLabelTextPosition, textColor, 0f, Vector2.Zero, currentTextScale, SpriteEffects.None, 0f);

            string levelNumberText = rpgPlayer.Level.ToString();
            Vector2 levelNumberSize = font.MeasureString(levelNumberText) * currentTextScale;
            Vector2 levelNumberPosition = new Vector2(
                basePosition.X + levelBoxWidth / 2 - levelNumberSize.X / 2,
                levelLabelTextPosition.Y + levelLabelTextSize.Y * 0.8f);

            spriteBatch.DrawString(font, levelNumberText, levelNumberPosition + new Vector2(1,1)*currentTextScale, textShadowColor, 0f, Vector2.Zero, currentTextScale, SpriteEffects.None, 0f);
            spriteBatch.DrawString(font, levelNumberText, levelNumberPosition, textColor, 0f, Vector2.Zero, currentTextScale, SpriteEffects.None, 0f);

            if (rpgPlayer.RebirthCount > 0)
            {
                string rebirthText = $"R{rpgPlayer.RebirthCount}";
                float rebirthTextScale = 0.8f;
                Vector2 rebirthTextSize = font.MeasureString(rebirthText) * rebirthTextScale;
                Vector2 rebirthPosition = new Vector2(
                    basePosition.X + levelBoxWidth / 2 - rebirthTextSize.X / 2,
                    levelNumberPosition.Y + levelNumberSize.Y * 0.9f);

                spriteBatch.DrawString(font, rebirthText, rebirthPosition + new Vector2(1, 1)*rebirthTextScale, Color.Black *0.7f, 0f, Vector2.Zero, rebirthTextScale, SpriteEffects.None, 0f);
                spriteBatch.DrawString(font, rebirthText, rebirthPosition, rebirthTextColorDarkPurple, 0f, Vector2.Zero, rebirthTextScale, SpriteEffects.None, 0f);
            }
        }

        private void DrawXpBar(SpriteBatch spriteBatch, RPGPlayer rpgPlayer)
        {
            int xpBarHeight = barHeight / 2;
            Vector2 xpBarPosition = new Vector2(
                basePosition.X + levelBoxWidth + barPadding,
                basePosition.Y + (barHeight + barPadding) * 2);

            spriteBatch.Draw(
                pixelTexture,
                new Rectangle((int)xpBarPosition.X, (int)xpBarPosition.Y, barWidth, xpBarHeight),
                new Rectangle(0, 0, 1, 1),
                xpBgColor);

            float xpPercent = rpgPlayer.XPToNext > 0 ? (float)rpgPlayer.XP / rpgPlayer.XPToNext : 0;
            int xpFillWidth = (int)(barWidth * xpPercent);
            spriteBatch.Draw(
                pixelTexture,
                new Rectangle((int)xpBarPosition.X, (int)xpBarPosition.Y, xpFillWidth, xpBarHeight),
                new Rectangle(0, 0, 1, 1),
                xpColor);

            DrawBarBorders(spriteBatch, xpBarPosition, barWidth, xpBarHeight);

        }

        public override bool PreHover(out bool hoveringLife)
        {
            Player player = Main.LocalPlayer;
            Rectangle healthBarRect = new Rectangle(
                (int)(basePosition.X + levelBoxWidth + barPadding),
                (int)basePosition.Y,
                barWidth,
                barHeight);

            Rectangle manaBarRect = new Rectangle(
                (int)(basePosition.X + levelBoxWidth + barPadding),
                (int)(basePosition.Y + barHeight + barPadding),
                barWidth,
                barHeight);

            Point mousePoint = new Point(Main.mouseX, Main.mouseY);

            if (healthBarRect.Contains(mousePoint))
            {
                hoveringLife = true;
                return true;
            }
            else if (manaBarRect.Contains(mousePoint))
            {
                hoveringLife = false;
                return true;
            }

            hoveringLife = false;
            return false;
        }
    }
}