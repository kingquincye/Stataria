using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using ReLogic.Graphics;
using Terraria.UI;
using Terraria.ID;

namespace Stataria.UI
{
    public static class RoleCooldownUI
    {
        private struct ActiveCooldown
        {
            public string Name;
            public string Initial;
            public float RemainingTime;
            public float MaxTime;
            public Color Color;

            public ActiveCooldown(string name, string initial, float remaining, float max, Color color)
            {
                Name = name;
                Initial = initial;
                RemainingTime = remaining;
                MaxTime = max;
                Color = color;
            }
        }

        private static bool _dragging = false;
        private static Vector2 _dragOffset;

        public static void Draw(SpriteBatch spriteBatch)
        {
            if (Main.dedServ)
                return;

            var clientConfig = ModContent.GetInstance<StatariaClientConfig>();
            if (clientConfig == null || !clientConfig.EnableRoleCooldownHUD)
                return;

            Player player = Main.LocalPlayer;
            if (player == null || !player.active || player.dead)
                return;

            var rpg = player.GetModPlayer<RPGPlayer>();
            var config = ModContent.GetInstance<StatariaConfig>();

            // 1. Gather all active cooldowns
            List<ActiveCooldown> activeCooldowns = new List<ActiveCooldown>();

            // Berserker: Savage Roar
            var berserker = player.GetModPlayer<BerserkerPlayer>();
            if (berserker.IsBerserkerActive && berserker.SavageRoarCooldownTimer > 0)
            {
                activeCooldowns.Add(new ActiveCooldown(
                    Terraria.Localization.Language.GetTextValue("Mods.Stataria.AbilityName.SavageRoar"), "R", 
                    berserker.SavageRoarCooldownTimer / 60f, 
                    config.roleSettings.BerserkerSavageRoarCooldown, 
                    Color.Red
                ));
            }


            // Cleric: Divine Intervention
            if (rpg.ActiveRole?.ID == "Cleric" && rpg.divineInterventionCooldownTimer > 0)
            {
                activeCooldowns.Add(new ActiveCooldown(
                    Terraria.Localization.Language.GetTextValue("Mods.Stataria.AbilityName.DivineIntervention"), "I", 
                    rpg.divineInterventionCooldownTimer / 60f, 
                    config.roleSettings.DivineInterventionCooldown, 
                    Color.Gold
                ));
            }

            // Angel: Divine Resurrection
            var cleric = player.GetModPlayer<ClericPlayer>();
            if (cleric.IsAngelActive && cleric.DivineResurrectionCooldownTimer > 0)
            {
                activeCooldowns.Add(new ActiveCooldown(
                    Terraria.Localization.Language.GetTextValue("Mods.Stataria.AbilityName.DivineResurrection"), "R", 
                    cleric.DivineResurrectionCooldownTimer / 60f, 
                    config.roleSettings.AngelResurrectionCooldown, 
                    Color.Gold
                ));
            }

            // Shinobi: Mortal Draw
            var shinobi = player.GetModPlayer<ShinobiPlayer>();
            if (shinobi.IsShinobiActive && shinobi.MortalDrawCooldownTimer > 0)
            {
                activeCooldowns.Add(new ActiveCooldown(
                    Terraria.Localization.Language.GetTextValue("Mods.Stataria.AbilityName.MortalDraw"), "H", 
                    shinobi.MortalDrawCooldownTimer / 60f, 
                    config.roleSettings.ShinobiMortalDrawCooldown, 
                    Color.Purple
                ));
            }


            // Rebirth: Teleport
            if (rpg.teleportCooldownTimer > 0)
            {
                activeCooldowns.Add(new ActiveCooldown(
                    Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityName.Teleport"), "T", 
                    rpg.teleportCooldownTimer / 60f, 
                    config.rebirthAbilities.TeleportCooldown, 
                    Color.Violet
                ));
            }

            // Rebirth: Last Stand
            if (rpg.lastStandCooldownTimer > 0)
            {
                activeCooldowns.Add(new ActiveCooldown(
                    Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityName.LastStand"), "L", 
                    rpg.lastStandCooldownTimer / 60f, 
                    config.rebirthAbilities.LastStandCooldown, 
                    Color.LimeGreen
                ));
            }

            // Hotkey check for dragging placeholder
            bool isHoldingModKey = Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftControl) || 
                                   Main.keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftAlt);

            // Hide UI if nothing to draw and not editing position
            if (activeCooldowns.Count == 0 && !isHoldingModKey)
                return;

            // 2. Initialize default coordinates if not set
            if (rpg.CooldownHUDX < 0 || rpg.CooldownHUDY < 0)
            {
                rpg.CooldownHUDX = Main.screenWidth * 0.5f - 25f; // Centered horizontally by default
                rpg.CooldownHUDY = Main.screenHeight * 0.35f;     // Positioned slightly above center
            }

            // Determine dimensions for dragging (Increased diameter from 40 to 50 for readability)
            int circleDiameter = 50;
            int padding = 12;
            int displayCount = Math.Max(1, activeCooldowns.Count);
            int hudWidth = displayCount * circleDiameter + (displayCount - 1) * padding;
            int hudHeight = circleDiameter + 15; // circle + text height

            // Clamp stored coordinates to screen limits (if screen resolution changed)
            rpg.CooldownHUDX = Math.Clamp(rpg.CooldownHUDX, 10f, Math.Max(10f, Main.screenWidth - hudWidth - 10f));
            rpg.CooldownHUDY = Math.Clamp(rpg.CooldownHUDY, 10f, Math.Max(10f, Main.screenHeight - hudHeight - 10f));

            Vector2 position = new Vector2(rpg.CooldownHUDX, rpg.CooldownHUDY);

            Rectangle hudBounds = new Rectangle((int)position.X - 10, (int)position.Y - 10, hudWidth + 20, hudHeight + 20);

            // 3. Handle Dragging
            Vector2 mouseScreen = Main.MouseScreen;
            if (isHoldingModKey)
            {
                // Draw a dashed helper border to show the drag bounds
                Texture2D pixelTex = TextureAssets.MagicPixel.Value;
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
                        rpg.CooldownHUDX = Math.Clamp(newPos.X, 10f, Main.screenWidth - hudWidth - 10f);
                        rpg.CooldownHUDY = Math.Clamp(newPos.Y, 10f, Main.screenHeight - hudHeight - 10f);
                        position = new Vector2(rpg.CooldownHUDX, rpg.CooldownHUDY);
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
                    Main.instance.MouseText(Terraria.Localization.Language.GetTextValue("Mods.Stataria.UI.CooldownHUDDragInstruction"));
                }
            }

            // 4. Render the Cooldowns
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            DynamicSpriteFont font = FontAssets.ItemStack.Value;

            if (activeCooldowns.Count == 0 && isHoldingModKey)
            {
                // Draw placeholder drag bubble when holding key and no active cooldowns
                Vector2 center = position + new Vector2(circleDiameter / 2f, circleDiameter / 2f);
                
                // Draw circle background
                DrawFilledCircle(spriteBatch, center, circleDiameter / 2f, Color.Black * 0.5f);
                DrawCircleOutline(spriteBatch, center, circleDiameter / 2f, 1f, Color.White * 0.5f);

                // Draw helper text
                string initial = Terraria.Localization.Language.GetTextValue("Mods.Stataria.UI.CooldownHUDPlaceholder");
                Vector2 initialSize = font.MeasureString(initial) * 0.8f;
                spriteBatch.DrawString(font, initial, center - initialSize / 2f, Color.LightGray * 0.7f, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);

                string helpText = Terraria.Localization.Language.GetTextValue("Mods.Stataria.UI.CooldownHUDDragLabel");
                Vector2 helpSize = font.MeasureString(helpText) * 0.7f;
                Vector2 helpPos = new Vector2(center.X - helpSize.X / 2f, center.Y + circleDiameter / 2f + 4f);
                spriteBatch.DrawString(font, helpText, helpPos + new Vector2(1, 1), Color.Black * 0.8f, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
                spriteBatch.DrawString(font, helpText, helpPos, Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
                
                return;
            }

            for (int i = 0; i < activeCooldowns.Count; i++)
            {
                ActiveCooldown cooldown = activeCooldowns[i];
                Vector2 circleCenter = position + new Vector2(
                    i * (circleDiameter + padding) + circleDiameter / 2f, 
                    circleDiameter / 2f
                );

                // Draw filled circle background
                DrawFilledCircle(spriteBatch, circleCenter, circleDiameter / 2f, Color.Black * 0.6f);

                // Draw circular progress border
                float progress = 1f - (cooldown.RemainingTime / cooldown.MaxTime);
                DrawCircleOutline(spriteBatch, circleCenter, circleDiameter / 2f - 2f, progress, cooldown.Color);

                // Split ability name by space if it consists of two words
                string[] words = cooldown.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (words.Length == 2)
                {
                    string line1 = words[0];
                    string line2 = words[1];
                    Vector2 size1 = font.MeasureString(line1);
                    Vector2 size2 = font.MeasureString(line2);

                    float maxWidth = Math.Max(size1.X, size2.X);
                    float totalHeight = size1.Y + size2.Y - 2f;

                    // Fit bounding box within circle (with padding)
                    float targetDim = circleDiameter - 10f;
                    float scaleX = targetDim / maxWidth;
                    float scaleY = targetDim / totalHeight;
                    float scale = Math.Min(0.8f, Math.Min(scaleX, scaleY));

                    Vector2 drawScale = new Vector2(scale, scale);
                    Vector2 line1Size = size1 * scale;
                    Vector2 line2Size = size2 * scale;

                    // Compute vertical centers
                    Vector2 line1Pos = new Vector2(circleCenter.X - line1Size.X / 2f, circleCenter.Y - (totalHeight * scale) / 2f);
                    Vector2 line2Pos = new Vector2(circleCenter.X - line2Size.X / 2f, line1Pos.Y + size1.Y * scale - 2f * scale);

                    // Text shadows
                    spriteBatch.DrawString(font, line1, line1Pos + new Vector2(1, 1), Color.Black * 0.8f, 0f, Vector2.Zero, drawScale, SpriteEffects.None, 0f);
                    spriteBatch.DrawString(font, line2, line2Pos + new Vector2(1, 1), Color.Black * 0.8f, 0f, Vector2.Zero, drawScale, SpriteEffects.None, 0f);

                    // Text fores
                    spriteBatch.DrawString(font, line1, line1Pos, cooldown.Color, 0f, Vector2.Zero, drawScale, SpriteEffects.None, 0f);
                    spriteBatch.DrawString(font, line2, line2Pos, cooldown.Color, 0f, Vector2.Zero, drawScale, SpriteEffects.None, 0f);
                }
                else
                {
                    // Draw single word centered
                    Vector2 nameSize = font.MeasureString(cooldown.Name);
                    float targetDim = circleDiameter - 10f;
                    float scaleX = targetDim / nameSize.X;
                    float scaleY = targetDim / nameSize.Y;
                    float scale = Math.Min(0.8f, Math.Min(scaleX, scaleY));

                    Vector2 drawScale = new Vector2(scale, scale);
                    Vector2 textSizeToDraw = nameSize * scale;
                    Vector2 textPos = circleCenter - textSizeToDraw / 2f;

                    // Text shadow
                    spriteBatch.DrawString(font, cooldown.Name, textPos + new Vector2(1, 1), Color.Black * 0.8f, 0f, Vector2.Zero, drawScale, SpriteEffects.None, 0f);

                    // Text fore
                    spriteBatch.DrawString(font, cooldown.Name, textPos, cooldown.Color, 0f, Vector2.Zero, drawScale, SpriteEffects.None, 0f);
                }

                // Draw remaining seconds text below circle
                string secText = $"{cooldown.RemainingTime:F1}s";
                Vector2 textSize = font.MeasureString(secText) * 0.75f;
                Vector2 secondsPos = new Vector2(circleCenter.X - textSize.X / 2f, circleCenter.Y + circleDiameter / 2f + 2f);

                // Text Shadow
                spriteBatch.DrawString(font, secText, secondsPos + new Vector2(1, 1), Color.Black * 0.8f, 0f, Vector2.Zero, 0.75f, SpriteEffects.None, 0f);
                // Text Fore
                spriteBatch.DrawString(font, secText, secondsPos, Color.White, 0f, Vector2.Zero, 0.75f, SpriteEffects.None, 0f);
            }
        }

        private static void DrawFilledCircle(SpriteBatch spriteBatch, Vector2 center, float radius, Color color)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            // Approximate a filled circle using stacked horizontal lines
            int r = (int)radius;
            for (int y = -r; y <= r; y++)
            {
                int width = (int)Math.Sqrt(r * r - y * y);
                spriteBatch.Draw(pixel, new Rectangle((int)center.X - width, (int)center.Y + y, width * 2, 1), color);
            }
        }

        private static void DrawCircleOutline(SpriteBatch spriteBatch, Vector2 center, float radius, float progress, Color color)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            int numSegments = 60;
            for (int i = 0; i < numSegments; i++)
            {
                float segmentPct = (float)i / numSegments;
                if (segmentPct <= progress)
                {
                    float angle = i * MathHelper.TwoPi / numSegments - MathHelper.PiOver2; // Starts from top
                    Vector2 offset = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * radius;
                    Vector2 pos = center + offset;
                    spriteBatch.Draw(pixel, new Rectangle((int)pos.X - 1, (int)pos.Y - 1, 2, 2), color);
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
