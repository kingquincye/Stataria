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
using Terraria.ModLoader.IO;
using Terraria.DataStructures;

namespace Stataria
{
    public class StatariaScalingGlobalNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        public bool IsElite { get; set; }
        public int Level { get; set; }

        private static readonly HashSet<int> CrawlerNpcTypes = new HashSet<int>
        {
            NPCID.BloodCrawler, NPCID.BloodCrawlerWall,
            NPCID.JungleCreeper, NPCID.JungleCreeperWall,
            NPCID.WallCreeper, NPCID.WallCreeperWall,
            NPCID.BlackRecluse, NPCID.BlackRecluseWall,
            NPCID.DesertScorpionWalk, NPCID.DesertScorpionWall
        };

        private float damageMult = 1f;
        public bool hasBeenScaled = false;

        private bool IsProblematicCrawler(NPC npc)
        {
            return CrawlerNpcTypes.Contains(npc.type);
        }

        private bool IsWormSegment(NPC npc)
        {
            return npc.realLife >= 0 && npc.realLife != npc.whoAmI;
        }

        private NPC GetWormHead(NPC segment)
        {
            if (segment.realLife >= 0 && segment.realLife < Main.npc.Length)
            {
                return Main.npc[segment.realLife];
            }

            return segment;
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

            var config = ModContent.GetInstance<StatariaConfig>();

            if (!config.enemyScaling.ScaleCrawlerEnemies && IsProblematicCrawler(npc))
            {
                hasBeenScaled = true;
                return;
            }

            IsElite = false;
            Level = 1;

            if (!config.enemyScaling.EnableEnemyScaling)
                return;

            if (npc.townNPC || npc.friendly || NPCID.Sets.CountsAsCritter[npc.type] || npc.lifeMax <= 9)
                return;

            if (IsWormSegment(npc))
            {
                NPC head = GetWormHead(npc);

                if (head != null && head.active)
                {
                    var headScaling = head.GetGlobalNPC<StatariaScalingGlobalNPC>();

                    Level = headScaling.Level;
                    IsElite = headScaling.IsElite;

                    ApplyScaling(npc);
                    hasBeenScaled = true;
                    return;
                }
            }

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                CalculateEnemyLevel(npc);
                TryMakeElite(npc);
            }

            ApplyScaling(npc);

            hasBeenScaled = true;
        }

        public override void SetDefaults(NPC npc)
        {
            IsElite = false;
            Level = 1;
            hasBeenScaled = false;

            var config = ModContent.GetInstance<StatariaConfig>();

            if (!config.enemyScaling.EnableEnemyScaling)
                return;

            if (npc.townNPC || npc.friendly || NPCID.Sets.CountsAsCritter[npc.type] || npc.lifeMax <= 9)
                return;
        }

        private void CalculateEnemyLevel(NPC npc)
        {
            if (IsWormSegment(npc))
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
                int worldSeed = Main.worldName?.GetHashCode() ?? 0;
                Random npcRandom = new Random(npc.whoAmI + worldSeed);
                int variation = npcRandom.Next(-config.enemyScaling.MaxLevelVariation, config.enemyScaling.MaxLevelVariation + 1);
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

            else if (IsWormSegment(npc))
            {
                NPC head = GetWormHead(npc);
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

            int worldSeed = Main.worldName?.GetHashCode() ?? 0;
            Random npcRandom = new Random(npc.whoAmI + 1000 + worldSeed);
            IsElite = npcRandom.NextDouble() < config.enemyScaling.EliteEnemyChance;
        }

        public void ApplyScaling(NPC npc)
        {
            var config = ModContent.GetInstance<StatariaConfig>();

            if (!config.enemyScaling.EnableEnemyScaling)
                return;

            float healthMult = 1f;

            if (npc.boss)
            {
                healthMult = 1f + ((Level - 1) * config.enemyScaling.EnemyHealthScaling * config.enemyScaling.BossHealthScaling);
                damageMult = 1f + ((Level - 1) * config.enemyScaling.EnemyDamageScaling * config.enemyScaling.BossDamageScaling);

            }
            else
            {
                healthMult = 1f + ((Level - 1) * config.enemyScaling.EnemyHealthScaling);
                damageMult = 1f + ((Level - 1) * config.enemyScaling.EnemyDamageScaling);

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

                if (!npc.boss)
                {
                    npc.defense = (int)(npc.defense * config.enemyScaling.EliteDefenseMultiplier);

                    if (config.enemyScaling.EliteScaleIncrease)
                        npc.scale *= config.enemyScaling.EliteScaleMultiplier;
                }

                npc.knockBackResist *= (1f - config.enemyScaling.EliteKnockbackResistance);
            }

            int newHealth = (int)(npc.lifeMax * healthMult);

            const int MAX_SAFE_HEALTH = 1000000000;
            if (newHealth > MAX_SAFE_HEALTH)
                newHealth = MAX_SAFE_HEALTH;

            npc.lifeMax = newHealth;
            npc.life = npc.lifeMax;
        }

        public override void ModifyHitPlayer(NPC npc, Player target, ref Player.HurtModifiers modifiers)
        {
            if (!ModContent.GetInstance<StatariaConfig>().enemyScaling.EnableEnemyScaling)
                return;

            modifiers.FinalDamage *= damageMult;
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

            if (!config.enemyScaling.EnableEnemyScaling || !config.enemyScaling.ShowEnemyLevelIndicator)
                return;

            if (npc.townNPC || npc.friendly || NPCID.Sets.CountsAsCritter[npc.type] || npc.lifeMax <= 9)
                return;

            if (IsMultiPartBoss(npc) && npc.whoAmI != GetMainPartID(npc))
                return;

            if (npc.realLife >= 0 && npc.realLife != npc.whoAmI)
                return;

            if (npc.type == NPCID.EaterofWorldsBody || npc.type == NPCID.EaterofWorldsTail)
                return;

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

            if (isBehindWall && !config.enemyScaling.ShowEnemyLevelBehindWalls)
                return;

            float opacity = config.enemyScaling.EnemyIndicatorOpacity;

            string levelText = $"Lv.{Level}";
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
        }

        public override void PostAI(NPC npc)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                if (npc.active && !this.hasBeenScaled)
                {
                    if (Stataria.pendingNpcScaling.TryGetValue(npc.whoAmI, out var scalingData))
                    {
                        this.IsElite = scalingData.IsElite;
                        this.Level = scalingData.Level;
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

        public override void OnKill(NPC npc)
        {
            base.OnKill(npc);
            hasBeenScaled = false;
        }

        public override void SaveData(NPC npc, TagCompound tag)
        {
            tag["IsElite"] = IsElite;
            tag["Level"] = Level;
            tag["hasBeenScaled"] = hasBeenScaled;
        }

        public override void LoadData(NPC npc, TagCompound tag)
        {
            IsElite = tag.GetBool("IsElite");
            Level = tag.GetInt("Level");
            hasBeenScaled = tag.GetBool("hasBeenScaled");
        }
    }
}