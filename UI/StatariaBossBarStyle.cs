using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.BigProgressBar;
using Terraria.ModLoader;
using System;
using System.Collections.Generic;
using Terraria.ID;

namespace Stataria
{
    public class StatariaBossBarStyle : ModBossBarStyle
    {
        public override string DisplayName => "Stataria";

        public override bool PreventDraw => true;
        public override bool PreventUpdate => true;

        private List<int> activeBossIndices = new List<int>();
        private Dictionary<int, (int mainType, int current, int max)> bossHealthCache = new Dictionary<int, (int, int, int)>();

        private Texture2D barTexture;
        private Texture2D barBackgroundTexture;
        private Texture2D bossIconDefault;

        public override void OnSelected()
        {
            barTexture = TextureAssets.MagicPixel.Value;
            barBackgroundTexture = TextureAssets.MagicPixel.Value;
            bossIconDefault = TextureAssets.NpcHead[0].Value;
        }

        public override void Update(IBigProgressBar currentBar, ref BigProgressBarInfo info)
        {
            activeBossIndices.Clear();
            bossHealthCache.Clear();

            FindActiveBosses();

            UpdateBossHealthData();

            if (activeBossIndices.Count > 0)
            {
                info.npcIndexToAimAt = activeBossIndices[0];
            }
        }

        private void FindActiveBosses()
        {
            HashSet<int> processedGroups = new HashSet<int>();

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.active || npc.friendly || !BossHealthHelper.IsBoss(npc))
                    continue;

                int mainPartId = BossHealthHelper.GetMainPartId(npc.type);

                if (mainPartId == npc.type && !processedGroups.Contains(mainPartId))
                {
                    activeBossIndices.Add(i);
                    processedGroups.Add(mainPartId);
                }
                else if (!processedGroups.Contains(mainPartId))
                {
                    bool foundMainPart = false;
                    for (int j = 0; j < Main.maxNPCs; j++)
                    {
                        if (Main.npc[j].active && Main.npc[j].type == mainPartId)
                        {
                            activeBossIndices.Add(j);
                            processedGroups.Add(mainPartId);
                            foundMainPart = true;
                            break;
                        }
                    }

                    if (!foundMainPart)
                    {
                        activeBossIndices.Add(i);
                        processedGroups.Add(mainPartId);
                    }
                }
            }
        }

        private void UpdateBossHealthData()
        {
            BossHealthHelper.CleanupInactiveBossEncounters();

            foreach (int bossIndex in activeBossIndices)
            {
                NPC npc = Main.npc[bossIndex];
                int mainType = BossHealthHelper.GetMainPartId(npc.type);

                (int current, int max) healthData = BossHealthHelper.GetBossGroupHealth(mainType);

                bossHealthCache[bossIndex] = (mainType, healthData.current, healthData.max);
            }
        }

        public override void Draw(SpriteBatch spriteBatch, IBigProgressBar currentBar, BigProgressBarInfo info)
        {
            var config = ModContent.GetInstance<StatariaConfig>();
            float scale = config.resourceBars.BossBarScale / 100f;
            float yOffsetPercent = config.resourceBars.BossBarYOffsetPercent;

            int baseHeight = 20;
            int minSpacing = 60;

            int barWidth = (int)(config.resourceBars.BossBarWidth * scale);
            int barHeight = (int)(baseHeight * scale);

            bool stackUpward = yOffsetPercent > 0.5f;

            int screenWidth = Main.screenWidth;
            int screenHeight = Main.screenHeight;

            int baseY = (int)(screenHeight * yOffsetPercent);

            int maxBars = Math.Min(activeBossIndices.Count, config.resourceBars.MaxVisibleBossBars);

            List<Rectangle> barPositions = new List<Rectangle>();
            List<int> visibleBossIndices = new List<int>();

            for (int i = 0; i < Math.Min(activeBossIndices.Count, maxBars); i++)
            {
                int npcIndex = activeBossIndices[i];
                if (npcIndex >= 0 && npcIndex < Main.maxNPCs && Main.npc[npcIndex].active)
                {
                    visibleBossIndices.Add(npcIndex);
                }
            }

            for (int i = 0; i < visibleBossIndices.Count; i++)
            {
                int npcIndex = visibleBossIndices[i];

                int x = (screenWidth / 2) - (barWidth / 2);

                int y;
                if (stackUpward)
                {
                    y = baseY - (i * (barHeight + minSpacing));
                }
                else
                {
                    y = baseY + (i * (barHeight + minSpacing));
                }

                int textSpaceAbove = config.resourceBars.ShowBossName ? 25 : 0;
                int textSpaceBelow = config.resourceBars.ShowBossHealthText ? 25 : 0;

                int fullHeight = barHeight + textSpaceAbove + textSpaceBelow;
                int fullY = stackUpward ? y - textSpaceAbove : y;

                barPositions.Add(new Rectangle(x, fullY, barWidth, fullHeight));
            }

            for (int i = 1; i < barPositions.Count; i++)
            {
                Rectangle current = barPositions[i];
                Rectangle previous = barPositions[i - 1];

                if (stackUpward)
                {
                    if (current.Bottom > previous.Top)
                    {
                        int adjustment = current.Bottom - previous.Top + 10;
                        current.Y -= adjustment;
                        barPositions[i] = current;
                    }
                }
                else
                {
                    if (current.Top < previous.Bottom)
                    {
                        int adjustment = previous.Bottom - current.Top + 10;
                        current.Y += adjustment;
                        barPositions[i] = current;
                    }
                }
            }

            for (int i = 0; i < barPositions.Count; i++)
            {
                Rectangle current = barPositions[i];

                if (current.Y < 5)
                {
                    current.Y = 5;
                }
                else if (current.Bottom > screenHeight - 5)
                {
                    current.Y = screenHeight - current.Height - 5;
                }

                barPositions[i] = current;
            }

            for (int i = 0; i < barPositions.Count; i++)
            {
                int npcIndex = visibleBossIndices[i];
                NPC npc = Main.npc[npcIndex];

                if (!bossHealthCache.TryGetValue(npcIndex, out var healthData))
                    continue;

                int currentHealth = healthData.current;
                int maxHealth = healthData.max;

                float fillPercent = MathHelper.Clamp((float)currentHealth / maxHealth, 0f, 1f);

                Rectangle position = barPositions[i];

                int barY;
                if (stackUpward)
                {
                    barY = position.Bottom - barHeight;
                    if (config.resourceBars.ShowBossHealthText)
                        barY -= 25;
                }
                else
                {
                    barY = position.Y;
                    if (config.resourceBars.ShowBossName)
                        barY += 25;
                }

                spriteBatch.Draw(
                    barBackgroundTexture,
                    new Rectangle(position.X, barY, barWidth, barHeight),
                    null,
                    Color.DarkGray
                );

                spriteBatch.Draw(
                    barTexture,
                    new Rectangle(position.X, barY, (int)(barWidth * fillPercent), barHeight),
                    null,
                    GetHealthColor(fillPercent)
                );

                DrawBarBorder(spriteBatch, position.X, barY, barWidth, barHeight);

                if (config.resourceBars.ShowBossName)
                {
                    string bossName = GetSpecialBossName(npc);
                    Vector2 nameSize = FontAssets.MouseText.Value.MeasureString(bossName);
                    Utils.DrawBorderString(
                        spriteBatch,
                        bossName,
                        new Vector2(position.X + (barWidth / 2) - (nameSize.X / 2), stackUpward ? barY - 25 : position.Y),
                        Color.White
                    );
                }

                if (config.resourceBars.ShowBossHealthText)
                {
                    string healthText = $"{currentHealth:N0}/{maxHealth:N0}";
                    Vector2 healthSize = FontAssets.MouseText.Value.MeasureString(healthText);
                    Utils.DrawBorderString(
                        spriteBatch,
                        healthText,
                        new Vector2(position.X + (barWidth / 2) - (healthSize.X / 2), stackUpward ? barY - 50 : barY + barHeight + 5),
                        Color.White
                    );
                }

                if (i == barPositions.Count - 1 && activeBossIndices.Count > maxBars)
                {
                    int extraBosses = activeBossIndices.Count - maxBars;
                    string extraBossText = $"+{extraBosses} more boss{(extraBosses > 1 ? "es" : "")}";
                    Vector2 textSize = FontAssets.MouseText.Value.MeasureString(extraBossText);

                    int textY;
                    if (stackUpward)
                    {
                        textY = position.Y - (int)textSize.Y - 20;
                    }
                    else
                    {
                        textY = position.Bottom + 20;
                    }

                    Utils.DrawBorderString(
                        spriteBatch,
                        extraBossText,
                        new Vector2(position.X + (barWidth / 2) - (textSize.X / 2), textY),
                        Color.Yellow
                    );
                }
            }
        }

        private string GetSpecialBossName(NPC npc)
        {
            if (npc.type == NPCID.Retinazer || npc.type == NPCID.Spazmatism)
            {
                bool retinazerAlive = false;
                bool spazmatismAlive = false;

                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (Main.npc[i].active)
                    {
                        if (Main.npc[i].type == NPCID.Retinazer)
                            retinazerAlive = true;
                        else if (Main.npc[i].type == NPCID.Spazmatism)
                            spazmatismAlive = true;
                    }
                }

                if (retinazerAlive && spazmatismAlive)
                    return "The Twins";
                else if (npc.type == NPCID.Retinazer)
                    return "Retinazer";
                else
                    return "Spazmatism";
            }
            else if (npc.type == NPCID.MoonLordCore || npc.type == NPCID.MoonLordHead || npc.type == NPCID.MoonLordHand)
            {
                return "Moon Lord";
            }
            else if (npc.type == NPCID.Golem || npc.type == NPCID.GolemHead)
            {
                return "Golem";
            }

            return npc.GivenOrTypeName;
        }

        private void DrawBarBorder(SpriteBatch spriteBatch, int x, int y, int width, int height)
        {
            int borderThickness = 2;
            Color borderColor = Color.White;

            spriteBatch.Draw(barTexture, new Rectangle(x, y, width, borderThickness), borderColor);
            spriteBatch.Draw(barTexture, new Rectangle(x, y + height - borderThickness, width, borderThickness), borderColor);
            spriteBatch.Draw(barTexture, new Rectangle(x, y, borderThickness, height), borderColor);
            spriteBatch.Draw(barTexture, new Rectangle(x + width - borderThickness, y, borderThickness, height), borderColor);
        }

        private Color GetHealthColor(float healthPercent)
        {
            if (healthPercent <= 0.2f)
                return Color.Red;
            else if (healthPercent <= 0.5f)
                return Color.Orange;
            else
                return Color.LimeGreen;
        }
    }
}