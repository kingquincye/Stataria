using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.ID;
using Terraria.DataStructures;
using Microsoft.Xna.Framework;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Terraria.Audio;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;
using Terraria.GameInput;

namespace Stataria
{
    public class RPGPlayer : ModPlayer
    {
        public int xpBarTimer = 0;
        private const int xpBarDuration = 120;
        public int levelCapMessageTimer = 0;
        private const int levelCapMessageCooldown = 1800;
        public int teleportCooldownTimer = 0;
        public int Level = 1;
        public long XP = 0L;
        public long XPToNext = 100L;
        public int StatPoints = 0;
        private XPVerificationSystem xpVerifier;
        private int customRegenDelayTimer = 0;
        private float regenCarryover = 0f;
        public int lastStandCooldownTimer = 0;
        public int lastStandImmunityTimer = 0;
        private bool wasLastStandTriggered = false;
        private int lastStandHealAmount;

        private bool appliedPotionReduction = false;
        private bool appliedManaSickReduction = false;
        public int RebirthCount = 0;
        public int RebirthPoints = 0;
        public bool WasRetroRPGranted = false;
        public Dictionary<string, RebirthAbility> RebirthAbilities { get; private set; } = new Dictionary<string, RebirthAbility>();

        public int VIT = 0, STR = 0, AGI = 0, INT = 0, LUC = 0, END = 0, POW = 0, DEX = 0, SPR = 0, RGE = 0, TCH = 0, BRD = 0, HLR = 0, CLK = 0;
        public HashSet<int> rewardedBosses = new();

        public bool AutoAllocateEnabled { get; set; } = false;
        public HashSet<string> AutoAllocateStats { get; private set; } = new HashSet<string>();
        public Dictionary<string, int> GhostStats { get; private set; } = new Dictionary<string, int>();

        public override void Initialize()
        {
            var config = ModContent.GetInstance<StatariaConfig>();
            Level = 1;
            XP = 0L;
            XPToNext = (long)(100L * Math.Pow(Level, config.generalBalance.LevelScalingFactor));
            StatPoints = 0;
            VIT = STR = AGI = INT = LUC = END = POW = DEX = SPR = RGE = TCH = BRD = HLR = CLK = 0;
            GhostStats = new Dictionary<string, int>();
            rewardedBosses.Clear();
            lastStandCooldownTimer = 0;
            lastStandImmunityTimer = 0;
            wasLastStandTriggered = false;
            RebirthCount = 0;
            RebirthPoints = 0;
            RebirthAbilities = new Dictionary<string, RebirthAbility>();
            RegisterDefaultAbilities();
            xpVerifier = new XPVerificationSystem(this);
        }

        public override void SaveData(TagCompound tag)
        {
            tag["Level"] = Level;
            tag["XP"] = XP;
            tag["XPToNext"] = XPToNext;
            tag["StatPoints"] = StatPoints;
            tag["VIT"] = VIT; tag["STR"] = STR; tag["AGI"] = AGI;
            tag["INT"] = INT; tag["LUC"] = LUC; tag["END"] = END;
            tag["POW"] = POW; tag["DEX"] = DEX; tag["SPR"] = SPR;
            tag["RGE"] = RGE; tag["TCH"] = TCH; tag["BRD"] = BRD;
            tag["HLR"] = HLR; tag["CLK"] = CLK;
            tag["RewardedBosses"] = new List<int>(rewardedBosses);
            tag["lastStandCooldownTimer"] = lastStandCooldownTimer;
            tag["RebirthCount"] = RebirthCount;
            tag["RebirthPoints"] = RebirthPoints;
            tag["WasRetroRPGranted"] = WasRetroRPGranted;
            var abilitiesData = new List<TagCompound>();
            foreach (var kvp in RebirthAbilities)
            {
                var abilityTag = kvp.Value.Save();
                abilityTag["AbilityId"] = kvp.Key;
                abilitiesData.Add(abilityTag);
            }
            tag["RebirthAbilities"] = abilitiesData;
            tag["AutoAllocateEnabled"] = AutoAllocateEnabled;
            tag["AutoAllocateStats"] = AutoAllocateStats.ToList();
        }

        public override void LoadData(TagCompound tag)
        {
            Level = tag.GetInt("Level");
            XP = tag.GetAsLong("XP");
            XPToNext = tag.GetAsLong("XPToNext");
            StatPoints = tag.GetInt("StatPoints");
            VIT = tag.GetInt("VIT"); STR = tag.GetInt("STR"); AGI = tag.GetInt("AGI");
            INT = tag.GetInt("INT"); LUC = tag.GetInt("LUC"); END = tag.GetInt("END");
            POW = tag.GetInt("POW"); DEX = tag.GetInt("DEX"); SPR = tag.GetInt("SPR");
            RGE = tag.ContainsKey("RGE") ? tag.GetInt("RGE") : 0;
            TCH = tag.ContainsKey("TCH") ? tag.GetInt("TCH") : 0;
            BRD = tag.ContainsKey("BRD") ? tag.GetInt("BRD") : 0;
            HLR = tag.ContainsKey("HLR") ? tag.GetInt("HLR") : 0;
            CLK = tag.ContainsKey("CLK") ? tag.GetInt("CLK") : 0;
            if (tag.ContainsKey("RewardedBosses"))
                rewardedBosses = tag.Get<List<int>>("RewardedBosses").ToHashSet();
            lastStandCooldownTimer = tag.ContainsKey("lastStandCooldownTimer") ? tag.GetInt("lastStandCooldownTimer") : 0;
            RebirthCount = tag.ContainsKey("RebirthCount") ? tag.GetInt("RebirthCount") : 0;
            RebirthPoints = tag.ContainsKey("RebirthPoints") ? tag.GetInt("RebirthPoints") : 0;
            WasRetroRPGranted = tag.ContainsKey("WasRetroRPGranted") ? tag.GetBool("WasRetroRPGranted") : false;
            RegisterDefaultAbilities();
            if (tag.ContainsKey("RebirthAbilities"))
            {
                var abilitiesData = tag.Get<List<TagCompound>>("RebirthAbilities");
                foreach (var abilityTag in abilitiesData)
                {
                    string abilityId = abilityTag.GetString("AbilityId");
                    if (RebirthAbilities.ContainsKey(abilityId))
                    {
                        RebirthAbilities[abilityId].Load(abilityTag);
                    }
                }
            }
            if (!WasRetroRPGranted && RebirthCount > 0)
            {
                var config = ModContent.GetInstance<StatariaConfig>();
                if (config.rebirthSystem.EnableRebirthSystem)
                {
                    int calculatedTotalRPShouldHave = 0;
                    for (int i = 1; i <= RebirthCount; i++)
                    {
                        int levelRequirementForThisRebirth = config.rebirthSystem.RebirthLevelRequirement;
                        if (config.rebirthSystem.IncreaseLevelRequirement && (i - 1) > 0)
                        {
                            levelRequirementForThisRebirth += (i - 1) * config.rebirthSystem.AdditionalLevelRequirementPerRebirth;
                        }
                        int pointsForThisRebirth = (int)(levelRequirementForThisRebirth * config.rebirthSystem.RebirthPointsMultiplier);
                        calculatedTotalRPShouldHave += pointsForThisRebirth;
                    }

                    int currentRPSpenOnAbilities = 0;
                    foreach (var kvp in RebirthAbilities)
                    {
                        RebirthAbility abilityInstance = kvp.Value;
                        if (abilityInstance.IsUnlocked)
                        {
                            int costForThisAbility = abilityInstance.IsStackable ? abilityInstance.Cost * abilityInstance.Level : abilityInstance.Cost;
                            currentRPSpenOnAbilities += costForThisAbility;
                        }
                    }

                    int correctUnspentRP = Math.Max(0, calculatedTotalRPShouldHave - currentRPSpenOnAbilities);

                    int difference = correctUnspentRP - RebirthPoints;
                    if (difference != 0)
                    {
                        RebirthPoints = correctUnspentRP;
                        if (Main.netMode != NetmodeID.Server)
                        {
                            string sign = difference > 0 ? "+" : "";
                            Color textColor = difference > 0 ? Color.Gold : Color.OrangeRed;
                            CombatText.NewText(Player.Hitbox, textColor, $"{sign}{difference} RP (Sync)", true);
                        }
                    }

                    WasRetroRPGranted = true;
                }
            }
            CalculateGhostStats();
            AutoAllocateEnabled = tag.ContainsKey("AutoAllocateEnabled") ? tag.GetBool("AutoAllocateEnabled") : false;
            AutoAllocateStats = tag.ContainsKey("AutoAllocateStats")
                ? new HashSet<string>(tag.Get<List<string>>("AutoAllocateStats"))
                : new HashSet<string>();
        }

        private void RegisterDefaultAbilities()
        {
            var config = ModContent.GetInstance<StatariaConfig>();

            RebirthAbilities.Clear();

            RebirthAbilities["ReducedPotionSickness"] = new RebirthAbility(
            "Reduced Potion Sickness", "Reduces potion sickness duration by 50%", 20, false, 1);

            RebirthAbilities["ExtraAccessorySlot"] = new RebirthAbility(
                "Extra Accessory Slot", "Grants an additional accessory slot per level", 30, true,
                Math.Min(config.rebirthAbilities.MaxExtraAccessorySlots, 50));

            RebirthAbilities["LastStand"] = new RebirthAbility(
                "Last Stand", "When you would die, heal for 10% of your max health and gain 3 seconds of immunity. 3 minute cooldown.", 40, false, 1);

            RebirthAbilities["Dash"] = new RebirthAbility(
                "Dash", "Grants the ability to dash", 20, false, 1);

            RebirthAbilities["AutoJump"] = new RebirthAbility(
                "Auto-Jump", "Allows jumping automatically when holding jump button", 15, false, 1);

            RebirthAbilities["NoFallDamage"] = new RebirthAbility(
                "No Fall Damage", "Completely immune to fall damage", 25, false, 1);

            RebirthAbilities["WaterFreedom"] = new RebirthAbility(
                "Water Freedom", "Grants free movement in water, water breathing and flippers effect", 20, false, 1);

            RebirthAbilities["Teleport"] = new RebirthAbility(
                "Teleport", "Ability to teleport to cursor position using assigned key", 50, false, 1);

            RebirthAbilities["TreasureHunter"] = new RebirthAbility(
                "Treasure Hunter", "Grants Spelunker, Dangersense, and Hunter effects.", 30, false, 1
            ) { AbilityType = RebirthAbilityType.Toggleable };

            RebirthAbilities["Sustenance"] = new RebirthAbility(
                "Sustenance", "Toggleable food buff. Level 1: Well Fed, Level 2: Plenty Satisfied, Level 3: Exquisitely Stuffed.", 25, true, 3
            ) { AbilityType = RebirthAbilityType.Toggleable };

            RebirthAbilities["ArcheryMastery"] = new RebirthAbility(
                "Archery Mastery", "Grants Ammo Reservation and Archery buffs.", 15, false, 1
            ) { AbilityType = RebirthAbilityType.Toggleable };

            RebirthAbilities["BattleReady"] = new RebirthAbility(
                "Battle Ready", "Grants Battle buff.", 10, false, 1
            ) { AbilityType = RebirthAbilityType.Toggleable };

            RebirthAbilities["AnglerLuck"] = new RebirthAbility(
                "Angler's Luck", "Grants Crate, Fishing, and Sonar buffs.", 20, false, 1
            ) { AbilityType = RebirthAbilityType.Toggleable };

            RebirthAbilities["VitalityFortitude"] = new RebirthAbility(
                "Vitality & Fortitude", "Grants Lifeforce, Endurance, Ironskin, Regeneration, and Heartreach buffs.", 40, false, 1
            ) { AbilityType = RebirthAbilityType.Toggleable };

            RebirthAbilities["InnerCalm"] = new RebirthAbility(
                "Inner Calm", "Grants Calm buff.", 10, false, 1
            ) { AbilityType = RebirthAbilityType.Toggleable };

            RebirthAbilities["ElementalResistance"] = new RebirthAbility(
                "Elemental Resistance", "Grants Inferno and Warmth buffs.", 15, false, 1
            ) { AbilityType = RebirthAbilityType.Toggleable };

            RebirthAbilities["ShadowVeil"] = new RebirthAbility(
                "Shadow Veil", "Grants Invisibility buff.", 20, false, 1
            ) { AbilityType = RebirthAbilityType.Toggleable };

            RebirthAbilities["FortuneFavored"] = new RebirthAbility(
                "Fortune Favored", "Grants Lucky buff.", 25, false, 1
            ) { AbilityType = RebirthAbilityType.Toggleable };

            RebirthAbilities["ArcaneMastery"] = new RebirthAbility(
                "Arcane Mastery", "Grants Magic Power and Mana Regeneration buffs.", 20, false, 1
            ) { AbilityType = RebirthAbilityType.Toggleable };

            RebirthAbilities["MasterBuilder"] = new RebirthAbility(
                "Master Builder", "Grants Builder and Mining buffs.", 20, false, 1
            ) { AbilityType = RebirthAbilityType.Toggleable };

            RebirthAbilities["BattleFury"] = new RebirthAbility(
                "Battle Fury", "Grants Rage and Wrath buffs.", 25, false, 1
            ) { AbilityType = RebirthAbilityType.Toggleable };

            RebirthAbilities["SurfaceSkimmer"] = new RebirthAbility(
                "Surface Skimmer", "Grants Water Walking buff.", 15, false, 1
            ) { AbilityType = RebirthAbilityType.Toggleable };

            RebirthAbilities["ThornGuard"] = new RebirthAbility(
                "Thorn Guard", "Grants Thorns buff.", 15, false, 1
            ) { AbilityType = RebirthAbilityType.Toggleable };

            RebirthAbilities["FleetFooted"] = new RebirthAbility(
                "Fleet Footed", "Grants Swiftness buff.", 15, false, 1
            ) { AbilityType = RebirthAbilityType.Toggleable };

            RebirthAbilities["SummonerPact"] = new RebirthAbility(
                "Summoner's Pact", "Grants Summoning buff.", 20, false, 1
            ) { AbilityType = RebirthAbilityType.Toggleable };

            RebirthAbilities["LavaWalker"] = new RebirthAbility(
                "Lava Walker", "Grants Obsidian Skin buff.", 20, false, 1
            ) { AbilityType = RebirthAbilityType.Toggleable };

            RebirthAbilities["ZeroGravity"] = new RebirthAbility(
                "Zero Gravity", "Grants Gravitation buff.", 35, false, 1
            ) { AbilityType = RebirthAbilityType.Toggleable };

            RebirthAbilities["NightVision"] = new RebirthAbility(
                "Night Vision", "Grants Shine and Night Owl buffs.", 15, false, 1
            ) { AbilityType = RebirthAbilityType.Toggleable };

            RebirthAbilities["Sanctuary"] = new RebirthAbility(
                "Sanctuary", "Grants The Bast Defence, Star in a Bottle, Honey, Heart Lamp, Dryad's Blessing, and Cozy Fire buffs.", 50, false, 1
            ) { AbilityType = RebirthAbilityType.Toggleable };

            RebirthAbilities["CombatStations"] = new RebirthAbility(
                "Combat Stations", "Grants Ammo Box, Bewitched, Clairvoyance, Sharpened, Strategist (Tipsy), and Sugar Rush buffs.", 50, false, 1
            ) { AbilityType = RebirthAbilityType.Toggleable };

            RebirthAbilities["GiantsGrip"] = new RebirthAbility(
                "Giant's Grip", "Increases the size of melee weapons by 33%.", 25, false, 1
            );

            RebirthAbilities["GoldenTouch"] = new RebirthAbility(
                "Golden Touch", $"Increases the amount of coins picked up by {config.rebirthAbilities.GoldenTouchPercentPerLevel}% per level.", 15, true,
                config.rebirthAbilities.MaxGoldenTouchLevel);

            RebirthAbilities["EnhancedSpawns"] = new RebirthAbility(
                "Enhanced Spawns",
                $"Increases enemy spawn rate by {config.rebirthAbilities.SpawnRatePercentPerLevel}% per level. Stacks with other spawn rate modifiers.",
                35,
                true,
                config.rebirthAbilities.MaxEnhancedSpawnsLevel
                ) { AbilityType = RebirthAbilityType.Toggleable };

            RebirthAbilities["ShadowTrail"] = new RebirthAbility(
                "Shadow Trail", "Creates a trailing afterimage effect like Shadow Armor.", 5, false, 1
            ) { AbilityType = RebirthAbilityType.Toggleable };

            RebirthAbilities["AuraPulse"] = new RebirthAbility(
                "Aura Pulse", "Creates a pulsing outline aura effect like Hallowed Armor.", 5, false, 1
            ) { AbilityType = RebirthAbilityType.Toggleable };

            RebirthAbilities["AutoClicker"] = new RebirthAbility(
                "Auto-Clicker",
                "When toggled on, automatically clicks. Speed improves with levels. Effect interaction is set in mod config.",
                25,
                true,
                config.rebirthAbilities.AutoClickerMaxLevel
            ) { AbilityType = RebirthAbilityType.Toggleable };

            foreach (var kvp in RebirthAbilities)
            {
                if (kvp.Value.AbilityType == RebirthAbilityType.Toggleable && !kvp.Value.AbilityData.ContainsKey("Enabled"))
                {
                    kvp.Value.AbilityData["Enabled"] = false;
                }
            }
        }

        public override void ProcessTriggers(TriggersSet triggersSet)
        {
            if (StatariaKeybinds.ToggleStatUI.JustPressed
                && !Terraria.GameInput.PlayerInput.WritingText)
            {
                if (StatariaUI.StatUI.CurrentState == null)
                {
                    if (StatariaUI.SkillTreeUI.CurrentState != null)
                        StatariaUI.SkillTreeUI.SetState(null);

                    StatariaUI.StatUI.SetState(StatariaUI.Panel);
                }
                else
                {
                    StatariaUI.StatUI.SetState(null);
                }
            }
        }

        private void RecalculateXPToNext()
        {
            var config = ModContent.GetInstance<StatariaConfig>();
            XPToNext = (long)(100L * Math.Pow(Level, config.generalBalance.LevelScalingFactor));
        }

        public void PerformRebirth()
        {
            var config = ModContent.GetInstance<StatariaConfig>();

            if (!config.rebirthSystem.EnableRebirthSystem)
                return;

            int currentLevelRequirement = config.rebirthSystem.RebirthLevelRequirement;

            if (config.rebirthSystem.IncreaseLevelRequirement && RebirthCount > 0)
            {
                currentLevelRequirement += RebirthCount * config.rebirthSystem.AdditionalLevelRequirementPerRebirth;
            }

            if (Level < currentLevelRequirement)
                return;

            int pointsToAward = (int)(currentLevelRequirement * config.rebirthSystem.RebirthPointsMultiplier);

            if (config.rebirthSystem.BonusPointsForExcessLevels && Level > currentLevelRequirement)
            {
                int excessLevels = Level - currentLevelRequirement;
                pointsToAward += (int)(excessLevels * config.rebirthSystem.ExcessLevelPointMultiplier);
            }

            RebirthPoints += pointsToAward;

            RebirthCount++;

            Level = 1;
            XP = 0;
            XPToNext = (int)(100 * Math.Pow(Level, config.generalBalance.LevelScalingFactor));

            if (config.rebirthSystem.ResetStatsOnRebirth)
            {
                StatPoints = config.generalBalance.StatPointsPerLevel;
                VIT = STR = AGI = INT = LUC = END = POW = DEX = SPR = TCH = RGE = BRD = HLR = CLK = 0;
            }
            else
            {
                StatPoints += config.generalBalance.StatPointsPerLevel;
            }

            if (config.rebirthSystem.ResetBossRewardsOnRebirth)
            {
                rewardedBosses.Clear();
            }

            if (Main.netMode != NetmodeID.Server)
            {
                CombatText.NewText(Player.Hitbox, Color.Purple, $"Rebirth {RebirthCount}!");
                CombatText.NewText(Player.Hitbox, Color.Gold, $"+{pointsToAward} Rebirth Points!");
            }

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                SyncPlayer(-1, Player.whoAmI, false);
            }
        }

        public void RecalculateStatPoints()
        {
            var config = ModContent.GetInstance<StatariaConfig>();

            if (!config.generalBalance.EnableStatPointRecalculation)
                return;

            int shouldHaveStatPoints = (Level - 1) * config.generalBalance.StatPointsPerLevel;

            int spentPoints = VIT + STR + AGI + INT + LUC + END + POW + DEX + SPR + RGE + TCH + BRD + HLR + CLK;

            int totalPointsShould = shouldHaveStatPoints;

            int currentTotalPoints = spentPoints + StatPoints;

            if (totalPointsShould > currentTotalPoints)
            {
                int difference = totalPointsShould - currentTotalPoints;
                StatPoints += difference;

                if (Main.netMode != NetmodeID.Server)
                {
                    CombatText.NewText(Player.Hitbox, Color.Green, $"+{difference} Stat Points", true);
                }
            }
        }

        public void CalculateGhostStats()
        {
            var config = ModContent.GetInstance<StatariaConfig>();

            GhostStats.Clear();

            if (!config.rebirthSystem.EnableGhostStats || RebirthCount <= 0)
                return;

            int currentLevelRequirement = config.rebirthSystem.RebirthLevelRequirement;
            if (config.rebirthSystem.IncreaseLevelRequirement && RebirthCount > 0)
            {
                currentLevelRequirement += (RebirthCount - 1) * config.rebirthSystem.AdditionalLevelRequirementPerRebirth;
            }

            int ghostStatValue;
            if (config.rebirthSystem.UsePercentageGhostStats)
            {
                ghostStatValue = (int)(currentLevelRequirement * config.rebirthSystem.GhostStatsPercentage);
            }
            else
            {
                ghostStatValue = RebirthCount * config.rebirthSystem.GhostStatsFlatAmount;
            }

            foreach (string statName in config.rebirthSystem.GhostStatsAffectedStats)
            {
                string normalizedStat = statName.ToUpper();
                GhostStats[normalizedStat] = ghostStatValue;
            }
        }

        public int GetEffectiveStat(string statName)
        {
            var config = ModContent.GetInstance<StatariaConfig>();
            bool capsEnabled = config.statSettings.EnableStatCaps;

            int baseStat = 0;
            int cap = int.MaxValue;

            switch (statName)
            {
                case "VIT":
                    baseStat = VIT;
                    cap = config.statSettings.VIT_Cap;
                    break;
                case "STR":
                    baseStat = STR;
                    cap = config.statSettings.STR_Cap;
                    break;
                case "AGI":
                    baseStat = AGI;
                    cap = config.statSettings.AGI_Cap;
                    break;
                case "INT":
                    baseStat = INT;
                    cap = config.statSettings.INT_Cap;
                    break;
                case "LUC":
                    baseStat = LUC;
                    cap = config.statSettings.LUC_Cap;
                    break;
                case "END":
                    baseStat = END;
                    cap = config.statSettings.END_Cap;
                    break;
                case "POW":
                    baseStat = POW;
                    cap = config.statSettings.POW_Cap;
                    break;
                case "DEX":
                    baseStat = DEX;
                    cap = config.statSettings.DEX_Cap;
                    break;
                case "SPR":
                    baseStat = SPR;
                    cap = config.statSettings.SPR_Cap;
                    break;
                case "TCH":
                    baseStat = TCH;
                    cap = config.statSettings.TCH_Cap;
                    break;
                case "RGE":
                    baseStat = RGE;
                    cap = config.statSettings.RGE_Cap;
                    break;
                case "BRD":
                    baseStat = BRD;
                    cap = config.statSettings.BRD_Cap;
                    break;
                case "HLR":
                    baseStat = HLR;
                    cap = config.statSettings.HLR_Cap;
                    break;
                case "CLK":
                    baseStat = CLK;
                    cap = config.statSettings.CLK_Cap;
                    break;
            }

            int totalStat = baseStat;
            if (GhostStats.TryGetValue(statName, out int ghostBonus))
            {
                totalStat += ghostBonus;
            }

            if (capsEnabled)
            {
                totalStat = Math.Min(totalStat, cap);
            }

            return totalStat;
        }

        public void RecalculateRebirthPoints()
        {
            var config = ModContent.GetInstance<StatariaConfig>();
            if (!config.rebirthSystem.EnableRebirthSystem || RebirthCount <= 0 || !config.rebirthSystem.EnableRebirthPointRecalculation)
                return;

            int calculatedTotalRP = 0;
            for (int i = 1; i <= RebirthCount; i++)
            {
                int levelRequirementForThisRebirth = config.rebirthSystem.RebirthLevelRequirement;
                if (config.rebirthSystem.IncreaseLevelRequirement && (i - 1) > 0)
                {
                    levelRequirementForThisRebirth += (i - 1) * config.rebirthSystem.AdditionalLevelRequirementPerRebirth;
                }
                int pointsForThisRebirth = (int)(levelRequirementForThisRebirth * config.rebirthSystem.RebirthPointsMultiplier);
                calculatedTotalRP += pointsForThisRebirth;
            }

            int currentRPSpentOnAbilities = 0;
            foreach (var ability in RebirthAbilities.Values)
            {
                if (ability.IsUnlocked)
                {
                    int costForThisAbility = ability.IsStackable ? ability.Cost * ability.Level : ability.Cost;
                    currentRPSpentOnAbilities += costForThisAbility;
                }
            }

            int correctUnspentRP = Math.Max(0, calculatedTotalRP - currentRPSpentOnAbilities);

            if (correctUnspentRP > RebirthPoints)
            {
                int difference = correctUnspentRP - RebirthPoints;
                RebirthPoints = correctUnspentRP;

                if (Main.netMode != NetmodeID.Server)
                {
                    CombatText.NewText(Player.Hitbox, Color.Gold, $"+{difference} RP", true);
                }
            }
        }

        private int GetEffectiveLevelCap()
        {
            var config = ModContent.GetInstance<StatariaConfig>();
            int cap = int.MaxValue;

            if (config.rebirthSystem.EnableDynamicRebirthLevelCap)
            {
                int nextRebirthRequirement = config.rebirthSystem.RebirthLevelRequirement +
                                           (RebirthCount * config.rebirthSystem.AdditionalLevelRequirementPerRebirth);
                cap = (int)(nextRebirthRequirement * config.rebirthSystem.DynamicRebirthLevelCapMultiplier);
            }
            else if (config.generalBalance.EnableLevelCap)
            {
                cap = config.generalBalance.LevelCapValue;
            }
            return cap;
        }

        public void GainXP(long amount, string source = "Unknown")
        {
            var config = ModContent.GetInstance<StatariaConfig>();
            int effectiveLevelCap = GetEffectiveLevelCap();

            if (Level >= effectiveLevelCap)
            {
                XP = XPToNext;

                if (Main.netMode != NetmodeID.Server && levelCapMessageTimer <= 0)
                {
                    CombatText.NewText(Player.Hitbox, Color.Gray, "Level Cap Reached!");
                    levelCapMessageTimer = levelCapMessageCooldown;
                }
                return;
            }

            if (config.rebirthSystem.EnableRebirthSystem && RebirthCount > 0)
            {
                float bonus = 1f + (RebirthCount * config.rebirthSystem.RebirthXPMultiplier);
                amount = (long)(amount * bonus);
            }

            if (config.xpVerification.EnableXPVerification && xpVerifier.IsSuspiciousXPGain(amount, source))
            {
                xpVerifier.QueueXPForVerification(amount, source);
                return;
            }

            ApplyXPDirectly(amount, source);
        }

        public void ApplyXPDirectly(long amount, string source)
        {
            var config = ModContent.GetInstance<StatariaConfig>();
            int effectiveLevelCap = GetEffectiveLevelCap();

            XP += amount;

            if (Main.netMode != NetmodeID.Server)
            {
                xpBarTimer = xpBarDuration;

                bool showPopup = config.uiSettings.ShowXPGainPopups;

                if (source.Contains("Melee") || source.Contains("Proj") || source.Contains("Damage"))
                {
                    showPopup = showPopup && config.uiSettings.ShowDamageXPPopups;

                    if (config.generalBalance.DamageXP <= 0)
                        showPopup = false;
                }
                else if (source.Contains("Kill"))
                {
                    showPopup = showPopup && config.uiSettings.ShowKillXPPopups;

                    if (config.generalBalance.KillXP <= 0)
                        showPopup = false;
                }
                else if (source.Contains("Boss"))
                {
                    showPopup = showPopup && config.uiSettings.ShowBossXPPopups;

                    if ((config.generalBalance.UseFlatBossXP && config.generalBalance.DefaultFlatBossXP <= 0) ||
                        (!config.generalBalance.UseFlatBossXP && config.generalBalance.BossXP <= 0))
                        showPopup = false;
                }

                if (showPopup && amount > 0)
                {
                    CombatText.NewText(Player.Hitbox, Color.Gold, $"+{amount:N0} XP");

                    if (StatariaLogger.GlobalDebugMode)
                    {
                        Vector2 position = Player.Hitbox.TopLeft();
                        position.Y -= 20;
                        CombatText.NewText(new Rectangle((int)position.X, (int)position.Y, Player.Hitbox.Width, 20),
                            Color.Cyan, $"From: {source}");
                    }
                }
            }

            while (XP >= XPToNext)
            {
                effectiveLevelCap = GetEffectiveLevelCap();
                if (Level >= effectiveLevelCap)
                {
                    XP = XPToNext;
                    if (Main.netMode != NetmodeID.Server && levelCapMessageTimer <= 0)
                    {
                        CombatText.NewText(Player.Hitbox, Color.Gray, "Level Cap Reached!");
                        levelCapMessageTimer = levelCapMessageCooldown;
                    }
                    break;
                }

                XP -= XPToNext;
                LevelUp();
            }

            if (Main.netMode == NetmodeID.Server)
            {
                SyncPlayer(-1, Player.whoAmI, false);
            }
        }

        private void LevelUp()
        {
            var config = ModContent.GetInstance<StatariaConfig>();
            int effectiveLevelCap = GetEffectiveLevelCap();
            if (Level >= effectiveLevelCap)
            {
                XP = Math.Min(XP, XPToNext);
                return;
            }

            Level++;
            StatPoints += config.generalBalance.StatPointsPerLevel;
            XPToNext = (long)(100L * Math.Pow(Level, config.generalBalance.LevelScalingFactor));

            if (Main.netMode != NetmodeID.Server)
            {
                CombatText.NewText(Player.Hitbox, Color.LightGreen, $"Level Up! Level {Level}");
            }
        }

        public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (target.friendly || target.lifeMax <= 5)
                return;

            var config = ModContent.GetInstance<StatariaConfig>();

            if (config.advanced.BlacklistedNPCs.Any(entry =>
                entry.Equals(Lang.GetNPCNameValue(target.type), StringComparison.OrdinalIgnoreCase) ||
                entry.Equals(target.TypeName, StringComparison.OrdinalIgnoreCase) ||
                (int.TryParse(entry, out int id) && id == target.type)))
            {
                return;
            }

            GainXP((long)(damageDone * config.generalBalance.DamageXP), "Melee");
        }

        public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (target.friendly || target.lifeMax <= 5 || proj.owner != Player.whoAmI)
                return;

            var config = ModContent.GetInstance<StatariaConfig>();

            if (config.advanced.BlacklistedNPCs.Any(entry =>
                entry.Equals(Lang.GetNPCNameValue(target.type), StringComparison.OrdinalIgnoreCase) ||
                entry.Equals(target.TypeName, StringComparison.OrdinalIgnoreCase) ||
                (int.TryParse(entry, out int id) && id == target.type)))
            {
                return;
            }

            GainXP((long)(damageDone * config.generalBalance.DamageXP), "Projectile");
        }

        public override void ModifyLuck(ref float luck)
        {
            var config = ModContent.GetInstance<StatariaConfig>();

            int effectiveLUC = LUC;
            if (GhostStats.TryGetValue("LUC", out int lucBonus))
                effectiveLUC += lucBonus;

            if (config.statSettings.LUC_EnableLuckBonus)
                luck += effectiveLUC * config.statSettings.LUC_LuckBonus;

            luck = Math.Clamp(luck, -0.7f, 1f);
        }

        public override void ResetEffects()
        {
            var config = ModContent.GetInstance<StatariaConfig>();

            int effectiveVIT = VIT;
            if (GhostStats.TryGetValue("VIT", out int vitBonus))
                effectiveVIT += vitBonus;

            Player.statLifeMax2 += effectiveVIT * config.statSettings.VIT_HP;

            int effectiveSTR = STR;
            if (GhostStats.TryGetValue("STR", out int strBonus))
                effectiveSTR += strBonus;

            Player.GetArmorPenetration(DamageClass.Melee) += effectiveSTR * config.statSettings.STR_ArmorPen;

            int effectiveINT = INT;
            if (GhostStats.TryGetValue("INT", out int intBonus))
                effectiveINT += intBonus;

            Player.statManaMax2 += effectiveINT * config.statSettings.INT_MP;
            float rawReduction = effectiveINT * config.statSettings.INT_ManaCostReduction / 100f;
            float diminishingReduction = 1f - (1f / (1f + rawReduction));
            Player.manaCost -= diminishingReduction;
            Player.GetArmorPenetration(DamageClass.Magic) += effectiveINT * config.statSettings.INT_ArmorPen;

            int effectiveEND = END;
            if (GhostStats.TryGetValue("END", out int endBonus))
                effectiveEND += endBonus;

            if (config.statSettings.END_DefensePerX > 0)
            {
                Player.statDefense += effectiveEND / config.statSettings.END_DefensePerX;
            }
            Player.aggro += effectiveEND * config.statSettings.END_Aggro;

            int effectiveAGI = AGI;
            if (GhostStats.TryGetValue("AGI", out int agiBonus))
                effectiveAGI += agiBonus;

            float diminishedAGI = effectiveAGI <= 50 ? effectiveAGI : 50 + (effectiveAGI - 50) * 0.5f;
            Player.moveSpeed += diminishedAGI * (config.statSettings.AGI_MoveSpeed / 100f);
            Player.GetAttackSpeed(DamageClass.Generic) += diminishedAGI * (config.statSettings.AGI_AttackSpeed / 100f);
            float jumpHeightMultiplier = 1f - (float)Math.Pow(0.98, effectiveAGI);
            Player.jumpHeight += (int)(15 * jumpHeightMultiplier * config.statSettings.AGI_JumpHeight);
            Player.jumpSpeedBoost += effectiveAGI * config.statSettings.AGI_JumpSpeed;

            int effectiveLUC = LUC;
            if (GhostStats.TryGetValue("LUC", out int lucBonus))
                effectiveLUC += lucBonus;

            if (config.statSettings.LUC_EnableFishing)
                Player.fishingSkill += effectiveLUC * config.statSettings.LUC_Fishing;

            Player.aggro -= effectiveLUC * config.statSettings.LUC_AggroReduction;

            int effectiveSPR = SPR;
            if (GhostStats.TryGetValue("SPR", out int sprBonus))
                effectiveSPR += sprBonus;

            Player.maxMinions += effectiveSPR / config.statSettings.SPR_MinionsPerX;
            Player.maxTurrets += effectiveSPR / config.statSettings.SPR_SentriesPerX;
            Player.GetDamage(DamageClass.Summon) += effectiveSPR * (config.statSettings.SPR_Damage / 100f);

            int effectiveTCH = TCH;
            if (GhostStats.TryGetValue("TCH", out int tchBonus))
                effectiveTCH += tchBonus;

            if (config.statSettings.TCH_EnableMiningSpeed)
                Player.pickSpeed -= effectiveTCH * config.statSettings.TCH_MiningSpeed * 0.01f;

            if (config.statSettings.TCH_EnableBuildSpeed)
                Player.tileSpeed += effectiveTCH * config.statSettings.TCH_BuildSpeed;

            if (config.statSettings.TCH_EnableRange)
            {
                Player.tileRangeX += effectiveTCH * config.statSettings.TCH_Range;
                Player.tileRangeY += effectiveTCH * config.statSettings.TCH_Range;
            }

            if (config.modIntegration.EnableCalamityIntegration && CalamitySupportHelper.CalamityLoaded)
            {
                int effectiveRGE = RGE;
                if (GhostStats.TryGetValue("RGE", out int rgeBonus))
                    effectiveRGE += rgeBonus;

                if (effectiveRGE >= 1)
                {
                    CalamitySupportHelper.SetFieldValue(Player, "wearingRogueArmor", true);
                }

                ApplyRogueStatEffects();

                if (effectiveRGE > 0 && config.modIntegration.RGE_MaxStealthPerPoint > 0)
                {
                    float stealthBonus = effectiveRGE * (config.modIntegration.RGE_MaxStealthPerPoint / 100f);
                    CalamitySupportHelper.CallAddMaxStealth(Player, stealthBonus);
                }
            }


            if (config.modIntegration.EnableCalamityIntegration && CalamitySupportHelper.CalamityLoaded)
            {
                ApplyPowerCalamityEffects();
            }

            if (config.modIntegration.EnableThoriumIntegration && ThoriumSupportHelper.ThoriumLoaded)
            {
                ApplyBardStatEffects();

                ApplyHealerStatEffects();
            }

            int effectiveDEX = DEX;
            if (GhostStats.TryGetValue("DEX", out int dexBonus))
                effectiveDEX += dexBonus;

            Player.GetArmorPenetration(DamageClass.Ranged) += effectiveDEX * config.statSettings.DEX_ArmorPen;

            if (config.modIntegration.EnableThoriumIntegration && ThoriumSupportHelper.ThoriumLoaded)
            {
                ApplyThoriumArmorPenetration();
            }

            if (config.modIntegration.EnableCalamityIntegration && CalamitySupportHelper.CalamityLoaded)
            {
                ApplyRogueStatEffects();
            }

            if (config.modIntegration.EnableClickerClassIntegration && ClickerSupportHelper.ClickerClassLoaded)
            {
                int effectiveCLK = CLK;
                if (GhostStats.TryGetValue("CLK", out int clkBonus))
                    effectiveCLK += clkBonus;

                if (effectiveCLK > 0)
                {
                    float radiusBonus = effectiveCLK * config.modIntegration.CLK_Radius / 100f;
                    ClickerSupportHelper.AddClickerRadius(Player, radiusBonus);

                    float perPointFactor = config.modIntegration.CLK_EffectThreshold / 100f;
                    float linearPotentialReduction = effectiveCLK * perPointFactor;
                    float effectiveReductionFactor = 0f;
                    if (linearPotentialReduction > 0)
                    {
                        effectiveReductionFactor = 1f - (1f / (1f + linearPotentialReduction));
                    }
                    ClickerSupportHelper.ReduceClickEffectThresholdPercent(Player, -effectiveReductionFactor);
                }
            }

            ApplyAbilityEffects1();
        }

        private void ApplyRogueStatEffects()
        {
            if (!CalamitySupportHelper.CalamityLoaded)
                return;

            var config = ModContent.GetInstance<StatariaConfig>();

            int effectiveRGE = RGE;
            if (GhostStats.TryGetValue("RGE", out int rgeBonus))
                effectiveRGE += rgeBonus;

            if (config.modIntegration.RGE_EnableStealthConsumptionReduction)
            {
                if (effectiveRGE >= config.modIntegration.RGE_StealthConsumptionReductionThreshold)
                    CalamitySupportHelper.SetFieldValue(Player, "stealthStrikeHalfCost", true);
                else if (effectiveRGE >= config.modIntegration.RGE_StealthConsumption75Threshold)
                    CalamitySupportHelper.SetFieldValue(Player, "stealthStrike75Cost", true);
                else if (effectiveRGE >= config.modIntegration.RGE_StealthConsumption85Threshold)
                    CalamitySupportHelper.SetFieldValue(Player, "stealthStrike85Cost", true);
            }

            float rogueVelocity = CalamitySupportHelper.GetRogueVelocity(Player);
            rogueVelocity += effectiveRGE * (config.modIntegration.RGE_Velocity / 100f);
            CalamitySupportHelper.SetRogueVelocity(Player, rogueVelocity);

            float rogueAmmoCost = CalamitySupportHelper.GetRogueAmmoCost(Player);
            rogueAmmoCost -= effectiveRGE * (config.modIntegration.RGE_AmmoConsumptionReduction / 100f);
            CalamitySupportHelper.SetRogueAmmoCost(Player, rogueAmmoCost);
        }

        private void ApplyPowerCalamityEffects()
        {
            if (!CalamitySupportHelper.CalamityLoaded)
                return;

            var config = ModContent.GetInstance<StatariaConfig>();

            int effectivePOW = POW;
            if (GhostStats.TryGetValue("POW", out int powBonus))
                effectivePOW += powBonus;

            float rage = CalamitySupportHelper.GetRage(Player);
            float rageMax = CalamitySupportHelper.GetRageMax(Player);

            if (rageMax > 0)
            {
                if (CalamitySupportHelper.InfiniteRageEnabled)
                {
                    CalamitySupportHelper.SetRage(Player, rageMax);
                }

                float rageDamageBoost = CalamitySupportHelper.GetRageDamageBoost(Player);
                rageDamageBoost += effectivePOW * (config.modIntegration.POW_RageDamage / 100f);
                CalamitySupportHelper.SetRageDamageBoost(Player, rageDamageBoost);

                int rageDuration = CalamitySupportHelper.GetRageDuration(Player);
                int powRageDurationBonus = Math.Min(effectivePOW * config.modIntegration.POW_RageDuration, config.modIntegration.POW_MaxRageDurationBonus);
                rageDuration += powRageDurationBonus;
                CalamitySupportHelper.SetRageDuration(Player, rageDuration);
            }

            float adrenaline = CalamitySupportHelper.GetAdrenaline(Player);
            float adrenalineMax = CalamitySupportHelper.GetAdrenalineMax(Player);

            if (adrenalineMax > 0)
            {
                if (CalamitySupportHelper.InfiniteAdrenalineEnabled)
                {
                    CalamitySupportHelper.SetAdrenaline(Player, adrenalineMax);
                }

                int adrenalineDuration = CalamitySupportHelper.GetAdrenalineDuration(Player);
                adrenalineDuration += effectivePOW * config.modIntegration.POW_AdrenalineDuration;
                CalamitySupportHelper.SetAdrenalineDuration(Player, adrenalineDuration);
            }
        }

        public void ApplyBardStatEffects()
        {
            if (!ThoriumSupportHelper.ThoriumLoaded)
                return;

            var config = ModContent.GetInstance<StatariaConfig>();

            int effectiveBRD = BRD;
            if (GhostStats.TryGetValue("BRD", out int brdBonus))
                effectiveBRD += brdBonus;

            if (effectiveBRD > 0 && config.modIntegration.BRD_PointsPerMaxInspiration > 0)
            {
                int bonusInspiration = effectiveBRD / config.modIntegration.BRD_PointsPerMaxInspiration;

                if (bonusInspiration > 0)
                {
                    ThoriumSupportHelper.CallAddBardInspirationMax(Player, bonusInspiration);
                }
            }

            if (effectiveBRD > 0 && config.modIntegration.BRD_EnableEmpowermentBoost)
            {
                if (config.modIntegration.BRD_EmpowermentDuration > 0)
                {
                    float totalSecondsToAdd = effectiveBRD * config.modIntegration.BRD_EmpowermentDuration;
                    short ticksToAdd = (short)(totalSecondsToAdd * 60f);
                    if (ticksToAdd > 0)
                    {
                        ThoriumSupportHelper.CallBonusBardEmpowermentDuration(Player, ticksToAdd);
                    }
                }
            }
        }

        public void ApplyHealerStatEffects()
        {
            if (!ThoriumSupportHelper.ThoriumLoaded)
                return;

            var config = ModContent.GetInstance<StatariaConfig>();

            int effectiveHLR = HLR;
            if (GhostStats.TryGetValue("HLR", out int hlrBonus))
                effectiveHLR += hlrBonus;

            if (effectiveHLR > 0)
            {
                int effectiveHLRPoints = effectiveHLR / config.modIntegration.HLR_PointsPerEffectPoint;

                if (effectiveHLRPoints > 0 && config.modIntegration.HLR_HealingPower > 0)
                {
                    int bonusToHealPower = effectiveHLRPoints * config.modIntegration.HLR_HealingPower;
                    ThoriumSupportHelper.CallBonusHealerHealBonus(Player, bonusToHealPower);

                    int bonusToLifeRecoveryAmount = (effectiveHLRPoints * config.modIntegration.HLR_HealingPower) / 2;
                    ThoriumSupportHelper.CallBonusLifeRecovery(Player, bonusToLifeRecoveryAmount);
                }

                int intervalReduction = Math.Min(effectiveHLRPoints / 2, 30);
                if (intervalReduction > 0)
                {
                    ThoriumSupportHelper.CallBonusLifeRecoveryIntervalReduction(Player, intervalReduction);
                }
            }
        }

        private void ApplyThoriumArmorPenetration()
        {
            var config = ModContent.GetInstance<StatariaConfig>();
            Mod thoriumMod = ModLoader.GetMod("ThoriumMod");

            if (thoriumMod == null)
                return;

            int effectiveBRD = BRD;
            if (GhostStats.TryGetValue("BRD", out int brdBonus))
                effectiveBRD += brdBonus;

            int effectiveHLR = HLR;
            if (GhostStats.TryGetValue("HLR", out int hlrBonus))
                effectiveHLR += hlrBonus;

            if (effectiveBRD > 0 && thoriumMod.TryFind("BardDamage", out DamageClass bardDamageClass))
            {
                Player.GetArmorPenetration(bardDamageClass) += effectiveBRD * config.modIntegration.BRD_ArmorPen;
            }

            if (effectiveHLR > 0)
            {
                if (thoriumMod.TryFind("HealerDamage", out DamageClass healerDamageClass))
                {
                    Player.GetArmorPenetration(healerDamageClass) += effectiveHLR * config.modIntegration.HLR_ArmorPen;
                }
            }
        }

        public void AutoAllocatePoints()
        {
            if (!AutoAllocateEnabled || AutoAllocateStats.Count == 0 || StatPoints <= 0)
                return;

            var config = ModContent.GetInstance<StatariaConfig>();
            int statsCount = AutoAllocateStats.Count;

            if (StatPoints < statsCount)
                return;

            int completeSetCount = StatPoints / statsCount;

            int totalPointsToAllocate = 0;

            foreach (string statName in AutoAllocateStats)
            {
                int pointsToAdd = completeSetCount;
                bool isAtCap = false;

                if (config.statSettings.EnableStatCaps)
                {
                    switch (statName)
                    {
                        case "VIT":
                            isAtCap = VIT >= config.statSettings.VIT_Cap;
                            pointsToAdd = Math.Min(pointsToAdd, config.statSettings.VIT_Cap - VIT);
                            break;
                        case "STR":
                            isAtCap = STR >= config.statSettings.STR_Cap;
                            pointsToAdd = Math.Min(pointsToAdd, config.statSettings.STR_Cap - STR);
                            break;
                        case "AGI":
                            isAtCap = AGI >= config.statSettings.AGI_Cap;
                            pointsToAdd = Math.Min(pointsToAdd, config.statSettings.AGI_Cap - AGI);
                            break;
                        case "INT":
                            isAtCap = INT >= config.statSettings.INT_Cap;
                            pointsToAdd = Math.Min(pointsToAdd, config.statSettings.INT_Cap - INT);
                            break;
                        case "LUC":
                            isAtCap = LUC >= config.statSettings.LUC_Cap;
                            pointsToAdd = Math.Min(pointsToAdd, config.statSettings.LUC_Cap - LUC);
                            break;
                        case "END":
                            isAtCap = END >= config.statSettings.END_Cap;
                            pointsToAdd = Math.Min(pointsToAdd, config.statSettings.END_Cap - END);
                            break;
                        case "POW":
                            isAtCap = POW >= config.statSettings.POW_Cap;
                            pointsToAdd = Math.Min(pointsToAdd, config.statSettings.POW_Cap - POW);
                            break;
                        case "DEX":
                            isAtCap = DEX >= config.statSettings.DEX_Cap;
                            pointsToAdd = Math.Min(pointsToAdd, config.statSettings.DEX_Cap - DEX);
                            break;
                        case "SPR":
                            isAtCap = SPR >= config.statSettings.SPR_Cap;
                            pointsToAdd = Math.Min(pointsToAdd, config.statSettings.SPR_Cap - SPR);
                            break;
                        case "TCH":
                            isAtCap = TCH >= config.statSettings.TCH_Cap;
                            pointsToAdd = Math.Min(pointsToAdd, config.statSettings.TCH_Cap - TCH);
                            break;
                        case "RGE":
                            isAtCap = RGE >= config.statSettings.RGE_Cap;
                            pointsToAdd = Math.Min(pointsToAdd, config.statSettings.RGE_Cap - RGE);
                            break;
                        case "BRD":
                            isAtCap = BRD >= config.statSettings.BRD_Cap;
                            pointsToAdd = Math.Min(pointsToAdd, config.statSettings.BRD_Cap - BRD);
                            break;
                        case "HLR":
                            isAtCap = HLR >= config.statSettings.HLR_Cap;
                            pointsToAdd = Math.Min(pointsToAdd, config.statSettings.HLR_Cap - HLR);
                            break;
                        case "CLK":
                            isAtCap = CLK >= config.statSettings.CLK_Cap;
                            pointsToAdd = Math.Min(pointsToAdd, config.statSettings.CLK_Cap - CLK);
                            break;
                    }
                }

                if (isAtCap || pointsToAdd <= 0)
                    continue;

                switch (statName)
                {
                    case "VIT": VIT += pointsToAdd; break;
                    case "STR": STR += pointsToAdd; break;
                    case "AGI": AGI += pointsToAdd; break;
                    case "INT": INT += pointsToAdd; break;
                    case "LUC": LUC += pointsToAdd; break;
                    case "END": END += pointsToAdd; break;
                    case "POW": POW += pointsToAdd; break;
                    case "DEX": DEX += pointsToAdd; break;
                    case "SPR": SPR += pointsToAdd; break;
                    case "TCH": TCH += pointsToAdd; break;
                    case "RGE": RGE += pointsToAdd; break;
                    case "BRD": BRD += pointsToAdd; break;
                    case "HLR": HLR += pointsToAdd; break;
                    case "CLK": CLK += pointsToAdd; break;
                }

                totalPointsToAllocate += pointsToAdd;
            }

            StatPoints -= totalPointsToAllocate;
        }

        public override void PostUpdateEquips()
        {
            var config = ModContent.GetInstance<StatariaConfig>();
            Player.wingTimeMax += AGI * config.statSettings.AGI_WingTime;
        }

        public override void PostUpdate()
        {
            var config = ModContent.GetInstance<StatariaConfig>();

            if (xpBarTimer > 0)
                xpBarTimer--;

            if (levelCapMessageTimer > 0)
                levelCapMessageTimer--;

            if (lastStandCooldownTimer > 0)
                lastStandCooldownTimer--;

            if (lastStandImmunityTimer > 0)
            {
                lastStandImmunityTimer--;
                Player.immune = true;
                Player.immuneTime = 2;
            }

            RecalculateXPToNext();

            CalculateGhostStats();

            int effectiveVIT = VIT;
            if (GhostStats.TryGetValue("VIT", out int vitBonus))
                effectiveVIT += vitBonus;

            if (config.statSettings.UseCustomHpRegen)
            {
                if (customRegenDelayTimer > 0)
                    customRegenDelayTimer--;
                else if (Player.statLife < Player.statLifeMax2 && !Player.dead)
                {
                    float hpPerSecond = effectiveVIT * config.statSettings.CustomHpRegenPerVIT;
                    regenCarryover += hpPerSecond / 60f;

                    if (regenCarryover >= 1f)
                    {
                        int healAmount = (int)regenCarryover;
                        regenCarryover -= healAmount;
                        Player.statLife += healAmount;
                        if (Player.statLife > Player.statLifeMax2)
                            Player.statLife = Player.statLifeMax2;

                        if (Main.netMode != NetmodeID.SinglePlayer)
                            Player.HealEffect(healAmount, true);
                    }
                }
            }
            else
            {
                Player.lifeRegen += effectiveVIT / 2;
            }

            int effectiveINT = INT;
            if (GhostStats.TryGetValue("INT", out int intBonus))
                effectiveINT += intBonus;

            Player.manaRegenBonus += effectiveINT / 2;

            if (teleportCooldownTimer > 0)
                teleportCooldownTimer--;

            if (AutoAllocateEnabled && AutoAllocateStats.Count > 0 && StatPoints > 0)
            {
                AutoAllocatePoints();
            }

            if (RebirthAbilities.TryGetValue("Teleport", out RebirthAbility teleport) &&
                teleport.IsUnlocked &&
                StatariaKeybinds.TeleportKey.JustPressed &&
                teleportCooldownTimer <= 0)
            {
                Vector2 mouseWorld = Main.MouseWorld;

                Point tileCoordinates = mouseWorld.ToTileCoordinates();

                if (tileCoordinates.X >= 0 && tileCoordinates.X < Main.maxTilesX &&
                    tileCoordinates.Y >= 0 && tileCoordinates.Y < Main.maxTilesY)
                {
                    Vector2 checkPositionTopLeft = mouseWorld - new Vector2(Player.width / 2f, Player.height);

                    if (!Collision.SolidCollision(checkPositionTopLeft, Player.width, Player.height))
                    {
                        Player.Teleport(mouseWorld, 2);
                        teleportCooldownTimer = config.rebirthAbilities.TeleportCooldown * 60;

                        for (int i = 0; i < 30; i++)
                        {
                            Dust.NewDust(Player.position, Player.width, Player.height, DustID.MagicMirror);
                        }
                        SoundEngine.PlaySound(SoundID.Item6, Player.position);
                    }
                }
            }

            ApplyAbilityEffects2();
        }

        private void ApplyAbilityEffects1()
        {
            var config = ModContent.GetInstance<StatariaConfig>();

            if (RebirthAbilities.TryGetValue("Dash", out RebirthAbility dash) && dash.IsUnlocked) Player.dashType = 1;
            if (RebirthAbilities.TryGetValue("AutoJump", out RebirthAbility autoJump) && autoJump.IsUnlocked) Player.autoJump = true;
            if (RebirthAbilities.TryGetValue("NoFallDamage", out RebirthAbility noFall) && noFall.IsUnlocked) Player.noFallDmg = true;
            if (RebirthAbilities.TryGetValue("WaterFreedom", out RebirthAbility water) && water.IsUnlocked) { Player.accFlipper = true; Player.ignoreWater = true; Player.gills = true; }

            foreach (var kvp in RebirthAbilities)
            {
                RebirthAbility ability = kvp.Value;
                string abilityId = kvp.Key;

                if (ability.AbilityType == RebirthAbilityType.Toggleable &&
                    ability.IsUnlocked &&
                    ability.AbilityData.TryGetValue("Enabled", out object isEnabledObj) &&
                    isEnabledObj is bool isEnabled && isEnabled)
                {
                    switch (abilityId)
                    {
                        case "Sustenance":
                            int buffToApply = -1;
                            switch (ability.Level)
                            {
                                case 1:
                                    buffToApply = BuffID.WellFed;
                                    break;
                                case 2:
                                    buffToApply = BuffID.WellFed2;
                                    break;
                                case 3:
                                    buffToApply = BuffID.WellFed3;
                                    break;
                            }
                            if (buffToApply != -1)
                            {
                                Player.AddBuff(buffToApply, 2);
                            }
                            break;
                        case "TreasureHunter":
                            Player.AddBuff(BuffID.Spelunker, 2);
                            Player.AddBuff(BuffID.Dangersense, 2);
                            Player.AddBuff(BuffID.Hunter, 2);
                            break;
                        case "ArcheryMastery":
                            Player.AddBuff(BuffID.AmmoReservation, 2);
                            Player.AddBuff(BuffID.Archery, 2);
                            break;
                        case "BattleReady":
                            Player.AddBuff(BuffID.Battle, 2);
                            break;
                        case "AnglerLuck":
                            Player.AddBuff(BuffID.Crate, 2);
                            Player.AddBuff(BuffID.Fishing, 2);
                            Player.AddBuff(BuffID.Sonar, 2);
                            break;
                        case "VitalityFortitude":
                            Player.AddBuff(BuffID.Lifeforce, 2);
                            Player.AddBuff(BuffID.Endurance, 2);
                            Player.AddBuff(BuffID.Ironskin, 2);
                            Player.AddBuff(BuffID.Regeneration, 2);
                            Player.AddBuff(BuffID.Heartreach, 2);
                            break;
                        case "InnerCalm":
                            Player.AddBuff(BuffID.Calm, 2);
                            break;
                        case "ElementalResistance":
                            Player.AddBuff(BuffID.Inferno, 2);
                            Player.AddBuff(BuffID.Warmth, 2);
                            break;
                        case "ShadowVeil":
                            Player.AddBuff(BuffID.Invisibility, 2);
                            break;
                        case "FortuneFavored":
                            Player.AddBuff(BuffID.Lucky, 2);
                            break;
                        case "ArcaneMastery":
                            Player.AddBuff(BuffID.MagicPower, 2);
                            Player.AddBuff(BuffID.ManaRegeneration, 2);
                            break;
                        case "MasterBuilder":
                            Player.AddBuff(BuffID.Builder, 2);
                            Player.AddBuff(BuffID.Mining, 2);
                            break;
                        case "BattleFury":
                            Player.AddBuff(BuffID.Rage, 2);
                            Player.AddBuff(BuffID.Wrath, 2);
                            break;
                        case "SurfaceSkimmer":
                            Player.AddBuff(BuffID.WaterWalking, 2);
                            break;
                        case "ThornGuard":
                            Player.AddBuff(BuffID.Thorns, 2);
                            break;
                        case "FleetFooted":
                            Player.AddBuff(BuffID.Swiftness, 2);
                            break;
                        case "SummonerPact":
                            Player.AddBuff(BuffID.Summoning, 2);
                            break;
                        case "LavaWalker":
                            Player.AddBuff(BuffID.ObsidianSkin, 2);
                            break;
                        case "ZeroGravity":
                            Player.AddBuff(BuffID.Gravitation, 2);
                            break;
                        case "NightVision":
                            Player.AddBuff(BuffID.Shine, 2);
                            Player.AddBuff(BuffID.NightOwl, 2);
                            break;
                        case "Sanctuary":
                            Player.AddBuff(BuffID.CatBast, 2);
                            Player.AddBuff(BuffID.StarInBottle, 2);
                            Player.AddBuff(BuffID.Honey, 2);
                            Player.AddBuff(BuffID.HeartLamp, 2);
                            Player.AddBuff(BuffID.DryadsWard, 2);
                            Player.AddBuff(BuffID.Campfire, 2);
                            break;
                        case "CombatStations":
                            Player.AddBuff(BuffID.AmmoBox, 2);
                            Player.AddBuff(BuffID.Bewitched, 2);
                            Player.AddBuff(BuffID.Clairvoyance, 2);
                            Player.AddBuff(BuffID.Sharpened, 2);
                            Player.AddBuff(BuffID.WarTable, 2);
                            Player.AddBuff(BuffID.SugarRush, 2);
                            break;
                        case "AutoClicker":
                            if (ability.Level > 0 && ClickerSupportHelper.ClickerClassLoaded)
                            {
                                int abilityLevel = ability.Level;

                                float currentSpeedFactor = config.rebirthAbilities.AutoClickerSpeedFactorAtLevel1 +
                                                        ((abilityLevel - 1) * config.rebirthAbilities.AutoClickerSpeedFactorImprovementPerLevel);

                                currentSpeedFactor = Math.Max(2f, currentSpeedFactor);
                                currentSpeedFactor = Math.Min(120f, currentSpeedFactor);

                                bool preventEffects = config.rebirthAbilities.AutoClickerPreventsEffects;

                                ClickerCompat.SetAutoReuseEffect(Player,
                                    currentSpeedFactor,
                                    controlledByKeyBind: false,
                                    preventEffects);
                            }
                            break;
                    }
                }
                else
                {
                    switch(abilityId)
                    {
                        case "CombatStations":
                            if (ability.IsUnlocked)
                            {
                                Player.ClearBuff(BuffID.AmmoBox);
                                Player.ClearBuff(BuffID.Bewitched);
                                Player.ClearBuff(BuffID.Clairvoyance);
                                Player.ClearBuff(BuffID.Sharpened);
                                Player.ClearBuff(BuffID.WarTable);
                            }
                            break;
                    }
                }
            }
        }

        private void ApplyAbilityEffects2()
        {

            if (RebirthAbilities.TryGetValue("ReducedPotionSickness", out RebirthAbility ability) && ability.IsUnlocked)
            {
                int idx = Player.FindBuffIndex(BuffID.PotionSickness);
                if (idx != -1 && !appliedPotionReduction)
                {
                    int remaining = Player.buffTime[idx];
                    int reduced  = (int)(remaining * 0.5f);
                    Player.buffTime[idx]   = reduced;
                    Player.potionDelay     = reduced;
                    appliedPotionReduction = true;
                }
                else if (idx == -1)
                {
                    appliedPotionReduction = false;
                }

                if (Player.manaSickTime > 0 && !appliedManaSickReduction)
                {
                    int m = (int)(Player.manaSickTime * 0.5f);
                    Player.manaSickTime       = m;
                    appliedManaSickReduction  = true;
                }
                else if (Player.manaSickTime <= 0)
                {
                    appliedManaSickReduction = false;
                }
            }

            if (RebirthAbilities.TryGetValue("ShadowTrail", out RebirthAbility shadowTrail) &&
                shadowTrail.IsUnlocked &&
                shadowTrail.AbilityData.TryGetValue("Enabled", out object shadowTrailEnabled) &&
                (bool)shadowTrailEnabled)
            {
                Player.armorEffectDrawShadow = true;
            }

            if (RebirthAbilities.TryGetValue("AuraPulse", out RebirthAbility auraPulse) &&
                auraPulse.IsUnlocked &&
                auraPulse.AbilityData.TryGetValue("Enabled", out object auraPulseEnabled) &&
                (bool)auraPulseEnabled)
            {
                Player.armorEffectDrawOutlines = true;
            }
        }

        public override void GetHealLife(Item item, bool quickHeal, ref int healValue)
        {
            var config = ModContent.GetInstance<StatariaConfig>();

            int effectiveVIT = VIT;
            if (GhostStats.TryGetValue("VIT", out int vitBonus))
                effectiveVIT += vitBonus;

            if (config.statSettings.EnableHealingPotionBoost && effectiveVIT > 0 && healValue > 0)
            {
                float boostMultiplier = 1f + (effectiveVIT * config.statSettings.HealingPotionBoostPercent / 100f);
                int boostedHeal = (int)(healValue * boostMultiplier);

                healValue = boostedHeal;
            }
        }

        public override void ModifyHurt(ref Player.HurtModifiers modifiers)
        {
            var config = ModContent.GetInstance<StatariaConfig>();

            int effectiveEND = END;
            if (GhostStats.TryGetValue("END", out int endBonus))
                effectiveEND += endBonus;

            if (config.statSettings.EnableKnockbackResist)
            {
                float kbResist = Math.Min(effectiveEND * 0.01f, 1f);
                modifiers.Knockback *= 1f - kbResist;
            }

            if (config.statSettings.EnableDR)
            {
                float diminishingDR = 1f - (1f / (1f + effectiveEND * 0.01f));
                modifiers.FinalDamage *= 1f - diminishingDR;
            }

            if (RebirthAbilities.TryGetValue("LastStand", out RebirthAbility lastStand) &&
                lastStand.IsUnlocked &&
                lastStandCooldownTimer <= 0)
            {
                modifiers.ModifyHurtInfo += (ref Player.HurtInfo info) =>
                {
                    if (Player.statLife <= info.Damage)
                    {
                        wasLastStandTriggered = true;

                        lastStandHealAmount = (int)(Player.statLifeMax2 * (config.rebirthAbilities.LastStandHealPercent / 100f));

                        info.Damage = 0;

                        if (Main.netMode != NetmodeID.Server)
                        {
                            CombatText.NewText(Player.Hitbox, Color.LimeGreen, "Last Stand!");
                        }

                        lastStandImmunityTimer = config.rebirthAbilities.LastStandImmunityTime * 60;

                        lastStandCooldownTimer = config.rebirthAbilities.LastStandCooldown * 60;
                    }
                };
            }
        }

        public override void OnHurt(Player.HurtInfo info)
        {
            var config = ModContent.GetInstance<StatariaConfig>();

            if (wasLastStandTriggered)
            {
                Player.statLife = lastStandHealAmount;
                Player.HealEffect(lastStandHealAmount);
                wasLastStandTriggered = false;
            }

            customRegenDelayTimer = config.statSettings.CustomHpRegenDelay * 60;

            if (!config.statSettings.EnableEnemyKnockback)
                return;

            int effectiveEND = END;
            if (GhostStats.TryGetValue("END", out int endBonus))
                effectiveEND += endBonus;

            if (!info.DamageSource.TryGetCausingEntity(out Entity entity) || entity is not NPC npc)
                return;

            if (!npc.boss)
            {
                Vector2 knockbackDir = npc.Center - Player.Center;
                knockbackDir.Normalize();

                float knockbackStrength = Math.Clamp(effectiveEND * config.statSettings.END_EnemyKnockbackMultiplier, 2f, 12f);
                npc.velocity += knockbackDir * knockbackStrength;
            }
        }

        public override void ModifyWeaponDamage(Item item, ref StatModifier damage)
        {
            var config = ModContent.GetInstance<StatariaConfig>();

            float bonus = 0f;

            int effectiveSTR = STR;
            if (GhostStats.TryGetValue("STR", out int strBonus))
                effectiveSTR += strBonus;

            int effectiveINT = INT;
            if (GhostStats.TryGetValue("INT", out int intBonus))
                effectiveINT += intBonus;

            int effectiveDEX = DEX;
            if (GhostStats.TryGetValue("DEX", out int dexBonus))
                effectiveDEX += dexBonus;

            int effectivePOW = POW;
            if (GhostStats.TryGetValue("POW", out int powBonus))
                effectivePOW += powBonus;

            int effectiveRGE = RGE;
            if (GhostStats.TryGetValue("RGE", out int rgeBonus))
                effectiveRGE += rgeBonus;

            int effectiveBRD = BRD;
            if (GhostStats.TryGetValue("BRD", out int brdBonus))
                effectiveBRD += brdBonus;

            int effectiveHLR = HLR;
            if (GhostStats.TryGetValue("HLR", out int hlrBonus))
                effectiveHLR += hlrBonus;

            int effectiveCLK = CLK;
            if (GhostStats.TryGetValue("CLK", out int clkBonus))
                effectiveCLK += clkBonus;

            bool isRogueWeapon = config.modIntegration.EnableCalamityIntegration &&
                                CalamitySupportHelper.CalamityLoaded &&
                                CalamitySupportHelper.IsRogueWeapon(item);

            bool isSymphonicWeapon = config.modIntegration.EnableThoriumIntegration &&
                                    ThoriumSupportHelper.ThoriumLoaded &&
                                    ThoriumSupportHelper.IsSymphonicWeapon(item);

            bool isRadiantWeapon = config.modIntegration.EnableThoriumIntegration &&
                                ThoriumSupportHelper.ThoriumLoaded &&
                                (ThoriumSupportHelper.IsRadiantWeapon(item) ||
                                (ThoriumSupportHelper.GetHealerDamageClass() != DamageClass.Generic &&
                                    item.DamageType == ThoriumSupportHelper.GetHealerDamageClass()));

            bool isClickerWeapon = config.modIntegration.EnableClickerClassIntegration &&
                                ClickerSupportHelper.ClickerClassLoaded &&
                                ClickerSupportHelper.IsClickerWeapon(item);

            if (item.CountsAsClass(DamageClass.Melee))
                bonus += effectiveSTR * (config.statSettings.STR_Damage / 100f);

            if (item.CountsAsClass(DamageClass.Magic))
                bonus += effectiveINT * (config.statSettings.INT_Damage / 100f);

            if (item.CountsAsClass(DamageClass.Ranged))
                bonus += effectiveDEX * (config.statSettings.DEX_Damage / 100f);

            if (isRogueWeapon)
                bonus += effectiveRGE * (config.modIntegration.RGE_Damage / 100f);

            if (isSymphonicWeapon)
                bonus += effectiveBRD * (config.modIntegration.BRD_Damage / 100f);

            if (isRadiantWeapon)
                bonus += effectiveHLR * (config.modIntegration.HLR_Damage / 100f);

            if (isClickerWeapon)
                bonus += effectiveCLK * (config.modIntegration.CLK_Damage / 100f);

            if (!item.CountsAsClass(DamageClass.Melee) &&
                !item.CountsAsClass(DamageClass.Ranged) &&
                !item.CountsAsClass(DamageClass.Magic) &&
                !item.CountsAsClass(DamageClass.Summon) &&
                !isRogueWeapon &&
                !isSymphonicWeapon &&
                !isRadiantWeapon &&
                !isClickerWeapon)
            {
                bonus += effectivePOW * (config.statSettings.POW_Damage / 100f);
            }
            else
            {
                bonus += effectivePOW * 0.001f;
            }

            if (config.generalBalance.UseMultiplicativeDamage)
            {
                damage *= 1f + bonus;
            }
            else
            {
                damage += bonus;
            }
        }

        public override void ModifyWeaponKnockback(Item item, ref StatModifier knockback)
        {
            var config = ModContent.GetInstance<StatariaConfig>();

            int effectiveSTR = STR;
            if (GhostStats.TryGetValue("STR", out int strBonus))
                effectiveSTR += strBonus;

            if (item.CountsAsClass(DamageClass.Melee))
                knockback += effectiveSTR * (config.statSettings.STR_Knockback / 100f);
        }

        public override void ModifyWeaponCrit(Item item, ref float crit)
        {
            var config = ModContent.GetInstance<StatariaConfig>();

            int effectiveLUC = LUC;
            if (GhostStats.TryGetValue("LUC", out int lucBonus))
                effectiveLUC += lucBonus;

            crit += effectiveLUC * config.statSettings.LUC_Crit;
        }

        public override bool CanConsumeAmmo(Item weapon, Item ammo)
        {
            var config = ModContent.GetInstance<StatariaConfig>();

            int effectiveDEX = DEX;
            if (GhostStats.TryGetValue("DEX", out int dexBonus))
                effectiveDEX += dexBonus;

            if (weapon.useAmmo > 0 && effectiveDEX > 0)
            {
                float chance = effectiveDEX * (config.statSettings.DEX_AmmoConservation / 100f);
                if (Main.rand.NextFloat() < chance)
                    return false;
            }
            return true;
        }



       public override void SyncPlayer(int toWho, int fromWho, bool newPlayer)
        {
            var packet = ModContent.GetInstance<Stataria>().GetPacket();
            packet.Write((byte)StatariaMessageType.SyncPlayer);
            packet.Write(Player.whoAmI);
            packet.Write(Level);
            packet.Write(XP);
            packet.Write(XPToNext);
            packet.Write(StatPoints);
            packet.Write(VIT);
            packet.Write(STR);
            packet.Write(AGI);
            packet.Write(INT);
            packet.Write(LUC);
            packet.Write(END);
            packet.Write(POW);
            packet.Write(DEX);
            packet.Write(SPR);
            packet.Write(RGE);
            packet.Write(TCH);
            packet.Write(BRD);
            packet.Write(HLR);
            packet.Write(CLK);
            packet.Write(lastStandCooldownTimer);
            packet.Write(RebirthCount);
            packet.Write(RebirthPoints);

            packet.Write(AutoAllocateEnabled);
            packet.Write(AutoAllocateStats.Count);
            foreach (string stat in AutoAllocateStats)
            {
                packet.Write(stat);
            }

            packet.Write(rewardedBosses.Count);
            foreach (int bossId in rewardedBosses)
            {
                packet.Write(bossId);
            }

            packet.Send(toWho, fromWho);
        }

        public void SyncAbilities(int toWho = -1, int fromWho = -1)
        {
            if (Main.netMode == NetmodeID.SinglePlayer)
                return;

            var packet = ModContent.GetInstance<Stataria>().GetPacket();
            packet.Write((byte)StatariaMessageType.SyncAbilities);
            packet.Write(Player.whoAmI);

            int unlockedCount = RebirthAbilities.Count(a => a.Value.IsUnlocked);
            packet.Write(unlockedCount);

            foreach (var kvp in RebirthAbilities)
            {
                if (kvp.Value.IsUnlocked)
                {
                    packet.Write(kvp.Key);
                    packet.Write(kvp.Value.Level);

                    if (kvp.Value.AbilityType == RebirthAbilityType.Toggleable &&
                        kvp.Value.AbilityData.ContainsKey("Enabled"))
                    {
                        packet.Write((bool)kvp.Value.AbilityData["Enabled"]);
                    }
                    else
                    {
                        packet.Write(false);
                    }
                }
            }

            packet.Send(toWho, fromWho);
        }
    }
}