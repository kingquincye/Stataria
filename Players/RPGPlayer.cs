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
using Stataria.Buffs;

namespace Stataria
{
    public class RPGPlayer : ModPlayer
    {
        public TabBarUI.TabType LastActiveTab { get; set; } = TabBarUI.TabType.Stats;
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
        public int divineInterventionCooldownTimer = 0;
        public float CooldownHUDX = -1f;
        public float CooldownHUDY = -1f;

        private bool appliedPotionReduction = false;
        private bool appliedManaSickReduction = false;
        public int RebirthCount = 0;
        public int RebirthPoints = 0;
        public int BossKillsCount { get; set; } = 0;
        public bool WasRetroRPGranted = false;
        public Dictionary<string, RebirthAbility> RebirthAbilities { get; private set; } = new Dictionary<string, RebirthAbility>();

        public Color? OriginalEyeColor { get; set; } = null;

        private Role _activeRole;
        public Role ActiveRole
        {
            get
            {
                var config = ModContent.GetInstance<StatariaConfig>();
                if (config != null && !config.roleSettings.EnableRoleSystem)
                    return null;
                return _activeRole;
            }
            set => _activeRole = value;
        }
        public Role RawActiveRole
        {
            get => _activeRole;
            set => _activeRole = value;
        }
        public int RoleSwitchCount { get; set; }
        public Dictionary<string, Role> AvailableRoles { get; private set; } = new Dictionary<string, Role>();
        public HashSet<string> AscendedRoles { get; private set; } = new HashSet<string>();
        private HashSet<int> currentMinionTypes = new HashSet<int>();
        private int beastmasterBonusSlots = 0;
        private HashSet<int> apexSummonerMinionTypes = new HashSet<int>();
        private float apexSummonerDamageBonus = 0f;
        private int arcaneSurgeDamageBonus = 0;

        public int VIT = 0, STR = 0, AGI = 0, INT = 0, LUC = 0, END = 0, POW = 0, DEX = 0, SPR = 0, RGE = 0, TCH = 0, BRD = 0, HLR = 0, CLK = 0, BLH = 0, HNT = 0, GMB = 0, SHM = 0, THR = 0, PST = 0;
        public HashSet<int> rewardedBosses = new();

        public bool AutoAllocateEnabled { get; set; } = false;
        public HashSet<string> AutoAllocateStats { get; private set; } = new HashSet<string>();
        public Dictionary<string, int> GhostStats { get; private set; } = new Dictionary<string, int>();

        public override void Initialize()
        {
            var config = ModContent.GetInstance<StatariaConfig>();
            Level = 1;
            XP = 0L;
            RecalculateXPToNext();
            StatPoints = 0;
            VIT = STR = AGI = INT = LUC = END = POW = DEX = SPR = RGE = TCH = BRD = HLR = CLK = BLH = HNT = GMB = SHM = THR = PST = 0;
            GhostStats = new Dictionary<string, int>();
            rewardedBosses.Clear();
            lastStandCooldownTimer = 0;
            lastStandImmunityTimer = 0;
            wasLastStandTriggered = false;
            RebirthCount = 0;
            RebirthPoints = 0;
            BossKillsCount = 0;
            RebirthAbilities = new Dictionary<string, RebirthAbility>();
            AscendedRoles = new HashSet<string>();
            RegisterDefaultAbilities();
            RegisterDefaultRoles();
            xpVerifier = new XPVerificationSystem(this);
        }

        public override void SaveData(TagCompound tag)
        {
            tag["LastActiveTab"] = (int)LastActiveTab;
            tag["Level"] = Level;
            tag["XP"] = XP;
            tag["XPToNext"] = XPToNext;
            tag["StatPoints"] = StatPoints;
            tag["VIT"] = VIT; tag["STR"] = STR; tag["AGI"] = AGI;
            tag["INT"] = INT; tag["LUC"] = LUC; tag["END"] = END;
            tag["POW"] = POW; tag["DEX"] = DEX; tag["SPR"] = SPR;
            tag["RGE"] = RGE; tag["TCH"] = TCH; tag["BRD"] = BRD;
            tag["HLR"] = HLR; tag["CLK"] = CLK;
            tag["BLH"] = BLH;
            tag["HNT"] = HNT; tag["GMB"] = GMB;
            tag["SHM"] = SHM; tag["THR"] = THR;
            tag["PST"] = PST;
            tag["RewardedBosses"] = new List<int>(rewardedBosses);
            tag["lastStandCooldownTimer"] = lastStandCooldownTimer;
            tag["divineInterventionCooldownTimer"] = divineInterventionCooldownTimer;
            tag["CooldownHUDX"] = CooldownHUDX;
            tag["CooldownHUDY"] = CooldownHUDY;
            tag["RebirthCount"] = RebirthCount;
            tag["RebirthPoints"] = RebirthPoints;
            tag["BossKillsCount"] = BossKillsCount;
            tag["WasRetroRPGranted"] = WasRetroRPGranted;
            var abilitiesData = new List<TagCompound>();
            foreach (var kvp in RebirthAbilities)
            {
                var abilityTag = kvp.Value.Save();
                abilityTag["AbilityId"] = kvp.Key;
                abilitiesData.Add(abilityTag);
            }
            tag["RebirthAbilities"] = abilitiesData;
            if (_activeRole != null)
                tag["ActiveRoleID"] = _activeRole.ID;
            tag["RoleSwitchCount"] = RoleSwitchCount;
            tag["AscendedRoles"] = new List<string>(AscendedRoles);

            if (OriginalEyeColor.HasValue)
            {
                tag["OriginalEyeR"] = (int)OriginalEyeColor.Value.R;
                tag["OriginalEyeG"] = (int)OriginalEyeColor.Value.G;
                tag["OriginalEyeB"] = (int)OriginalEyeColor.Value.B;
                tag["OriginalEyeA"] = (int)OriginalEyeColor.Value.A;
            }

            tag["AutoAllocateEnabled"] = AutoAllocateEnabled;
            tag["AutoAllocateStats"] = AutoAllocateStats.ToList();
        }

        public override void LoadData(TagCompound tag)
        {
            var config = ModContent.GetInstance<StatariaConfig>();
            LastActiveTab = tag.ContainsKey("LastActiveTab")
                ? (TabBarUI.TabType)tag.GetInt("LastActiveTab")
                : TabBarUI.TabType.Stats;
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
            BLH = tag.ContainsKey("BLH") ? tag.GetInt("BLH") : 0;
            HNT = tag.ContainsKey("HNT") ? tag.GetInt("HNT") : 0;
            GMB = tag.ContainsKey("GMB") ? tag.GetInt("GMB") : 0;
            SHM = tag.ContainsKey("SHM") ? tag.GetInt("SHM") : 0;
            THR = tag.ContainsKey("THR") ? tag.GetInt("THR") : 0;
            PST = tag.ContainsKey("PST") ? tag.GetInt("PST") : 0;
            if (tag.ContainsKey("RewardedBosses"))
                rewardedBosses = tag.Get<List<int>>("RewardedBosses").ToHashSet();
            lastStandCooldownTimer = tag.ContainsKey("lastStandCooldownTimer") ? tag.GetInt("lastStandCooldownTimer") : 0;
            divineInterventionCooldownTimer = tag.ContainsKey("divineInterventionCooldownTimer") ? tag.GetInt("divineInterventionCooldownTimer") : 0;
            CooldownHUDX = tag.ContainsKey("CooldownHUDX") ? tag.GetFloat("CooldownHUDX") : -1f;
            CooldownHUDY = tag.ContainsKey("CooldownHUDY") ? tag.GetFloat("CooldownHUDY") : -1f;
            RebirthCount = tag.ContainsKey("RebirthCount") ? tag.GetInt("RebirthCount") : 0;
            RebirthPoints = tag.ContainsKey("RebirthPoints") ? tag.GetInt("RebirthPoints") : 0;
            BossKillsCount = tag.ContainsKey("BossKillsCount") ? tag.GetInt("BossKillsCount") : 0;
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
            RegisterDefaultRoles();
            if (tag.ContainsKey("ActiveRoleID"))
            {
                string activeRoleID = tag.GetString("ActiveRoleID");
                if (AvailableRoles.ContainsKey(activeRoleID))
                {
                    _activeRole = AvailableRoles[activeRoleID];
                    _activeRole.Status = RoleStatus.Active;
                }
            }
            RoleSwitchCount = tag.ContainsKey("RoleSwitchCount") ? tag.GetInt("RoleSwitchCount") : 0;
            AscendedRoles = tag.ContainsKey("AscendedRoles") ? new HashSet<string>(tag.Get<List<string>>("AscendedRoles")) : new HashSet<string>();
            UpdateAscendedRoleProperties();


            if (tag.ContainsKey("OriginalEyeR"))
            {
                OriginalEyeColor = new Color(
                    tag.GetInt("OriginalEyeR"),
                    tag.GetInt("OriginalEyeG"),
                    tag.GetInt("OriginalEyeB"),
                    tag.GetInt("OriginalEyeA")
                );
            }
            else
            {
                OriginalEyeColor = null;
            }

            if (!WasRetroRPGranted && RebirthCount > 0)
            {
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
                            CombatText.NewText(Player.Hitbox, textColor, Terraria.Localization.Language.GetTextValue("Mods.Stataria.RPGPlayer.RetroRPSync", $"{sign}{difference}"), true);
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

            RebirthAbilities["EnhancedFortune"] = new RebirthAbility(
                Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityName.EnhancedFortune"), Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityDescription.EnhancedFortune"), 30, true, config.rebirthAbilities.MaxEnhancedFortuneLevel
            ) { AbilityType = RebirthAbilityType.Toggleable };

            RebirthAbilities["ReducedPotionSickness"] = new RebirthAbility(
                Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityName.ReducedPotionSickness"), Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityDescription.ReducedPotionSickness"), 20, false, 1);

            RebirthAbilities["ExtraAccessorySlot"] = new RebirthAbility(
                Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityName.ExtraAccessorySlot"), Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityDescription.ExtraAccessorySlot"), 30, true,
                Math.Min(config.rebirthAbilities.MaxExtraAccessorySlots, 50));

            RebirthAbilities["LastStand"] = new RebirthAbility(
                Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityName.LastStand"), Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityDescription.LastStand"), 40, false, 1);

            RebirthAbilities["Dash"] = new RebirthAbility(
                Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityName.Dash"), Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityDescription.Dash"), 20, false, 1);

            RebirthAbilities["AutoJump"] = new RebirthAbility(
                Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityName.AutoJump"), Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityDescription.AutoJump"), 15, false, 1);

            RebirthAbilities["NoFallDamage"] = new RebirthAbility(
                Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityName.NoFallDamage"), Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityDescription.NoFallDamage"), 25, false, 1);

            RebirthAbilities["WaterFreedom"] = new RebirthAbility(
                Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityName.WaterFreedom"), Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityDescription.WaterFreedom"), 20, false, 1);

            RebirthAbilities["Teleport"] = new RebirthAbility(
                Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityName.Teleport"), Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityDescription.Teleport"), 50, false, 1);

            RebirthAbilities["TreasureHunter"] = new RebirthAbility(
                Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityName.TreasureHunter"), Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityDescription.TreasureHunter"), 30, false, 1
            ) { AbilityType = RebirthAbilityType.Toggleable };

            RebirthAbilities["Sustenance"] = new RebirthAbility(
                Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityName.Sustenance"), Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityDescription.Sustenance"), 25, true, 3
            ) { AbilityType = RebirthAbilityType.Toggleable };

            RebirthAbilities["ArcheryMastery"] = new RebirthAbility(
                Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityName.ArcheryMastery"), Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityDescription.ArcheryMastery"), 15, false, 1
            ) { AbilityType = RebirthAbilityType.Toggleable };

            RebirthAbilities["BattleReady"] = new RebirthAbility(
                Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityName.BattleReady"), Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityDescription.BattleReady"), 10, false, 1
            ) { AbilityType = RebirthAbilityType.Toggleable };

            RebirthAbilities["AnglerLuck"] = new RebirthAbility(
                Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityName.AnglerLuck"), Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityDescription.AnglerLuck"), 20, false, 1
            ) { AbilityType = RebirthAbilityType.Toggleable };

            RebirthAbilities["VitalityFortitude"] = new RebirthAbility(
                Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityName.VitalityFortitude"), Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityDescription.VitalityFortitude"), 40, false, 1
            ) { AbilityType = RebirthAbilityType.Toggleable };

            RebirthAbilities["InnerCalm"] = new RebirthAbility(
                Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityName.InnerCalm"), Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityDescription.InnerCalm"), 10, false, 1
            ) { AbilityType = RebirthAbilityType.Toggleable };

            RebirthAbilities["ElementalResistance"] = new RebirthAbility(
                Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityName.ElementalResistance"), Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityDescription.ElementalResistance"), 15, false, 1
            ) { AbilityType = RebirthAbilityType.Toggleable };

            RebirthAbilities["ShadowVeil"] = new RebirthAbility(
                Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityName.ShadowVeil"), Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityDescription.ShadowVeil"), 20, false, 1
            ) { AbilityType = RebirthAbilityType.Toggleable };

            RebirthAbilities["FortuneFavored"] = new RebirthAbility(
                Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityName.FortuneFavored"), Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityDescription.FortuneFavored"), 25, false, 1
            ) { AbilityType = RebirthAbilityType.Toggleable };

            RebirthAbilities["ArcaneMastery"] = new RebirthAbility(
                Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityName.ArcaneMastery"), Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityDescription.ArcaneMastery"), 20, false, 1
            ) { AbilityType = RebirthAbilityType.Toggleable };

            RebirthAbilities["MasterBuilder"] = new RebirthAbility(
                Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityName.MasterBuilder"), Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityDescription.MasterBuilder"), 20, false, 1
            ) { AbilityType = RebirthAbilityType.Toggleable };

            RebirthAbilities["BattleFury"] = new RebirthAbility(
                Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityName.BattleFury"), Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityDescription.BattleFury"), 25, false, 1
            ) { AbilityType = RebirthAbilityType.Toggleable };

            RebirthAbilities["SurfaceSkimmer"] = new RebirthAbility(
                Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityName.SurfaceSkimmer"), Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityDescription.SurfaceSkimmer"), 15, false, 1
            ) { AbilityType = RebirthAbilityType.Toggleable };

            RebirthAbilities["ThornGuard"] = new RebirthAbility(
                Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityName.ThornGuard"), Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityDescription.ThornGuard"), 15, false, 1
            ) { AbilityType = RebirthAbilityType.Toggleable };

            RebirthAbilities["FleetFooted"] = new RebirthAbility(
                Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityName.FleetFooted"), Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityDescription.FleetFooted"), 15, false, 1
            ) { AbilityType = RebirthAbilityType.Toggleable };

            RebirthAbilities["SummonerPact"] = new RebirthAbility(
                Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityName.SummonerPact"), Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityDescription.SummonerPact"), 20, false, 1
            ) { AbilityType = RebirthAbilityType.Toggleable };

            RebirthAbilities["LavaWalker"] = new RebirthAbility(
                Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityName.LavaWalker"), Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityDescription.LavaWalker"), 20, false, 1
            ) { AbilityType = RebirthAbilityType.Toggleable };

            RebirthAbilities["ZeroGravity"] = new RebirthAbility(
                Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityName.ZeroGravity"), Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityDescription.ZeroGravity"), 35, false, 1
            ) { AbilityType = RebirthAbilityType.Toggleable };

            RebirthAbilities["NightVision"] = new RebirthAbility(
                Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityName.NightVision"), Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityDescription.NightVision"), 15, false, 1
            ) { AbilityType = RebirthAbilityType.Toggleable };

            RebirthAbilities["Sanctuary"] = new RebirthAbility(
                Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityName.Sanctuary"), Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityDescription.Sanctuary"), 50, false, 1
            ) { AbilityType = RebirthAbilityType.Toggleable };

            RebirthAbilities["CombatStations"] = new RebirthAbility(
                Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityName.CombatStations"), Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityDescription.CombatStations"), 50, false, 1
            ) { AbilityType = RebirthAbilityType.Toggleable };

            RebirthAbilities["GiantsGrip"] = new RebirthAbility(
                Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityName.GiantsGrip"), Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityDescription.GiantsGrip"), 25, false, 1
            );

            RebirthAbilities["GoldenTouch"] = new RebirthAbility(
                Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityName.GoldenTouch"), Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityDescription.GoldenTouch"), 15, true,
                config.rebirthAbilities.MaxGoldenTouchLevel);

            RebirthAbilities["EnhancedSpawns"] = new RebirthAbility(
                Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityName.EnhancedSpawns"), Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityDescription.EnhancedSpawns"),
                35,
                true,
                config.rebirthAbilities.MaxEnhancedSpawnsLevel
                ) { AbilityType = RebirthAbilityType.Toggleable };

            RebirthAbilities["ShadowTrail"] = new RebirthAbility(
                Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityName.ShadowTrail"), Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityDescription.ShadowTrail"), 5, false, 1
            ) { AbilityType = RebirthAbilityType.Toggleable };

            RebirthAbilities["AuraPulse"] = new RebirthAbility(
                Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityName.AuraPulse"), Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityDescription.AuraPulse"), 5, false, 1
            ) { AbilityType = RebirthAbilityType.Toggleable };

            RebirthAbilities["AutoClicker"] = new RebirthAbility(
                Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityName.AutoClicker"), Terraria.Localization.Language.GetTextValue("Mods.Stataria.RebirthAbilityDescription.AutoClicker"),
                25,
                true,
                config.rebirthAbilities.AutoClickerMaxLevel
            ) { AbilityType = RebirthAbilityType.Toggleable };

            foreach (var kvp in RebirthAbilities)
            {
                kvp.Value.ID = kvp.Key;
                if (kvp.Value.AbilityType == RebirthAbilityType.Toggleable && !kvp.Value.AbilityData.ContainsKey("Enabled"))
                {
                    kvp.Value.AbilityData["Enabled"] = false;
                }
            }
        }

        private void RegisterDefaultRoles()
        {
            var config = ModContent.GetInstance<StatariaConfig>();
            AvailableRoles.Clear();

            var critGod = new Role(
                "CritGod",
                Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleName.CritGod"),
                Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleDescription.CritGod"),
                Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleFlavorText.CritGod")
            );
            AvailableRoles["CritGod"] = critGod;

            var vampire = new Role(
                "Vampire",
                Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleName.Vampire"),
                Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleDescription.Vampire"),
                Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleFlavorText.Vampire")
            );
            AvailableRoles["Vampire"] = vampire;

            var beastmaster = new Role(
                "Beastmaster",
                Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleName.Beastmaster"),
                Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleDescription.Beastmaster"),
                Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleFlavorText.Beastmaster")
            );
            AvailableRoles["Beastmaster"] = beastmaster;

            var apexSummoner = new Role(
                "ApexSummoner",
                Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleName.ApexSummoner"),
                Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleDescription.ApexSummoner"),
                Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleFlavorText.ApexSummoner")
            );
            AvailableRoles["ApexSummoner"] = apexSummoner;

            var blackKnight = new Role(
                "BlackKnight",
                Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleName.BlackKnight"),
                Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleDescription.BlackKnight"),
                Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleFlavorText.BlackKnight")
            );
            AvailableRoles["BlackKnight"] = blackKnight;

            var cleric = new Role(
                "Cleric",
                Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleName.Cleric"),
                Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleDescription.Cleric"),
                Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleFlavorText.Cleric")
            );
            AvailableRoles["Cleric"] = cleric;

            var guardian = new Role(
                "Guardian",
                Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleName.Guardian"),
                Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleDescription.Guardian"),
                Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleFlavorText.Guardian")
            );
            AvailableRoles["Guardian"] = guardian;

            var necromancer = new Role(
                "Necromancer",
                Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleName.Necromancer"),
                Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleDescription.Necromancer"),
                Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleFlavorText.Necromancer")
            );
            AvailableRoles["Necromancer"] = necromancer;

            var berserker = new Role(
                "Berserker",
                Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleName.Berserker"),
                Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleDescription.Berserker"),
                Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleFlavorText.Berserker")
            );
            AvailableRoles["Berserker"] = berserker;

            var spellweaver = new Role(
                "Spellweaver",
                Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleName.Spellweaver"),
                Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleDescription.Spellweaver"),
                Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleFlavorText.Spellweaver")
            );
            AvailableRoles["Spellweaver"] = spellweaver;

            if (config.modIntegration.EnableSekirariaIntegration && SekirariaSupportHelper.SekirariaLoaded)
            {
                var shinobi = new Role(
                    "Shinobi",
                    Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleName.Shinobi"),
                    Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleDescription.Shinobi"),
                    Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleFlavorText.Shinobi")
                );
                AvailableRoles["Shinobi"] = shinobi;
            }

            foreach (var role in AvailableRoles.Values)
            {
                if (ActiveRole?.ID == role.ID)
                    role.Status = RoleStatus.Active;
                else
                    role.Status = RoleStatus.Available;
            }
            UpdateAscendedRoleProperties();
        }

        public void UpdateAscendedRoleProperties()
        {
            if (AvailableRoles.TryGetValue("Cleric", out Role clericRole))
            {
                if (AscendedRoles.Contains("Cleric"))
                {
                    clericRole.Name = Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleName.Angel");
                    clericRole.Description = Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleDescription.Angel");
                    clericRole.FlavorText = Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleFlavorText.Angel");
                }
                else
                {
                    clericRole.Name = Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleName.Cleric");
                    clericRole.Description = Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleDescription.Cleric");
                    clericRole.FlavorText = Terraria.Localization.Language.GetTextValue("Mods.Stataria.RoleFlavorText.Cleric");
                }
            }
        }


        public bool SwitchToRole(string roleID)
        {
            var config = ModContent.GetInstance<StatariaConfig>();
            if (!config.roleSettings.EnableRoleSystem)
                return false;

            if (!AvailableRoles.ContainsKey(roleID))
                return false;

            var newRole = AvailableRoles[roleID];
            if (!newRole.CanActivate(this))
                return false;

            bool isReactivation = newRole.Status == RoleStatus.Deactivated;
            int cost = isReactivation ? 0 : newRole.GetCurrentSwitchCost(this);

            // Restore original eye color when switching away from Vampire
            if (_activeRole != null && _activeRole.ID == "Vampire" && _activeRole.ID != roleID)
            {
                RestoreOriginalEyeColor();
            }

            if (_activeRole != null && _activeRole.ID != roleID)
            {
                RebirthPoints -= cost;
                RoleSwitchCount++;
                _activeRole.Status = RoleStatus.Available;
            }

            // Capture original eye color when switching to Vampire
            if (roleID == "Vampire" && !OriginalEyeColor.HasValue)
            {
                OriginalEyeColor = Player.eyeColor;
            }

            _activeRole = newRole;
            _activeRole.Status = RoleStatus.Active;

            if (Main.netMode != NetmodeID.SinglePlayer)
            {
                SyncPlayer(-1, Player.whoAmI, false);
            }

            return true;
        }

        public bool DeactivateRole()
        {
            if (_activeRole == null || _activeRole.Status != RoleStatus.Active)
                return false;

            // Restore original eye color when deactivating Vampire
            if (_activeRole.ID == "Vampire")
            {
                RestoreOriginalEyeColor();
            }

            _activeRole.Status = RoleStatus.Deactivated;

            if (Main.netMode != NetmodeID.SinglePlayer)
            {
                SyncPlayer(-1, Player.whoAmI, false);
            }

            return true;
        }

        public void ResetRoles()
        {
            // Restore original eye color when resetting roles
            if (_activeRole?.ID == "Vampire")
            {
                RestoreOriginalEyeColor();
            }

            if (_activeRole != null)
            {
                _activeRole.Status = RoleStatus.Available;
                _activeRole = null;
            }
            RoleSwitchCount = 0;

            // Revert Angel ascension role properties and clear AscendedRoles
            AscendedRoles.Clear();
            UpdateAscendedRoleProperties();

            // Reset Cleric / Angel player state fields
            var clericPlayer = Player.GetModPlayer<ClericPlayer>();
            if (clericPlayer != null)
            {
                clericPlayer.IsInSpiritForm = false;
                clericPlayer.SpiritFormTimer = 0;
                clericPlayer.SpiritAngelWhoAmI = -1;
                clericPlayer.DivineResurrectionCooldownTimer = 0;
                clericPlayer.IsResurrectionChanneling = false;
                clericPlayer.ChannelingTargetWhoAmI = -1;
                clericPlayer.ChannelingTimer = 0;
                clericPlayer.ChannelingMaxTime = 0;
                clericPlayer.ResurrectionInvincibilityTimer = 0;
                clericPlayer.SyncAngelState();
            }

            if (Main.netMode != NetmodeID.Server)
            {
                StatariaUI.RoleSelectionPanel?.RefreshRolesList();
            }

            if (Main.netMode != NetmodeID.SinglePlayer)
            {
                SyncPlayer(-1, Player.whoAmI, false);
            }
        }

        private void RestoreOriginalEyeColor()
        {
            if (OriginalEyeColor.HasValue)
            {
                Player.eyeColor = OriginalEyeColor.Value;
                OriginalEyeColor = null;
            }
        }

        public override void ProcessTriggers(TriggersSet triggersSet)
        {
            if (StatariaKeybinds.ToggleStatariaUI.JustPressed
                && !Terraria.GameInput.PlayerInput.WritingText)
            {
                bool anyUIOpen = StatariaUI.StatUI.CurrentState != null ||
                                StatariaUI.SkillTreeUI.CurrentState != null ||
                                StatariaUI.RoleSelectionUI.CurrentState != null ||
                                StatariaUI.SocketingUI.CurrentState != null;

                if (anyUIOpen)
                {
                    if (StatariaUI.TabBarPanel != null)
                    {
                        LastActiveTab = StatariaUI.TabBarPanel.CurrentTab;
                    }

                    StatariaUI.StatUI.SetState(null);
                    StatariaUI.SkillTreeUI.SetState(null);
                    StatariaUI.RoleSelectionUI.SetState(null);
                    StatariaUI.SocketingUI.SetState(null);
                    StatariaUI.TabBarInterface.SetState(null);
                }
                else
                {
                    StatariaUI.TabBarInterface.SetState(StatariaUI.TabBarPanel);
                    OpenUIOnTab(LastActiveTab);
                }
            }
            if (StatariaKeybinds.DivineInterventionKey.JustPressed &&
                !Terraria.GameInput.PlayerInput.WritingText &&
                ActiveRole?.ID == "Cleric" && ActiveRole.Status == RoleStatus.Active &&
                divineInterventionCooldownTimer <= 0)
            {
                var config = ModContent.GetInstance<StatariaConfig>();
                var clericPlayer = Player.GetModPlayer<ClericPlayer>();

                clericPlayer.ActivateDivineIntervention();
                divineInterventionCooldownTimer = (int)(config.roleSettings.DivineInterventionCooldown * 60f);

                if (Main.netMode != NetmodeID.Server)
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item29, Player.position);
                }
            }
            if (StatariaKeybinds.SoulRecallKey.JustPressed &&
                !Terraria.GameInput.PlayerInput.WritingText &&
                ActiveRole?.ID == "Necromancer" && ActiveRole.Status == RoleStatus.Active)
            {
                var necromancerPlayer = Player.GetModPlayer<NecromancerPlayer>();
                necromancerPlayer.PerformSoulRecall();
            }
            if (StatariaKeybinds.SavageRoarKey.JustPressed &&
                !Terraria.GameInput.PlayerInput.WritingText &&
                ActiveRole?.ID == "Berserker" && ActiveRole.Status == RoleStatus.Active)
            {
                var berserkerPlayer = Player.GetModPlayer<BerserkerPlayer>();
                if (berserkerPlayer.SavageRoarCooldownTimer <= 0)
                {
                    berserkerPlayer.ActivateSavageRoar();
                }
            }
            if (StatariaKeybinds.ElementalDischargeKey.JustPressed &&
                !Terraria.GameInput.PlayerInput.WritingText &&
                ActiveRole?.ID == "Spellweaver" && ActiveRole.Status == RoleStatus.Active)
            {
                var spellweaverPlayer = Player.GetModPlayer<SpellweaverPlayer>();
                if (spellweaverPlayer.ElementalCharge > 0)
                {
                    spellweaverPlayer.ActivateElementalDischarge();
                }
            }
            if (StatariaKeybinds.MortalDrawKey.JustPressed &&
                !Terraria.GameInput.PlayerInput.WritingText &&
                ActiveRole?.ID == "Shinobi" && ActiveRole.Status == RoleStatus.Active)
            {
                var shinobiPlayer = Player.GetModPlayer<ShinobiPlayer>();
                if (shinobiPlayer.MortalDrawCooldownTimer <= 0 && shinobiPlayer.MortalDrawAnimationTimer <= 0)
                {
                    if (SekirariaSupportHelper.HasParrySword(Player, out _))
                    {
                        shinobiPlayer.ActivateMortalDraw();
                    }
                    else
                    {
                        if (Main.netMode != NetmodeID.Server)
                        {
                            CombatText.NewText(Player.Hitbox, Color.Red, Terraria.Localization.Language.GetTextValue("Mods.Stataria.Combat.RequiresParrySword"), true);
                        }
                    }
                }
            }
        }

        private void OpenUIOnTab(TabBarUI.TabType tab)
        {
            var config = ModContent.GetInstance<StatariaConfig>();

            switch (tab)
            {
                case TabBarUI.TabType.Stats:
                    StatariaUI.StatUI.SetState(StatariaUI.Panel);
                    break;
                case TabBarUI.TabType.Abilities:
                    if (config.rebirthSystem.EnableRebirthSystem && config.rebirthSystem.EnableRebirthAbilities)
                    {
                        StatariaUI.SkillTreeUI.SetState(StatariaUI.SkillTreePanel);
                        StatariaUI.SkillTreePanel?.RefreshAbilitiesList();
                    }
                    else
                    {
                        LastActiveTab = TabBarUI.TabType.Stats;
                        StatariaUI.StatUI.SetState(StatariaUI.Panel);
                    }
                    break;
                case TabBarUI.TabType.Roles:
                    if (config.roleSettings.EnableRoleSystem)
                    {
                        StatariaUI.RoleSelectionUI.SetState(StatariaUI.RoleSelectionPanel);
                        StatariaUI.RoleSelectionPanel?.RefreshRolesList();
                    }
                    else
                    {
                        LastActiveTab = TabBarUI.TabType.Stats;
                        StatariaUI.StatUI.SetState(StatariaUI.Panel);
                    }
                    break;
                case TabBarUI.TabType.Socketing:
                    if (config.socketingSystem.EnableSocketingSystem)
                    {
                        StatariaUI.SocketingUI.SetState(StatariaUI.SocketingPanel);
                        StatariaUI.SocketingPanel?.RefreshUI();
                    }
                    else
                    {
                        LastActiveTab = TabBarUI.TabType.Stats;
                        StatariaUI.StatUI.SetState(StatariaUI.Panel);
                    }
                    break;
            }

            if (StatariaUI.TabBarPanel != null)
            {
                StatariaUI.TabBarPanel.SetActiveTab(LastActiveTab);
            }
        }

        private void RecalculateXPToNext()
        {
            var config = ModContent.GetInstance<StatariaConfig>();
            if (config.generalBalance.EnableXPCurve)
            {
                float dynamicExponent = config.generalBalance.LevelScalingFactor + (Level * 0.01f * config.generalBalance.XPCurveSteepness);
                XPToNext = (long)(100L * Math.Pow(Level, dynamicExponent));
            }
            else
            {
                XPToNext = (long)(100L * Math.Pow(Level, config.generalBalance.LevelScalingFactor));
            }
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
            RecalculateXPToNext();

            if (config.rebirthSystem.ResetStatsOnRebirth)
            {
                StatPoints = config.generalBalance.StatPointsPerLevel;
                VIT = STR = AGI = INT = LUC = END = POW = DEX = SPR = RGE = TCH = BRD = HLR = CLK = BLH = HNT = GMB = SHM = THR = PST = 0;
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
                CombatText.NewText(Player.Hitbox, Color.Purple, Terraria.Localization.Language.GetTextValue("Mods.Stataria.RPGPlayer.RebirthCount", RebirthCount));
                CombatText.NewText(Player.Hitbox, Color.Gold, Terraria.Localization.Language.GetTextValue("Mods.Stataria.RPGPlayer.RebirthPoints", pointsToAward));
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

            int spentPoints = VIT + STR + AGI + INT + LUC + END + POW + DEX + SPR + RGE + TCH + BRD + HLR + CLK + 
                  BLH + HNT + GMB + SHM + THR + PST;

            int totalPointsShould = shouldHaveStatPoints;

            int currentTotalPoints = spentPoints + StatPoints;

            if (totalPointsShould > currentTotalPoints)
            {
                int difference = totalPointsShould - currentTotalPoints;
                StatPoints += difference;

                if (Main.netMode != NetmodeID.Server)
                {
                    CombatText.NewText(Player.Hitbox, Color.Green, Terraria.Localization.Language.GetTextValue("Mods.Stataria.RPGPlayer.StatPointsSync", difference), true);
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
                
                int cap = int.MaxValue;
                if (config.statSettings.EnableStatCaps)
                {
                    switch (normalizedStat)
                    {
                        case "VIT": cap = config.statSettings.VIT_Cap; break;
                        case "STR": cap = config.statSettings.STR_Cap; break;
                        case "AGI": cap = config.statSettings.AGI_Cap; break;
                        case "INT": cap = config.statSettings.INT_Cap; break;
                        case "LUC": cap = config.statSettings.LUC_Cap; break;
                        case "END": cap = config.statSettings.END_Cap; break;
                        case "POW": cap = config.statSettings.POW_Cap; break;
                        case "DEX": cap = config.statSettings.DEX_Cap; break;
                        case "SPR": cap = config.statSettings.SPR_Cap; break;
                        case "TCH": cap = config.statSettings.TCH_Cap; break;
                        case "RGE": cap = config.statSettings.RGE_Cap; break;
                        case "BRD": cap = config.statSettings.BRD_Cap; break;
                        case "HLR": cap = config.statSettings.HLR_Cap; break;
                        case "CLK": cap = config.statSettings.CLK_Cap; break;
                        case "BLH": cap = config.statSettings.BLH_Cap; break;
                        case "HNT": cap = config.statSettings.HNT_Cap; break;
                        case "GMB": cap = config.statSettings.GMB_Cap; break;
                        case "SHM": cap = config.statSettings.SHM_Cap; break;
                        case "THR": cap = config.statSettings.THR_Cap; break;
                        case "PST": cap = config.statSettings.PST_Cap; break;
                    }

                    if (cap != -1 && config.rebirthSystem.EnableProgressiveStatCaps && RebirthCount > 0)
                    {
                        float capMultiplier = 1f + (RebirthCount * config.rebirthSystem.ProgressiveStatCapMultiplier);
                        cap = (int)(cap * capMultiplier);
                    }
                }

                int baseStat = 0;
                switch (normalizedStat)
                {
                    case "VIT": baseStat = VIT; break;
                    case "STR": baseStat = STR; break;
                    case "AGI": baseStat = AGI; break;
                    case "INT": baseStat = INT; break;
                    case "LUC": baseStat = LUC; break;
                    case "END": baseStat = END; break;
                    case "POW": baseStat = POW; break;
                    case "DEX": baseStat = DEX; break;
                    case "SPR": baseStat = SPR; break;
                    case "TCH": baseStat = TCH; break;
                    case "RGE": baseStat = RGE; break;
                    case "BRD": baseStat = BRD; break;
                    case "HLR": baseStat = HLR; break;
                    case "CLK": baseStat = CLK; break;
                    case "BLH": baseStat = BLH; break;
                    case "HNT": baseStat = HNT; break;
                    case "GMB": baseStat = GMB; break;
                    case "SHM": baseStat = SHM; break;
                    case "THR": baseStat = THR; break;
                    case "PST": baseStat = PST; break;
                }

                int clampedGhostValue = ghostStatValue;
                if (config.statSettings.EnableStatCaps && cap != -1)
                {
                    clampedGhostValue = Math.Max(0, Math.Min(ghostStatValue, cap - baseStat));
                }

                GhostStats[normalizedStat] = clampedGhostValue;
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
                case "BLH":
                    baseStat = BLH;
                    cap = config.statSettings.BLH_Cap;
                    break;
                case "HNT":
                    baseStat = HNT;
                    cap = config.statSettings.HNT_Cap;
                    break;
                case "GMB":
                    baseStat = GMB;
                    cap = config.statSettings.GMB_Cap;
                    break;
                case "SHM":
                    baseStat = SHM;
                    cap = config.statSettings.SHM_Cap;
                    break;
                case "THR":
                    baseStat = THR;
                    cap = config.statSettings.THR_Cap;
                    break;
                case "PST":
                    baseStat = PST;
                    cap = config.statSettings.PST_Cap;
                    break;
            }

            int totalStat = baseStat;
            if (GhostStats.TryGetValue(statName, out int ghostBonus))
            {
                totalStat += ghostBonus;
            }

            if (config.generalBalance.EnableDiminishingReturns && totalStat > 0)
            {
                float diminishingScale = config.generalBalance.DiminishingReturnsRate;
                totalStat = (int)(totalStat / (1f + (totalStat * diminishingScale)));
            }

            if (capsEnabled)
            {
                int finalCap = cap;
                if (finalCap != -1)
                {
                    if (config.rebirthSystem.EnableProgressiveStatCaps && RebirthCount > 0)
                    {
                        float capMultiplier = 1f + (RebirthCount * config.rebirthSystem.ProgressiveStatCapMultiplier);
                        finalCap = (int)(finalCap * capMultiplier);
                    }
                    totalStat = Math.Min(totalStat, finalCap);
                }
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
                    CombatText.NewText(Player.Hitbox, Color.Gold, Terraria.Localization.Language.GetTextValue("Mods.Stataria.RPGPlayer.RPSync", difference), true);
                }
            }
        }

        public void RecalculateRebirthStatPoints()
        {
            var config = ModContent.GetInstance<StatariaConfig>();

            if (!config.rebirthSystem.EnableRebirthStatPointRecalculation || !config.rebirthSystem.EnableRebirthBonusStatPoints)
                return;

            int shouldHaveBaseStatPoints = (Level - 1) * config.generalBalance.StatPointsPerLevel;
            int shouldHaveBonusStatPoints = 0;

            if (RebirthCount > 0)
            {
                shouldHaveBonusStatPoints = (Level - 1) * (int)(config.generalBalance.StatPointsPerLevel * RebirthCount * config.rebirthSystem.RebirthStatPointsMultiplier);
            }

            int spentPoints = VIT + STR + AGI + INT + LUC + END + POW + DEX + SPR + RGE + TCH + BRD + HLR + CLK + 
                  BLH + HNT + GMB + SHM + THR + PST;
            int totalPointsShould = shouldHaveBaseStatPoints + shouldHaveBonusStatPoints;
            int currentTotalPoints = spentPoints + StatPoints;

            if (totalPointsShould > currentTotalPoints)
            {
                int difference = totalPointsShould - currentTotalPoints;
                StatPoints += difference;

                if (Main.netMode != NetmodeID.Server)
                {
                    CombatText.NewText(Player.Hitbox, Color.Gold, Terraria.Localization.Language.GetTextValue("Mods.Stataria.RPGPlayer.RebirthStatPointsSync", difference), true);
                }
            }
        }

        public int GetEffectiveLevelCap()
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
                    CombatText.NewText(Player.Hitbox, Color.Gray, Terraria.Localization.Language.GetTextValue("Mods.Stataria.RPGPlayer.LevelCapReached"));
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

        public bool CanRespec(out string reason)
        {
            reason = "";
            return true;
        }

        public bool PerformRespec(out string reason)
        {
            reason = "";
            return true;
        }

        public void ApplyXPDirectly(long amount, string source)
        {
            var config = ModContent.GetInstance<StatariaConfig>();
            var configClient = ModContent.GetInstance<StatariaClientConfig>();
            int effectiveLevelCap = GetEffectiveLevelCap();

            XP += amount;

            if (Main.netMode != NetmodeID.Server)
            {
                xpBarTimer = xpBarDuration;

                bool showPopup = configClient.ShowXPGainPopups;

                if (source.Contains("Melee") || source.Contains("Proj") || source.Contains("Damage"))
                {
                    showPopup = showPopup && configClient.ShowDamageXPPopups;

                    if (config.generalBalance.DamageXP <= 0)
                        showPopup = false;
                }
                else if (source.Contains("Kill"))
                {
                    showPopup = showPopup && configClient.ShowKillXPPopups;

                    if (config.generalBalance.KillXP <= 0)
                        showPopup = false;
                }
                else if (source.Contains("Boss"))
                {
                    showPopup = showPopup && configClient.ShowBossXPPopups;

                    if ((config.generalBalance.UseFlatBossXP && config.generalBalance.DefaultFlatBossXP <= 0) ||
                        (!config.generalBalance.UseFlatBossXP && config.generalBalance.BossXP <= 0))
                        showPopup = false;
                }

                if (showPopup && amount > 0)
                {
                    CombatText.NewText(Player.Hitbox, Color.Gold, Terraria.Localization.Language.GetTextValue("Mods.Stataria.RPGPlayer.XPGain", amount.ToString("N0")));

                    if (StatariaLogger.GlobalDebugMode)
                    {
                        Vector2 position = Player.Hitbox.TopLeft();
                        position.Y -= 20;
                        CombatText.NewText(new Rectangle((int)position.X, (int)position.Y, Player.Hitbox.Width, 20),
                            Color.Cyan, Terraria.Localization.Language.GetTextValue("Mods.Stataria.RPGPlayer.XPSource", source));
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
                        CombatText.NewText(Player.Hitbox, Color.Gray, Terraria.Localization.Language.GetTextValue("Mods.Stataria.RPGPlayer.LevelCapReached"));
                        levelCapMessageTimer = levelCapMessageCooldown;
                    }
                    break;
                }

                XP -= XPToNext;
                LevelUp();
            }

            if (Main.netMode != NetmodeID.SinglePlayer)
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

            int baseStatPoints = config.generalBalance.StatPointsPerLevel;
            int bonusStatPoints = 0;

            if (config.rebirthSystem.EnableRebirthBonusStatPoints && RebirthCount > 0)
            {
                bonusStatPoints = (int)(baseStatPoints * RebirthCount * config.rebirthSystem.RebirthStatPointsMultiplier);
            }

            StatPoints += baseStatPoints + bonusStatPoints;
            RecalculateXPToNext();

            if (Main.netMode != NetmodeID.Server)
            {
                CombatText.NewText(Player.Hitbox, Color.LightGreen, Terraria.Localization.Language.GetTextValue("Mods.Stataria.RPGPlayer.LevelUp", Level));
                var clientConfig = ModContent.GetInstance<StatariaClientConfig>();
                if (clientConfig.EnableLevelUpSound)
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item82, Player.position);
                }
            }
        }

        public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (target.friendly || target.lifeMax <= 5)
                return;

            var config = ModContent.GetInstance<StatariaConfig>();

            HandleBlackKnightMechanics(item, target, hit, damageDone);

            if (config.advanced.XPBlacklistedNPCs.Any(entry =>
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

            Item heldItem = Player.HeldItem;
            HandleBlackKnightMechanics(heldItem, target, hit, damageDone, proj);

            if (config.advanced.XPBlacklistedNPCs.Any(entry =>
                entry.Equals(Lang.GetNPCNameValue(target.type), StringComparison.OrdinalIgnoreCase) ||
                entry.Equals(target.TypeName, StringComparison.OrdinalIgnoreCase) ||
                (int.TryParse(entry, out int id) && id == target.type)))
            {
                return;
            }

            GainXP((long)(damageDone * config.generalBalance.DamageXP), "Projectile");
        }

        public override void ModifyHitNPCWithItem(Item item, NPC target, ref NPC.HitModifiers modifiers)
        {
            ApplyCritGodEffects(item, ref modifiers);
            ApplyBlackKnightMeleeEffects(item, ref modifiers);
        }

        public override void ModifyHitNPCWithProj(Projectile proj, NPC target, ref NPC.HitModifiers modifiers)
        {
            if (proj.owner == Player.whoAmI)
            {
                Item item = Player.HeldItem;
                ApplyCritGodEffects(item, ref modifiers);
                ApplyBlackKnightProjectileEffects(proj, ref modifiers);
            }
        }

        private void ApplyCritGodEffects(Item item, ref NPC.HitModifiers modifiers)
        {
            if (ActiveRole?.ID != "CritGod" || ActiveRole.Status != RoleStatus.Active)
                return;

            var config = ModContent.GetInstance<StatariaConfig>();

            float totalCrit = Player.GetWeaponCrit(item);

            if (totalCrit > 100f)
            {
                float excessCrit = totalCrit - 100f;
                float critDamageBonus = excessCrit * config.roleSettings.CritGodExcessCritToDamage / 100f;
                modifiers.CritDamage += critDamageBonus;
            }
        }

        private void UpdateBeastmasterEffects()
        {
            if (ActiveRole?.ID != "Beastmaster" || ActiveRole.Status != RoleStatus.Active)
            {
                currentMinionTypes.Clear();
                beastmasterBonusSlots = 0;
                return;
            }

            currentMinionTypes.Clear();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile proj = Main.projectile[i];
                if (!proj.active || proj.owner != Player.whoAmI || !proj.minion)
                    continue;

                if (!ProjectileID.Sets.MinionSacrificable[proj.type])
                    continue;

                int weapon = proj.GetGlobalProjectile<SummonSourceGlobalProjectile>().summonWeaponType;
                if (weapon > 0)
                    currentMinionTypes.Add(weapon);
            }

            var config = ModContent.GetInstance<StatariaConfig>();

            int effectiveSPR = GetEffectiveStat("SPR");
            int sprMinions = effectiveSPR / config.statSettings.SPR_MinionsPerX;
            int totalSlots = Player.maxMinions - beastmasterBonusSlots;
            int baseSlots = totalSlots - sprMinions;

            int bonusFromBaseSlots = (baseSlots / config.roleSettings.BeastmasterSlotsPerBonusSlot) * config.roleSettings.BeastmasterBonusSlotsGained;

            int bonusFromSPRSlots = 0;
            if (config.roleSettings.BeastmasterReduceSPRSlotEfficiency)
            {
                int sprSlotRequirement = (int)(config.roleSettings.BeastmasterSlotsPerBonusSlot * config.roleSettings.BeastmasterSPRSlotPenaltyMultiplier);
                bonusFromSPRSlots = (sprMinions / sprSlotRequirement) * config.roleSettings.BeastmasterBonusSlotsGained;
            }
            else
            {
                bonusFromSPRSlots = (sprMinions / config.roleSettings.BeastmasterSlotsPerBonusSlot) * config.roleSettings.BeastmasterBonusSlotsGained;
            }

            beastmasterBonusSlots = bonusFromBaseSlots + bonusFromSPRSlots;
        }

        private void UpdateApexSummonerEffects()
        {
            apexSummonerDamageBonus = 0f;
            if (ActiveRole?.ID != "ApexSummoner" || ActiveRole.Status != RoleStatus.Active)
            {
                apexSummonerMinionTypes.Clear();
                return;
            }

            apexSummonerMinionTypes.Clear();
            float slotsUsed = 0f;

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile proj = Main.projectile[i];
                if (!proj.active || proj.owner != Player.whoAmI || !proj.minion)
                    continue;

                if (!ProjectileID.Sets.MinionSacrificable[proj.type])
                    continue;

                int weapon = proj.GetGlobalProjectile<SummonSourceGlobalProjectile>().summonWeaponType;
                if (weapon > 0)
                    apexSummonerMinionTypes.Add(weapon);

                slotsUsed += proj.minionSlots;
            }

            if (apexSummonerMinionTypes.Count == 1)
            {
                int unusedSlots = (int)Math.Floor(Player.maxMinions - slotsUsed);
                if (unusedSlots > 0)
                {
                    var cfg = ModContent.GetInstance<StatariaConfig>();
                    apexSummonerDamageBonus = unusedSlots *
                        (cfg.roleSettings.ApexSummonerDamagePerUnusedSlot / 100f);
                }
            }
        }

        public float GetArcaneSurgeDamageBonus()
        {
            if (!Player.HasBuff(ModContent.BuffType<ArcaneSurgeBuff>()))
                return 0f;

            return arcaneSurgeDamageBonus;
        }

        private void ApplyBlackKnightMeleeEffects(Item item, ref NPC.HitModifiers modifiers)
        {
            if (ActiveRole?.ID != "BlackKnight" || ActiveRole.Status != RoleStatus.Active)
                return;

            if (!item.CountsAsClass(DamageClass.Melee))
                return;

            var config = ModContent.GetInstance<StatariaConfig>();

            if (Player.HasBuff(ModContent.BuffType<DarkFocusBuff>()))
            {
                int buffIndex = Player.FindBuffIndex(ModContent.BuffType<DarkFocusBuff>());
                if (buffIndex >= 0)
                {
                    int stacks = Math.Min((Player.buffTime[buffIndex] + 59) / 60, config.roleSettings.BlackKnightMaxDarkFocusStacks);
                    float critDamageBonus = stacks * config.roleSettings.BlackKnightDarkFocusCritDamagePerStack / 100f;
                    modifiers.CritDamage += critDamageBonus;
                }
            }
        }

        private void ApplyBlackKnightProjectileEffects(Projectile proj, ref NPC.HitModifiers modifiers)
        {
            if (ActiveRole?.ID != "BlackKnight" || ActiveRole.Status != RoleStatus.Active)
                return;

            Item heldItem = Player.HeldItem;
            var config = ModContent.GetInstance<StatariaConfig>();

            if (heldItem.CountsAsClass(DamageClass.Melee) && Player.HasBuff(ModContent.BuffType<DarkFocusBuff>()))
            {
                int buffIndex = Player.FindBuffIndex(ModContent.BuffType<DarkFocusBuff>());
                if (buffIndex >= 0)
                {
                    int stacks = Math.Min((Player.buffTime[buffIndex] + 59) / 60, config.roleSettings.BlackKnightMaxDarkFocusStacks);
                    float critDamageBonus = stacks * config.roleSettings.BlackKnightDarkFocusCritDamagePerStack / 100f;
                    modifiers.CritDamage += critDamageBonus;
                }
            }
        }

        private void HandleBlackKnightMechanics(Item item, NPC target, NPC.HitInfo hit, int damageDone, Projectile proj = null)
        {
            if (ActiveRole?.ID != "BlackKnight" || ActiveRole.Status != RoleStatus.Active)
                return;

            if (target.friendly || target.lifeMax <= 5)
                return;

            var config = ModContent.GetInstance<StatariaConfig>();

            if (item.CountsAsClass(DamageClass.Magic) && hit.Crit)
            {
                int currentBuffIndex = Player.FindBuffIndex(ModContent.BuffType<DarkFocusBuff>());
                int maxStacks = config.roleSettings.BlackKnightMaxDarkFocusStacks;

                if (currentBuffIndex >= 0)
                {
                    int currentStacks = Math.Min((Player.buffTime[currentBuffIndex] + 59) / 60, maxStacks);
                    if (currentStacks < maxStacks)
                    {
                        Player.buffTime[currentBuffIndex] = (currentStacks + 1) * 60;
                    }
                }
                else
                {
                    Player.AddBuff(ModContent.BuffType<DarkFocusBuff>(), 60);
                }
            }

            if (item.CountsAsClass(DamageClass.Melee) && Player.HasBuff(ModContent.BuffType<DarkFocusBuff>()))
            {
                Player.ClearBuff(ModContent.BuffType<DarkFocusBuff>());
            }

            if (item.CountsAsClass(DamageClass.Melee) && hit.Crit)
            {
                int manaRestore = config.roleSettings.BlackKnightManaRestoreOnMeleeCrit;
                Player.statMana += manaRestore;
                if (Player.statMana > Player.statManaMax2)
                    Player.statMana = Player.statManaMax2;

                int surgeDuration = (int)(config.roleSettings.BlackKnightArcaneSurgeDuration * 60f);

                if (config.roleSettings.BlackKnightArcaneSurgeScaleWithDamage)
                {
                    float scaledBonus = config.roleSettings.BlackKnightArcaneSurgeMagicDamage +
                                    (damageDone * config.roleSettings.BlackKnightArcaneSurgeDamageScaling);
                    arcaneSurgeDamageBonus = (int)scaledBonus;
                }
                else
                {
                    arcaneSurgeDamageBonus = (int)config.roleSettings.BlackKnightArcaneSurgeMagicDamage;
                }

                Player.AddBuff(ModContent.BuffType<ArcaneSurgeBuff>(), surgeDuration);
            }
        }

        public override void ModifyLuck(ref float luck)
        {
            var config = ModContent.GetInstance<StatariaConfig>();

            int effectiveLUC = GetEffectiveStat("LUC");

            if (config.statSettings.LUC_EnableLuckBonus)
                luck += effectiveLUC * config.statSettings.LUC_LuckBonus;

            luck = Math.Clamp(luck, -0.7f, 1f);
        }

        public override void ResetEffects()
        {
            var config = ModContent.GetInstance<StatariaConfig>();

            int effectiveVIT = GetEffectiveStat("VIT");

            Player.statLifeMax2 += (int)(effectiveVIT * config.statSettings.VIT_HP);

            int effectiveSTR = GetEffectiveStat("STR");

            Player.GetArmorPenetration(DamageClass.Melee) += effectiveSTR * config.statSettings.STR_ArmorPen;
            float meleeBonus = effectiveSTR * (config.statSettings.STR_Damage / 100f);
            if (meleeBonus > 0f)
            {
                if (config.generalBalance.UseMultiplicativeDamage)
                    Player.GetDamage(DamageClass.Melee) *= 1f + meleeBonus;
                else
                    Player.GetDamage(DamageClass.Melee) += meleeBonus;
            }
            Player.GetKnockback(DamageClass.Melee) += effectiveSTR * (config.statSettings.STR_Knockback / 100f);

            int effectiveINT = GetEffectiveStat("INT");

            Player.statManaMax2 += (int)(effectiveINT * config.statSettings.INT_MP);
            float rawReduction = effectiveINT * config.statSettings.INT_ManaCostReduction / 100f;
            float diminishingReduction = 1f - (1f / (1f + rawReduction));
            Player.manaCost -= diminishingReduction;
            Player.GetArmorPenetration(DamageClass.Magic) += effectiveINT * config.statSettings.INT_ArmorPen;
            float magicBonus = effectiveINT * (config.statSettings.INT_Damage / 100f);
            if (magicBonus > 0f)
            {
                if (config.generalBalance.UseMultiplicativeDamage)
                    Player.GetDamage(DamageClass.Magic) *= 1f + magicBonus;
                else
                    Player.GetDamage(DamageClass.Magic) += magicBonus;
            }

            int effectiveEND = GetEffectiveStat("END");

            if (config.statSettings.END_Defense > 0f)
            {
                Player.statDefense += (int)(effectiveEND * config.statSettings.END_Defense);
            }
            Player.aggro += (int)(effectiveEND * config.statSettings.END_Aggro);

            int effectiveAGI = GetEffectiveStat("AGI");

            float diminishedAGI = effectiveAGI <= 50 ? effectiveAGI : 50 + (effectiveAGI - 50) * 0.5f;
            Player.moveSpeed += diminishedAGI * (config.statSettings.AGI_MoveSpeed / 100f);
            Player.GetAttackSpeed(DamageClass.Generic) += diminishedAGI * (config.statSettings.AGI_AttackSpeed / 100f);
            float jumpHeightMultiplier = 1f - (float)Math.Pow(0.98, effectiveAGI);
            Player.jumpHeight += (int)(15 * jumpHeightMultiplier * config.statSettings.AGI_JumpHeight);
            Player.jumpSpeedBoost += effectiveAGI * config.statSettings.AGI_JumpSpeed;

            int effectiveLUC = GetEffectiveStat("LUC");

            if (config.statSettings.LUC_EnableFishing)
                Player.fishingSkill += (int)(effectiveLUC * config.statSettings.LUC_Fishing);

            Player.aggro -= (int)(effectiveLUC * config.statSettings.LUC_AggroReduction);

            Player.GetCritChance(DamageClass.Generic) += effectiveLUC * config.statSettings.LUC_Crit;

            if (ActiveRole?.ID == "CritGod" && ActiveRole.Status == RoleStatus.Active)
            {
                Player.GetCritChance(DamageClass.Generic) += config.roleSettings.CritGodCritChance;
            }

            int effectiveSPR = GetEffectiveStat("SPR");

            Player.maxMinions += effectiveSPR / config.statSettings.SPR_MinionsPerX;
            Player.maxTurrets += effectiveSPR / config.statSettings.SPR_SentriesPerX;
            float summonBonus = effectiveSPR * (config.statSettings.SPR_Damage / 100f);
            if (summonBonus > 0f)
            {
                if (config.generalBalance.UseMultiplicativeDamage)
                    Player.GetDamage(DamageClass.Summon) *= 1f + summonBonus;
                else
                    Player.GetDamage(DamageClass.Summon) += summonBonus;
            }

            if (ActiveRole?.ID == "Beastmaster" && ActiveRole.Status == RoleStatus.Active)
            {
                Player.maxMinions += beastmasterBonusSlots;

                int uniqueWeapons = Math.Max(0, currentMinionTypes.Count - 1);
                if (uniqueWeapons > 0)
                {
                    float damageBonus = uniqueWeapons * (config.roleSettings.BeastmasterDamagePerUniqueMinion / 100f);
                    Player.GetDamage(DamageClass.Summon) += damageBonus;
                }
            }
            if (ActiveRole?.ID == "ApexSummoner" && ActiveRole.Status == RoleStatus.Active)
            {
                Player.GetDamage(DamageClass.Summon) += apexSummonerDamageBonus;
            }

            int effectiveTCH = GetEffectiveStat("TCH");

            if (config.statSettings.TCH_EnableMiningSpeed)
                Player.pickSpeed -= effectiveTCH * config.statSettings.TCH_MiningSpeed * 0.01f;

            if (config.statSettings.TCH_EnableBuildSpeed)
            {
                Player.tileSpeed += effectiveTCH * config.statSettings.TCH_BuildSpeed;
                Player.wallSpeed += effectiveTCH * config.statSettings.TCH_BuildSpeed;
            }

            if (config.statSettings.TCH_EnableRange)
            {
                Player.tileRangeX += (int)(effectiveTCH * config.statSettings.TCH_Range);
                Player.tileRangeY += (int)(effectiveTCH * config.statSettings.TCH_Range);
            }

            if (config.modIntegration.EnableCalamityIntegration && CalamitySupportHelper.CalamityLoaded)
            {
                int effectiveRGE = GetEffectiveStat("RGE");

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

            int effectiveDEX = GetEffectiveStat("DEX");

            Player.GetArmorPenetration(DamageClass.Ranged) += effectiveDEX * config.statSettings.DEX_ArmorPen;
            float rangedBonus = effectiveDEX * (config.statSettings.DEX_Damage / 100f);
            if (rangedBonus > 0f)
            {
                if (config.generalBalance.UseMultiplicativeDamage)
                    Player.GetDamage(DamageClass.Ranged) *= 1f + rangedBonus;
                else
                    Player.GetDamage(DamageClass.Ranged) += rangedBonus;
            }

            if (config.modIntegration.EnableThoriumIntegration && ThoriumSupportHelper.ThoriumLoaded)
            {
                ApplyThoriumArmorPenetration();
            }

            if (config.modIntegration.EnableClickerClassIntegration && ClickerSupportHelper.ClickerClassLoaded)
            {
                int effectiveCLK = GetEffectiveStat("CLK");

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
            ApplyRoleEffects();
        }

        private void ApplyRogueStatEffects()
        {
            if (!CalamitySupportHelper.CalamityLoaded)
                return;

            var config = ModContent.GetInstance<StatariaConfig>();

            int effectiveRGE = GetEffectiveStat("RGE");

            if (config.modIntegration.RGE_EnableStealthConsumptionReduction)
            {
                if (effectiveRGE >= config.modIntegration.RGE_StealthConsumptionReductionThreshold)
                    CalamitySupportHelper.SetFieldValue(Player, "stealthStrikeHalfCost", true);
                else if (effectiveRGE >= config.modIntegration.RGE_StealthConsumption75Threshold)
                    CalamitySupportHelper.SetFieldValue(Player, "stealthStrike75Cost", true);
                else if (effectiveRGE >= config.modIntegration.RGE_StealthConsumption90Threshold)
                    CalamitySupportHelper.SetFieldValue(Player, "stealthStrike90Cost", true);
            }

            float rogueVelocity = CalamitySupportHelper.GetRogueVelocity(Player);
            rogueVelocity += effectiveRGE * (config.modIntegration.RGE_Velocity / 100f);
            CalamitySupportHelper.SetRogueVelocity(Player, rogueVelocity);
            
            float stealthGenStandstill = CalamitySupportHelper.GetStealthGenStandstill(Player);
            stealthGenStandstill += effectiveRGE * (config.modIntegration.RGE_StealthRegenBonus / 100f);
            CalamitySupportHelper.SetStealthGenStandstill(Player, stealthGenStandstill);

            float stealthGenMoving = CalamitySupportHelper.GetStealthGenMoving(Player);
            stealthGenMoving += effectiveRGE * (config.modIntegration.RGE_StealthRegenBonus / 100f);
            CalamitySupportHelper.SetStealthGenMoving(Player, stealthGenMoving);
        }

        private void ApplyPowerCalamityEffects()
        {
            if (!CalamitySupportHelper.CalamityLoaded)
                return;

            var config = ModContent.GetInstance<StatariaConfig>();

            int effectivePOW = GetEffectiveStat("POW");

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

            int effectiveBRD = GetEffectiveStat("BRD");

            if (config.modIntegration.BRD_PointsPerMaxInspiration > 0)
            {
                int inspirationBonus = effectiveBRD / config.modIntegration.BRD_PointsPerMaxInspiration;
                ThoriumSupportHelper.CallAddBardInspirationMax(Player, inspirationBonus);
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

            int effectiveHLR = GetEffectiveStat("HLR");

            if (effectiveHLR > 0)
            {
                int effectiveHLRPoints = effectiveHLR / config.modIntegration.HLR_PointsPerEffectPoint;

                if (effectiveHLRPoints > 0 && config.modIntegration.HLR_HealingPower > 0)
                {
                    int bonusToHealPower = (int)(effectiveHLRPoints * config.modIntegration.HLR_HealingPower);
                    ThoriumSupportHelper.CallBonusHealerHealBonus(Player, bonusToHealPower);

                    int bonusToLifeRecoveryAmount = (int)(effectiveHLRPoints * config.modIntegration.HLR_HealingPower) / 2;
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

            int effectiveBRD = GetEffectiveStat("BRD");

            int effectiveHLR = GetEffectiveStat("HLR");

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
            
            var availableStats = new List<string>();
            
            foreach (string statName in AutoAllocateStats)
            {
                bool isAtCap = false;

                if (config.statSettings.EnableStatCaps)
                {
                    int cap = 0;
                    int currentBaseStat = 0;

                    switch (statName)
                    {
                        case "VIT": currentBaseStat = VIT; cap = config.statSettings.VIT_Cap; break;
                        case "STR": currentBaseStat = STR; cap = config.statSettings.STR_Cap; break;
                        case "AGI": currentBaseStat = AGI; cap = config.statSettings.AGI_Cap; break;
                        case "INT": currentBaseStat = INT; cap = config.statSettings.INT_Cap; break;
                        case "LUC": currentBaseStat = LUC; cap = config.statSettings.LUC_Cap; break;
                        case "END": currentBaseStat = END; cap = config.statSettings.END_Cap; break;
                        case "POW": currentBaseStat = POW; cap = config.statSettings.POW_Cap; break;
                        case "DEX": currentBaseStat = DEX; cap = config.statSettings.DEX_Cap; break;
                        case "SPR": currentBaseStat = SPR; cap = config.statSettings.SPR_Cap; break;
                        case "TCH": currentBaseStat = TCH; cap = config.statSettings.TCH_Cap; break;
                        case "RGE": currentBaseStat = RGE; cap = config.statSettings.RGE_Cap; break;
                        case "BRD": currentBaseStat = BRD; cap = config.statSettings.BRD_Cap; break;
                        case "HLR": currentBaseStat = HLR; cap = config.statSettings.HLR_Cap; break;
                        case "CLK": currentBaseStat = CLK; cap = config.statSettings.CLK_Cap; break;
                        case "BLH": currentBaseStat = BLH; cap = config.statSettings.BLH_Cap; break;
                        case "HNT": currentBaseStat = HNT; cap = config.statSettings.HNT_Cap; break;
                        case "GMB": currentBaseStat = GMB; cap = config.statSettings.GMB_Cap; break;
                        case "SHM": currentBaseStat = SHM; cap = config.statSettings.SHM_Cap; break;
                        case "THR": currentBaseStat = THR; cap = config.statSettings.THR_Cap; break;
                        case "PST": currentBaseStat = PST; cap = config.statSettings.PST_Cap; break;
                        default: continue;
                    }

                    if (cap != -1)
                    {
                        if (config.rebirthSystem.EnableProgressiveStatCaps && RebirthCount > 0)
                        {
                            float capMultiplier = 1f + (RebirthCount * config.rebirthSystem.ProgressiveStatCapMultiplier);
                            cap = (int)(cap * capMultiplier);
                        }

                        int effectiveStat = GetEffectiveStat(statName);
                        if (effectiveStat >= cap)
                        {
                            isAtCap = true;
                        }
                        else
                        {
                            int ghostBonus = GhostStats.TryGetValue(statName, out int ghost) ? ghost : 0;
                            int maxUsefulBaseStat = cap - ghostBonus;

                            if (currentBaseStat >= maxUsefulBaseStat)
                            {
                                isAtCap = true;
                            }
                        }
                    }
                }

                if (!isAtCap)
                {
                    availableStats.Add(statName);
                }
            }

            if (availableStats.Count == 0)
                return;

            int statsCount = availableStats.Count;
            if (StatPoints < statsCount)
                return;

            int completeSetCount = StatPoints / statsCount;
            int totalPointsToAllocate = 0;

            foreach (string statName in availableStats)
            {
                int pointsToAdd = completeSetCount;

                if (config.statSettings.EnableStatCaps)
                {
                    int cap = 0;
                    int currentBaseStat = 0;

                    switch (statName)
                    {
                        case "VIT": currentBaseStat = VIT; cap = config.statSettings.VIT_Cap; break;
                        case "STR": currentBaseStat = STR; cap = config.statSettings.STR_Cap; break;
                        case "AGI": currentBaseStat = AGI; cap = config.statSettings.AGI_Cap; break;
                        case "INT": currentBaseStat = INT; cap = config.statSettings.INT_Cap; break;
                        case "LUC": currentBaseStat = LUC; cap = config.statSettings.LUC_Cap; break;
                        case "END": currentBaseStat = END; cap = config.statSettings.END_Cap; break;
                        case "POW": currentBaseStat = POW; cap = config.statSettings.POW_Cap; break;
                        case "DEX": currentBaseStat = DEX; cap = config.statSettings.DEX_Cap; break;
                        case "SPR": currentBaseStat = SPR; cap = config.statSettings.SPR_Cap; break;
                        case "TCH": currentBaseStat = TCH; cap = config.statSettings.TCH_Cap; break;
                        case "RGE": currentBaseStat = RGE; cap = config.statSettings.RGE_Cap; break;
                        case "BRD": currentBaseStat = BRD; cap = config.statSettings.BRD_Cap; break;
                        case "HLR": currentBaseStat = HLR; cap = config.statSettings.HLR_Cap; break;
                        case "CLK": currentBaseStat = CLK; cap = config.statSettings.CLK_Cap; break;
                        case "BLH": currentBaseStat = BLH; cap = config.statSettings.BLH_Cap; break;
                        case "HNT": currentBaseStat = HNT; cap = config.statSettings.HNT_Cap; break;
                        case "GMB": currentBaseStat = GMB; cap = config.statSettings.GMB_Cap; break;
                        case "SHM": currentBaseStat = SHM; cap = config.statSettings.SHM_Cap; break;
                        case "THR": currentBaseStat = THR; cap = config.statSettings.THR_Cap; break;
                        case "PST": currentBaseStat = PST; cap = config.statSettings.PST_Cap; break;
                        default: continue;
                    }

                    if (cap != -1)
                    {
                        if (config.rebirthSystem.EnableProgressiveStatCaps && RebirthCount > 0)
                        {
                            float capMultiplier = 1f + (RebirthCount * config.rebirthSystem.ProgressiveStatCapMultiplier);
                            cap = (int)(cap * capMultiplier);
                        }

                        int ghostBonus = GhostStats.TryGetValue(statName, out int ghost) ? ghost : 0;
                        int maxUsefulBaseStat = cap - ghostBonus;

                        if (currentBaseStat + pointsToAdd > maxUsefulBaseStat)
                        {
                            pointsToAdd = Math.Max(0, maxUsefulBaseStat - currentBaseStat);
                        }
                    }
                }

                if (pointsToAdd > 0)
                {
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
                        case "BLH": BLH += pointsToAdd; break;
                        case "HNT": HNT += pointsToAdd; break;
                        case "GMB": GMB += pointsToAdd; break;
                        case "SHM": SHM += pointsToAdd; break;
                        case "THR": THR += pointsToAdd; break;
                        case "PST": PST += pointsToAdd; break;
                    }

                    totalPointsToAllocate += pointsToAdd;
                }
            }

            StatPoints -= totalPointsToAllocate;
        }

        public override void PostUpdateEquips()
        {
            var config = ModContent.GetInstance<StatariaConfig>();
            int effectiveAGI = GetEffectiveStat("AGI");
            Player.wingTimeMax += (int)(effectiveAGI * config.statSettings.AGI_WingTime);

            if (config.modIntegration.EnableSekirariaIntegration && SekirariaSupportHelper.SekirariaLoaded)
            {
                int effectivePST = GetEffectiveStat("PST");
                if (effectivePST > 0)
                {
                    float maxPostureBonus = effectivePST * config.modIntegration.PST_MaxPosture;
                    float damageFlatBonus = effectivePST * config.modIntegration.PST_PostureDamage;
                    float blockDamageMult = 1f / (1f + effectivePST * config.modIntegration.PST_BlockDamageReduction);

                    SekirariaSupportHelper.AddPlayerPostureMaxBonus(Player, maxPostureBonus);
                    SekirariaSupportHelper.AddPlayerPostureDamageFlatBonus(Player, damageFlatBonus);
                    SekirariaSupportHelper.AddPlayerBlockDamageMultiplier(Player, blockDamageMult - 1f);
                    SekirariaSupportHelper.SyncPlayerPostureMax(Player);
                }
            }
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

            int effectiveVIT = GetEffectiveStat("VIT");
            bool isCleric = ActiveRole?.ID == "Cleric" && ActiveRole.Status == RoleStatus.Active;
            bool isGuardian = ActiveRole?.ID == "Guardian" && ActiveRole.Status == RoleStatus.Active;
            bool blockVITRegen = isCleric && config.roleSettings.ClericDisableVitRegen;

            if (config.statSettings.UseCustomHpRegen && !blockVITRegen)
            {
                if (customRegenDelayTimer > 0)
                    customRegenDelayTimer--;
                else if (Player.statLife < Player.statLifeMax2 && !Player.dead)
                {
                    float hpPerSecond = effectiveVIT * config.statSettings.CustomHpRegenPerVIT;

                    if (isGuardian && config.roleSettings.GuardianReduceVitEffects)
                    {
                        float reductionFactor = 1f - (config.roleSettings.GuardianVitEffectReduction / 100f);
                        hpPerSecond *= reductionFactor;
                    }

                    regenCarryover += hpPerSecond / 60f;

                    if (regenCarryover >= 1f)
                    {
                        int healAmount = (int)regenCarryover;
                        regenCarryover -= healAmount;
                        Player.statLife += healAmount;
                        if (Player.statLife > Player.statLifeMax2)
                            Player.statLife = Player.statLifeMax2;

                        if (Main.netMode != NetmodeID.SinglePlayer)
                            Player.HealEffect(healAmount, false);
                    }
                }
            }
            else if (!blockVITRegen)
            {
                float vitRegenMultiplier = 1f;
                if (isGuardian && config.roleSettings.GuardianReduceVitEffects)
                {
                    vitRegenMultiplier = 1f - (config.roleSettings.GuardianVitEffectReduction / 100f);
                }
                Player.lifeRegen += (int)((effectiveVIT / 2) * vitRegenMultiplier);
            }

            int effectiveINT = GetEffectiveStat("INT");

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

            if (divineInterventionCooldownTimer > 0)
                divineInterventionCooldownTimer--;

            ApplyAbilityEffects2();
            UpdateBeastmasterEffects();
            UpdateApexSummonerEffects();
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

        private void ApplyRoleEffects()
        {
            if (ActiveRole == null || ActiveRole.Status != RoleStatus.Active)
                return;

            var config = ModContent.GetInstance<StatariaConfig>();

            if (ActiveRole.ID == "CritGod")
            {
                Player.GetModPlayer<CritGodPlayer>().EnableSummonCrits = true;
            }
        }

        public override void GetHealLife(Item item, bool quickHeal, ref int healValue)
        {
            var config = ModContent.GetInstance<StatariaConfig>();

            int effectiveVIT = GetEffectiveStat("VIT");

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

            int effectiveEND = GetEffectiveStat("END");
            bool isGuardian = ActiveRole?.ID == "Guardian" && ActiveRole.Status == RoleStatus.Active;

            if (config.statSettings.EnableKnockbackResist && !isGuardian)
            {
                float kbResist = Math.Min(effectiveEND * (config.statSettings.END_KnockbackResistPerPoint / 100f), 1f);
                modifiers.Knockback *= 1f - kbResist;
            }

            if (config.statSettings.EnableDR && (!isGuardian || !config.roleSettings.GuardianDisableEndEffects))
            {
                float diminishingDR = 1f - (1f / (1f + (effectiveEND * config.statSettings.END_DRPerPoint / 100f)));
                modifiers.FinalDamage *= 1f - diminishingDR;
            }

            if (isGuardian)
            {
                float guardianDR = config.roleSettings.GuardianDamageReduction / 100f;
                modifiers.FinalDamage *= (1f - guardianDR);
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
                            CombatText.NewText(Player.Hitbox, Color.LimeGreen, Terraria.Localization.Language.GetTextValue("Mods.Stataria.RPGPlayer.LastStand"));
                        }

                        lastStandImmunityTimer = config.rebirthAbilities.LastStandImmunityTime * 60;

                        lastStandCooldownTimer = config.rebirthAbilities.LastStandCooldown * 60;
                    }
                };
            }
        }

        public override bool FreeDodge(Player.HurtInfo info)
        {
            if (Player.whoAmI != Main.myPlayer)
                return false;

            var config = ModContent.GetInstance<StatariaConfig>();
            float evadeChance = 0f;
            foreach (Item item in Player.armor)
            {
                if (item != null && !item.IsAir && item.TryGetGlobalItem<SocketingGlobalItem>(out var socketingParams))
                {
                    evadeChance += socketingParams.GetTotalCoreEffect(CoreType.Evasion);
                }
            }
            
            if (evadeChance > 0f)
            {
                if (Main.rand.NextFloat() < (evadeChance / 100f))
                {
                    Player.SetImmuneTimeForAllTypes(Player.longInvince ? 120 : 80);
                    return true;
                }
            }

            return false;
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

            int effectiveEND = GetEffectiveStat("END");

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
        
        public float GetGenericModDamageBonus(string statName, StatariaConfig config)
        {
            int effectiveStat = GetEffectiveStat(statName);
            
            return statName switch
            {
                "BLH" => effectiveStat * (config.modIntegration.BLH_Damage / 100f),
                "HNT" => effectiveStat * (config.modIntegration.HNT_Damage / 100f),
                "GMB" => effectiveStat * (config.modIntegration.GMB_Damage / 100f),
                "SHM" => effectiveStat * (config.modIntegration.SHM_Damage / 100f),
                "THR" => effectiveStat * (config.modIntegration.THR_Damage / 100f),
                _ => 0f
            };
        }

        public float GetGenericModFlatDamageBonus(string statName, StatariaConfig config)
        {
            int effectiveStat = GetEffectiveStat(statName);
            
            return statName switch
            {
                "BLH" => effectiveStat * config.modIntegration.BLH_FlatDamage,
                "HNT" => effectiveStat * config.modIntegration.HNT_FlatDamage,
                "GMB" => effectiveStat * config.modIntegration.GMB_FlatDamage,
                "SHM" => effectiveStat * config.modIntegration.SHM_FlatDamage,
                "THR" => effectiveStat * config.modIntegration.THR_FlatDamage,
                _ => 0f
            };
        }

        public override void ModifyWeaponDamage(Item item, ref StatModifier damage)
        {
            var config = ModContent.GetInstance<StatariaConfig>();

            float bonus = 0f;

            int effectiveSTR = GetEffectiveStat("STR");

            int effectiveINT = GetEffectiveStat("INT");

            int effectiveDEX = GetEffectiveStat("DEX");

            int effectiveSPR = GetEffectiveStat("SPR");

            int effectivePOW = GetEffectiveStat("POW");

            int effectiveRGE = GetEffectiveStat("RGE");

            int effectiveBRD = GetEffectiveStat("BRD");

            int effectiveHLR = GetEffectiveStat("HLR");

            int effectiveCLK = GetEffectiveStat("CLK");

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

            ModDefinition genericModDef = null;
            bool isGenericModWeapon = false;
            if (config.modIntegration.EnableGenericModIntegration)
            {
                genericModDef = GenericModSupportHelper.GetModDefinitionForWeapon(item);
                isGenericModWeapon = genericModDef != null;
            }

            if (item.CountsAsClass(DamageClass.Melee)) { } // Handled globally in ResetEffects

            if (item.CountsAsClass(DamageClass.Magic)) { } // Handled globally in ResetEffects

            if (item.CountsAsClass(DamageClass.Ranged)) { } // Handled globally in ResetEffects

            if (isRogueWeapon)
                bonus += effectiveRGE * (config.modIntegration.RGE_Damage / 100f);

            if (isSymphonicWeapon)
                bonus += effectiveBRD * (config.modIntegration.BRD_Damage / 100f);

            if (isRadiantWeapon)
                bonus += effectiveHLR * (config.modIntegration.HLR_Damage / 100f);

            if (isClickerWeapon)
                bonus += effectiveCLK * (config.modIntegration.CLK_Damage / 100f);

            if (isGenericModWeapon && genericModDef != null)
            {
                float damageBonus = GetGenericModDamageBonus(genericModDef.StatName, config);
                bonus += damageBonus;
            }

            if (!item.CountsAsClass(DamageClass.Melee) &&
                !item.CountsAsClass(DamageClass.Ranged) &&
                !item.CountsAsClass(DamageClass.Magic) &&
                !item.CountsAsClass(DamageClass.Summon) &&
                !isRogueWeapon &&
                !isSymphonicWeapon &&
                !isRadiantWeapon &&
                !isClickerWeapon &&
                !isGenericModWeapon)
            {
                bonus += effectivePOW * (config.statSettings.POW_Damage / 100f);
            }
            else
            {
                bonus += effectivePOW * 0.001f;
            }

            if (ActiveRole?.ID == "BlackKnight" && ActiveRole.Status == RoleStatus.Active)
            {
                if (item.CountsAsClass(DamageClass.Melee))
                {
                    bonus += effectiveINT * (config.roleSettings.BlackKnightINTToMeleeDamage / 100f);
                }

                if (item.CountsAsClass(DamageClass.Magic))
                {
                    bonus += effectiveSTR * (config.roleSettings.BlackKnightSTRToMagicDamage / 100f);

                    if (Player.HasBuff(ModContent.BuffType<ArcaneSurgeBuff>()))
                    {
                        bonus += GetArcaneSurgeDamageBonus() / 100f;
                    }
                }
            }

            if (config.statSettings.EnableFlatDamageIncrease)
            {
                float flatBonus = 0f;

                if (item.CountsAsClass(DamageClass.Melee))
                {
                    flatBonus += effectiveSTR * config.statSettings.STR_FlatDamage;
                }
                if (item.CountsAsClass(DamageClass.Magic))
                {
                    flatBonus += effectiveINT * config.statSettings.INT_FlatDamage;
                }
                if (item.CountsAsClass(DamageClass.Ranged))
                {
                    flatBonus += effectiveDEX * config.statSettings.DEX_FlatDamage;
                }
                if (item.CountsAsClass(DamageClass.Summon))
                {
                    flatBonus += effectiveSPR * config.statSettings.SPR_FlatDamage;
                }

                if (isRogueWeapon)
                    flatBonus += effectiveRGE * config.modIntegration.RGE_FlatDamage;

                if (isSymphonicWeapon)
                    flatBonus += effectiveBRD * config.modIntegration.BRD_FlatDamage;

                if (isRadiantWeapon)
                    flatBonus += effectiveHLR * config.modIntegration.HLR_FlatDamage;

                if (isClickerWeapon)
                    flatBonus += effectiveCLK * config.modIntegration.CLK_FlatDamage;

                if (isGenericModWeapon && genericModDef != null)
                {
                    flatBonus += GetGenericModFlatDamageBonus(genericModDef.StatName, config);
                }

                if (!item.CountsAsClass(DamageClass.Melee) &&
                    !item.CountsAsClass(DamageClass.Ranged) &&
                    !item.CountsAsClass(DamageClass.Magic) &&
                    !item.CountsAsClass(DamageClass.Summon) &&
                    !isRogueWeapon &&
                    !isSymphonicWeapon &&
                    !isRadiantWeapon &&
                    !isClickerWeapon &&
                    !isGenericModWeapon)
                {
                    flatBonus += effectivePOW * config.statSettings.POW_FlatDamage;
                }
                else
                {
                    flatBonus += effectivePOW * config.statSettings.POW_FlatDamage * 0.2f;
                }

                damage.Flat += flatBonus;
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
            // Handled globally in ResetEffects
        }

        public override void ModifyWeaponCrit(Item item, ref float crit)
        {
            var config = ModContent.GetInstance<StatariaConfig>();

            // Base LUC and CritGod crit chance are handled globally in ResetEffects

            if (ActiveRole?.ID == "BlackKnight" && ActiveRole.Status == RoleStatus.Active)
            {
                if (item.CountsAsClass(DamageClass.Melee) && Player.HasBuff(ModContent.BuffType<DarkFocusBuff>()))
                {
                    int buffIndex = Player.FindBuffIndex(ModContent.BuffType<DarkFocusBuff>());
                    if (buffIndex >= 0)
                    {
                        int stacks = Math.Min((Player.buffTime[buffIndex] + 59) / 60, config.roleSettings.BlackKnightMaxDarkFocusStacks);
                        float critBonus = stacks * config.roleSettings.BlackKnightDarkFocusCritChancePerStack;
                        crit += critBonus;
                    }
                }
            }
        }

        public override bool CanConsumeAmmo(Item weapon, Item ammo)
        {
            var config = ModContent.GetInstance<StatariaConfig>();

            int effectiveDEX = GetEffectiveStat("DEX");

            if (weapon.useAmmo > 0 && effectiveDEX > 0)
            {
                float chance = effectiveDEX * (config.statSettings.DEX_AmmoConservation / 100f);
                if (Main.rand.NextFloat() < chance)
                    return false;
            }

            if (config.modIntegration.EnableCalamityIntegration && CalamitySupportHelper.CalamityLoaded)
            {
                if (CalamitySupportHelper.IsRogueWeapon(weapon))
                {
                    int effectiveRGE = GetEffectiveStat("RGE");
                    if (effectiveRGE > 0)
                    {
                        float chance = effectiveRGE * (config.modIntegration.RGE_AmmoConsumptionReduction / 100f);
                        if (Main.rand.NextFloat() < chance)
                            return false;
                    }
                }
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
            packet.Write(BLH);
            packet.Write(HNT);
            packet.Write(GMB);
            packet.Write(SHM);
            packet.Write(THR);
            packet.Write(PST);
            packet.Write(lastStandCooldownTimer);
            packet.Write(divineInterventionCooldownTimer);
            packet.Write(RebirthCount);
            packet.Write(RebirthPoints);
            packet.Write(AscendedRoles.Count);
            foreach (string roleId in AscendedRoles)
            {
                packet.Write(roleId);
            }

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

            if (_activeRole != null)
            {
                packet.Write(true);
                packet.Write(_activeRole.ID);
                packet.Write((byte)_activeRole.Status);
            }
            else
            {
                packet.Write(false);
            }
            packet.Write(RoleSwitchCount);
            packet.Write(BossKillsCount);

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