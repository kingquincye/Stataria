using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria.GameContent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Terraria.ModLoader.IO;
using Terraria.DataStructures;
using Terraria.Localization;

namespace Stataria
{
    public class StatariaScalingGlobalNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        public bool IsElite { get; set; }
        public int Level { get; set; }
        public float damageMult = 1f;
        public bool hasBeenScaled = false;
        public double CustomLifeMax { get; set; } = -1;
        public double CustomLife { get; set; } = -1;
        public bool UsesCustomHP => CustomLifeMax > 0;
        private int lastLife = -1;
        private Vector2 _customGrabbedBarPos;
        private bool _drawCustomBarThisFrame;
        private byte _customGrabbedHbPosition;

        private bool IsWormSegmentOrPart(NPC npc)
        {
            if (npc.realLife >= 0 && npc.realLife != npc.whoAmI)
                return true;

            if (IsSplittingWormSegment(npc))
            {
                NPC head = FindSplittingWormHead(npc);
                if (head != null && head.whoAmI != npc.whoAmI)
                    return true;
            }

            if (npc.ModNPC != null)
            {
                if (npc.boss)
                    return false;

                string name = npc.ModNPC.Name;
                if (name.Contains("Body", StringComparison.OrdinalIgnoreCase) || 
                    name.Contains("Tail", StringComparison.OrdinalIgnoreCase) || 
                    name.Contains("Segment", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsSplittingWormSegment(NPC npc)
        {
            return (npc.aiStyle == NPCAIStyleID.Worm || npc.type == NPCID.EaterofWorldsBody || npc.type == NPCID.EaterofWorldsTail) &&
                npc.ai[0] >= 0 && npc.ai[0] < Main.maxNPCs && 
                npc.ai[1] >= 0 && npc.ai[1] < Main.maxNPCs;
        }

        private NPC FindSplittingWormHead(NPC segment)
        {
            NPC current = segment;
            int segmentsChecked = 0;
            
            while (current != null && current.active && segmentsChecked < 100)
            {
                int prevIndex = (int)current.ai[1];
                if (prevIndex >= 0 && prevIndex < Main.maxNPCs)
                {
                    NPC prevSegment = Main.npc[prevIndex];
                    if (prevSegment.active && prevSegment.ai[0] == current.whoAmI)
                    {
                        current = prevSegment;
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
            
            return current;
        }

        private NPC FindWormHead(NPC segment)
        {
            if (segment.realLife >= 0 && segment.realLife < Main.npc.Length)
            {
                NPC head = Main.npc[segment.realLife];
                if (head != null && head.active)
                    return head;
            }

            if (segment.ModNPC != null)
            {
                string segmentName = segment.ModNPC.Name;
                string baseName = segmentName;
                if (segmentName.EndsWith("Body"))
                {
                    baseName = segmentName.Substring(0, segmentName.Length - 4);
                }
                else if (segmentName.EndsWith("Tail"))
                {
                    baseName = segmentName.Substring(0, segmentName.Length - 4);
                }
                else if (segmentName.Contains("Body"))
                {
                    int idx = segmentName.IndexOf("Body");
                    baseName = segmentName.Substring(0, idx);
                }
                else if (segmentName.Contains("Tail"))
                {
                    int idx = segmentName.IndexOf("Tail");
                    baseName = segmentName.Substring(0, idx);
                }

                NPC closestHead = null;
                float closestDist = float.MaxValue;

                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC other = Main.npc[i];
                    if (other.active && other.ModNPC != null)
                    {
                        string otherName = other.ModNPC.Name;
                        if (otherName.StartsWith(baseName) && 
                            (otherName.EndsWith("Head") || (!otherName.Contains("Body") && !otherName.Contains("Tail"))))
                        {
                            float dist = Vector2.Distance(segment.Center, other.Center);
                            if (dist < closestDist)
                            {
                                closestDist = dist;
                                closestHead = other;
                            }
                        }
                    }
                }

                if (closestHead != null)
                    return closestHead;
            }

            return null;
        }

        private bool IsEaterOfWorldsSegment(NPC npc)
        {
            return npc.type == NPCID.EaterofWorldsBody ||
                npc.type == NPCID.EaterofWorldsHead ||
                npc.type == NPCID.EaterofWorldsTail;
        }

        private bool IsMultiPartBoss(NPC npc)
        {
            if (npc.type == NPCID.MoonLordCore ||
                npc.type == NPCID.MoonLordHand ||
                npc.type == NPCID.MoonLordHead)
                return true;

            if (npc.type == NPCID.Golem ||
                npc.type == NPCID.GolemHead ||
                npc.type == NPCID.GolemFistLeft ||
                npc.type == NPCID.GolemFistRight)
                return true;

            if (npc.type == NPCID.BrainofCthulhu ||
                npc.type == NPCID.Creeper)
                return true;

            if (npc.type == NPCID.PirateShip)
                return true;

            if (npc.type == NPCID.MartianSaucer ||
                npc.type == NPCID.MartianSaucerCannon ||
                npc.type == NPCID.MartianSaucerCore ||
                npc.type == NPCID.MartianSaucerTurret)
                return true;

            return false;
        }

        private int GetMainPartID(NPC npc)
        {
            if (npc.realLife >= 0 && npc.realLife != npc.whoAmI)
                return npc.realLife;

            switch (npc.type)
            {
                case NPCID.MoonLordHand:
                case NPCID.MoonLordHead:
                    return FindMoonLordCore();

                case NPCID.GolemFistLeft:
                case NPCID.GolemFistRight:
                case NPCID.Golem:
                    return FindGolemHead();

                case NPCID.Creeper:
                    return FindBrainOfCthulhu();

                case NPCID.MartianSaucerCannon:
                case NPCID.MartianSaucerCore:
                case NPCID.MartianSaucerTurret:
                    return FindMartianSaucer();
            }

            return npc.whoAmI;
        }

        private int FindMoonLordCore()
        {
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].active && Main.npc[i].type == NPCID.MoonLordCore)
                    return i;
            }
            return -1;
        }

        private int FindGolemHead()
        {
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].active && Main.npc[i].type == NPCID.GolemHead)
                    return i;
            }
            return -1;
        }

        private int FindBrainOfCthulhu()
        {
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].active && Main.npc[i].type == NPCID.BrainofCthulhu)
                    return i;
            }
            return -1;
        }

        private int FindMartianSaucer()
        {
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].active && Main.npc[i].type == NPCID.MartianSaucer)
                    return i;
            }
            return -1;
        }

        public override void OnSpawn(NPC npc, IEntitySource source)
        {
            if (Main.netMode == NetmodeID.Server)
            {
                ApplyScalingOnSpawn(npc);
                Stataria.SyncNPCScaling(npc.whoAmI);
            }
        }

        private void ApplyScalingOnSpawn(NPC npc)
        {
            if (hasBeenScaled)
                return;

            hasBeenScaled = true;

            var config = ModContent.GetInstance<StatariaConfig>();
            if (config.advanced.ScalingBlacklistedNPCs.Any(entry =>
                entry.Equals(Lang.GetNPCNameValue(npc.type), StringComparison.OrdinalIgnoreCase) ||
                (int.TryParse(entry, out int id) && id == npc.type)))
            {
                return;
            }

            IsElite = false;
            Level = 1;

            if (!config.enemyScaling.EnableEnemyScaling)
                return;

            if (npc.townNPC || npc.friendly || NPCID.Sets.CountsAsCritter[npc.type] || npc.lifeMax <= 9)
                return;

            if (IsWormSegmentOrPart(npc))
            {
                NPC head = FindWormHead(npc);
                if (head == null && IsSplittingWormSegment(npc))
                {
                    head = FindSplittingWormHead(npc);
                }

                if (head != null && head.active && head.whoAmI != npc.whoAmI)
                {
                    var headScaling = head.GetGlobalNPC<StatariaScalingGlobalNPC>();
                    if (!headScaling.hasBeenScaled)
                    {
                        headScaling.ApplyScalingOnSpawn(head);
                    }

                    Level = headScaling.Level;
                    IsElite = headScaling.IsElite;

                    ApplyScaling(npc);
                    return;
                }
            }

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                CalculateEnemyLevel(npc);
                TryMakeElite(npc);
            }

            ApplyScaling(npc);
        }

        public override void Load()
        {
            On_NPC.Transform += NPC_Transform;
        }

        public override void Unload()
        {
            On_NPC.Transform -= NPC_Transform;
        }

        private void NPC_Transform(On_NPC.orig_Transform orig, NPC self, int newType)
        {
            int cachedLevel = 0;
            bool cachedElite = false;
            int cachedLife = 0;

            if (self.TryGetGlobalNPC<StatariaScalingGlobalNPC>(out var oldGlobal) && oldGlobal.hasBeenScaled)
            {
                cachedLevel = oldGlobal.Level;
                cachedElite = oldGlobal.IsElite;
                cachedLife = self.life;
            }

            orig(self, newType); 

            if (cachedLevel > 0 && self.TryGetGlobalNPC<StatariaScalingGlobalNPC>(out var newGlobal))
            {
                newGlobal.Level = cachedLevel;
                newGlobal.IsElite = cachedElite;
                
                // Re-apply scaling with cached level and elite status
                newGlobal.ApplyScaling(self);
                
                // Restore the perfectly precise health it had before transforming
                self.life = cachedLife;
                newGlobal.hasBeenScaled = true;
            }
        }

        public override void SetDefaults(NPC npc)
        {
            IsElite = false;
            Level = 1;
            hasBeenScaled = false;
            CustomLifeMax = -1;
            CustomLife = -1;
            lastLife = -1;

            var config = ModContent.GetInstance<StatariaConfig>();

            if (!config.enemyScaling.EnableEnemyScaling)
                return;

            if (npc.townNPC || npc.friendly || NPCID.Sets.CountsAsCritter[npc.type] || npc.lifeMax <= 9)
                return;
        }



        private void CalculateEnemyLevel(NPC npc)
        {
            if (IsWormSegmentOrPart(npc))
            {
                return;
            }

            Level = 1;

            var config = ModContent.GetInstance<StatariaConfig>();

            if (Main.player.Count(p => p.active) == 0 || !config.enemyScaling.EnableEnemyScaling)
                return;

            var activePlayers = Main.player.Where(p => p.active).ToList();

            if (config.enemyScaling.UseProximityForScaling)
            {
                int proximityRange = config.enemyScaling.ScalingProximityRange;

                activePlayers = activePlayers.Where(p =>
                    Vector2.Distance(p.Center, npc.Center) <= proximityRange).ToList();

                if (!activePlayers.Any())
                    return;
            }

            int baseLevel = 1;
            switch (config.enemyScaling.ScalingType)
            {
                case 0:
                    {
                        int playerCount = activePlayers.Count;
                        baseLevel = 1 + (int)(playerCount * config.enemyScaling.LevelsPerPlayer);
                    }
                    break;

                case 1:
                    {
                        int highestLevel = activePlayers.Max(p => p.GetModPlayer<RPGPlayer>().Level);
                        baseLevel = highestLevel;
                    }
                    break;

                case 2:
                    {
                        float avgLevel = (float)activePlayers.Average(p => p.GetModPlayer<RPGPlayer>().Level);
                        baseLevel = (int)Math.Ceiling(avgLevel);
                    }
                    break;
            }

            if (config.enemyScaling.EnableLevelVariation)
            {
                int variation = Main.rand.Next(-config.enemyScaling.MaxLevelVariation, config.enemyScaling.MaxLevelVariation + 1);
                baseLevel += variation;

                if (config.enemyScaling.EnableMinimumLevelDifference &&
                    config.enemyScaling.ScalingType == 1)
                {
                    int playerLevel = activePlayers.Max(p => p.GetModPlayer<RPGPlayer>().Level);
                    int minLevel = playerLevel - config.enemyScaling.MinimumLevelDifference;

                    if (baseLevel < minLevel && config.enemyScaling.MaxLevelVariation > config.enemyScaling.MinimumLevelDifference)
                    {
                        baseLevel = minLevel;
                    }
                }
            }

            Level = Math.Max(1, baseLevel);
        }

        private void TryMakeElite(NPC npc)
        {
            if (IsMultiPartBoss(npc))
            {
                IsElite = false;
                return;
            }

            if (IsEaterOfWorldsSegment(npc))
            {
                IsElite = false;
                return;
            }

            else if (IsWormSegmentOrPart(npc))
            {
                NPC head = FindWormHead(npc);
                if (head == null && IsSplittingWormSegment(npc))
                {
                    head = FindSplittingWormHead(npc);
                }

                if (head != null && head.active)
                {
                    var headScaling = head.GetGlobalNPC<StatariaScalingGlobalNPC>();
                    IsElite = headScaling.IsElite;
                }
                return;
            }

            else
            {
                NPC bossHead = Main.npc
                    .FirstOrDefault(other =>
                        other.active &&
                        (other.boss || NPCID.Sets.BossHeadTextures[other.type] >= 0) &&
                        Vector2.Distance(other.Center, npc.Center) <= Math.Max(other.width, other.height) * 2f
                    );
                if (bossHead != null)
                {
                    IsElite = bossHead.GetGlobalNPC<StatariaScalingGlobalNPC>().IsElite;
                    return;
                }
            }

            var config = ModContent.GetInstance<StatariaConfig>();

            if (!config.enemyScaling.EnableEliteEnemies || npc.boss || npc.townNPC || npc.friendly || NPCID.Sets.CountsAsCritter[npc.type] || npc.lifeMax <= 9)
                return;

            IsElite = Main.rand.NextDouble() < config.enemyScaling.EliteEnemyChance;
        }

        public void ApplyScaling(NPC npc)
        {
            var config = ModContent.GetInstance<StatariaConfig>();

            if (!config.enemyScaling.EnableEnemyScaling)
                return;

            float healthMult = 1f;
            int additionalFlatHealth = 0;

            if (npc.boss)
            {
                if (config.enemyScaling.EnableBossScaling)
                {
                    healthMult = 1f + ((Level - 1) * config.enemyScaling.BossHealthScaling);
                    damageMult = 1f + ((Level - 1) * config.enemyScaling.BossDamageScaling);

                    if (config.enemyScaling.EnableFlatEnemyScaling)
                    {
                        additionalFlatHealth += (Level - 1) * config.enemyScaling.FlatBossHealthScaling;
                    }
                }
            }
            else
            {
                healthMult = 1f + ((Level - 1) * config.enemyScaling.EnemyHealthScaling);
                damageMult = 1f + ((Level - 1) * config.enemyScaling.EnemyDamageScaling);

                if (config.enemyScaling.EnableFlatEnemyScaling)
                {
                    additionalFlatHealth += (Level - 1) * config.enemyScaling.FlatEnemyHealthScaling;
                }

                float defenseMult = 1f + ((Level - 1) * config.enemyScaling.EnemyDefenseScaling);

                if (config.enemyScaling.EnableDefenseCap)
                {
                    defenseMult = Math.Min(defenseMult, config.enemyScaling.MaxDefenseMultiplier);
                }

                npc.defense = (int)(npc.defense * defenseMult);
            }

            if (IsElite)
            {
                healthMult *= config.enemyScaling.EliteHealthMultiplier;
                damageMult *= config.enemyScaling.EliteDamageMultiplier;
                additionalFlatHealth = (int)(additionalFlatHealth * config.enemyScaling.EliteHealthMultiplier);

                if (!npc.boss)
                {
                    npc.defense = (int)(npc.defense * config.enemyScaling.EliteDefenseMultiplier);

                    if (config.enemyScaling.EliteScaleIncrease)
                        npc.scale *= config.enemyScaling.EliteScaleMultiplier;
                }

                npc.knockBackResist *= (1f - config.enemyScaling.EliteKnockbackResistance);
            }

            float healthRatio = npc.lifeMax > 0 ? (float)npc.life / npc.lifeMax : 1f;
            double targetMaxHealth = (double)npc.lifeMax * healthMult + additionalFlatHealth;

            const int MAX_SAFE_HEALTH = 1500000000;
            bool useCustomHP = Main.netMode == NetmodeID.MultiplayerClient ? (CustomLifeMax > MAX_SAFE_HEALTH) : (targetMaxHealth > MAX_SAFE_HEALTH);

            if (useCustomHP)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    CustomLifeMax = targetMaxHealth;
                }
                CustomLife = CustomLifeMax * healthRatio;
                npc.lifeMax = MAX_SAFE_HEALTH;
                npc.life = (int)Math.Round(MAX_SAFE_HEALTH * healthRatio);
                lastLife = npc.life;
            }
            else
            {
                CustomLifeMax = -1;
                CustomLife = -1;
                lastLife = -1;
                npc.lifeMax = (int)targetMaxHealth;
                npc.life = (int)Math.Round(targetMaxHealth * healthRatio);
            }
        }

        public override void ModifyHitPlayer(NPC npc, Player target, ref Player.HurtModifiers modifiers)
        {
            var config = ModContent.GetInstance<StatariaConfig>();

            if (!config.enemyScaling.EnableEnemyScaling)
                return;

            modifiers.FinalDamage *= damageMult;

            if (config.enemyScaling.EnableFlatEnemyScaling)
            {
                int flatDamage = 0;
                if (npc.boss && config.enemyScaling.EnableBossScaling)
                {
                    flatDamage = (Level - 1) * config.enemyScaling.FlatBossDamageScaling;
                }
                else if (!npc.boss)
                {
                    flatDamage = (Level - 1) * config.enemyScaling.FlatEnemyDamageScaling;
                }

                if (IsElite)
                {
                    flatDamage = (int)(flatDamage * config.enemyScaling.EliteDamageMultiplier);
                }

                modifiers.FinalDamage += flatDamage;
            }
        }

        public override void ModifyHitByItem(NPC npc, Player player, Item item, ref NPC.HitModifiers modifiers)
        {
            var config = ModContent.GetInstance<StatariaConfig>();

            if (!config.enemyScaling.EnableEnemyScaling)
                return;

            if (IsElite && config.enemyScaling.EliteCriticalHitResistance > 0)
            {
                modifiers.CritDamage *= 1f - config.enemyScaling.EliteCriticalHitResistance;
            }
        }

        public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            var config = ModContent.GetInstance<StatariaConfig>();

            if (!config.enemyScaling.EnableEnemyScaling)
                return;

            if (IsElite && config.enemyScaling.EliteCriticalHitResistance > 0)
            {
                modifiers.CritDamage *= 1f - config.enemyScaling.EliteCriticalHitResistance;
            }
        }

        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            var config = ModContent.GetInstance<StatariaConfig>();
            var configClient = ModContent.GetInstance<StatariaClientConfig>();

            if (config.advanced.ScalingBlacklistedNPCs.Any(entry =>
                entry.Equals(Lang.GetNPCNameValue(npc.type), StringComparison.OrdinalIgnoreCase) ||
                (int.TryParse(entry, out int id) && id == npc.type)))
            {
                return;
            }

            if (!config.enemyScaling.EnableEnemyScaling || !configClient.ShowEnemyLevelIndicator)
                return;

            if (npc.townNPC || npc.friendly || NPCID.Sets.CountsAsCritter[npc.type] || npc.lifeMax <= 9)
                return;

            if (IsMultiPartBoss(npc) && npc.whoAmI != GetMainPartID(npc))
                return;

            if (IsWormSegmentOrPart(npc))
            {
                NPC head = FindWormHead(npc);
                if (head == null && IsSplittingWormSegment(npc))
                {
                    head = FindSplittingWormHead(npc);
                }

                if (head != null && head.whoAmI != npc.whoAmI)
                    return;
            }

            bool isBehindWall = false;

            Player closestPlayer = null;
            float closestDistance = float.MaxValue;

            foreach (Player player in Main.player)
            {
                if (player.active)
                {
                    float distance = Vector2.Distance(player.Center, npc.Center);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestPlayer = player;
                    }
                }
            }

            if (closestPlayer != null)
            {
                isBehindWall = !Collision.CanHit(
                    closestPlayer.position, closestPlayer.width, closestPlayer.height,
                    npc.position, npc.width, npc.height
                );
            }

            if (isBehindWall && !configClient.ShowEnemyLevelBehindWalls)
                return;

            float opacity = configClient.EnemyIndicatorOpacity;

            string levelText = Language.GetTextValue("Mods.Stataria.UI.EnemyLevel", Level);
            DynamicSpriteFont font = FontAssets.MouseText.Value;
            Vector2 textSize = font.MeasureString(levelText);

            Vector2 pos = npc.Center - screenPos;
            pos.Y -= npc.height / 2 + 15f;

            Vector2 textPos = new(pos.X - textSize.X / 2, pos.Y - textSize.Y / 2);

            Color shadowColor = Color.Black * (opacity * 0.5f);
            spriteBatch.DrawString(font, levelText, new Vector2(textPos.X + 2, textPos.Y + 2),
                shadowColor);

            Color textColor = IsElite ? new Color(255, 50, 50) * opacity : Color.White * opacity;
            spriteBatch.DrawString(font, levelText, textPos, textColor);

            bool isCustomBarEnabled = UsesCustomHP && !npc.boss && configClient.ShowCustomNormalMobHPBar;
            Rectangle hoverRect = npc.getRect();
            hoverRect.Inflate(32, 32);
            bool isMouseHovering = hoverRect.Contains(Main.MouseWorld.ToPoint());

            if (isCustomBarEnabled)
            {
                float barWidth = 40f;
                float barHeight = 4f;

                // Determine the bar's world position
                Vector2 barWorldPos;
                if (_drawCustomBarThisFrame)
                {
                    barWorldPos = _customGrabbedBarPos;
                }
                else
                {
                    float defaultY = npc.position.Y + (_customGrabbedHbPosition == 1 ? -16f : npc.height + 8f);
                    barWorldPos = new Vector2(npc.Center.X, defaultY);
                }

                Vector2 barPos = barWorldPos - screenPos;
                barPos.X -= barWidth / 2f;

                // Draw background
                Rectangle bgRect = new Rectangle((int)barPos.X, (int)barPos.Y, (int)barWidth, (int)barHeight);
                spriteBatch.Draw(TextureAssets.MagicPixel.Value, bgRect, new Color(30, 30, 30) * (opacity * 0.7f));

                // Draw health fill
                float lifePercent = CustomLifeMax > 0 ? (float)(CustomLife / CustomLifeMax) : 0f;
                lifePercent = Math.Clamp(lifePercent, 0f, 1f);
                if (lifePercent > 0f)
                {
                    Rectangle fgRect = new Rectangle((int)barPos.X, (int)barPos.Y, (int)(barWidth * lifePercent), (int)barHeight);
                    // Emerald Green to Coral Red
                    Color lowHPColor = new Color(240, 70, 70); // Soft crimson
                    Color highHPColor = new Color(50, 205, 110); // Mint/emerald green
                    Color barColor = Color.Lerp(lowHPColor, highHPColor, lifePercent);
                    spriteBatch.Draw(TextureAssets.MagicPixel.Value, fgRect, barColor * opacity);
                }

                // Draw thin border
                int borderThickness = 1;
                Color borderColor = Color.Black * opacity;
                spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle((int)barPos.X - borderThickness, (int)barPos.Y - borderThickness, (int)barWidth + borderThickness * 2, borderThickness), borderColor);
                spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle((int)barPos.X - borderThickness, (int)barPos.Y + (int)barHeight, (int)barWidth + borderThickness * 2, borderThickness), borderColor);
                spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle((int)barPos.X - borderThickness, (int)barPos.Y, borderThickness, (int)barHeight), borderColor);
                spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle((int)barPos.X + (int)barWidth, (int)barPos.Y, borderThickness, (int)barHeight), borderColor);

                // Draw health text below the bar if hovered or if numerical values are not hover-only
                if (!configClient.CustomNormalMobHPBarHoverOnly || isMouseHovering)
                {
                    string healthText = $"{(long)Math.Round(CustomLife):N0} / {(long)Math.Round(CustomLifeMax):N0}";
                    DynamicSpriteFont hpFont = FontAssets.ItemStack.Value;
                    float hpTextScale = 0.50f;
                    Vector2 hpTextSize = hpFont.MeasureString(healthText) * hpTextScale;
                    Vector2 hpTextPos = new Vector2(barWorldPos.X - hpTextSize.X / 2f - screenPos.X, barPos.Y + barHeight + 4f);

                    spriteBatch.DrawString(hpFont, healthText, hpTextPos + new Vector2(1, 1) * hpTextScale, Color.Black * (opacity * 0.7f), 0f, Vector2.Zero, hpTextScale, SpriteEffects.None, 0f);
                    spriteBatch.DrawString(hpFont, healthText, hpTextPos, Color.White * opacity, 0f, Vector2.Zero, hpTextScale, SpriteEffects.None, 0f);
                }

                // Queue the custom NPC name tooltip next to the cursor when hovered
                if (isMouseHovering)
                {
                    Main.instance.MouseText(npc.GivenOrTypeName);
                }

                _drawCustomBarThisFrame = false;
            }
        }

        public override bool? DrawHealthBar(NPC npc, byte hbPosition, ref float scale, ref Vector2 position)
        {
            var configClient = ModContent.GetInstance<StatariaClientConfig>();
            if (UsesCustomHP && !npc.boss && configClient.ShowCustomNormalMobHPBar)
            {
                _customGrabbedBarPos = position;
                _customGrabbedHbPosition = hbPosition;
                _drawCustomBarThisFrame = true;
                return false;
            }
            return null;
        }

        public override void AI(NPC npc)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                if (npc.active && !this.hasBeenScaled)
                {
                    if (Stataria.pendingNpcScaling.TryGetValue(npc.whoAmI, out var scalingData))
                    {
                        this.IsElite = scalingData.IsElite;
                        this.Level = scalingData.Level;
                        this.CustomLifeMax = scalingData.CustomLifeMax;
                        this.ApplyScaling(npc);
                        this.hasBeenScaled = true;

                        Stataria.pendingNpcScaling.Remove(npc.whoAmI);
                    }
                }
            }
            else
            {
                if (!this.hasBeenScaled && !npc.townNPC && !npc.friendly && !NPCID.Sets.CountsAsCritter[npc.type] && npc.lifeMax > 9)
                {
                    ApplyScalingOnSpawn(npc);

                    if (Main.netMode == NetmodeID.Server)
                    {
                        Stataria.SyncNPCScaling(npc.whoAmI);
                    }
                }
            }
        }

        public override void PostAI(NPC npc)
        {
            if (UsesCustomHP)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    if (lastLife != -1 && npc.life < lastLife)
                    {
                        int damageTaken = lastLife - npc.life;
                        CustomLife -= damageTaken;
                        if (CustomLife <= 0)
                        {
                            npc.life = 0;
                            npc.checkDead();
                        }
                        else
                        {
                            double percentage = CustomLife / CustomLifeMax;
                            int newLife = (int)Math.Max(1, Math.Round(npc.lifeMax * percentage));
                            if (newLife != npc.life)
                            {
                                npc.life = newLife;
                                if (Main.netMode == NetmodeID.Server)
                                {
                                    npc.netUpdate = true;
                                }
                            }
                        }
                    }
                    lastLife = npc.life;
                }
                else // MultiplayerClient
                {
                    if (npc.lifeMax > 0)
                    {
                        CustomLife = CustomLifeMax * ((double)npc.life / npc.lifeMax);
                    }
                }
            }

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                if (npc.active && !this.hasBeenScaled)
                {
                    if (Stataria.pendingNpcScaling.TryGetValue(npc.whoAmI, out var scalingData))
                    {
                        this.IsElite = scalingData.IsElite;
                        this.Level = scalingData.Level;
                        this.CustomLifeMax = scalingData.CustomLifeMax;
                        this.ApplyScaling(npc);
                        this.hasBeenScaled = true;

                        Stataria.pendingNpcScaling.Remove(npc.whoAmI);
                    }
                }
            }
            else
            {
                if (!this.hasBeenScaled && !npc.townNPC && !npc.friendly && !NPCID.Sets.CountsAsCritter[npc.type] && npc.lifeMax > 9)
                {
                    ApplyScalingOnSpawn(npc);

                    if (Main.netMode == NetmodeID.Server)
                    {
                        Stataria.SyncNPCScaling(npc.whoAmI);
                    }
                }
            }

            if (Main.netMode != NetmodeID.Server)
            {
                if (npc.active && !npc.townNPC && !npc.friendly && !NPCID.Sets.CountsAsCritter[npc.type] && npc.lifeMax > 9)
                {
                    var configClient = ModContent.GetInstance<StatariaClientConfig>();
                    if (UsesCustomHP && !npc.boss && configClient.ShowCustomNormalMobHPBar)
                    {
                        npc.ShowNameOnHover = false;
                    }
                    else
                    {
                        npc.ShowNameOnHover = true;
                    }
                }
            }
        }

        public override void OnKill(NPC npc)
        {
            base.OnKill(npc);
            hasBeenScaled = false;
            CustomLifeMax = -1;
            CustomLife = -1;
            lastLife = -1;

            if (npc.boss)
            {
                if (Main.netMode == NetmodeID.SinglePlayer)
                {
                    Player player = Main.LocalPlayer;
                    if (player.active)
                    {
                        RPGPlayer rpg = player.GetModPlayer<RPGPlayer>();
                        rpg.BossKillsCount++;
                    }
                }
                else if (Main.netMode == NetmodeID.Server)
                {
                    for (int i = 0; i < Main.maxPlayers; i++)
                    {
                        Player player = Main.player[i];
                        if (player != null && player.active)
                        {
                            RPGPlayer rpg = player.GetModPlayer<RPGPlayer>();
                            rpg.BossKillsCount++;
                            rpg.SyncPlayer(-1, i, false);
                        }
                    }
                }
            }
        }

        public override void SaveData(NPC npc, TagCompound tag)
        {
            tag["IsElite"] = IsElite;
            tag["Level"] = Level;
            tag["hasBeenScaled"] = hasBeenScaled;
            tag["CustomLifeMax"] = CustomLifeMax;
            tag["CustomLife"] = CustomLife;
        }

        public override void LoadData(NPC npc, TagCompound tag)
        {
            IsElite = tag.GetBool("IsElite");
            Level = tag.GetInt("Level");
            hasBeenScaled = tag.GetBool("hasBeenScaled");
            CustomLifeMax = tag.ContainsKey("CustomLifeMax") ? tag.GetDouble("CustomLifeMax") : -1;
            CustomLife = tag.ContainsKey("CustomLife") ? tag.GetDouble("CustomLife") : -1;
        }
    }
}