using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.BigProgressBar;
using Terraria.ModLoader;
using System.Collections.Generic;
using System.Linq;
using System;
using Terraria.ID;
using ReLogic.Graphics;

namespace Stataria
{
    public class StatariaBossBarStyle : ModBossBarStyle
    {
        public override string DisplayName => "Stataria";

        public override bool PreventDraw => true;

        private List<BossBarUIData> currentlyDisplayedBars = new List<BossBarUIData>();
        private static readonly Dictionary<int, int> BossPartGroups = new Dictionary<int, int>();
        private static readonly HashSet<int> TreatAsBoss = new HashSet<int>();
        private static readonly HashSet<int> ExcludeFromBossBar = new HashSet<int>();

        public override void Load()
        {
            InitializeBossDefinitions();
        }

        private void InitializeBossDefinitions()
        {
            BossPartGroups.Clear();

            BossPartGroups[NPCID.Creeper] = NPCID.BrainofCthulhu;

            BossPartGroups[NPCID.EaterofWorldsBody] = NPCID.EaterofWorldsHead;
            BossPartGroups[NPCID.EaterofWorldsTail] = NPCID.EaterofWorldsHead;

            BossPartGroups[NPCID.GolemHead] = NPCID.Golem;
            BossPartGroups[NPCID.GolemFistLeft] = NPCID.Golem;
            BossPartGroups[NPCID.GolemFistRight] = NPCID.Golem;

            BossPartGroups[NPCID.MoonLordHand] = NPCID.MoonLordCore;
            BossPartGroups[NPCID.MoonLordHead] = NPCID.MoonLordCore;

            TreatAsBoss.Clear();
            TreatAsBoss.Add(NPCID.DD2DarkMageT1);
            TreatAsBoss.Add(NPCID.DD2DarkMageT3);
            TreatAsBoss.Add(NPCID.DD2OgreT2);
            TreatAsBoss.Add(NPCID.DD2OgreT3);
            TreatAsBoss.Add(NPCID.DD2Betsy);
            TreatAsBoss.Add(NPCID.IceGolem);
            TreatAsBoss.Add(NPCID.SandElemental);
            TreatAsBoss.Add(NPCID.Paladin);
            TreatAsBoss.Add(NPCID.BloodNautilus);
            TreatAsBoss.Add(NPCID.Mothron);
            TreatAsBoss.Add(NPCID.BigMimicCorruption);
            TreatAsBoss.Add(NPCID.BigMimicCrimson);
            TreatAsBoss.Add(NPCID.BigMimicHallow);
            TreatAsBoss.Add(NPCID.BigMimicJungle);
            TreatAsBoss.Add(NPCID.WyvernHead);
            TreatAsBoss.Add(NPCID.EaterofWorldsHead);

            ExcludeFromBossBar.Clear();
            ExcludeFromBossBar.Add(NPCID.TorchGod);
            ExcludeFromBossBar.Add(NPCID.None);
            ExcludeFromBossBar.Add(NPCID.MoonLordFreeEye);
        }

        public override void Draw(SpriteBatch spriteBatch, IBigProgressBar currentBar, BigProgressBarInfo info)
        {
            DrawAllBossBars(spriteBatch);
        }

        private void DrawAllBossBars(SpriteBatch spriteBatch)
        {
            var config = ModContent.GetInstance<StatariaConfig>();

            UpdateBossBarData();

            if (currentlyDisplayedBars.Count == 0) return;

            CalculateBarPositions(config);

            foreach (var barData in currentlyDisplayedBars)
            {
                DrawSingleBossBar(spriteBatch, barData, config);
            }
        }

        private void UpdateBossBarData()
        {
            var config = ModContent.GetInstance<StatariaConfig>();
            var newBars = new List<BossBarUIData>();

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.active) continue;
                
                if (!IsBossForBar(npc)) continue;
                
                if (npc.realLife >= 0 && npc.realLife != npc.whoAmI) continue;
                
                if (BossPartGroups.ContainsKey(npc.type))
                {
                    int mainType = BossPartGroups[npc.type];
                    if (mainType != npc.type)
                    {
                        bool mainPartExists = false;
                        for (int j = 0; j < Main.maxNPCs; j++)
                        {
                            NPC mainNpc = Main.npc[j];
                            if (mainNpc.active && mainNpc.type == mainType)
                            {
                                mainPartExists = true;
                                break;
                            }
                        }
                        
                        if (mainPartExists) continue;
                    }
                }

                var barData = CreateBossBarData(npc, config);
                if (barData != null)
                {
                    newBars.Add(barData);
                }
            }

            newBars.Sort((a, b) => a.DistanceToPlayer.CompareTo(b.DistanceToPlayer));
            
            if (config.resourceBars.MaxVisibleBossBars > 0 && newBars.Count > config.resourceBars.MaxVisibleBossBars)
            {
                newBars = newBars.Take(config.resourceBars.MaxVisibleBossBars).ToList();
            }

            currentlyDisplayedBars = newBars;
        }

        private bool IsBossForBar(NPC npc)
        {
            var config = ModContent.GetInstance<StatariaConfig>();

            if (ExcludeFromBossBar.Contains(npc.type)) return false;

            if (config.resourceBars.ExcludedBossNPCIDs?.Contains(npc.type) == true) return false;

            if (npc.boss && !npc.friendly) return true;

            if (TreatAsBoss.Contains(npc.type)) return true;

            if (config.resourceBars.MiniBossNPCIDs?.Contains(npc.type) == true) return true;
            if (config.resourceBars.ForcedBossNPCIDs?.Contains(npc.type) == true) return true;

            return false;
        }

        private BossBarUIData CreateBossBarData(NPC npc, StatariaConfig config)
        {
            var barData = new BossBarUIData
            {
                NpcWhoAmI = npc.whoAmI,
                DisplayName = npc.FullName,
                DistanceToPlayer = Vector2.Distance(npc.Center, Main.LocalPlayer.Center)
            };

            CalculateHealthValues(npc, out float currentLife, out float maxLife);

            barData.CurrentHp = currentLife;
            barData.MaxHp = maxLife;

            barData.HeadTextureId = GetBossHeadTextureIndex(npc);

            return barData;
        }

        private void CalculateHealthValues(NPC npc, out float currentLife, out float maxLife)
        {
            currentLife = npc.life;
            maxLife = npc.lifeMax;
            
            if (npc.type == NPCID.TheDestroyer)
            {
                return;
            }

            if (npc.realLife >= 0 && npc.realLife == npc.whoAmI)
            {
                currentLife = 0;
                maxLife = 0;

                for (int j = 0; j < Main.maxNPCs; j++)
                {
                    NPC segment = Main.npc[j];
                    if (segment.active && segment.realLife == npc.whoAmI)
                    {
                        currentLife += segment.life;
                        maxLife += segment.lifeMax;
                    }
                }
            }
            else if (BossPartGroups.ContainsKey(npc.type) || BossPartGroups.ContainsValue(npc.type))
            {
                int mainType;
                if (BossPartGroups.ContainsKey(npc.type))
                {
                    mainType = BossPartGroups[npc.type];
                }
                else
                {
                    mainType = npc.type;
                }

                float sumLife = 0, sumMax = 0;

                for (int j = 0; j < Main.maxNPCs; j++)
                {
                    NPC part = Main.npc[j];
                    if (!part.active) continue;

                    if (part.type == mainType)
                    {
                        sumLife += part.life;
                        sumMax += part.lifeMax;
                    }
                    else if (BossPartGroups.ContainsKey(part.type) && BossPartGroups[part.type] == mainType)
                    {
                        sumLife += part.life;
                        sumMax += part.lifeMax;
                    }
                }

                currentLife = sumLife;
                maxLife = sumMax;
            }
        }

        private int GetBossHeadTextureIndex(NPC npc)
        {
            if (NPCID.Sets.BossHeadTextures[npc.type] >= 0)
            {
                return NPCID.Sets.BossHeadTextures[npc.type];
            }

            if (npc.ModNPC != null)
            {
                return npc.GetBossHeadTextureIndex();
            }

            return -1;
        }

        private void CalculateBarPositions(StatariaConfig config)
        {
            if (currentlyDisplayedBars.Count == 0) return;

            float anchorX = Main.screenWidth * (config.resourceBars.BossBarXOffsetPercent / 100f);
            float anchorY = Main.screenHeight * (config.resourceBars.BossBarYOffsetPercent / 100f);

            bool expandDown = config.resourceBars.BossBarYOffsetPercent < 50f;

            float barWidth = config.resourceBars.BossBarWidth * config.resourceBars.BossBarScale;
            float barHeight = 22f * config.resourceBars.BossBarScale;
            float verticalSpacing = 6f * config.resourceBars.BossBarScale;

            float nameHeight = 0f;
            if (config.resourceBars.ShowBossName)
            {
                nameHeight = FontAssets.MouseText.Value.LineSpacing * config.resourceBars.BossBarScale;
            }

            float totalEntryHeight = nameHeight + barHeight + verticalSpacing;

            for (int i = 0; i < currentlyDisplayedBars.Count; i++)
            {
                float offsetY = i * totalEntryHeight;
                float barY;

                if (expandDown)
                {
                    barY = anchorY + offsetY + nameHeight;
                }
                else
                {
                    barY = anchorY - offsetY - barHeight;
                }

                currentlyDisplayedBars[i].CalculatedPosition = new Vector2(anchorX - barWidth / 2f, barY);
                currentlyDisplayedBars[i].CurrentWidth = (int)barWidth;
                currentlyDisplayedBars[i].CurrentHeight = (int)barHeight;
                currentlyDisplayedBars[i].CurrentScale = config.resourceBars.BossBarScale;
                currentlyDisplayedBars[i].NameHeight = nameHeight;
            }
        }

        private void DrawSingleBossBar(SpriteBatch spriteBatch, BossBarUIData barData, StatariaConfig config)
        {
            Vector2 position = barData.CalculatedPosition;
            int width = barData.CurrentWidth;
            int height = barData.CurrentHeight;
            float scale = barData.CurrentScale;

            float lifePercent = barData.MaxHp > 0 ? barData.CurrentHp / barData.MaxHp : 0f;

            Rectangle bgRect = new Rectangle((int)position.X - 2, (int)position.Y - 2, width + 4, height + 4);
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, bgRect, Color.Black * 0.7f);

            Rectangle barRect = new Rectangle((int)position.X, (int)position.Y, width, height);
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, barRect, Color.DarkGray * 0.8f);

            if (lifePercent > 0)
            {
                DrawHealthGradient(spriteBatch, barRect, lifePercent);
            }

            if (barData.HeadTextureId >= 0)
            {
                DrawBossIcon(spriteBatch, barData, position, scale);
            }

            if (config.resourceBars.ShowBossHealthText)
            {
                DrawHealthText(spriteBatch, barData, position, height);
            }

            if (config.resourceBars.ShowBossName)
            {
                DrawBossName(spriteBatch, barData, position, height);
            }
        }

        private void DrawHealthGradient(SpriteBatch spriteBatch, Rectangle area, float healthPercent)
        {
            int healthWidth = (int)(area.Width * healthPercent);

            for (int i = 0; i < healthWidth; i++)
            {
                Color currentColor;

                if (healthPercent > 0.6f)
                    currentColor = Color.Lerp(Color.Yellow, Color.Green, (healthPercent - 0.6f) / 0.4f);
                else if (healthPercent > 0.3f)
                    currentColor = Color.Lerp(Color.Orange, Color.Yellow, (healthPercent - 0.3f) / 0.3f);
                else
                    currentColor = Color.Lerp(Color.Red, Color.Orange, healthPercent / 0.3f);

                spriteBatch.Draw(TextureAssets.MagicPixel.Value,
                    new Rectangle(area.X + i, area.Y, 1, area.Height),
                    currentColor);
            }
        }

        private void DrawBossIcon(SpriteBatch spriteBatch, BossBarUIData barData, Vector2 position, float scale)
        {
            if (barData.HeadTextureId < 0 || barData.HeadTextureId >= TextureAssets.NpcHeadBoss.Length) return;

            var iconTexture = TextureAssets.NpcHeadBoss[barData.HeadTextureId].Value;
            float iconSize = 26f * scale;

            Vector2 iconPos = new Vector2(
                position.X - iconSize - 6f,
                position.Y + (barData.CurrentHeight / 2f) - (iconSize / 2f)
            );

            Rectangle iconRect = new Rectangle((int)iconPos.X, (int)iconPos.Y, (int)iconSize, (int)iconSize);
            spriteBatch.Draw(iconTexture, iconRect, Color.White);
        }

        private void DrawHealthText(SpriteBatch spriteBatch, BossBarUIData barData, Vector2 position, int height)
        {
            string healthText = $"{(int)barData.CurrentHp} / {(int)barData.MaxHp}";
            Vector2 textSize = FontAssets.MouseText.Value.MeasureString(healthText);

            Vector2 textPos = new Vector2(
                position.X + (barData.CurrentWidth / 2f) - (textSize.X / 2f),
                position.Y + (height / 2f) - (textSize.Y / 2f)
            );

            spriteBatch.DrawString(FontAssets.MouseText.Value, healthText, textPos + Vector2.One, Color.Black);
            spriteBatch.DrawString(FontAssets.MouseText.Value, healthText, textPos, Color.White);
        }

        private void DrawBossName(SpriteBatch spriteBatch, BossBarUIData barData, Vector2 position, int height)
        {
            Vector2 textSize = FontAssets.MouseText.Value.MeasureString(barData.DisplayName);

            Vector2 textPos = new Vector2(
                position.X + (barData.CurrentWidth / 2f) - (textSize.X / 2f),
                position.Y - textSize.Y - 4f
            );

            spriteBatch.DrawString(FontAssets.MouseText.Value, barData.DisplayName, textPos + Vector2.One, Color.Black);
            spriteBatch.DrawString(FontAssets.MouseText.Value, barData.DisplayName, textPos, Color.White);
        }
    }

    public class BossBarUIData
    {
        public int NpcWhoAmI;
        public float CurrentHp;
        public float MaxHp;
        public string DisplayName;
        public int HeadTextureId;
        public Vector2 CalculatedPosition;
        public float CurrentScale;
        public int CurrentWidth;
        public int CurrentHeight;
        public float DistanceToPlayer;
        public float NameHeight;
    }
}