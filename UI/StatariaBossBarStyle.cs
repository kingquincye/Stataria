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
        public static readonly HashSet<int> TreatAsBoss = new HashSet<int>();
        private static readonly HashSet<int> ExcludeFromBossBar = new HashSet<int>();
        private Color borderColor = Color.White * 0.8f;
        private Color textColor = Color.White;
        private Color textShadowColor = Color.Black * 0.7f;
        private Color bgColor = Color.DarkGray * 0.6f;
        private float textScale = 0.8f;

        public override void Load()
        {
            InitializeBossDefinitions();
        }

        private void InitializeBossDefinitions()
        {
            BossPartGroups.Clear();
            TreatAsBoss.Clear();
            ExcludeFromBossBar.Clear();

            BossPartGroups[NPCID.Creeper] = NPCID.BrainofCthulhu;
            BossPartGroups[NPCID.SkeletronHand] = NPCID.SkeletronHead;
            BossPartGroups[NPCID.PrimeCannon] = NPCID.SkeletronPrime;
            BossPartGroups[NPCID.PrimeLaser] = NPCID.SkeletronPrime;
            BossPartGroups[NPCID.PrimeSaw] = NPCID.SkeletronPrime;
            BossPartGroups[NPCID.PrimeVice] = NPCID.SkeletronPrime;
            BossPartGroups[NPCID.GolemHead] = NPCID.Golem;
            BossPartGroups[NPCID.GolemFistLeft] = NPCID.Golem;
            BossPartGroups[NPCID.GolemFistRight] = NPCID.Golem;
            BossPartGroups[NPCID.MoonLordHand] = NPCID.MoonLordCore;
            BossPartGroups[NPCID.MoonLordHead] = NPCID.MoonLordCore;
            BossPartGroups[NPCID.PirateShipCannon] = NPCID.PirateShip;
            BossPartGroups[NPCID.MartianSaucerCannon] = NPCID.MartianSaucerCore;
            BossPartGroups[NPCID.MartianSaucerTurret] = NPCID.MartianSaucerCore;

            TreatAsBoss.Add(NPCID.DD2DarkMageT1);
            TreatAsBoss.Add(NPCID.DD2DarkMageT3);
            TreatAsBoss.Add(NPCID.DD2OgreT2);
            TreatAsBoss.Add(NPCID.DD2OgreT3);
            TreatAsBoss.Add(NPCID.DD2Betsy);
            TreatAsBoss.Add(NPCID.PirateShip);
            TreatAsBoss.Add(NPCID.MourningWood);
            TreatAsBoss.Add(NPCID.Pumpking);
            TreatAsBoss.Add(NPCID.Everscream);
            TreatAsBoss.Add(NPCID.SantaNK1);
            TreatAsBoss.Add(NPCID.IceQueen);
            TreatAsBoss.Add(NPCID.MartianSaucerCore);
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

            ExcludeFromBossBar.Add(NPCID.TorchGod);
            ExcludeFromBossBar.Add(NPCID.None);

            if (ModLoader.HasMod("CalamityMod"))
            {
                AddCalamityBossDefinitions();
            }
        }

        private void AddCalamityBossDefinitions()
        {
            try
            {
                Mod calamity = ModLoader.GetMod("CalamityMod");

                BossPartGroups[calamity.Find<ModNPC>("RavagerHead").Type] = calamity.Find<ModNPC>("RavagerBody").Type;
                BossPartGroups[calamity.Find<ModNPC>("RavagerClawRight").Type] = calamity.Find<ModNPC>("RavagerBody").Type;
                BossPartGroups[calamity.Find<ModNPC>("RavagerClawLeft").Type] = calamity.Find<ModNPC>("RavagerBody").Type;
                BossPartGroups[calamity.Find<ModNPC>("RavagerLegRight").Type] = calamity.Find<ModNPC>("RavagerBody").Type;
                BossPartGroups[calamity.Find<ModNPC>("RavagerLegLeft").Type] = calamity.Find<ModNPC>("RavagerBody").Type;
                BossPartGroups[calamity.Find<ModNPC>("DarkEnergy").Type] = calamity.Find<ModNPC>("CeaselessVoid").Type;

                TreatAsBoss.Add(calamity.Find<ModNPC>("CrimulanPaladin").Type);
                TreatAsBoss.Add(calamity.Find<ModNPC>("SplitCrimulanPaladin").Type);
                TreatAsBoss.Add(calamity.Find<ModNPC>("EbonianPaladin").Type);
                TreatAsBoss.Add(calamity.Find<ModNPC>("SplitEbonianPaladin").Type);
                TreatAsBoss.Add(calamity.Find<ModNPC>("EbonianPaladin").Type);
                TreatAsBoss.Add(calamity.Find<ModNPC>("GiantClam").Type);
                TreatAsBoss.Add(calamity.Find<ModNPC>("PerforatorHeadSmall").Type);
                TreatAsBoss.Add(calamity.Find<ModNPC>("PerforatorHeadMedium").Type);
                TreatAsBoss.Add(calamity.Find<ModNPC>("PerforatorHeadLarge").Type);
                TreatAsBoss.Add(calamity.Find<ModNPC>("ThiccWaifu").Type);
                TreatAsBoss.Add(calamity.Find<ModNPC>("Horse").Type);
                TreatAsBoss.Add(calamity.Find<ModNPC>("GreatSandShark").Type);
                TreatAsBoss.Add(calamity.Find<ModNPC>("PlaguebringerMiniboss").Type);
                TreatAsBoss.Add(calamity.Find<ModNPC>("ArmoredDiggerHead").Type);
                TreatAsBoss.Add(calamity.Find<ModNPC>("Cataclysm").Type);
                TreatAsBoss.Add(calamity.Find<ModNPC>("Catastrophe").Type);
                TreatAsBoss.Add(calamity.Find<ModNPC>("SupremeCataclysm").Type);
                TreatAsBoss.Add(calamity.Find<ModNPC>("SupremeCatastrophe").Type);
                TreatAsBoss.Add(calamity.Find<ModNPC>("ProvSpawnDefense").Type);
                TreatAsBoss.Add(calamity.Find<ModNPC>("ProvSpawnOffense").Type);
                TreatAsBoss.Add(calamity.Find<ModNPC>("ProvSpawnHealer").Type);
                TreatAsBoss.Add(calamity.Find<ModNPC>("ProfanedGuardianDefender").Type);
                TreatAsBoss.Add(calamity.Find<ModNPC>("ProfanedGuardianHealer").Type);

                ExcludeFromBossBar.Add(calamity.Find<ModNPC>("SlimeGodCore").Type);
            }
            catch (Exception ex)
            {
                StatariaLogger.Error($"Failed to load Calamity boss definitions: {ex.Message}");
            }
        }

        public override void Draw(SpriteBatch spriteBatch, IBigProgressBar currentBar, BigProgressBarInfo info)
        {
            DrawAllBossBars(spriteBatch);
        }

        private void DrawAllBossBars(SpriteBatch spriteBatch)
        {
            var config = ModContent.GetInstance<StatariaClientConfig>();

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
            var config = ModContent.GetInstance<StatariaClientConfig>();
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
            
            if (config.MaxVisibleBossBars > 0 && newBars.Count > config.MaxVisibleBossBars)
            {
                newBars = newBars.Take(config.MaxVisibleBossBars).ToList();
            }

            currentlyDisplayedBars = newBars;
        }

        private void CalculateEoWChainHealth(NPC headNpc, out double currentLife, out double maxLife)
        {
            currentLife = 0;
            maxLife = 0;
            
            NPC currentSegment = headNpc;
            int segmentsChecked = 0;
            
            while (currentSegment != null && currentSegment.active && segmentsChecked < 100)
            {
                if (currentSegment.TryGetGlobalNPC<StatariaScalingGlobalNPC>(out var scalingNPC) && scalingNPC.UsesCustomHP)
                {
                    maxLife += scalingNPC.CustomLifeMax;
                    currentLife += (Main.netMode == NetmodeID.SinglePlayer)
                        ? scalingNPC.CustomLife
                        : scalingNPC.CustomLifeMax * ((double)currentSegment.life / currentSegment.lifeMax);
                }
                else
                {
                    currentLife += currentSegment.life;
                    maxLife += currentSegment.lifeMax;
                }
                
                int nextIndex = (int)currentSegment.ai[0];
                if (nextIndex >= 0 && nextIndex < Main.maxNPCs)
                {
                    NPC nextSegment = Main.npc[nextIndex];
                    if (nextSegment.active && nextSegment.ai[1] == currentSegment.whoAmI)
                    {
                        currentSegment = nextSegment;
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
                
                segmentsChecked++;
            }
        }

        private bool IsBossForBar(NPC npc)
        {
            var config = ModContent.GetInstance<StatariaClientConfig>();

            if (ExcludeFromBossBar.Contains(npc.type)) return false;

            if (config.ExcludedBossNPCIDs?.Contains(npc.type) == true) return false;

            if (npc.boss && !npc.friendly) return true;

            if (TreatAsBoss.Contains(npc.type)) return true;

            if (config.MiniBossNPCIDs?.Contains(npc.type) == true) return true;
            if (config.ForcedBossNPCIDs?.Contains(npc.type) == true) return true;

            return false;
        }

        private BossBarUIData CreateBossBarData(NPC npc, StatariaClientConfig config)
        {
            var barData = new BossBarUIData
            {
                NpcWhoAmI = npc.whoAmI,
                DisplayName = npc.FullName,
                DistanceToPlayer = Vector2.Distance(npc.Center, Main.LocalPlayer.Center)
            };

            CalculateHealthValues(npc, out double currentLife, out double maxLife);

            barData.CurrentHp = currentLife;
            barData.MaxHp = maxLife;

            barData.HeadTextureId = GetBossHeadTextureIndex(npc);

            return barData;
        }

        private void CalculateHealthValues(NPC npc, out double currentLife, out double maxLife)
        {
            if (npc.TryGetGlobalNPC<StatariaScalingGlobalNPC>(out var scalingNPC) && scalingNPC.UsesCustomHP)
            {
                maxLife = scalingNPC.CustomLifeMax;
                currentLife = (Main.netMode == NetmodeID.SinglePlayer)
                    ? scalingNPC.CustomLife
                    : scalingNPC.CustomLifeMax * ((double)npc.life / npc.lifeMax);
                return;
            }

            currentLife = npc.life;
            maxLife = npc.lifeMax;
            
            if (npc.type == NPCID.TheDestroyer)
            {
                return;
            }

            if (npc.type == NPCID.EaterofWorldsHead)
            {
                CalculateEoWChainHealth(npc, out currentLife, out maxLife);
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
                        if (segment.TryGetGlobalNPC<StatariaScalingGlobalNPC>(out var segScaling) && segScaling.UsesCustomHP)
                        {
                            maxLife += segScaling.CustomLifeMax;
                            currentLife += (Main.netMode == NetmodeID.SinglePlayer)
                                ? segScaling.CustomLife
                                : segScaling.CustomLifeMax * ((double)segment.life / segment.lifeMax);
                        }
                        else
                        {
                            currentLife += segment.life;
                            maxLife += segment.lifeMax;
                        }
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

                double sumLife = 0, sumMax = 0;

                for (int j = 0; j < Main.maxNPCs; j++)
                {
                    NPC part = Main.npc[j];
                    if (!part.active) continue;

                    if (part.type == mainType)
                    {
                        if (part.TryGetGlobalNPC<StatariaScalingGlobalNPC>(out var partScaling) && partScaling.UsesCustomHP)
                        {
                            sumMax += partScaling.CustomLifeMax;
                            sumLife += (Main.netMode == NetmodeID.SinglePlayer)
                                ? partScaling.CustomLife
                                : partScaling.CustomLifeMax * ((double)part.life / part.lifeMax);
                        }
                        else
                        {
                            sumLife += part.life;
                            sumMax += part.lifeMax;
                        }
                    }
                    else if (BossPartGroups.ContainsKey(part.type) && BossPartGroups[part.type] == mainType)
                    {
                        if (part.TryGetGlobalNPC<StatariaScalingGlobalNPC>(out var partScaling) && partScaling.UsesCustomHP)
                        {
                            sumMax += partScaling.CustomLifeMax;
                            sumLife += (Main.netMode == NetmodeID.SinglePlayer)
                                ? partScaling.CustomLife
                                : partScaling.CustomLifeMax * ((double)part.life / part.lifeMax);
                        }
                        else
                        {
                            sumLife += part.life;
                            sumMax += part.lifeMax;
                        }
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

            var headTextureOverrides = new Dictionary<int, int>
            {
                [NPCID.MoonLordCore] = NPCID.Sets.BossHeadTextures[NPCID.MoonLordHead],
                [NPCID.Golem] = NPCID.Sets.BossHeadTextures[NPCID.GolemHead],
            };

            if (headTextureOverrides.ContainsKey(npc.type))
            {
                return headTextureOverrides[npc.type];
            }

            if (npc.ModNPC != null)
            {
                return npc.GetBossHeadTextureIndex();
            }

            return -1;
        }

        private void CalculateBarPositions(StatariaClientConfig config)
        {
            if (currentlyDisplayedBars.Count == 0) return;

            float anchorX = Main.screenWidth * (config.BossBarXOffsetPercent / 100f);
            float anchorY = Main.screenHeight * (config.BossBarYOffsetPercent / 100f);

            bool expandDown = config.BossBarYOffsetPercent < 50f;

            float barWidth = config.BossBarWidth * config.BossBarScale;
            float barHeight = 22f * config.BossBarScale;
            float verticalSpacing = 6f * config.BossBarScale;

            float nameHeight = 0f;
            if (config.ShowBossName)
            {
                nameHeight = FontAssets.MouseText.Value.LineSpacing * config.BossBarScale;
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

                float finalX = anchorX - barWidth / 2f;
                float finalY = barY;

                float minY = 10f;
                if (config.ShowBossName)
                {
                    minY += nameHeight;
                }

                finalX = Math.Clamp(finalX, 10f, Math.Max(10f, Main.screenWidth - barWidth - 10f));
                finalY = Math.Clamp(finalY, minY, Math.Max(minY, Main.screenHeight - barHeight - 10f));

                currentlyDisplayedBars[i].CalculatedPosition = new Vector2(finalX, finalY);
                currentlyDisplayedBars[i].CurrentWidth = (int)barWidth;
                currentlyDisplayedBars[i].CurrentHeight = (int)barHeight;
                currentlyDisplayedBars[i].CurrentScale = config.BossBarScale;
                currentlyDisplayedBars[i].NameHeight = nameHeight;
            }
        }

        private void DrawSingleBossBar(SpriteBatch spriteBatch, BossBarUIData barData, StatariaClientConfig config)
        {
            Vector2 position = barData.CalculatedPosition;
            int width = barData.CurrentWidth;
            int height = barData.CurrentHeight;
            float scale = barData.CurrentScale;

            float lifePercent = barData.MaxHp > 0 ? (float)(barData.CurrentHp / barData.MaxHp) : 0f;

            Rectangle barRect = new Rectangle((int)position.X, (int)position.Y, width, height);
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, barRect, bgColor);

            if (lifePercent > 0)
            {
                DrawHealthGradient(spriteBatch, barRect, lifePercent);
            }

            DrawBarBorders(spriteBatch, position, width, height);

            if (barData.HeadTextureId >= 0)
            {
                DrawBossIcon(spriteBatch, barData, position, scale);
            }

            if (config.ShowBossHealthText)
            {
                DrawHealthText(spriteBatch, barData, position, height);
            }

            if (config.ShowBossName)
            {
                DrawBossName(spriteBatch, barData, position, height);
            }
        }

        private void DrawBarBorders(SpriteBatch spriteBatch, Vector2 position, int width, int height)
        {
            int borderThickness = 1;
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, 
                new Rectangle((int)position.X, (int)position.Y, width, borderThickness), borderColor);
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, 
                new Rectangle((int)position.X, (int)position.Y + height - borderThickness, width, borderThickness), borderColor);
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, 
                new Rectangle((int)position.X, (int)position.Y, borderThickness, height), borderColor);
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, 
                new Rectangle((int)position.X + width - borderThickness, (int)position.Y, borderThickness, height), borderColor);
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
            string healthText = $"{(long)Math.Round(barData.CurrentHp)} / {(long)Math.Round(barData.MaxHp)}";
            DynamicSpriteFont font = FontAssets.ItemStack.Value;
            Vector2 textSize = font.MeasureString(healthText) * textScale;

            Vector2 textPos = new Vector2(
                position.X + (barData.CurrentWidth / 2f) - (textSize.X / 2f),
                position.Y + (height / 2f) - (textSize.Y / 2f)
            );

            spriteBatch.DrawString(font, healthText, textPos + new Vector2(1, 1) * textScale, textShadowColor, 0f, Vector2.Zero, textScale, SpriteEffects.None, 0f);
            spriteBatch.DrawString(font, healthText, textPos, textColor, 0f, Vector2.Zero, textScale, SpriteEffects.None, 0f);
        }

        private void DrawBossName(SpriteBatch spriteBatch, BossBarUIData barData, Vector2 position, int height)
        {
            DynamicSpriteFont font = FontAssets.ItemStack.Value;
            Vector2 textSize = font.MeasureString(barData.DisplayName) * textScale;

            Vector2 textPos = new Vector2(
                position.X + (barData.CurrentWidth / 2f) - (textSize.X / 2f),
                position.Y - textSize.Y - 4f
            );

            spriteBatch.DrawString(font, barData.DisplayName, textPos + new Vector2(1, 1) * textScale, textShadowColor, 0f, Vector2.Zero, textScale, SpriteEffects.None, 0f);
            spriteBatch.DrawString(font, barData.DisplayName, textPos, textColor, 0f, Vector2.Zero, textScale, SpriteEffects.None, 0f);
        }
    }

    public class BossBarUIData
    {
        public int NpcWhoAmI;
        public double CurrentHp;
        public double MaxHp;
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