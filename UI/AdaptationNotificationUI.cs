using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.UI;
using Stataria.Core;
using Terraria.ModLoader;
using ReLogic.Graphics;

namespace Stataria.UI
{
    public class AdaptationNotificationUI : UIState
    {
        private static readonly List<AdaptationNotification> notifications = new List<AdaptationNotification>();

        public static void AddNotification(AdaptationNotification notification)
        {
            var configClient = ModContent.GetInstance<StatariaClientConfig>();
            if (configClient != null && !configClient.ShowAdaptationNotifications)
                return;

            int maxNotifications = configClient != null ? Math.Clamp(configClient.MaxAdaptationNotifications, 1, 20) : 5;

            lock (notifications)
            {
                var existing = notifications.Find(n => n.Category == notification.Category && n.DisplayName == notification.DisplayName && n.IsOffensive == notification.IsOffensive);
                if (existing != null)
                {
                    existing.Level = notification.Level;
                    existing.ExpProgress = notification.ExpProgress;
                    existing.IsLevelUp = notification.IsLevelUp;
                    existing.TimeRemaining = notification.MaxDuration;
                    existing.Alpha = 1.0f;
                    return;
                }

                while (notifications.Count >= maxNotifications && notifications.Count > 0)
                {
                    notifications.RemoveAt(0);
                }

                notifications.Add(notification);
            }
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            float delta = (float)gameTime.ElapsedGameTime.TotalSeconds;
            lock (notifications)
            {
                for (int i = notifications.Count - 1; i >= 0; i--)
                {
                    if (notifications[i].Update(delta))
                    {
                        notifications.RemoveAt(i);
                    }
                }
            }
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);

            if (notifications.Count == 0)
                return;

            var configClient = ModContent.GetInstance<StatariaClientConfig>();
            if (configClient != null && !configClient.ShowAdaptationNotifications)
                return;

            float xPct = configClient != null ? configClient.NotificationPositionXPercent : 0.02f;
            float yPct = configClient != null ? configClient.NotificationPositionYPercent : 0.80f;

            // Percentage-based positioning adapting dynamically to any screen resolution / slider setting
            int startX = (int)(Main.screenWidth * xPct);
            int startY = (int)(Main.screenHeight * yPct);

            // Card width scales adaptively with screen resolution (min 240px, max 380px)
            int cardWidth = Math.Clamp((int)(Main.screenWidth * 0.18f), 240, 380);
            int cardHeight = 36;
            int cardPadding = 6;

            List<AdaptationNotification> currentList;
            lock (notifications)
            {
                currentList = new List<AdaptationNotification>(notifications);
            }

            ReLogic.Graphics.DynamicSpriteFont font = FontAssets.MouseText.Value;
            int maxLevel = AdaptationData.GetMaxLevel();

            for (int i = 0; i < currentList.Count; i++)
            {
                var item = currentList[i];
                int drawY = startY - (i * (cardHeight + cardPadding));
                float alpha = item.Alpha;

                Color catColor = item.Category.GetCategoryColor();
                Color bgColor = new Color(15, 18, 25, 210) * alpha;
                Color barBgColor = new Color(30, 35, 45, 230) * alpha;
                Color barFillColor = catColor * alpha;
                Color textColor = Color.White * alpha;
                Color catAccentColor = catColor * alpha;

                Rectangle cardRect = new Rectangle(startX, drawY, cardWidth, cardHeight);

                // Draw background panel
                spriteBatch.Draw(TextureAssets.MagicPixel.Value, cardRect, bgColor);

                // Draw left accent bar (Category color)
                Rectangle accentRect = new Rectangle(startX, drawY, 5, cardHeight);
                spriteBatch.Draw(TextureAssets.MagicPixel.Value, accentRect, catAccentColor);

                // Level Text calculation
                string levelText = item.Level >= maxLevel 
                    ? Terraria.Localization.Language.GetTextValue("Mods.Stataria.AdaptationUI.MaxLevel", maxLevel)
                    : Terraria.Localization.Language.GetTextValue("Mods.Stataria.AdaptationUI.LevelProgress", item.Level, maxLevel);

                if (item.IsLevelUp)
                {
                    levelText = Terraria.Localization.Language.GetTextValue("Mods.Stataria.AdaptationUI.LevelUp", item.Level);
                }

                float levelScale = 0.7f;
                Vector2 levelSize = font.MeasureString(levelText) * levelScale;
                Vector2 levelPos = new Vector2(startX + cardWidth - levelSize.X - 8, drawY + 4);

                // Title Text measurement & responsive truncation
                string prefix = item.IsOffensive 
                    ? Terraria.Localization.Language.GetTextValue("Mods.Stataria.AdaptationUI.OffensePrefix") 
                    : Terraria.Localization.Language.GetTextValue("Mods.Stataria.AdaptationUI.DefensePrefix");

                if (item.Category != AdaptationCategory.Mob && item.Category != AdaptationCategory.Boss)
                {
                    prefix = "";
                }

                string rawTitle = $"{prefix}{item.DisplayName}";
                float titleScale = 0.75f;
                float maxTitleWidth = cardWidth - levelSize.X - 24f;

                string titleText = rawTitle;
                if (font.MeasureString(titleText).X * titleScale > maxTitleWidth)
                {
                    while (titleText.Length > 3 && font.MeasureString(titleText + "..").X * titleScale > maxTitleWidth)
                    {
                        titleText = titleText.Substring(0, titleText.Length - 1);
                    }
                    titleText += "..";
                }

                Vector2 titlePos = new Vector2(startX + 12, drawY + 3);

                // Draw Title Text
                DynamicSpriteFontExtensionMethods.DrawString(spriteBatch, font, titleText, titlePos, textColor, 0f, Vector2.Zero, titleScale, SpriteEffects.None, 0f);

                // Draw Level Text
                Color levelColor = item.IsLevelUp ? Color.Gold * alpha : (item.Level >= maxLevel ? Color.DeepSkyBlue * alpha : textColor);
                DynamicSpriteFontExtensionMethods.DrawString(spriteBatch, font, levelText, levelPos, levelColor, 0f, Vector2.Zero, levelScale, SpriteEffects.None, 0f);

                // Draw Progress Bar
                int barX = startX + 12;
                int barY = drawY + 22;
                int barW = cardWidth - 20;
                int barH = 7;

                Rectangle barBgRect = new Rectangle(barX, barY, barW, barH);
                spriteBatch.Draw(TextureAssets.MagicPixel.Value, barBgRect, barBgColor);

                float fillPct = item.Level >= maxLevel ? 1.0f : item.ExpProgress;
                int fillW = (int)(barW * fillPct);
                if (fillW > 0)
                {
                    Rectangle barFillRect = new Rectangle(barX, barY, fillW, barH);
                    spriteBatch.Draw(TextureAssets.MagicPixel.Value, barFillRect, barFillColor);
                }
            }
        }
    }
}
