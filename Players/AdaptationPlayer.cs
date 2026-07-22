using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Stataria.Core;
using Stataria.Globals;
using Stataria.Helpers;
using Stataria.UI;

namespace Stataria.Players
{
    public class AdaptationPlayer : ModPlayer
    {
        private readonly Dictionary<AdaptationKey, AdaptationData> adaptations = new Dictionary<AdaptationKey, AdaptationData>();

        public int CheatDeathCooldownTimer { get; set; }
        public int CheatDeathInvincibilityTimer { get; set; }
        public float HaloRotation { get; set; }
        public int HaloSpinTimer { get; set; }
        public AdaptationCategory ActiveCategory { get; set; } = AdaptationCategory.Mob;
        public bool InstantAdaptationMode { get; set; }
        public bool PendingFullHeal { get; set; }

        public bool IsAdaptorActive
        {
            get
            {
                var rpgPlayer = Player.GetModPlayer<RPGPlayer>();
                var config = ModContent.GetInstance<StatariaConfig>();
                return rpgPlayer != null &&
                       rpgPlayer.ActiveRole != null &&
                       rpgPlayer.ActiveRole.ID == "Adaptor" &&
                       rpgPlayer.ActiveRole.Status == RoleStatus.Active &&
                       config != null &&
                       config.roleSettings.EnableRoleSystem;
            }
        }

        public override void ResetEffects()
        {
            if (CheatDeathCooldownTimer > 0)
            {
                CheatDeathCooldownTimer--;
            }

            if (CheatDeathInvincibilityTimer > 0)
            {
                CheatDeathInvincibilityTimer--;
                Player.immune = true;
                Player.immuneTime = Math.Max(Player.immuneTime, 2);
                Player.lavaImmune = true;
                Player.lavaTime = Player.lavaMax;
            }

            if (HaloSpinTimer > 0)
            {
                HaloSpinTimer--;
                HaloRotation += 0.25f;
            }
            else
            {
                HaloRotation += 0.02f; // Idle spin
            }
        }

        public void ResetAllAdaptations()
        {
            adaptations.Clear();
            CheatDeathCooldownTimer = 0;
            ActiveCategory = AdaptationCategory.Mob;
        }

        public void MaxOutAllAdaptations()
        {
            int maxLevel = AdaptationData.GetMaxLevel();
            foreach (var kvp in adaptations)
            {
                kvp.Value.Level = maxLevel;
                kvp.Value.CurrentExp = 0f;
            }
        }

        public AdaptationData GetAdaptation(AdaptationKey key)
        {
            if (!adaptations.TryGetValue(key, out var data))
            {
                data = new AdaptationData(0, 0f);
                adaptations[key] = data;
            }
            return data;
        }

        public void GainExp(AdaptationKey key, float amount)
        {
            if (!IsAdaptorActive)
                return;

            var config = ModContent.GetInstance<StatariaConfig>();
            float mult = config != null ? config.roleSettings.AdaptorGlobalExpMultiplier : 1.0f;
            float expToAdd = amount * mult;

            if (expToAdd <= 0f && !InstantAdaptationMode)
                return;

            ActiveCategory = key.Category;
            var data = GetAdaptation(key);
            int maxLevel = AdaptationData.GetMaxLevel();

            if (data.Level >= maxLevel)
                return;

            bool leveledUp;
            int levelsGained;

            if (InstantAdaptationMode)
            {
                levelsGained = maxLevel - data.Level;
                data.Level = maxLevel;
                data.CurrentExp = 0f;
                leveledUp = true;
            }
            else
            {
                leveledUp = data.AddExp(key.Category, expToAdd, out levelsGained, key.TargetId);
            }

            if (leveledUp)
            {
                // Full heal on level up (HP & Mana)
                PendingFullHeal = true;
                Player.statLife = Player.statLifeMax2;
                Player.statMana = Player.statManaMax2;

                HaloSpinTimer = 120;
                AuraFlash.TriggerLevelUpEffect(Player, key.Category);

                AdaptationNotificationUI.AddNotification(new AdaptationNotification(
                    key.DisplayName,
                    key.Category,
                    data.Level,
                    1.0f,
                    key.IsOffensive,
                    isLevelUp: true
                ));
            }
            else
            {
                HaloSpinTimer = Math.Max(HaloSpinTimer, 30);

                AdaptationNotificationUI.AddNotification(new AdaptationNotification(
                    key.DisplayName,
                    key.Category,
                    data.Level,
                    data.GetProgressPercentage(key.Category, key.TargetId),
                    key.IsOffensive,
                    isLevelUp: false
                ));
            }
        }

        #region Combat Hooks

        public static NPC GetPrimaryNPC(NPC npc)
        {
            if (npc == null || !npc.active)
                return npc;

            if (npc.realLife >= 0 && npc.realLife < Main.maxNPCs && npc.realLife != npc.whoAmI)
            {
                NPC parent = Main.npc[npc.realLife];
                if (parent != null && parent.active)
                    return parent;
            }

            if (IsSplittingWormSegment(npc))
            {
                NPC head = FindSplittingWormHead(npc);
                if (head != null && head.active && head.whoAmI != npc.whoAmI)
                    return head;
            }

            if (npc.ModNPC != null)
            {
                NPC wormHead = FindModdedWormHead(npc);
                if (wormHead != null && wormHead.active && wormHead.whoAmI != npc.whoAmI)
                    return wormHead;
            }

            return npc;
        }

        private static bool IsSplittingWormSegment(NPC npc)
        {
            return (npc.aiStyle == NPCAIStyleID.Worm || npc.type == NPCID.EaterofWorldsBody || npc.type == NPCID.EaterofWorldsTail) &&
                   npc.ai[0] >= 0 && npc.ai[0] < Main.maxNPCs &&
                   npc.ai[1] >= 0 && npc.ai[1] < Main.maxNPCs;
        }

        private static NPC FindSplittingWormHead(NPC segment)
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

        private static NPC FindModdedWormHead(NPC segment)
        {
            if (segment.ModNPC == null) return null;

            string segmentName = segment.ModNPC.Name;
            string baseName = segmentName;
            if (segmentName.EndsWith("Body")) baseName = segmentName.Substring(0, segmentName.Length - 4);
            else if (segmentName.EndsWith("Tail")) baseName = segmentName.Substring(0, segmentName.Length - 4);
            else if (segmentName.Contains("Body")) baseName = segmentName.Substring(0, segmentName.IndexOf("Body"));
            else if (segmentName.Contains("Tail")) baseName = segmentName.Substring(0, segmentName.IndexOf("Tail"));

            NPC closestHead = null;
            float closestDist = float.MaxValue;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC other = Main.npc[i];
                if (other.active && other.ModNPC != null)
                {
                    string otherName = other.ModNPC.Name;
                    if (otherName.StartsWith(baseName) && (otherName.EndsWith("Head") || (!otherName.Contains("Body") && !otherName.Contains("Tail"))))
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
            return closestHead;
        }

        public static bool IsBossNPC(NPC npc)
        {
            if (npc == null || !npc.active)
                return false;

            NPC primary = GetPrimaryNPC(npc);

            if (primary.boss && !primary.friendly)
                return true;

            if (NPCID.Sets.ShouldBeCountedAsBoss[primary.type])
                return true;

            if (NPCID.Sets.BossHeadTextures[primary.type] >= 0)
                return true;

            if (primary.BossBar != null)
                return true;

            if (StatariaBossBarStyle.TreatAsBoss.Contains(primary.type))
                return true;

            return false;
        }

        public static void GetNPCTargetIdAndName(NPC rawNpc, out string targetId, out string name)
        {
            if (rawNpc == null)
            {
                targetId = "";
                name = "";
                return;
            }

            NPC npc = GetPrimaryNPC(rawNpc);

            int bannerId = Item.NPCtoBanner(npc.type);
            if (bannerId > 0)
            {
                int mainNpcType = Item.BannerToNPC(bannerId);
                if (mainNpcType > 0)
                {
                    targetId = "Banner_" + bannerId;
                    name = Lang.GetNPCName(mainNpcType).Value;
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        name = npc.TypeName;
                    }
                    return;
                }
            }

            targetId = npc.ModNPC != null ? npc.ModNPC.FullName : npc.type.ToString();
            name = npc.TypeName;
        }

        public override bool CanBeHitByNPC(NPC npc, ref int cooldownSlot)
        {
            if (IsAdaptorActive && npc != null && npc.active)
            {
                var config = ModContent.GetInstance<StatariaConfig>();
                AdaptationCategory cat = IsBossNPC(npc) ? AdaptationCategory.Boss : AdaptationCategory.Mob;
                GetNPCTargetIdAndName(npc, out string targetId, out string name);

                AdaptationKey key = new AdaptationKey(cat, targetId, name, isOffensive: false);
                AdaptationData data = GetAdaptation(key);
                int maxLevel = AdaptationData.GetMaxLevel();

                if (data.Level >= maxLevel && (config == null || config.roleSettings.AdaptorMaxLevelDamageImmunity))
                {
                    return false; // Complete hit cancellation at max level!
                }
            }
            return base.CanBeHitByNPC(npc, ref cooldownSlot);
        }

        public override bool CanBeHitByProjectile(Projectile proj)
        {
            if (IsAdaptorActive && proj != null && proj.active && proj.hostile)
            {
                var config = ModContent.GetInstance<StatariaConfig>();
                int maxLevel = AdaptationData.GetMaxLevel();
                bool immunityAllowed = config == null || config.roleSettings.AdaptorMaxLevelDamageImmunity;

                // 1. Check shooter NPC adaptation if available
                if (proj.TryGetGlobalProjectile<AdaptationGlobalProjectile>(out var globalProj) && globalProj.HasNPCSource)
                {
                    AdaptationCategory cat = globalProj.SourceNPCIsBoss ? AdaptationCategory.Boss : AdaptationCategory.Mob;
                    AdaptationKey npcKey = new AdaptationKey(cat, globalProj.SourceNPCTargetId, globalProj.SourceNPCName, isOffensive: false);

                    if (InstantAdaptationMode)
                    {
                        GainExp(npcKey, 999999f);
                    }

                    if (GetAdaptation(npcKey).Level >= maxLevel && immunityAllowed)
                    {
                        return false; // Immune to the shooter!
                    }
                }

                // 2. Check projectile type adaptation
                string projIdName = ProjectileID.Search.GetName(proj.type);
                if (string.IsNullOrEmpty(projIdName))
                {
                    var modProj = proj.ModProjectile;
                    projIdName = modProj != null ? modProj.FullName : proj.type.ToString();
                }

                string projName = Lang.GetProjectileName(proj.type).Value;
                if (string.IsNullOrWhiteSpace(projName))
                {
                    projName = proj.Name;
                }

                bool isBoss = globalProj != null && globalProj.SourceNPCIsBoss;
                AdaptationCategory projCat = isBoss ? AdaptationCategory.Boss : AdaptationCategory.Mob;
                AdaptationKey projKey = new AdaptationKey(projCat, "Proj_" + projIdName, projName, isOffensive: false);

                if (InstantAdaptationMode)
                {
                    GainExp(projKey, 999999f);
                }

                if (GetAdaptation(projKey).Level >= maxLevel && immunityAllowed)
                {
                    return false; // Immune to this specific projectile type!
                }
            }
            return base.CanBeHitByProjectile(proj);
        }

        public override void ModifyHurt(ref Player.HurtModifiers modifiers)
        {
            if (!IsAdaptorActive)
                return;

            var config = ModContent.GetInstance<StatariaConfig>();
            float reductionPerLevel = config != null ? config.roleSettings.AdaptorDefensiveDamageReductionPerLevel : 0.08f;
            int maxLevel = AdaptationData.GetMaxLevel();

            // Try to get causing entity (NPC or Projectile)
            if (modifiers.DamageSource.TryGetCausingEntity(out Entity causingEntity))
            {
                if (causingEntity is NPC attackerNPC && attackerNPC.active)
                {
                    AdaptationCategory cat = IsBossNPC(attackerNPC) ? AdaptationCategory.Boss : AdaptationCategory.Mob;
                    GetNPCTargetIdAndName(attackerNPC, out string targetId, out string name);

                    AdaptationKey key = new AdaptationKey(cat, targetId, name, isOffensive: false);
                    AdaptationData data = GetAdaptation(key);

                    if (data.Level >= maxLevel && config.roleSettings.AdaptorMaxLevelDamageImmunity)
                    {
                        modifiers.FinalDamage *= 0f;
                    }
                    else if (data.Level > 0)
                    {
                        float reductionPct = data.Level * reductionPerLevel;
                        modifiers.FinalDamage *= Math.Max(0f, 1.0f - reductionPct);
                    }
                }
                else if (causingEntity is Projectile proj && proj.active)
                {
                    float bestReduction = 0f;

                    if (proj.TryGetGlobalProjectile<AdaptationGlobalProjectile>(out var globalProj) && globalProj.HasNPCSource)
                    {
                        AdaptationCategory cat = globalProj.SourceNPCIsBoss ? AdaptationCategory.Boss : AdaptationCategory.Mob;
                        AdaptationKey npcKey = new AdaptationKey(cat, globalProj.SourceNPCTargetId, globalProj.SourceNPCName, isOffensive: false);
                        int npcLevel = GetAdaptation(npcKey).Level;
                        if (npcLevel >= maxLevel && config.roleSettings.AdaptorMaxLevelDamageImmunity) bestReduction = 1.0f;
                        else if (npcLevel > 0) bestReduction = Math.Max(bestReduction, npcLevel * reductionPerLevel);
                    }

                    string projIdName = ProjectileID.Search.GetName(proj.type);
                    if (string.IsNullOrEmpty(projIdName))
                    {
                        var modProj = proj.ModProjectile;
                        projIdName = modProj != null ? modProj.FullName : proj.type.ToString();
                    }

                    string projName = Lang.GetProjectileName(proj.type).Value;
                    if (string.IsNullOrWhiteSpace(projName)) projName = proj.Name;

                    bool isBoss = globalProj != null && globalProj.SourceNPCIsBoss;
                    AdaptationCategory projCat = isBoss ? AdaptationCategory.Boss : AdaptationCategory.Mob;
                    AdaptationKey projKey = new AdaptationKey(projCat, "Proj_" + projIdName, projName, isOffensive: false);
                    int projLevel = GetAdaptation(projKey).Level;

                    if (projLevel >= maxLevel && config.roleSettings.AdaptorMaxLevelDamageImmunity) bestReduction = 1.0f;
                    else if (projLevel > 0) bestReduction = Math.Max(bestReduction, projLevel * reductionPerLevel);

                    modifiers.FinalDamage *= Math.Max(0f, 1.0f - bestReduction);
                }
            }

            // Fall damage adaptation (SourceOtherIndex == 0)
            if (modifiers.DamageSource.SourceOtherIndex == 0)
            {
                AdaptationKey fallKey = GetEnvironmentKey("FallDamage", "Mods.Stataria.Adaptation.FallDamage");
                AdaptationData fallData = GetAdaptation(fallKey);

                if (fallData.Level >= maxLevel)
                {
                    modifiers.FinalDamage *= 0f;
                }
                else if (fallData.Level > 0)
                {
                    modifiers.FinalDamage *= Math.Max(0f, 1.0f - fallData.Level * (1.0f / maxLevel));
                }
            }

            // Drowning damage adaptation (SourceOtherIndex == 1 or 3)
            if (modifiers.DamageSource.SourceOtherIndex == 1 || modifiers.DamageSource.SourceOtherIndex == 3)
            {
                AdaptationKey breathKey = GetEnvironmentKey("Breath", "Mods.Stataria.Adaptation.Drowning");
                AdaptationData breathData = GetAdaptation(breathKey);

                if (breathData.Level >= maxLevel)
                {
                    modifiers.FinalDamage *= 0f;
                }
                else if (breathData.Level > 0)
                {
                    modifiers.FinalDamage *= Math.Max(0f, 1.0f - breathData.Level * (1.0f / maxLevel));
                }
            }

            // Lava damage adaptation (SourceOtherIndex == 2)
            if (modifiers.DamageSource.SourceOtherIndex == 2)
            {
                AdaptationKey lavaKey = GetEnvironmentKey("Lava", "Mods.Stataria.Adaptation.Lava");
                AdaptationData lavaData = GetAdaptation(lavaKey);

                if (lavaData.Level >= maxLevel)
                {
                    modifiers.FinalDamage *= 0f;
                }
                else if (lavaData.Level > 0)
                {
                    modifiers.FinalDamage *= Math.Max(0f, 1.0f - lavaData.Level * (1.0f / maxLevel));
                }
            }

            // Knockback adaptation scaling
            AdaptationKey kbKey = GetEnvironmentKey("Knockback", "Mods.Stataria.Adaptation.Knockback");
            AdaptationData kbData = GetAdaptation(kbKey);
            if (kbData.Level >= maxLevel)
            {
                modifiers.Knockback *= 0f;
            }
            else if (kbData.Level > 0)
            {
                float kbReductionPct = Math.Clamp(kbData.Level * (1.0f / maxLevel), 0f, 1.0f);
                modifiers.Knockback *= (1.0f - kbReductionPct);
            }
        }

        public override void OnHurt(Player.HurtInfo info)
        {
            if (!IsAdaptorActive)
                return;

            var config = ModContent.GetInstance<StatariaConfig>();
            float hurtMult = config != null ? config.roleSettings.AdaptorExpHurtMultiplier : 1.5f;

            if (info.DamageSource.TryGetCausingEntity(out Entity causingEntity))
            {
                if (causingEntity is NPC attackerNPC && attackerNPC.active)
                {
                    AdaptationCategory cat = IsBossNPC(attackerNPC) ? AdaptationCategory.Boss : AdaptationCategory.Mob;
                    GetNPCTargetIdAndName(attackerNPC, out string targetId, out string name);

                    AdaptationKey key = new AdaptationKey(cat, targetId, name, isOffensive: false);
                    GainExp(key, Math.Max(25f, info.Damage * hurtMult));
                }
                else if (causingEntity is Projectile proj && proj.active)
                {
                    if (proj.TryGetGlobalProjectile<AdaptationGlobalProjectile>(out var globalProj) && globalProj.HasNPCSource)
                    {
                        AdaptationCategory cat = globalProj.SourceNPCIsBoss ? AdaptationCategory.Boss : AdaptationCategory.Mob;
                        AdaptationKey npcKey = new AdaptationKey(cat, globalProj.SourceNPCTargetId, globalProj.SourceNPCName, isOffensive: false);
                        GainExp(npcKey, Math.Max(25f, info.Damage * hurtMult));
                    }

                    string projIdName = ProjectileID.Search.GetName(proj.type);
                    if (string.IsNullOrEmpty(projIdName))
                    {
                        var modProj = proj.ModProjectile;
                        projIdName = modProj != null ? modProj.FullName : proj.type.ToString();
                    }

                    string projName = Lang.GetProjectileName(proj.type).Value;
                    if (string.IsNullOrWhiteSpace(projName)) projName = proj.Name;

                    bool isBoss = globalProj != null && globalProj.SourceNPCIsBoss;
                    AdaptationCategory projCat = isBoss ? AdaptationCategory.Boss : AdaptationCategory.Mob;
                    AdaptationKey projKey = new AdaptationKey(projCat, "Proj_" + projIdName, projName, isOffensive: false);
                    GainExp(projKey, Math.Max(25f, info.Damage * hurtMult));
                }
            }

            if (info.DamageSource.SourceOtherIndex == 0)
            {
                AdaptationKey fallKey = GetEnvironmentKey("FallDamage", "Mods.Stataria.Adaptation.FallDamage");
                GainExp(fallKey, Math.Max(25f, info.Damage * hurtMult * 1.33f));
            }
            else if (info.DamageSource.SourceOtherIndex == 1 || info.DamageSource.SourceOtherIndex == 3)
            {
                AdaptationKey breathKey = GetEnvironmentKey("Breath", "Mods.Stataria.Adaptation.Drowning");
                GainExp(breathKey, Math.Max(25f, info.Damage * hurtMult * 1.33f));
            }
            else if (info.DamageSource.SourceOtherIndex == 2)
            {
                AdaptationKey lavaKey = GetEnvironmentKey("Lava", "Mods.Stataria.Adaptation.Lava");
                GainExp(lavaKey, Math.Max(25f, info.Damage * hurtMult * 1.33f));
            }

            // Gain Knockback EXP (only when actually receiving knockback and not from fall damage)
            if (info.DamageSource.SourceOtherIndex != 0 && info.Knockback > 0f)
            {
                AdaptationKey kbKey = GetEnvironmentKey("Knockback", "Mods.Stataria.Adaptation.Knockback");
                float kbExp = Math.Max(25f, info.Damage * 0.8f + info.Knockback * 10f);
                GainExp(kbKey, kbExp);
            }
        }

        public override bool PreKill(double damage, int hitDirection, bool pvp, ref bool playSound, ref bool genDust, ref PlayerDeathReason damageSource)
        {
            if (!IsAdaptorActive)
                return true;

            var config = ModContent.GetInstance<StatariaConfig>();
            bool enableCheatDeath = config == null || config.roleSettings.AdaptorEnableCheatDeath;
            int maxLevel = AdaptationData.GetMaxLevel();

            AdaptationKey deathKey = GetDeathKey();
            AdaptationData deathData = GetAdaptation(deathKey);

            if (enableCheatDeath && (deathData.Level >= maxLevel || CheatDeathCooldownTimer <= 0))
            {
                // Prevent death!
                float healPct = deathData.Level >= maxLevel ? 1.0f : Math.Max(0.15f, 0.15f + deathData.Level * (0.85f / maxLevel));
                Player.statLife = Math.Max(1, (int)(Player.statLifeMax2 * healPct));
                if (deathData.Level >= maxLevel)
                {
                    Player.statMana = Player.statManaMax2;
                }
                float invincibilitySeconds = config != null ? config.roleSettings.AdaptorCheatDeathInvincibilitySeconds : 3.0f;
                int invincibilityTicks = Math.Max(10, (int)(invincibilitySeconds * 60f));
                Player.immune = true;
                Player.immuneTime = invincibilityTicks;
                CheatDeathInvincibilityTimer = invincibilityTicks;
                Player.lavaTime = Player.lavaMax;

                if (deathData.Level >= maxLevel)
                {
                    CheatDeathCooldownTimer = 0; // Permanent immortality at max level!
                }
                else
                {
                    int baseCdSeconds = config != null ? config.roleSettings.AdaptorCheatDeathCooldownSeconds : 60;
                    int effectiveCdSeconds = Math.Max(5, (int)(baseCdSeconds * (1.0f - deathData.Level * (0.9f / maxLevel))));
                    CheatDeathCooldownTimer = effectiveCdSeconds * 60;
                }

                // Gain Death EXP
                float deathExpGain = config != null ? config.roleSettings.AdaptorDeathExpGain : 300f;
                GainExp(deathKey, deathExpGain);

                AuraFlash.TriggerCheatDeathEffect(Player);

                playSound = false;
                genDust = false;
                return false; // Cancel death!
            }

            return true;
        }

        #endregion

        #region Environmental & Buff/Debuff Update Hooks

        private static AdaptationKey GetEnvironmentKey(string targetId, string locKey)
        {
            string displayName = Terraria.Localization.Language.GetTextValue(locKey);
            return new AdaptationKey(AdaptationCategory.Environment, targetId, displayName, isOffensive: false);
        }

        private static AdaptationKey GetDeathKey()
        {
            string displayName = Terraria.Localization.Language.GetTextValue("Mods.Stataria.Adaptation.Death");
            return new AdaptationKey(AdaptationCategory.Death, "Death", displayName, isOffensive: false);
        }

        public override void PreUpdateBuffs()
        {
            if (!IsAdaptorActive)
                return;

            var config = ModContent.GetInstance<StatariaConfig>();
            int maxLevel = AdaptationData.GetMaxLevel();

            for (int i = 0; i < Player.MaxBuffs; i++)
            {
                int buffType = Player.buffType[i];
                int buffTime = Player.buffTime[i];

                if (buffType <= 0 || buffTime <= 0)
                    continue;

                string buffIdName = BuffID.Search.GetName(buffType);
                if (string.IsNullOrEmpty(buffIdName))
                {
                    var modBuff = ModContent.GetModBuff(buffType);
                    buffIdName = modBuff != null ? modBuff.FullName : buffType.ToString();
                }

                string displayName = Lang.GetBuffName(buffType);
                if (string.IsNullOrWhiteSpace(displayName))
                    displayName = buffIdName;

                if (config != null)
                {
                    bool isWhitelisted = MatchesBuffList(config.roleSettings.AdaptorDebuffWhitelist, buffIdName, displayName, buffType);
                    bool isBlacklisted = MatchesBuffList(config.roleSettings.AdaptorDebuffBlacklist, buffIdName, displayName, buffType);

                    if (isWhitelisted)
                    {
                        // Whitelisted entries take priority over blacklist and allow positive buffs to be adapted to
                    }
                    else
                    {
                        if (isBlacklisted)
                            continue;

                        if (Main.lightPet[buffType] || Main.vanityPet[buffType] || !Main.debuff[buffType])
                            continue;
                    }
                }
                else
                {
                    if (Main.lightPet[buffType] || Main.vanityPet[buffType] || !Main.debuff[buffType])
                        continue;
                }

                AdaptationKey key = new AdaptationKey(AdaptationCategory.Debuff, buffIdName, displayName, isOffensive: false);
                AdaptationData data = GetAdaptation(key);

                bool isTileAura = Main.buffNoTimeDisplay[buffType] || buffTime <= 10;

                if (data.Level >= maxLevel)
                {
                    Player.buffImmune[buffType] = true;
                    Player.DelBuff(i);
                    i--;
                }
                else
                {
                    float debuffGain = config != null ? config.roleSettings.AdaptorExpDebuffTickGain : 0.5f;
                    GainExp(key, debuffGain);

                    float debuffLevelRatio = (float)(data.Level - 1) / Math.Max(1, maxLevel - 1);
                    int debuffInterval = Math.Max(1, (int)Math.Round(10f * (1.0f - debuffLevelRatio) + 1f * debuffLevelRatio));
                    if (data.Level > 0 && !isTileAura && buffTime > 300 && Main.GameUpdateCount % debuffInterval == 0)
                    {
                        Player.buffTime[i] = Math.Max(0, Player.buffTime[i] - 1);
                    }
                }
            }

            if (Player.breath < Player.breathMax)
            {
                AdaptationKey breathKey = GetEnvironmentKey("Breath", "Mods.Stataria.Adaptation.Drowning");
                GainExp(breathKey, 0.8f);
            }
        }

        private static bool MatchesBuffList(List<string> list, string buffName, string displayName, int buffType)
        {
            if (list == null || list.Count == 0) return false;
            string typeStr = buffType.ToString();

            foreach (var item in list)
            {
                if (string.IsNullOrWhiteSpace(item)) continue;
                string entry = item.Trim();

                if (string.Equals(entry, buffName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(entry, typeStr, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(entry, displayName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(entry.Replace(" ", ""), buffName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(entry.Replace(" ", ""), displayName.Replace(" ", ""), StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        public override void PostUpdate()
        {
            if (!IsAdaptorActive)
                return;

            int maxLevel = AdaptationData.GetMaxLevel();

            // Handle Drowning breath retention
            AdaptationKey breathKey = GetEnvironmentKey("Breath", "Mods.Stataria.Adaptation.Drowning");
            AdaptationData breathData = GetAdaptation(breathKey);

            if (breathData.Level >= maxLevel)
            {
                Player.breath = Player.breathMax;
            }
            else if (breathData.Level > 0 && Player.breath < Player.breathMax)
            {
                float breathLevelRatio = (float)(breathData.Level - 1) / Math.Max(1, maxLevel - 1);
                int breathInterval = Math.Max(1, (int)Math.Round(10f * (1.0f - breathLevelRatio) + 1f * breathLevelRatio));
                if (Main.GameUpdateCount % breathInterval == 0)
                {
                    // Restore breath up to breathMax - 1 while underwater to prevent breath UI flickering
                    Player.breath = Math.Min(Player.breathMax - 1, Player.breath + 1);
                }
            }

            // Darkness Adaptation logic:
            AdaptationKey darkKey = GetEnvironmentKey("Darkness", "Mods.Stataria.Adaptation.Darkness");
            AdaptationData darkData = GetAdaptation(darkKey);

            Vector2 tileCoords = Player.Center / 16f;
            Color tileColor = Lighting.GetColor((int)tileCoords.X, (int)tileCoords.Y);
            Vector3 tileLight = tileColor.ToVector3();
            float rawBrightness = (tileLight.X + tileLight.Y + tileLight.Z) / 3f;

            // Subtract self-emitted ambient light so player's own adaptation doesn't throttle EXP gain
            float selfLightContrib = 0f;
            if (darkData.Level > 0)
            {
                float lightIntensity = (float)darkData.Level / maxLevel;
                selfLightContrib = 0.95f * lightIntensity;
            }

            // Subtract halo light contribution so halo wheel light doesn't affect darkness adaptation gain
            float haloLightContrib = 0f;
            var clientConfig = ModContent.GetInstance<StatariaClientConfig>();
            float haloOpacity = clientConfig != null ? clientConfig.HaloOpacity : 1.0f;
            if (haloOpacity > 0.01f)
            {
                Color haloColor = ActiveCategory.GetCategoryColor();
                float haloMult = HaloSpinTimer > 0 ? 1.0f : 0.75f;
                Vector3 haloVec = haloColor.ToVector3() * haloMult * haloOpacity * 0.45f * haloOpacity;
                haloLightContrib = (haloVec.X + haloVec.Y + haloVec.Z) / 3f;
            }

            float netBrightness = Math.Max(0f, rawBrightness - selfLightContrib - haloLightContrib);

            float darkThreshold = 0.20f;
            if (netBrightness < darkThreshold)
            {
                float darkExp = (darkThreshold - netBrightness) * 25.0f;
                GainExp(darkKey, darkExp);
            }

            if (darkData.Level > 0)
            {
                Player.nightVision = true;

                // Grant ambient night lighting that grows stronger with level!
                float lightIntensity = (float)darkData.Level / maxLevel;
                Vector3 lightColor = new Vector3(0.4f, 0.6f, 0.9f) * lightIntensity * 1.5f;
                Lighting.AddLight(Player.Center, lightColor);
            }
        }

        public override void PostUpdateEquips()
        {
            if (!IsAdaptorActive)
                return;

            if (PendingFullHeal)
            {
                Player.statLife = Player.statLifeMax2;
                Player.statMana = Player.statManaMax2;
                PendingFullHeal = false;
            }

            int maxLevel = AdaptationData.GetMaxLevel();

            foreach (var kvp in adaptations)
            {
                if (kvp.Key.Category == AdaptationCategory.Debuff && kvp.Value.Level >= maxLevel)
                {
                    if (int.TryParse(kvp.Key.TargetId, out int buffId))
                    {
                        if (buffId > 0 && buffId < Player.buffImmune.Length)
                        {
                            Player.buffImmune[buffId] = true;
                        }
                    }
                    else
                    {
                        int searchId = BuffID.Search.GetId(kvp.Key.TargetId);
                        if (searchId > 0 && searchId < Player.buffImmune.Length)
                        {
                            Player.buffImmune[searchId] = true;
                        }
                    }
                }
            }

            AdaptationKey fallKey = GetEnvironmentKey("FallDamage", "Mods.Stataria.Adaptation.FallDamage");
            AdaptationData fallData = GetAdaptation(fallKey);
            if (fallData.Level >= maxLevel)
            {
                Player.noFallDmg = true;
            }

            AdaptationKey kbKey = GetEnvironmentKey("Knockback", "Mods.Stataria.Adaptation.Knockback");
            AdaptationData kbData = GetAdaptation(kbKey);
            if (kbData.Level >= maxLevel)
            {
                Player.noKnockback = true;
            }

            AdaptationKey lavaKey = GetEnvironmentKey("Lava", "Mods.Stataria.Adaptation.Lava");
            AdaptationData lavaData = GetAdaptation(lavaKey);
            if (lavaData.Level >= maxLevel)
            {
                Player.lavaImmune = true;
            }
            else
            {
                if (lavaData.Level > 0)
                {
                    Player.lavaMax += lavaData.Level * 60;
                }
                if (Player.lavaWet)
                {
                    GainExp(lavaKey, 1.2f);
                }
            }
        }

        #endregion

        #region Save / Load

        public override void SaveData(TagCompound tag)
        {
            List<TagCompound> tagList = new List<TagCompound>();

            foreach (var kvp in adaptations)
            {
                TagCompound entry = new TagCompound
                {
                    ["cat"] = (int)kvp.Key.Category,
                    ["targetId"] = kvp.Key.TargetId,
                    ["displayName"] = kvp.Key.DisplayName,
                    ["isOffensive"] = kvp.Key.IsOffensive,
                    ["data"] = kvp.Value.Save()
                };
                tagList.Add(entry);
            }

            tag["adaptations"] = tagList;
            tag["cheatDeathCd"] = CheatDeathCooldownTimer;
        }

        public override void LoadData(TagCompound tag)
        {
            adaptations.Clear();

            if (tag.ContainsKey("adaptations"))
            {
                var tagList = tag.GetList<TagCompound>("adaptations");
                foreach (var entry in tagList)
                {
                    AdaptationCategory cat = (AdaptationCategory)entry.GetInt("cat");
                    string targetId = entry.GetString("targetId");
                    string displayName = entry.GetString("displayName");
                    bool isOffensive = entry.GetBool("isOffensive");
                    AdaptationData data = AdaptationData.Load(entry.GetCompound("data"));

                    AdaptationKey key = new AdaptationKey(cat, targetId, displayName, isOffensive);
                    adaptations[key] = data;
                }
            }

            if (tag.ContainsKey("cheatDeathCd"))
            {
                CheatDeathCooldownTimer = tag.GetInt("cheatDeathCd");
            }
        }

        #endregion
    }
}
