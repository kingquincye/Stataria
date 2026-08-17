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
        public int LevelUpFlashTimer { get; set; }
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
                       config.roleSettings.EnableRoleSystem &&
                       config.roleSettings.EnableAdaptorRole;
            }
        }

        private Vector2 lastValidPosition;
        public Vector2 LastValidPosition => lastValidPosition;

        public bool IsDeathAdapted(int maxLevel)
        {
            AdaptationKey deathKey = GetDeathKey();
            return GetAdaptation(deathKey).Level >= maxLevel;
        }

        public bool IsErasureAdapted(int maxLevel)
        {
            AdaptationKey erasureKey = GetErasureKey();
            return GetAdaptation(erasureKey).Level >= maxLevel;
        }

        public override void PreUpdate()
        {
            if (IsAdaptorActive && !Player.dead && Player.statLife > 0)
            {
                lastValidPosition = Player.position;
            }
        }

        public override void ResetEffects()
        {
            if (IsAdaptorActive)
            {
                CheckAndHandleErasureAdaptation(AdaptationData.GetMaxLevel());
            }

            if (CheatDeathCooldownTimer > 0)
            {
                CheatDeathCooldownTimer--;
            }

            if (CheatDeathInvincibilityTimer > 0)
            {
                CheatDeathInvincibilityTimer--;
                Player.immune = true;
                Player.immuneNoBlink = true;
                Player.immuneTime = Math.Max(Player.immuneTime, 2);
                Player.lavaImmune = true;
                Player.lavaTime = Player.lavaMax;
                Player.breath = Math.Max(Player.breath, Player.breathMax);
                Player.lifeRegen = Math.Max(Player.lifeRegen, 0);
                if (Player.statLife < 1)
                {
                    Player.statLife = 1;
                }
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

            if (LevelUpFlashTimer > 0)
            {
                LevelUpFlashTimer--;
            }
        }

        public override void SyncPlayer(int toWho, int fromWho, bool newPlayer)
        {
            Stataria.SyncAdaptationState(Player.whoAmI, toWho, fromWho);
            Stataria.SyncAllAdaptations(Player.whoAmI, toWho, fromWho);
        }

        public override void CopyClientState(ModPlayer targetCopy)
        {
            var clone = (AdaptationPlayer)targetCopy;
            clone.ActiveCategory = ActiveCategory;
            clone.HaloRotation = HaloRotation;
            clone.HaloSpinTimer = HaloSpinTimer;
            clone.LevelUpFlashTimer = LevelUpFlashTimer;
        }

        public override void SendClientChanges(ModPlayer clientPlayer)
        {
            var clone = (AdaptationPlayer)clientPlayer;
            if (clone.ActiveCategory != ActiveCategory || Math.Abs(clone.HaloRotation - HaloRotation) > 0.15f || clone.HaloSpinTimer != HaloSpinTimer || clone.LevelUpFlashTimer != LevelUpFlashTimer)
            {
                Stataria.SyncAdaptationState(Player.whoAmI);
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

        public Dictionary<AdaptationKey, AdaptationData> Adaptations => adaptations;

        public void SetAdaptationDisabled(AdaptationKey key, bool disabled)
        {
            var data = GetOrCreateAdaptation(key);
            if (data.Disabled != disabled)
            {
                data.Disabled = disabled;
                if (Main.netMode == NetmodeID.MultiplayerClient && Player.whoAmI == Main.myPlayer)
                {
                    Stataria.SyncAdaptationToggle(Player.whoAmI, key, disabled);
                }
            }
        }

        public override void ProcessTriggers(Terraria.GameInput.TriggersSet triggersSet)
        {
            if (StatariaKeybinds.ToggleAdaptationUI != null && StatariaKeybinds.ToggleAdaptationUI.JustPressed && !Terraria.GameInput.PlayerInput.WritingText)
            {
                StatariaUI.ToggleAdaptationUI();
            }
        }

        public AdaptationData GetAdaptation(AdaptationKey key)
        {
            if (adaptations.TryGetValue(key, out var data))
            {
                return data;
            }
            return new AdaptationData(0, 0f);
        }

        public AdaptationData GetOrCreateAdaptation(AdaptationKey key)
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

            var data = GetOrCreateAdaptation(key);
            if (data.Disabled)
                return;

            var config = ModContent.GetInstance<StatariaConfig>();
            float mult = config != null ? config.roleSettings.AdaptorGlobalExpMultiplier : 1.0f;
            float expToAdd = amount * mult;

            if (expToAdd <= 0f && !InstantAdaptationMode)
                return;

            ActiveCategory = key.Category;
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
                bool enableLevelUpHeal = config == null || config.roleSettings.AdaptorEnableLevelUpHeal;

                if (enableLevelUpHeal)
                {
                    // Full heal on level up (HP & Mana)
                    PendingFullHeal = true;
                    Player.statLife = Player.statLifeMax2;
                    Player.statMana = Player.statManaMax2;
                }

                HaloSpinTimer = 120;
                LevelUpFlashTimer = 120;
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
            string modName = segment.ModNPC.Mod.Name;

            // 1. Multi-part suffix stripping to extract base boss name
            string[] suffixes = new string[]
            {
                "Arms", "Limb", "Legs", "Hand", "Head", "Claw", "Body", "Part", "Core", "Turret",
                "Cannon", "Fist", "Wing", "Eye", "Jaw", "Charge", "Piece", "Segment", "Tail", "Body1", "Body2", "1", "2", "3"
            };

            string baseName = segmentName;
            foreach (string suffix in suffixes)
            {
                if (baseName.EndsWith(suffix))
                {
                    baseName = baseName.Substring(0, baseName.Length - suffix.Length);
                    break;
                }
                else if (baseName.Contains(suffix))
                {
                    int idx = baseName.IndexOf(suffix);
                    if (idx > 0)
                    {
                        baseName = baseName.Substring(0, idx);
                        break;
                    }
                }
            }

            if (string.IsNullOrEmpty(baseName))
                baseName = segmentName;

            NPC closestBoss = null;
            NPC closestHead = null;
            float closestBossDist = float.MaxValue;
            float closestHeadDist = float.MaxValue;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC other = Main.npc[i];
                if (other.active && other.whoAmI != segment.whoAmI && other.ModNPC != null && other.ModNPC.Mod.Name == modName)
                {
                    string otherName = other.ModNPC.Name;
                    if (otherName.StartsWith(baseName) || baseName.StartsWith(otherName))
                    {
                        float dist = Vector2.Distance(segment.Center, other.Center);

                        // Highest priority: The main Boss entity (e.g. TrojanSquirrel body, Ravager body, Thanatos head)
                        if (other.boss || NPCID.Sets.BossHeadTextures[other.type] >= 0 || other.BossBar != null)
                        {
                            if (dist < closestBossDist)
                            {
                                closestBossDist = dist;
                                closestBoss = other;
                            }
                        }
                        // Secondary priority: A head/core segment
                        else if (otherName.EndsWith("Head") || otherName == baseName)
                        {
                            if (dist < closestHeadDist)
                            {
                                closestHeadDist = dist;
                                closestHead = other;
                            }
                        }
                    }
                }
            }

            if (closestBoss != null)
                return closestBoss;

            if (closestHead != null)
                return closestHead;

            // 2. Fallback: Check if ai[0] or ai[1] points to an active NPC from the same mod matching baseName
            for (int a = 0; a <= 1; a++)
            {
                int parentIdx = (int)segment.ai[a];
                if (parentIdx >= 0 && parentIdx < Main.maxNPCs)
                {
                    NPC candidate = Main.npc[parentIdx];
                    if (candidate.active && candidate.whoAmI != segment.whoAmI && candidate.ModNPC != null && candidate.ModNPC.Mod.Name == modName)
                    {
                        if (candidate.ModNPC.Name.StartsWith(baseName) || baseName.StartsWith(candidate.ModNPC.Name))
                            return candidate;
                    }
                }
            }

            return null;
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
            int netId = npc.netID;

            string specificName = Lang.GetNPCName(netId).Value;
            if (string.IsNullOrWhiteSpace(specificName))
            {
                specificName = npc.TypeName;
            }
            if (string.IsNullOrWhiteSpace(specificName))
            {
                specificName = "Enemy";
            }

            int bannerId = Item.NPCtoBanner(npc.BannerID());
            if (bannerId > 0)
            {
                int mainNpcType = Item.BannerToNPC(bannerId);
                string mainBannerName = mainNpcType > 0 ? Lang.GetNPCName(mainNpcType).Value : "";
                if (string.IsNullOrWhiteSpace(mainBannerName))
                {
                    mainBannerName = npc.TypeName;
                }

                // If the specific NPC name matches or is a sub-variant of the main banner name (e.g., Zombie, Female Zombie, Bald Zombie),
                // group them under a clean, readable banner key like "Banner_Zombie".
                // Otherwise (e.g. Green Slime vs Blue Slime), treat it as a distinct species variant.
                if (!string.IsNullOrWhiteSpace(mainBannerName) &&
                    (specificName.Equals(mainBannerName, StringComparison.OrdinalIgnoreCase) ||
                     specificName.Contains(mainBannerName, StringComparison.OrdinalIgnoreCase) ||
                     mainBannerName.Contains(specificName, StringComparison.OrdinalIgnoreCase)))
                {
                    string cleanBannerName = mainBannerName.Replace(" ", "");
                    string modName = npc.ModNPC?.Mod?.Name;
                    if (string.IsNullOrEmpty(modName) && mainNpcType > 0)
                    {
                        var mainModNpc = Terraria.ModLoader.NPCLoader.GetNPC(mainNpcType);
                        if (mainModNpc != null && mainModNpc.Mod != null)
                        {
                            modName = mainModNpc.Mod.Name;
                        }
                    }

                    if (!string.IsNullOrEmpty(modName))
                    {
                        targetId = $"{modName}/Banner_{cleanBannerName}";
                    }
                    else
                    {
                        targetId = "Banner_" + cleanBannerName;
                    }
                    name = mainBannerName;
                    return;
                }
            }

            // For distinct variants (e.g. Green Slime, Red Slime) or non-banner entities
            if (npc.ModNPC != null)
            {
                targetId = npc.ModNPC.FullName;
            }
            else
            {
                string cleanSpecificName = specificName.Replace(" ", "");
                targetId = "NPC_" + cleanSpecificName;
            }
            name = specificName;
        }

        public override bool CanBeHitByNPC(NPC npc, ref int cooldownSlot)
        {
            if (IsAdaptorActive && CheatDeathInvincibilityTimer > 0)
            {
                return false; // Absolute contact immunity during Cheat Death invincibility!
            }

            if (IsAdaptorActive && npc != null && npc.active)
            {
                var config = ModContent.GetInstance<StatariaConfig>();
                AdaptationCategory cat = IsBossNPC(npc) ? AdaptationCategory.Boss : AdaptationCategory.Mob;
                GetNPCTargetIdAndName(npc, out string targetId, out string name);

                AdaptationKey key = new AdaptationKey(cat, targetId, name, isOffensive: false);
                AdaptationData data = GetAdaptation(key);
                int maxLevel = AdaptationData.GetMaxLevel();

                if (!data.Disabled && data.Level >= maxLevel && (config == null || config.roleSettings.AdaptorMaxLevelDamageImmunity))
                {
                    return false; // Complete hit cancellation at max level!
                }
            }
            return base.CanBeHitByNPC(npc, ref cooldownSlot);
        }

        public override bool CanBeHitByProjectile(Projectile proj)
        {
            if (IsAdaptorActive && CheatDeathInvincibilityTimer > 0)
            {
                return false; // Absolute projectile immunity during Cheat Death invincibility!
            }

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

                    var npcData = GetAdaptation(npcKey);
                    if (!npcData.Disabled && npcData.Level >= maxLevel && immunityAllowed)
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

                var projData = GetAdaptation(projKey);
                if (!projData.Disabled && projData.Level >= maxLevel && immunityAllowed)
                {
                    return false; // Immune to this specific projectile type!
                }
            }
            return base.CanBeHitByProjectile(proj);
        }

        public override bool FreeDodge(Player.HurtInfo info)
        {
            if (IsAdaptorActive && CheatDeathInvincibilityTimer > 0)
            {
                return true; // Absolute dodge during Cheat Death invincibility!
            }
            return base.FreeDodge(info);
        }

        public override void ModifyHurt(ref Player.HurtModifiers modifiers)
        {
            if (!IsAdaptorActive)
                return;

            if (CheatDeathInvincibilityTimer > 0)
            {
                modifiers.FinalDamage *= 0f;
                return;
            }

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

                    if (!data.Disabled && data.Level >= maxLevel && config.roleSettings.AdaptorMaxLevelDamageImmunity)
                    {
                        modifiers.FinalDamage *= 0f;
                    }
                    else if (!data.Disabled && data.Level > 0)
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
                        var npcData = GetAdaptation(npcKey);
                        if (!npcData.Disabled)
                        {
                            int npcLevel = npcData.Level;
                            if (npcLevel >= maxLevel && config.roleSettings.AdaptorMaxLevelDamageImmunity) bestReduction = 1.0f;
                            else if (npcLevel > 0) bestReduction = Math.Max(bestReduction, npcLevel * reductionPerLevel);
                        }
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
                    var projData = GetAdaptation(projKey);
                    if (!projData.Disabled)
                    {
                        int projLevel = projData.Level;
                        if (projLevel >= maxLevel && config.roleSettings.AdaptorMaxLevelDamageImmunity) bestReduction = 1.0f;
                        else if (projLevel > 0) bestReduction = Math.Max(bestReduction, projLevel * reductionPerLevel);
                    }

                    modifiers.FinalDamage *= Math.Max(0f, 1.0f - bestReduction);
                }
            }

            // Fall damage adaptation (SourceOtherIndex == 0)
            if (modifiers.DamageSource.SourceOtherIndex == 0)
            {
                AdaptationKey fallKey = GetEnvironmentKey("FallDamage", "Mods.Stataria.Adaptation.FallDamage");
                AdaptationData fallData = GetAdaptation(fallKey);

                if (!fallData.Disabled && fallData.Level >= maxLevel)
                {
                    modifiers.FinalDamage *= 0f;
                }
                else if (!fallData.Disabled && fallData.Level > 0)
                {
                    modifiers.FinalDamage *= Math.Max(0f, 1.0f - fallData.Level * (1.0f / maxLevel));
                }
            }

            // Breathlessness damage adaptation (SourceOtherIndex == 1 or 3)
            if (modifiers.DamageSource.SourceOtherIndex == 1 || modifiers.DamageSource.SourceOtherIndex == 3)
            {
                AdaptationKey breathKey = GetEnvironmentKey("Breath", "Mods.Stataria.Adaptation.Breathlessness");
                AdaptationData breathData = GetAdaptation(breathKey);

                if (!breathData.Disabled && breathData.Level >= maxLevel)
                {
                    modifiers.FinalDamage *= 0f;
                }
                else if (!breathData.Disabled && breathData.Level > 0)
                {
                    modifiers.FinalDamage *= Math.Max(0f, 1.0f - breathData.Level * (1.0f / maxLevel));
                }
            }

            // Lava damage adaptation (SourceOtherIndex == 2)
            if (modifiers.DamageSource.SourceOtherIndex == 2)
            {
                AdaptationKey lavaKey = GetEnvironmentKey("Lava", "Mods.Stataria.Adaptation.Lava");
                AdaptationData lavaData = GetAdaptation(lavaKey);

                if (!lavaData.Disabled && lavaData.Level >= maxLevel)
                {
                    modifiers.FinalDamage *= 0f;
                }
                else if (!lavaData.Disabled && lavaData.Level > 0)
                {
                    modifiers.FinalDamage *= Math.Max(0f, 1.0f - lavaData.Level * (1.0f / maxLevel));
                }
            }

            // Knockback adaptation scaling
            AdaptationKey kbKey = GetEnvironmentKey("Knockback", "Mods.Stataria.Adaptation.Knockback");
            AdaptationData kbData = GetAdaptation(kbKey);
            if (!kbData.Disabled && kbData.Level >= maxLevel)
            {
                modifiers.Knockback *= 0f;
            }
            else if (!kbData.Disabled && kbData.Level > 0)
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
            float rawDamage = Math.Max((float)info.SourceDamage, (float)info.Damage);

            if (info.DamageSource.TryGetCausingEntity(out Entity causingEntity))
            {
                if (causingEntity is NPC attackerNPC && attackerNPC.active)
                {
                    AdaptationCategory cat = IsBossNPC(attackerNPC) ? AdaptationCategory.Boss : AdaptationCategory.Mob;
                    GetNPCTargetIdAndName(attackerNPC, out string targetId, out string name);

                    AdaptationKey key = new AdaptationKey(cat, targetId, name, isOffensive: false);
                    GainExp(key, Math.Max(25f, rawDamage * hurtMult));
                }
                else if (causingEntity is Projectile proj && proj.active)
                {
                    if (proj.TryGetGlobalProjectile<AdaptationGlobalProjectile>(out var globalProj) && globalProj.HasNPCSource)
                    {
                        AdaptationCategory cat = globalProj.SourceNPCIsBoss ? AdaptationCategory.Boss : AdaptationCategory.Mob;
                        AdaptationKey npcKey = new AdaptationKey(cat, globalProj.SourceNPCTargetId, globalProj.SourceNPCName, isOffensive: false);
                        GainExp(npcKey, Math.Max(25f, rawDamage * hurtMult));
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
                    GainExp(projKey, Math.Max(25f, rawDamage * hurtMult));
                }
            }

            if (info.DamageSource.SourceOtherIndex == 0)
            {
                AdaptationKey fallKey = GetEnvironmentKey("FallDamage", "Mods.Stataria.Adaptation.FallDamage");
                GainExp(fallKey, Math.Max(25f, rawDamage * hurtMult * 1.33f));
            }
            else if (info.DamageSource.SourceOtherIndex == 1 || info.DamageSource.SourceOtherIndex == 3)
            {
                AdaptationKey breathKey = GetEnvironmentKey("Breath", "Mods.Stataria.Adaptation.Breathlessness");
                GainExp(breathKey, Math.Max(25f, rawDamage * hurtMult * 1.33f));
            }
            else if (info.DamageSource.SourceOtherIndex == 2)
            {
                AdaptationKey lavaKey = GetEnvironmentKey("Lava", "Mods.Stataria.Adaptation.Lava");
                GainExp(lavaKey, Math.Max(25f, rawDamage * hurtMult * 1.33f));
            }

            // Gain Knockback EXP (only when actually receiving knockback and not from fall damage)
            if (info.DamageSource.SourceOtherIndex != 0 && info.Knockback > 0f)
            {
                AdaptationKey kbKey = GetEnvironmentKey("Knockback", "Mods.Stataria.Adaptation.Knockback");
                float kbExp = Math.Max(25f, rawDamage * 0.8f + info.Knockback * 10f);
                GainExp(kbKey, kbExp);
            }
        }

        public override bool PreKill(double damage, int hitDirection, bool pvp, ref bool playSound, ref bool genDust, ref PlayerDeathReason damageSource)
        {
            if (!IsAdaptorActive)
                return true;

            // Absolute immortality during Cheat Death invincibility frames!
            if (CheatDeathInvincibilityTimer > 0)
            {
                Player.statLife = Math.Max(1, Player.statLife);
                playSound = false;
                genDust = false;
                return false;
            }

            var config = ModContent.GetInstance<StatariaConfig>();
            bool enableCheatDeath = config == null || config.roleSettings.AdaptorEnableCheatDeath;
            int maxLevel = AdaptationData.GetMaxLevel();

            AdaptationKey deathKey = GetDeathKey();
            AdaptationData deathData = GetAdaptation(deathKey);

            if (!deathData.Disabled && enableCheatDeath && (deathData.Level >= maxLevel || CheatDeathCooldownTimer <= 0))
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
                Player.immuneNoBlink = true;
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

                if (Main.netMode == NetmodeID.MultiplayerClient)
                {
                    NetMessage.SendData(MessageID.PlayerLifeMana, -1, -1, null, Player.whoAmI);
                    Stataria.SyncAdaptationState(Player.whoAmI);
                }

                playSound = false;
                genDust = false;
                return false; // Cancel death!
            }

            return true;
        }

        #endregion

        #region Environmental & Buff/Debuff Update Hooks

        private static AdaptationKey GetEnvironmentKey(string targetId, string locKey, string fallbackName = null)
        {
            string displayName = Terraria.Localization.Language.GetTextValue(locKey);
            if ((string.IsNullOrEmpty(displayName) || displayName == locKey) && !string.IsNullOrEmpty(fallbackName))
                displayName = fallbackName;
            return new AdaptationKey(AdaptationCategory.Environment, targetId, displayName, isOffensive: false);
        }

        private static AdaptationKey GetDeathKey()
        {
            string displayName = Terraria.Localization.Language.GetTextValue("Mods.Stataria.Adaptation.Death");
            return new AdaptationKey(AdaptationCategory.Death, "Death", displayName, isOffensive: false);
        }

        private static AdaptationKey GetErasureKey()
        {
            string displayName = Terraria.Localization.Language.GetTextValue("Mods.Stataria.Adaptation.Erasure");
            if (string.IsNullOrEmpty(displayName) || displayName == "Mods.Stataria.Adaptation.Erasure")
                displayName = "Erasure";
            return new AdaptationKey(AdaptationCategory.Death, "Erasure", displayName, isOffensive: false);
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

                bool nurseCannotRemove = IsNurseCannotRemove(buffType);

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
                        if (isBlacklisted || nurseCannotRemove)
                            continue;

                        if (Main.lightPet[buffType] || Main.vanityPet[buffType] || !Main.debuff[buffType])
                            continue;
                    }
                }
                else
                {
                    if (nurseCannotRemove || Main.lightPet[buffType] || Main.vanityPet[buffType] || !Main.debuff[buffType])
                        continue;
                }

                AdaptationKey key = new AdaptationKey(AdaptationCategory.Debuff, buffIdName, displayName, isOffensive: false);
                AdaptationData data = GetAdaptation(key);

                if (data.Disabled)
                    continue;

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
                AdaptationKey breathKey = GetEnvironmentKey("Breath", "Mods.Stataria.Adaptation.Breathlessness");
                GainExp(breathKey, 0.8f);
            }
        }

        private static bool IsNurseCannotRemove(int buffType)
        {
            return buffType > 0
                && buffType < BuffID.Sets.NurseCannotRemoveDebuff.Length
                && BuffID.Sets.NurseCannotRemoveDebuff[buffType];
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

        public void CheckAndHandleErasureAdaptation(int maxLevel)
        {
            if (WrathOfTheGodsSupportHelper.WotGLoaded)
            {
                AdaptationKey erasureKey = GetErasureKey();
                AdaptationData erasureData = GetAdaptation(erasureKey);

                bool wotgErasureActive = WrathOfTheGodsSupportHelper.IsPlayerWasDeletedActive();
                if (wotgErasureActive)
                {
                    WrathOfTheGodsSupportHelper.ClearErasureState();
                    erasureData = GetOrCreateAdaptation(erasureKey);
                }

                if (!erasureData.Disabled && (wotgErasureActive || (erasureData.Level >= maxLevel && Player.dead)))
                {
                    if (erasureData.Level < maxLevel)
                    {
                        erasureData = GetOrCreateAdaptation(erasureKey);
                        erasureData.Level = maxLevel;
                        erasureData.CurrentExp = 0f;

                    AuraFlash.TriggerLevelUpEffect(Player, AdaptationCategory.Death);

                    AdaptationNotificationUI.AddNotification(new AdaptationNotification(
                        erasureKey.DisplayName,
                        AdaptationCategory.Death,
                        maxLevel,
                        1.0f,
                        isOffensive: false,
                        isLevelUp: true
                    ));
                }

                // Restore Player
                bool wasDead = Player.dead;
                Player.dead = false;
                Player.respawnTimer = 0;
                Player.immuneAlpha = 0;
                Player.statLife = Player.statLifeMax2;
                Player.statMana = Player.statManaMax2;

                    if (wasDead && lastValidPosition != Vector2.Zero)
                    {
                        Player.position = lastValidPosition;
                        Player.velocity = Vector2.Zero;
                    }
                }
            }

            // Universal 0.1% edge case safety: If standard Death Adaptation is maxed, intercept direct player.dead force-assignments
            AdaptationKey deathKey = GetDeathKey();
            AdaptationData deathData = GetAdaptation(deathKey);
            if (!deathData.Disabled && deathData.Level >= maxLevel && Player.dead)
            {
                bool wasDead = Player.dead;
                Player.dead = false;
                Player.respawnTimer = 0;
                Player.immuneAlpha = 0;
                Player.statLife = Player.statLifeMax2;
                Player.statMana = Player.statManaMax2;

                if (wasDead && lastValidPosition != Vector2.Zero)
                {
                    Player.position = lastValidPosition;
                    Player.velocity = Vector2.Zero;
                }
            }
        }

        public override void PostUpdate()
        {
            if (!IsAdaptorActive)
                return;

            int maxLevel = AdaptationData.GetMaxLevel();

            CheckAndHandleErasureAdaptation(maxLevel);

            // Lock breath at max level of Breathlessness adaptation
            AdaptationKey breathKey = GetEnvironmentKey("Breath", "Mods.Stataria.Adaptation.Breathlessness");
            AdaptationData breathData = GetAdaptation(breathKey);
            if (!breathData.Disabled && breathData.Level >= maxLevel)
            {
                Player.breath = Player.breathMax;
            }

            // Darkness Adaptation logic:
            AdaptationKey darkKey = GetEnvironmentKey("Darkness", "Mods.Stataria.Adaptation.Darkness");
            AdaptationData darkData = GetAdaptation(darkKey);

            if (!darkData.Disabled)
            {
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

                // Subtract level up flash light contribution so level up visual effect light doesn't affect darkness adaptation gain
                float levelUpLightContrib = 0f;
                if (LevelUpFlashTimer > 0)
                {
                    float flashIntensity = LevelUpFlashTimer / 120f;
                    levelUpLightContrib = 2.5f * flashIntensity;
                }

                float netBrightness = Math.Max(0f, rawBrightness - selfLightContrib - haloLightContrib - levelUpLightContrib);

                bool isDaytimeSurface = Main.dayTime && (Player.Center.Y < Main.worldSurface * 16f);
                float darkThreshold = isDaytimeSurface ? 0.05f : 0.20f;
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

            // Calamity Environmental Adaptations (Sulphurous Water & Abyss Darkness)
            if (CalamitySupportHelper.CalamityLoaded)
            {
                // 1. Sulphurous Water Adaptation
                AdaptationKey sulphWaterKey = GetEnvironmentKey("SulphurousWater", "Mods.Stataria.Adaptation.SulphurousWater", "Sulphurous Water");
                AdaptationData sulphWaterData = GetAdaptation(sulphWaterKey);
                float currentPoison = CalamitySupportHelper.GetSulphWaterPoisoningLevel(Player);

                if (!sulphWaterData.Disabled)
                {
                    if (currentPoison > 0f || (CalamitySupportHelper.GetInZone(Player, "sulfur") && Player.wet && !Player.lavaWet && !Player.honeyWet))
                    {
                        GainExp(sulphWaterKey, 0.8f);
                    }

                    if (sulphWaterData.Level > 0)
                    {
                        if (sulphWaterData.Level >= maxLevel)
                        {
                            CalamitySupportHelper.SetSulphWaterPoisoningLevel(Player, 0f);
                        }
                        else if (currentPoison > 0f && Player.wet && !Player.lavaWet && !Player.honeyWet)
                        {
                            float adaptRatio = (float)sulphWaterData.Level / maxLevel;
                            float frameIncrement = 1f / 360f;
                            float newPoison = Math.Max(0f, currentPoison - (frameIncrement * adaptRatio));
                            CalamitySupportHelper.SetSulphWaterPoisoningLevel(Player, newPoison);
                        }
                    }
                }

                // 2. Abyss Darkness Adaptation
                AdaptationKey abyssDarkKey = GetEnvironmentKey("AbyssDarkness", "Mods.Stataria.Adaptation.AbyssDarkness", "Abyss Darkness");
                AdaptationData abyssDarkData = GetAdaptation(abyssDarkKey);

                if (!abyssDarkData.Disabled && CalamitySupportHelper.GetInZone(Player, "abyss"))
                {
                    GainExp(abyssDarkKey, 1.0f);
                    if (abyssDarkData.Level > 0)
                    {
                        float adaptRatio = (float)abyssDarkData.Level / maxLevel;

                        // Directly reduce Calamity's darknessIntensity shader opacity
                        float currentDarkness = CalamitySupportHelper.GetDarknessIntensity(Player);
                        if (currentDarkness > 0f)
                        {
                            float newDarkness = currentDarkness * (1.0f - adaptRatio);
                            CalamitySupportHelper.SetDarknessIntensity(Player, newDarkness);
                        }

                        CalamitySupportHelper.AddAbyssLightStrength(Player, adaptRatio * 5.0f);
                    }
                }
            }
        }

        public override void PostUpdateEquips()
        {
            if (!IsAdaptorActive)
                return;

            if (CheatDeathInvincibilityTimer > 0)
            {
                Player.lifeRegen = Math.Max(Player.lifeRegen, 0);
                if (Player.statLife < 1)
                {
                    Player.statLife = 1;
                }
            }

            if (PendingFullHeal)
            {
                Player.statLife = Player.statLifeMax2;
                Player.statMana = Player.statManaMax2;
                PendingFullHeal = false;

                if (Main.netMode == NetmodeID.MultiplayerClient)
                {
                    NetMessage.SendData(MessageID.PlayerLifeMana, -1, -1, null, Player.whoAmI);
                }
            }

            int maxLevel = AdaptationData.GetMaxLevel();

            foreach (var kvp in adaptations)
            {
                if (!kvp.Value.Disabled && kvp.Key.Category == AdaptationCategory.Debuff && kvp.Value.Level >= maxLevel)
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
            if (!fallData.Disabled && fallData.Level >= maxLevel)
            {
                Player.noFallDmg = true;
            }

            AdaptationKey kbKey = GetEnvironmentKey("Knockback", "Mods.Stataria.Adaptation.Knockback");
            AdaptationData kbData = GetAdaptation(kbKey);
            if (!kbData.Disabled && kbData.Level >= maxLevel)
            {
                Player.noKnockback = true;
            }

            AdaptationKey lavaKey = GetEnvironmentKey("Lava", "Mods.Stataria.Adaptation.Lava");
            AdaptationData lavaData = GetAdaptation(lavaKey);
            if (!lavaData.Disabled)
            {
                if (lavaData.Level >= maxLevel)
                {
                    Player.lavaImmune = true;
                }
                else if (Player.lavaWet)
                {
                    GainExp(lavaKey, 1.2f);
                }
            }

            // 3. Abyss Pressure Adaptation (Defense Loss Refund)
            if (CalamitySupportHelper.CalamityLoaded && CalamitySupportHelper.GetInZone(Player, "abyss"))
            {
                AdaptationKey abyssPressKey = GetEnvironmentKey("AbyssPressure", "Mods.Stataria.Adaptation.AbyssPressure", "Abyss Pressure");
                AdaptationData abyssPressData = GetAdaptation(abyssPressKey);
                if (!abyssPressData.Disabled)
                {
                    int defenseLoss = CalamitySupportHelper.GetAbyssDefenseLoss(Player);

                    if (defenseLoss > 0)
                    {
                        GainExp(abyssPressKey, 0.5f);
                    }

                    if (abyssPressData.Level > 0 && defenseLoss > 0)
                    {
                        int refundedDefense = (int)(defenseLoss * ((float)abyssPressData.Level / maxLevel));
                        Player.statDefense += refundedDefense;
                    }
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
