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
        private static bool _dragging = false;
        private static Vector2 _dragOffset;

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
            if (clientConfig == null || !clientConfig.EnableNecromancerHUD)
                return;

            var config = ModContent.GetInstance<StatariaConfig>();

            // 1. Initialize default coordinates if not set
            if (necPlayer.NecromancerHUDX < 0 || necPlayer.NecromancerHUDY < 0)
            {
                // Default position: near the bottom-middle of the screen or below default resource bars position
                necPlayer.NecromancerHUDX = Main.screenWidth * 0.79f + 120f;
                necPlayer.NecromancerHUDY = Main.screenHeight * 0.15f;
            }

            int outerDiameter = 60;
            int innerDiameter = 44;
            int padding = 4;
            int textHeight = 14;
            int hudWidth = outerDiameter;
            int hudHeight = outerDiameter + padding + textHeight;

            // Clamp coordinates to screen limits
            necPlayer.NecromancerHUDX = Math.Clamp(necPlayer.NecromancerHUDX, 10f, Math.Max(10f, Main.screenWidth - hudWidth - 10f));
            necPlayer.NecromancerHUDY = Math.Clamp(necPlayer.NecromancerHUDY, 10f, Math.Max(10f, Main.screenHeight - hudHeight - 10f));

            Vector2 position = new Vector2(necPlayer.NecromancerHUDX, necPlayer.NecromancerHUDY);
            Rectangle hudBounds = new Rectangle((int)position.X - 10, (int)position.Y - 10, hudWidth + 20, hudHeight + 20);

            // 2. Handle Dragging
            Vector2 mouseScreen = Main.MouseScreen;
            bool isHoldingModKey = Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftControl) || 
                                   Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftAlt);

            if (isHoldingModKey)
            {
                // Draw dashed helper border to show the drag bounds
                DrawDashedBorder(spriteBatch, hudBounds, Color.White * 0.5f);

                if (hudBounds.Contains(mouseScreen.ToPoint()))
                {
                    player.mouseInterface = true; // Prevent clicking through UI

                    if (Main.mouseLeft && !_dragging)
                    {
                        _dragging = true;
                        _dragOffset = position - mouseScreen;
                    }
                }

                if (_dragging)
                {
                    player.mouseInterface = true;
                    if (Main.mouseLeft)
                    {
                        Vector2 newPos = mouseScreen + _dragOffset;
                        necPlayer.NecromancerHUDX = Math.Clamp(newPos.X, 10f, Main.screenWidth - hudWidth - 10f);
                        necPlayer.NecromancerHUDY = Math.Clamp(newPos.Y, 10f, Main.screenHeight - hudHeight - 10f);
                        position = new Vector2(necPlayer.NecromancerHUDX, necPlayer.NecromancerHUDY);
                    }
                    else
                    {
                        _dragging = false;
                    }
                }
            }
            else
            {
                _dragging = false;

                // Show tooltip when hovering over HUD bounds to inform the user they can drag it
                if (hudBounds.Contains(mouseScreen.ToPoint()))
                {
                    player.mouseInterface = true; // Prevent clicking through
                    Main.instance.MouseText(Terraria.Localization.Language.GetTextValue("Mods.Stataria.UI.NecromancerHUDDragInstruction"));
                }
            }

            // 3. Render the Soul Orb Widget
            Vector2 widgetCenter = position + new Vector2(outerDiameter / 2f, outerDiameter / 2f);
            
            // Draw filled purple background orb (Soul Reserve Core)
            DrawFilledCircle(spriteBatch, widgetCenter, innerDiameter / 2f, new Color(30, 10, 45, 230));

            // Outer Active Thralls Ring (emerald green)
            int baseLimit = config.roleSettings.NecromancerBaseThralls;
            int spr = player.GetModPlayer<RPGPlayer>().GetEffectiveStat("SPR");
            int sprPerThrall = config.roleSettings.NecromancerSPRPerThrall;
            int calculatedLimit = baseLimit + (spr / sprPerThrall);

            int thrallLimit = calculatedLimit;
            if (config.roleSettings.NecromancerLimitZombieThralls)
            {
                thrallLimit = Math.Min(thrallLimit, config.roleSettings.NecromancerActiveThrallsLimit);
            }
            int currentActive = necPlayer.GetActiveThrallCount();
            float thrallPct = thrallLimit > 0 ? (float)currentActive / thrallLimit : 0f;
            Color thrallColor = new Color(46, 204, 113); // Emerald / cursed green
            DrawCircleOutline(spriteBatch, widgetCenter, outerDiameter / 2f, Math.Min(thrallPct, 1f), thrallColor, 2f);

            // Inner Soul Reserve Ring (purple)
            int currentSouls = necPlayer.SoulReserveLifetimes.Count;
            int maxCapacity = necPlayer.GetMaxSoulCapacity();
            float soulPct = maxCapacity > 0 ? (float)currentSouls / maxCapacity : 0f;
            Color soulColor = new Color(155, 89, 182); // Amethyst purple
            DrawCircleOutline(spriteBatch, widgetCenter, innerDiameter / 2f - 1f, Math.Min(soulPct, 1f), soulColor, 2f);

            // Draw Soul count text centered inside the orb
            DynamicSpriteFont font = FontAssets.ItemStack.Value;
            string line1 = Terraria.Localization.Language.GetTextValue("Mods.Stataria.UI.NecromancerHUDReserve");
            string line2 = $"{currentSouls}/{maxCapacity}";
            Vector2 size1 = font.MeasureString(line1);
            Vector2 size2 = font.MeasureString(line2);

            float maxWidth = Math.Max(size1.X, size2.X);
            float totalHeight = size1.Y + size2.Y - 2f;

            // Fit bounding box within circle (with padding)
            float targetDim = innerDiameter - 10f;
            float scaleX = targetDim / maxWidth;
            float scaleY = targetDim / totalHeight;
            float scale = Math.Min(0.7f, Math.Min(scaleX, scaleY));

            Vector2 drawScale = new Vector2(scale, scale);
            Vector2 line1Size = size1 * scale;
            Vector2 line2Size = size2 * scale;

            // Compute vertical centers relative to widgetCenter
            Vector2 line1Pos = new Vector2(widgetCenter.X - line1Size.X / 2f, widgetCenter.Y - (totalHeight * scale) / 2f);
            Vector2 line2Pos = new Vector2(widgetCenter.X - line2Size.X / 2f, line1Pos.Y + size1.Y * scale - 2f * scale);

            // Draw shadows
            spriteBatch.DrawString(font, line1, line1Pos + new Vector2(1, 1) * scale, Color.Black * 0.8f, 0f, Vector2.Zero, drawScale, SpriteEffects.None, 0f);
            spriteBatch.DrawString(font, line2, line2Pos + new Vector2(1, 1) * scale, Color.Black * 0.8f, 0f, Vector2.Zero, drawScale, SpriteEffects.None, 0f);

            // Draw text
            spriteBatch.DrawString(font, line1, line1Pos, new Color(200, 180, 220), 0f, Vector2.Zero, drawScale, SpriteEffects.None, 0f);
            spriteBatch.DrawString(font, line2, line2Pos, Color.White, 0f, Vector2.Zero, drawScale, SpriteEffects.None, 0f);

            // Draw Thralls Count Overlay on the outer ring edge or just floating stats below
            int baseDamage = config.roleSettings.NecromancerThrallBaseDamage;
            int sprStat = player.GetModPlayer<RPGPlayer>().GetEffectiveStat("SPR");
            int rebirthCount = player.GetModPlayer<RPGPlayer>().RebirthCount;
            int level = player.GetModPlayer<RPGPlayer>().Level;
            float levelBonus = level * config.roleSettings.NecromancerThrallDamageIncreasePerLevel;
            float totalBaseDamage = baseDamage + levelBonus;
            float rebirthMult = 1f + (rebirthCount * config.roleSettings.NecromancerThrallDamageIncreasePerRebirth / 100f);
            int damage = (int)(totalBaseDamage * rebirthMult * (1f + sprStat * config.roleSettings.NecromancerThrallSPRScale / 100f));
            float maxDuration = necPlayer.GetMaxSoulDuration();

            // Stats text below the orb
            string statsText = Terraria.Localization.Language.GetTextValue("Mods.Stataria.UI.NecromancerHUDStats", damage, currentActive, thrallLimit);
            float statsTextScale = 0.70f;
            Vector2 statsTextSize = font.MeasureString(statsText) * statsTextScale;
            Vector2 statsTextPosition = new Vector2(
                widgetCenter.X - statsTextSize.X / 2f,
                widgetCenter.Y + outerDiameter / 2f + padding
            );

            // Shadow
            spriteBatch.DrawString(font, statsText, statsTextPosition + new Vector2(1, 1) * statsTextScale, Color.Black * 0.7f, 0f, Vector2.Zero, statsTextScale, SpriteEffects.None, 0f);
            // Text
            spriteBatch.DrawString(font, statsText, statsTextPosition, new Color(200, 180, 220), 0f, Vector2.Zero, statsTextScale, SpriteEffects.None, 0f);
        }

        private static void DrawFilledCircle(SpriteBatch spriteBatch, Vector2 center, float radius, Color color)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            int r = (int)radius;
            for (int y = -r; y <= r; y++)
            {
                int width = (int)Math.Sqrt(r * r - y * y);
                spriteBatch.Draw(pixel, new Rectangle((int)center.X - width, (int)center.Y + y, width * 2, 1), color);
            }
        }

        private static void DrawCircleOutline(SpriteBatch spriteBatch, Vector2 center, float radius, float progress, Color color, float thickness)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            int numSegments = 80;
            for (int i = 0; i < numSegments; i++)
            {
                float segmentPct = (float)i / numSegments;
                if (segmentPct <= progress)
                {
                    float angle = i * MathHelper.TwoPi / numSegments - MathHelper.PiOver2; // Starts from top
                    Vector2 offset = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * radius;
                    Vector2 pos = center + offset;
                    spriteBatch.Draw(pixel, new Rectangle((int)pos.X - (int)(thickness / 2f), (int)pos.Y - (int)(thickness / 2f), (int)thickness, (int)thickness), color);
                }
            }
        }

        private static void DrawDashedBorder(SpriteBatch spriteBatch, Rectangle rect, Color color)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            int step = 4;

            // Top
            for (int x = rect.X; x < rect.X + rect.Width; x += step * 2)
            {
                spriteBatch.Draw(pixel, new Rectangle(x, rect.Y, Math.Min(step, rect.X + rect.Width - x), 1), color);
            }
            // Bottom
            for (int x = rect.X; x < rect.X + rect.Width; x += step * 2)
            {
                spriteBatch.Draw(pixel, new Rectangle(x, rect.Y + rect.Height - 1, Math.Min(step, rect.X + rect.Width - x), 1), color);
            }
            // Left
            for (int y = rect.Y; y < rect.Y + rect.Height; y += step * 2)
            {
                spriteBatch.Draw(pixel, new Rectangle(rect.X, y, 1, Math.Min(step, rect.Y + rect.Height - y)), color);
            }
            // Right
            for (int y = rect.Y; y < rect.Y + rect.Height; y += step * 2)
            {
                spriteBatch.Draw(pixel, new Rectangle(rect.X + rect.Width - 1, y, 1, Math.Min(step, rect.Y + rect.Height - y)), color);
            }
        }
    }
}
