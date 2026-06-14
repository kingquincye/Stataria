using Terraria.ModLoader.Config;
using System.ComponentModel;
using System.Collections.Generic;

namespace Stataria
{
    public class StatariaConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ServerSide;

        [SeparatePage]
        public GeneralBalance generalBalance { get; set; } = new GeneralBalance();

        [SeparatePage]
        public XPVerification xpVerification { get; set; } = new XPVerification();

        [SeparatePage]
        public SocketingSystem socketingSystem { get; set; } = new SocketingSystem();

        [SeparatePage]
        public RebirthSystem rebirthSystem { get; set; } = new RebirthSystem();

        [SeparatePage]
        public RebirthAbilities rebirthAbilities { get; set; } = new RebirthAbilities();


        [SeparatePage]
        public RoleSettings roleSettings { get; set; } = new RoleSettings();

        [SeparatePage]
        public MultiplayerSettings multiplayerSettings { get; set; } = new MultiplayerSettings();

        [SeparatePage]
        public EnemyScaling enemyScaling { get; set; } = new EnemyScaling();

        [SeparatePage]
        public StatSettings statSettings { get; set; } = new StatSettings();

        [SeparatePage]
        public ModIntegration modIntegration { get; set; } = new ModIntegration();

        [SeparatePage]
        public ExtraLuckSettings extraLuckSettings { get; set; } = new ExtraLuckSettings();

        [SeparatePage]
        public Advanced advanced { get; set; } = new Advanced();

        public class GeneralBalance
        {
            [Header("General_Balance")]
            [DefaultValue(true)] public bool EnableBossHPXP { get; set; } = true;
            [DefaultValue(true)] public bool BonusBossXPIsUnique { get; set; } = true;
            [DefaultValue(false)] public bool UseFlatBossXP { get; set; } = false;
            [Range(0, 50000000)][DefaultValue(5000)] public int DefaultFlatBossXP { get; set; } = 5000;
            [DefaultValue(false)] public bool EnableLevelCap { get; set; } = false;
            [Range(1, 100000000)][DefaultValue(50)] public int LevelCapValue { get; set; } = 50;
            [DefaultValue(true)] public bool EnableStatPointRecalculation { get; set; } = true;
            [Range(1, 1000)][DefaultValue(2)] public int StatPointsPerLevel { get; set; } = 2;
            [DefaultValue(true)] public bool EnableStatResetting { get; set; } = true;
            [DefaultValue(true)] public bool EnableSkillResetting { get; set; } = true;


            [Header("Diminishing_Returns")]
            [DefaultValue(false)] public bool EnableDiminishingReturns { get; set; } = false;
            [Increment(0.001f)][Range(0.001f, 1f)][DefaultValue(0.01f)] public float DiminishingReturnsRate { get; set; } = 0.01f;


            [Header("XP_Multipliers")]
            [Range(1f, 10f)][DefaultValue(2f)][SliderColor(150, 0, 150)] public float LevelScalingFactor { get; set; } = 2f;
            [DefaultValue(false)] public bool EnableXPCurve { get; set; } = false;
            [Range(1f, 5f)][DefaultValue(1.5f)][SliderColor(150, 0, 150)] public float XPCurveSteepness { get; set; } = 1.5f;

            [Range(0f, 10f)][DefaultValue(0.25f)][SliderColor(150, 0, 150)] public float DamageXP { get; set; } = 0.25f;
            [Range(0f, 10f)][DefaultValue(0.5f)][SliderColor(150, 0, 150)] public float KillXP { get; set; } = 0.5f;
            [Range(0, 1000)][DefaultValue(25)] public int BossXP { get; set; } = 25;

            [Header("Damage_Calculation")]
            [DefaultValue(false)] public bool UseMultiplicativeDamage { get; set; } = false;
        }

        public class XPVerification
        {
            [Header("XP_Verification")]
            [DefaultValue(true)] public bool EnableXPVerification { get; set; } = true;
            [Range(1000, 10000000)][DefaultValue(100000)] public int BaseXPThreshold { get; set; } = 100000;
            [Range(0.01f, 10f)][DefaultValue(0.5f)] public float LevelScalingFactor { get; set; } = 0.5f;
            [Range(0f, 10f)][DefaultValue(1f)] public float RebirthScalingFactor { get; set; } = 1f;
            [Range(0.1f, 5f)][DefaultValue(1.5f)] public float RelativeXPThreshold { get; set; } = 1.5f;
            [Range(0f, 0.5f)][DefaultValue(0.1f)] public float RebirthRelativeThresholdReduction { get; set; } = 0.1f;
            public List<string> WhitelistedSources { get; set; } = new List<string>{"Boss Bonus", "Boss HP"};
        }

        public class SocketingSystem
        {
            [Header("Socketing_System")]
            [DefaultValue(true)] public bool EnableSocketingSystem { get; set; } = true;

            [Header("Core_Effects")]
            [Range(0f, 100f)][DefaultValue(5f)] public float PowerT1Effect { get; set; } = 5f;
            [Range(0f, 100f)][DefaultValue(10f)] public float PowerT2Effect { get; set; } = 10f;
            [Range(0f, 100f)][DefaultValue(25f)] public float PowerT3Effect { get; set; } = 25f;
            [Range(0f, 200f)][DefaultValue(50f)] public float PowerT4Effect { get; set; } = 50f;
            [Range(0f, 100f)][DefaultValue(5f)] public float ForceT1Effect { get; set; } = 5f;
            [Range(0f, 100f)][DefaultValue(10f)] public float ForceT2Effect { get; set; } = 10f;
            [Range(0f, 100f)][DefaultValue(25f)] public float ForceT3Effect { get; set; } = 25f;
            [Range(0f, 200f)][DefaultValue(50f)] public float ForceT4Effect { get; set; } = 50f;
            [Range(0f, 100f)][DefaultValue(5f)] public float PrecisionT1Effect { get; set; } = 5f;
            [Range(0f, 100f)][DefaultValue(10f)] public float PrecisionT2Effect { get; set; } = 10f;
            [Range(0f, 100f)][DefaultValue(25f)] public float PrecisionT3Effect { get; set; } = 25f;
            [Range(0f, 200f)][DefaultValue(50f)] public float PrecisionT4Effect { get; set; } = 50f;
            [Range(0f, 100f)][DefaultValue(2f)] public float DefenseT1Effect { get; set; } = 2f;
            [Range(0f, 100f)][DefaultValue(4f)] public float DefenseT2Effect { get; set; } = 4f;
            [Range(0f, 100f)][DefaultValue(8f)] public float DefenseT3Effect { get; set; } = 8f;
            [Range(0f, 100f)][DefaultValue(15f)] public float DefenseT4Effect { get; set; } = 15f;
            [Range(0f, 200f)][DefaultValue(10f)] public float VitalityT1Effect { get; set; } = 10f;
            [Range(0f, 200f)][DefaultValue(20f)] public float VitalityT2Effect { get; set; } = 20f;
            [Range(0f, 500f)][DefaultValue(40f)] public float VitalityT3Effect { get; set; } = 40f;
            [Range(0f, 1000f)][DefaultValue(100f)] public float VitalityT4Effect { get; set; } = 100f;
            [Range(0f, 100f)][DefaultValue(1f)] public float EvasionT1Effect { get; set; } = 1f;
            [Range(0f, 100f)][DefaultValue(3f)] public float EvasionT2Effect { get; set; } = 3f;
            [Range(0f, 100f)][DefaultValue(5f)] public float EvasionT3Effect { get; set; } = 5f;
            [Range(0f, 100f)][DefaultValue(10f)] public float EvasionT4Effect { get; set; } = 10f;

            [Header("Cost_Configuration")]
            [Range(0, 1000)][DefaultValue(10)] public int ExtractCost { get; set; } = 10;
            [Range(0, 1000)][DefaultValue(20)] public int BaseExpandCost { get; set; } = 20;
            [Range(0f, 500f)][DefaultValue(50f)] public float ExpandCostIncrease { get; set; } = 50f;
            [Range(0, 50)][DefaultValue(10)] public int MaxExpandedSlots { get; set; } = 10;
        }

        public class RebirthSystem
        {
            [Header("Rebirth_System")]
            [DefaultValue(true)] public bool EnableRebirthSystem { get; set; } = true;
            [DefaultValue(true)] public bool EnableRebirthAbilities { get; set; } = true;
            [Range(1, 10000)][DefaultValue(50)] public int RebirthLevelRequirement { get; set; } = 50;
            [Range(0f, 10f)][DefaultValue(0.25f)] public float RebirthXPMultiplier { get; set; } = 0.25f;
            [DefaultValue(true)] public bool ResetStatsOnRebirth { get; set; } = true;
            [DefaultValue(true)] public bool ResetBossRewardsOnRebirth { get; set; } = true;
            [DefaultValue(false)] public bool EnableDynamicRebirthLevelCap { get; set; } = false;
            [Range(1.1f, 10f)][DefaultValue(2f)][Increment(0.1f)] public float DynamicRebirthLevelCapMultiplier { get; set; } = 2f;
            [DefaultValue(false)] public bool EnableRebirthBonusStatPoints { get; set; } = false;
            [Range(0.1f, 10f)][DefaultValue(0.25f)] public float RebirthStatPointsMultiplier { get; set; } = 0.25f;
            [DefaultValue(true)] public bool EnableRebirthStatPointRecalculation { get; set; } = true;
            [DefaultValue(false)] public bool EnableProgressiveStatCaps { get; set; } = false;
            [Range(0.1f, 10f)][DefaultValue(0.5f)] public float ProgressiveStatCapMultiplier { get; set; } = 0.5f;

            [Header("Rebirth_Points")]
            [DefaultValue(true)] public bool EnableRebirthPointRecalculation { get; set; } = true;
            [Range(0.1f, 10f)][DefaultValue(0.5f)] public float RebirthPointsMultiplier { get; set; } = 0.5f;
            [DefaultValue(true)] public bool BonusPointsForExcessLevels { get; set; } = true;
            [Range(0.1f, 10f)][DefaultValue(0.25f)] public float ExcessLevelPointMultiplier { get; set; } = 0.25f;
            [DefaultValue(true)] public bool IncreaseLevelRequirement { get; set; } = true;
            [Range(1, 1000)][DefaultValue(50)] public int AdditionalLevelRequirementPerRebirth { get; set; } = 50;

            [Header("Ghost_Stats")]
            [DefaultValue(true)] public bool EnableGhostStats { get; set; } = true;
            [DefaultValue(false)] public bool UsePercentageGhostStats { get; set; } = false;
            [Range(0.1f, 10f)][DefaultValue(0.5f)] public float GhostStatsPercentage { get; set; } = 0.5f;
            [Range(1, 1000)][DefaultValue(10)] public int GhostStatsFlatAmount { get; set; } = 10;
            public List<string> GhostStatsAffectedStats { get; set; } = new List<string> {"VIT", "END"};
        }

        public class RebirthAbilities
        {
            [Header("Last_Stand")]
            [Range(0f, 100f)][DefaultValue(10f)] public float LastStandHealPercent { get; set; } = 10f;
            [Range(0, 10)][DefaultValue(3)] public int LastStandImmunityTime { get; set; } = 3;
            [Range(0, 300)][DefaultValue(180)] public int LastStandCooldown { get; set; } = 180;

            [Header("Teleport")]
            [Range(1, 60)][DefaultValue(3)] public int TeleportCooldown { get; set; } = 3;

            [Header("Extra_Accessory_Slots")]
            [Range(1, 29)][DefaultValue(5)] public int MaxExtraAccessorySlots { get; set; } = 5;

            [Header("Golden_Touch")]
            [Range(1, 1000)][DefaultValue(5)] public int MaxGoldenTouchLevel { get; set; } = 5;
            [Range(10, 10000)][DefaultValue(100)] public int GoldenTouchPercentPerLevel { get; set; } = 100;

            [Header("Enhanced_Spawns")]
            [Range(1, 1000)][DefaultValue(5)] public int MaxEnhancedSpawnsLevel { get; set; } = 5;
            [Range(10, 1000)][DefaultValue(100)] public int SpawnRatePercentPerLevel { get; set; } = 100;

            [Header("Auto_Clicker")]
            [DefaultValue(5)][Range(1, 100)] public int AutoClickerMaxLevel { get; set; } = 5;
            [DefaultValue(40f)][Range(2f, 120f)] public float AutoClickerSpeedFactorAtLevel1 { get; set; } = 40f;
            [DefaultValue(-7f)][Range(-20f, 0f)] public float AutoClickerSpeedFactorImprovementPerLevel { get; set; } = -7f;
            [DefaultValue(false)] public bool AutoClickerPreventsEffects { get; set; } = false;

            [Header("Enhanced_Fortune")]
            [DefaultValue(0.1f)][Range(0f, 10f)] public float LuckPerAbilityLevel { get; set; } = 0.1f;
            [Range(1, 50)][DefaultValue(10)] public int MaxEnhancedFortuneLevel { get; set; } = 10;
        }



        public class RoleSettings
        {
            [Header("Role_System")]
            [DefaultValue(true)] public bool EnableRoleSystem { get; set; } = true;
            [DefaultValue(50)][Range(0, 1000)] public int BaseSwitchCost { get; set; } = 50;
            [DefaultValue(1f)][Range(0f, 5f)] public float SwitchCostMultiplier { get; set; } = 1f;
            [DefaultValue(true)] public bool EnableRoleProximity { get; set; } = true;
            [Range(500, 10000)][DefaultValue(1000)] public int RoleProximityRange { get; set; } = 1000;

            [Header("Crit_God")]
            [DefaultValue(25f)][Range(0f, 200f)] public float CritGodCritChance { get; set; } = 25f;
            [DefaultValue(1f)][Range(0f, 10f)] public float CritGodExcessCritToDamage { get; set; } = 1f;
            [DefaultValue(true)] public bool CritGodEnableSummonCrits { get; set; } = true;

            [Header("Vampire")]
            [DefaultValue(false)] public bool VampireEnableEyeColorChange { get; set; } = false;
            [DefaultValue(15f)][Range(0f, 200f)] public float VampireHealthBonus { get; set; } = 15f;
            [DefaultValue(10f)][Range(0f, 100f)] public float VampireMovementSpeed { get; set; } = 10f;
            [DefaultValue(5f)][Range(0f, 100f)] public float VampireBleedChance { get; set; } = 5f;
            [DefaultValue(1f)][Range(0.1f, 50f)] public float VampireBleedDamagePercent { get; set; } = 1f;
            [DefaultValue(2f)][Range(1f, 30f)] public float VampireBleedDuration { get; set; } = 2f;
            [DefaultValue(0.4f)][Range(0.1f, 5f)] public float VampireBleedTickInterval { get; set; } = 0.4f;
            [DefaultValue(10f)][Range(0f, 100f)] public float VampireBleedHealPercent { get; set; } = 10f;
            [DefaultValue(5f)][Range(0f, 50f)] public float VampireKillHealPercent { get; set; } = 5f;
            [DefaultValue(3f)][Range(0f, 60f)] public float VampireBleedCooldown { get; set; } = 3f;

            [Header("Beastmaster")]
            [DefaultValue(15f)][Range(0f, 100f)] public float BeastmasterDamagePerUniqueMinion { get; set; } = 15f;
            [DefaultValue(3)][Range(1, 10)] public int BeastmasterSlotsPerBonusSlot { get; set; } = 3;
            [DefaultValue(1)][Range(1, 5)] public int BeastmasterBonusSlotsGained { get; set; } = 1;
            [DefaultValue(true)] public bool BeastmasterReduceSPRSlotEfficiency { get; set; } = true;
            [DefaultValue(2f)][Range(1f, 10f)] public float BeastmasterSPRSlotPenaltyMultiplier { get; set; } = 2f;

            [Header("Apex_Summoner")]
            [DefaultValue(20f)][Range(0f, 100f)] public float ApexSummonerDamagePerUnusedSlot { get; set; } = 20f;

            [Header("Black_Knight")]
            [DefaultValue(0.5f)][Range(0f, 10f)] public float BlackKnightINTToMeleeDamage { get; set; } = 0.5f;
            [DefaultValue(0.5f)][Range(0f, 10f)] public float BlackKnightSTRToMagicDamage { get; set; } = 0.5f;
            [DefaultValue(5)][Range(1, 20)] public int BlackKnightMaxDarkFocusStacks { get; set; } = 5;
            [DefaultValue(5f)][Range(0f, 50f)] public float BlackKnightDarkFocusCritChancePerStack { get; set; } = 5f;
            [DefaultValue(10f)][Range(0f, 100f)] public float BlackKnightDarkFocusCritDamagePerStack { get; set; } = 10f;
            [DefaultValue(10)][Range(1, 100)] public int BlackKnightManaRestoreOnMeleeCrit { get; set; } = 10;
            [DefaultValue(5f)][Range(1f, 30f)] public float BlackKnightArcaneSurgeDuration { get; set; } = 5f;
            [DefaultValue(20f)][Range(0f, 100f)] public float BlackKnightArcaneSurgeMagicDamage { get; set; } = 20f;
            [DefaultValue(false)] public bool BlackKnightArcaneSurgeScaleWithDamage { get; set; } = false;
            [DefaultValue(0.1f)][Range(0f, 1f)] public float BlackKnightArcaneSurgeDamageScaling { get; set; } = 0.1f;

            [Header("Cleric")]
            [Range(100f, 1000f)][DefaultValue(300f)] public float ClericAuraRadius { get; set; } = 300f;
            [Range(0f, 100f)][DefaultValue(20f)] public float ClericHealthBonus { get; set; } = 20f;
            [Range(0f, 90f)][DefaultValue(50f)] public float ClericDefensePenalty { get; set; } = 50f;
            [DefaultValue(true)] public bool ClericDisableVitRegen { get; set; } = true;
            [DefaultValue(false)] public bool ClericAllowAuraOnNoTeam { get; set; } = false;

            [Range(0f, 100f)][DefaultValue(15f)] public float ClericTeammateHealthBonus { get; set; } = 15f;
            [Range(0.1f, 10f)][DefaultValue(2f)] public float ClericSelfRegenPercent { get; set; } = 2f;
            [Range(0.1f, 10f)][DefaultValue(1f)] public float ClericTeammateRegenPercent { get; set; } = 1f;
            [Range(1f, 10f)][DefaultValue(3f)] public float ClericRegenInterval { get; set; } = 3f;
            [Range(1f, 30f)][DefaultValue(10f)] public float DivineInterventionDuration { get; set; } = 10f;
            [Range(30f, 600f)][DefaultValue(120f)] public float DivineInterventionCooldown { get; set; } = 120f;
            public List<string> DivineInterventionExemptBuffs { get; set; } = new List<string>();

            [Header("Angel")]
            [Range(1, 100)][DefaultValue(3)] public int AngelRebirthRequirement { get; set; } = 3;
            [Range(0, 1000)][DefaultValue(100)] public int AngelUnlockCost { get; set; } = 100;
            [Range(100f, 1000f)][DefaultValue(350f)] public float AngelAuraRadius { get; set; } = 350f;
            [Range(0f, 100f)][DefaultValue(30f)] public float AngelHealthBonus { get; set; } = 30f;
            [Range(0f, 90f)][DefaultValue(30f)] public float AngelDefensePenalty { get; set; } = 30f;
            [Range(0f, 100f)][DefaultValue(25f)] public float AngelTeammateHealthBonus { get; set; } = 25f;
            [Range(0.1f, 10f)][DefaultValue(3f)] public float AngelSelfRegenPercent { get; set; } = 3f;
            [Range(0.1f, 10f)][DefaultValue(1.5f)] public float AngelTeammateRegenPercent { get; set; } = 1.5f;
            [Range(1f, 10f)][DefaultValue(3f)] public float AngelRegenInterval { get; set; } = 3f;
            [Range(0f, 100f)][DefaultValue(25f)] public float AngelInAirMoveSpeedBonus { get; set; } = 25f;
            [Range(1f, 15f)][DefaultValue(5f)] public float AngelWingFlightTime { get; set; } = 5f;

            [Range(0f, 100f)][DefaultValue(20f)] public float AngelSoulAnchorDamageReduction { get; set; } = 20f;
            [Range(5f, 300f)][DefaultValue(30f)] public float AngelSpiritFormDuration { get; set; } = 30f;
            [Range(1f, 10f)][DefaultValue(3f)] public float AngelResurrectionChannelTime { get; set; } = 3f;
            [Range(1f, 100f)][DefaultValue(50f)] public float AngelResurrectionHealPercent { get; set; } = 50f;
            [Range(1f, 30f)][DefaultValue(3f)] public float AngelResurrectionInvulTime { get; set; } = 3f;
            [Range(10f, 600f)][DefaultValue(180f)] public float AngelResurrectionCooldown { get; set; } = 180f;

            [Header("Guardian")]
            [DefaultValue(300f)][Range(100f, 1000f)] public float GuardianAuraRadius { get; set; } = 300f;
            [DefaultValue(false)] public bool GuardianAllowAuraOnNoTeam { get; set; } = false;
            [DefaultValue(15f)][Range(0f, 100f)] public float GuardianTeammateDefenseBonus { get; set; } = 15f;
            [DefaultValue(20f)][Range(0f, 80f)] public float GuardianTeammateDamageReduction { get; set; } = 20f;
            [DefaultValue(25f)][Range(0f, 50f)] public float GuardianMovementSpeedPenalty { get; set; } = 25f;
            [DefaultValue(40f)][Range(0f, 75f)] public float GuardianDamageReduction { get; set; } = 40f;
            [DefaultValue(30f)][Range(0f, 200f)] public float GuardianHealthBonus { get; set; } = 30f;
            [DefaultValue(50f)][Range(0f, 100f)] public float GuardianDamagePenalty { get; set; } = 50f;
            [DefaultValue(25)][Range(0, 100)] public int GuardianDefenseBonus { get; set; } = 25;
            [DefaultValue(true)] public bool GuardianReduceVitEffects { get; set; } = true;
            [DefaultValue(50f)][Range(0f, 100f)] public float GuardianVitEffectReduction { get; set; } = 50f;
            [DefaultValue(true)] public bool GuardianDisableEndEffects { get; set; } = true;

            [Header("Necromancer")]
            [DefaultValue(5)][Range(1, 20)] public int NecromancerBaseSoulCapacity { get; set; } = 5;
            [DefaultValue(20)][Range(5, 100)] public int NecromancerSPRPerSoul { get; set; } = 20;
            [DefaultValue(30f)][Range(5f, 300f)] public float NecromancerBaseSoulDuration { get; set; } = 30f;
            [DefaultValue(0.5f)][Range(0.1f, 5f)] public float NecromancerSoulDurationPerSPR { get; set; } = 0.5f;
            [DefaultValue(10f)][Range(0f, 100f)] public float NecromancerBossSoulHarvestChance { get; set; } = 10f;
            [DefaultValue(true)] public bool NecromancerLimitZombieThralls { get; set; } = true;
            [DefaultValue(3)][Range(1, 1000)] public int NecromancerActiveThrallsLimit { get; set; } = 3;
            [DefaultValue(3)][Range(1, 100)] public int NecromancerBaseThralls { get; set; } = 3;
            [DefaultValue(10)][Range(1, 100)] public int NecromancerSPRPerThrall { get; set; } = 10;
            [DefaultValue(3f)][Range(0f, 10f)] public float NecromancerBoneArmorDRPerThrall { get; set; } = 3f;
            [DefaultValue(20)][Range(5, 500)] public int NecromancerThrallBaseDamage { get; set; } = 20;
            [DefaultValue(1.5f)][Range(0f, 10f)] public float NecromancerThrallSPRScale { get; set; } = 1.5f;
            [DefaultValue(10f)][Range(0f, 100f)] public float NecromancerThrallDamageIncreasePerRebirth { get; set; } = 10f;
            [DefaultValue(1f)][Range(0f, 100f)] public float NecromancerThrallDamageIncreasePerLevel { get; set; } = 1f;
            public List<string> NecromancerThrallBlacklistedNPCs { get; set; } = new List<string>();


            [Header("Berserker")]
            [DefaultValue(50f)][Range(0f, 200f)] public float BerserkerBloodbathMaxDamageBonus { get; set; } = 50f;
            [DefaultValue(30f)][Range(0f, 200f)] public float BerserkerBloodbathMaxSpeedBonus { get; set; } = 30f;
            [DefaultValue(40f)][Range(0f, 100f)] public float BerserkerBloodbathImmunityThreshold { get; set; } = 40f;
            [DefaultValue(5f)][Range(1f, 30f)] public float BerserkerSavageRoarDuration { get; set; } = 5f;
            [DefaultValue(60f)][Range(10f, 300f)] public float BerserkerSavageRoarCooldown { get; set; } = 60f;

            [Header("Spellweaver")]
            [DefaultValue(30f)][Range(0f, 100f)] public float SpellweaverManaAegisPercent { get; set; } = 30f;
            [DefaultValue(2f)][Range(0f, 50f)] public float SpellweaverManaCritRestorePercent { get; set; } = 2f;
            [DefaultValue(100f)][Range(100f, 2000f)] public float SpellweaverMaxElementalCharge { get; set; } = 100f;
            [DefaultValue(10f)][Range(1f, 50f)] public float SpellweaverElementalDischargeBaseMult { get; set; } = 10f;
            [DefaultValue(4f)][Range(0f, 50f)] public float SpellweaverElementalDischargeINTScale { get; set; } = 4f;

            [Header("Shinobi")]
            [DefaultValue(15f)][Range(0f, 100f)] public float ShinobiExecutionHealPercent { get; set; } = 15f;
            [DefaultValue(30f)][Range(1f, 300f)] public float ShinobiMortalDrawCooldown { get; set; } = 30f;
            [DefaultValue(1200f)][Range(100f, 5000f)] public float ShinobiMortalDrawRange { get; set; } = 1200f;
        }

        public class MultiplayerSettings
        {
            [Header("Multiplayer_Settings")]
            [DefaultValue(false)] public bool AllowSelfResetInMultiplayer { get; set; } = false;
            [DefaultValue(false)] public bool SplitKillXP { get; set; } = false;
            [DefaultValue(true)] public bool EnableXPProximity { get; set; } = true;
            [Range(500, 10000)][DefaultValue(1000)] public int XPProximityRange { get; set; } = 1000;
            public List<string> AdminSteamIDs { get; set; } = new List<string>();
        }

        public class EnemyScaling
        {
            [Header("Enemy_Scaling")]
            [DefaultValue(true)] public bool EnableEnemyScaling { get; set; } = true;
            [Range(0f, 5f)][DefaultValue(0.10f)] public float EnemyHealthScaling { get; set; } = 0.10f;
            [Range(0f, 5f)][DefaultValue(0.05f)] public float EnemyDamageScaling { get; set; } = 0.05f;
            [DefaultValue(true)] public bool EnableDefenseCap { get; set; } = true;
            [Range(1, 100)][DefaultValue(3)] public int MaxDefenseMultiplier { get; set; } = 3;
            [Range(0f, 5f)][DefaultValue(0.02f)] public float EnemyDefenseScaling { get; set; } = 0.02f;

            [Header("Boss_Scaling")]
            [DefaultValue(true)] public bool EnableBossScaling { get; set; } = true;
            [Range(0f, 10f)][DefaultValue(0.05f)] public float BossHealthScaling { get; set; } = 0.05f;
            [Range(0f, 10f)][DefaultValue(0.02f)] public float BossDamageScaling { get; set; } = 0.02f;

            [Header("Flat_Scaling")]
            [DefaultValue(false)] public bool EnableFlatEnemyScaling { get; set; } = false;
            [Range(0, 5000)][DefaultValue(10)] public int FlatEnemyHealthScaling { get; set; } = 10;
            [Range(0, 5000)][DefaultValue(2)] public int FlatEnemyDamageScaling { get; set; } = 2;
            [Range(0, 50000)][DefaultValue(100)] public int FlatBossHealthScaling { get; set; } = 100;
            [Range(0, 5000)][DefaultValue(10)] public int FlatBossDamageScaling { get; set; } = 10;

            [Header("Level_Variation")]
            [DefaultValue(true)] public bool EnableLevelVariation { get; set; } = true;
            [Range(1, 100)][DefaultValue(10)] public int MaxLevelVariation { get; set; } = 10;
            [DefaultValue(false)] public bool EnableMinimumLevelDifference { get; set; } = false;
            [Range(1, 100)][DefaultValue(25)] public int MinimumLevelDifference { get; set; } = 25;

            [Header("Multiplayer_Scaling")]
            [Range(0, 2)][DefaultValue(1)][Slider][SliderColor(150, 0, 150)][Increment(1)][DrawTicks] public int ScalingType { get; set; } = 1;
            [Range(1, 1000)][DefaultValue(5)] public int LevelsPerPlayer { get; set; } = 5;
            [DefaultValue(true)] public bool UseProximityForScaling { get; set; } = true;
            [Range(500, 10000)][DefaultValue(4000)] public int ScalingProximityRange { get; set; } = 4000;

            [Header("Elite_Enemies")]
            [DefaultValue(true)] public bool EnableEliteEnemies { get; set; } = true;
            [Range(0.01f, 1f)][DefaultValue(0.05f)] public float EliteEnemyChance { get; set; } = 0.05f;
            [Range(0f, 10f)][DefaultValue(1.5f)] public float EliteHealthMultiplier { get; set; } = 1.5f;
            [Range(0f, 10f)][DefaultValue(1.25f)] public float EliteDamageMultiplier { get; set; } = 1.25f;
            [Range(0f, 10f)][DefaultValue(1.15f)] public float EliteDefenseMultiplier { get; set; } = 1.15f;
            [DefaultValue(1f)] public float EliteKnockbackResistance { get; set; } = 1f;
            [DefaultValue(0.50f)] public float EliteCriticalHitResistance { get; set; } = 0.50f;
            [DefaultValue(true)] public bool EliteScaleIncrease { get; set; } = true;
            [Range(1f, 2f)][DefaultValue(1.15f)] public float EliteScaleMultiplier { get; set; } = 1.15f;
        }

        public class StatSettings
        {
            [Header("Stat_Caps")]
            [DefaultValue(false)] public bool EnableStatCaps { get; set; } = false;
            [Range(-1, 10000)][DefaultValue(1000)] public int VIT_Cap { get; set; } = 1000;
            [Range(-1, 10000)][DefaultValue(1000)] public int STR_Cap { get; set; } = 1000;
            [Range(-1, 10000)][DefaultValue(1000)] public int AGI_Cap { get; set; } = 1000;
            [Range(-1, 10000)][DefaultValue(1000)] public int INT_Cap { get; set; } = 1000;
            [Range(-1, 10000)][DefaultValue(1000)] public int LUC_Cap { get; set; } = 1000;
            [Range(-1, 10000)][DefaultValue(1000)] public int END_Cap { get; set; } = 1000;
            [Range(-1, 10000)][DefaultValue(1000)] public int POW_Cap { get; set; } = 1000;
            [Range(-1, 10000)][DefaultValue(1000)] public int DEX_Cap { get; set; } = 1000;
            [Range(-1, 10000)][DefaultValue(1000)] public int SPR_Cap { get; set; } = 1000;
            [Range(-1, 10000)][DefaultValue(1000)] public int TCH_Cap { get; set; } = 1000;
            [Range(-1, 10000)][DefaultValue(1000)] public int RGE_Cap { get; set; } = 1000;
            [Range(-1, 10000)][DefaultValue(1000)] public int BRD_Cap { get; set; } = 1000;
            [Range(-1, 10000)][DefaultValue(1000)] public int HLR_Cap { get; set; } = 1000;
            [Range(-1, 10000)][DefaultValue(1000)] public int CLK_Cap { get; set; } = 1000;
            [Range(-1, 10000)][DefaultValue(1000)] public int BLH_Cap { get; set; } = 1000;
            [Range(-1, 10000)][DefaultValue(1000)] public int HNT_Cap { get; set; } = 1000;
            [Range(-1, 10000)][DefaultValue(1000)] public int GMB_Cap { get; set; } = 1000;
            [Range(-1, 10000)][DefaultValue(1000)] public int SHM_Cap { get; set; } = 1000;
            [Range(-1, 10000)][DefaultValue(1000)] public int THR_Cap { get; set; } = 1000;
            [Range(-1, 10000)][DefaultValue(1000)] public int PST_Cap { get; set; } = 1000;

            [Header("VIT_Settings")]
            [Increment(0.1f)][Range(0f, 1000f)][DefaultValue(2f)] public float VIT_HP { get; set; } = 2f;
            [DefaultValue(false)] public bool UseCustomHpRegen { get; set; } = false;
            [Range(0f, 10f)][DefaultValue(0.25f)] public float CustomHpRegenPerVIT { get; set; } = 0.25f;
            [Range(0, 10)][DefaultValue(3)] public int CustomHpRegenDelay { get; set; } = 3;
            [DefaultValue(false)] public bool EnableHealingPotionBoost { get; set; } = false;
            [Range(0f, 10f)][DefaultValue(0.5f)] public float HealingPotionBoostPercent { get; set; } = 0.5f;

            [Header("STR_Settings")]
            [Increment(0.01f)][Range(0f, 10f)][DefaultValue(0.5f)] public float STR_Damage { get; set; } = 0.5f;
            [Increment(0.05f)][Range(0f, 10f)][DefaultValue(0.5f)] public float STR_Knockback { get; set; } = 0.5f;
            [Increment(0.05f)][DefaultValue(1f)] public float STR_ArmorPen { get; set; } = 1f;

            [Header("AGI_Settings")]
            [Increment(0.01f)][Range(0f, 10f)][DefaultValue(1f)] public float AGI_MoveSpeed { get; set; } = 1f;
            [Increment(0.01f)][Range(0f, 10f)][DefaultValue(0.5f)] public float AGI_AttackSpeed { get; set; } = 0.5f;
            [Increment(0.01f)][Range(0f, 2f)][DefaultValue(0.25f)] public float AGI_JumpHeight { get; set; } = 0.25f;
            [Increment(0.01f)][Range(0f, 1f)][DefaultValue(0.1f)] public float AGI_JumpSpeed { get; set; } = 0.1f;
            [Increment(0.01f)][DefaultValue(1f)] public float AGI_WingTime { get; set; } = 1f;

            [Header("INT_Settings")]
            [Increment(0.01f)][Range(0f, 10f)][DefaultValue(0.5f)] public float INT_Damage { get; set; } = 0.5f;
            [Increment(0.01f)][Range(0f, 1000f)][DefaultValue(2f)] public float INT_MP { get; set; } = 2f;
            [Increment(0.01f)][Range(0f, 10f)][DefaultValue(1.2f)] public float INT_ManaCostReduction { get; set; } = 1.2f;
            [Increment(0.01f)][DefaultValue(1f)] public float INT_ArmorPen { get; set; } = 1f;

            [Header("LUC_Settings")]
            [Increment(0.01f)][Range(0f, 10f)][DefaultValue(0.5f)] public float LUC_Crit { get; set; } = 0.5f;
            [DefaultValue(true)] public bool LUC_EnableFishing { get; set; } = true;
            [Increment(0.05f)][DefaultValue(1f)] public float LUC_Fishing { get; set; } = 1f;
            [Increment(0.5f)][DefaultValue(5f)] public float LUC_AggroReduction { get; set; } = 5f;
            [DefaultValue(true)] public bool LUC_EnableLuckBonus { get; set; } = true;
            [Range(-1f, 1f)][DefaultValue(0.01f)] public float LUC_LuckBonus { get; set; } = 0.01f;

            [Header("END_Settings")]
            [Increment(0.05f)][DefaultValue(0.2f)][Range(0f, 10f)] public float END_Defense { get; set; } = 0.2f;
            [Increment(0.5f)][DefaultValue(10f)] public float END_Aggro { get; set; } = 10f;
            [DefaultValue(false)] public bool EnableKnockbackResist { get; set; } = false;
            [Range(0f, 10f)][DefaultValue(1f)] public float END_KnockbackResistPerPoint { get; set; } = 1f;
            [DefaultValue(false)] public bool EnableDR { get; set; } = false;
            [Range(0f, 10f)][DefaultValue(1f)] public float END_DRPerPoint { get; set; } = 1f;
            [DefaultValue(false)] public bool EnableEnemyKnockback { get; set; } = false;
            [DefaultValue(0.1f)] public float END_EnemyKnockbackMultiplier { get; set; } = 0.1f;

            [Header("POW_Settings")]
            [Range(0f, 10f)][DefaultValue(0.5f)] public float POW_Damage { get; set; } = 0.5f;

            [Header("DEX_Settings")]
            [Increment(0.01f)][Range(0f, 10f)][DefaultValue(0.5f)] public float DEX_Damage { get; set; } = 0.5f;
            [Increment(0.01f)][DefaultValue(1f)] public float DEX_ArmorPen { get; set; } = 1f;
            [Increment(0.01f)][Range(0f, 10f)][DefaultValue(0.5f)] public float DEX_AmmoConservation { get; set; } = 0.5f;

            [Header("TCH_Settings")]
            [DefaultValue(true)] public bool TCH_EnableMiningSpeed { get; set; } = true;
            [DefaultValue(1f)] public float TCH_MiningSpeed { get; set; } = 1f;
            [DefaultValue(true)] public bool TCH_EnableBuildSpeed { get; set; } = true;
            [DefaultValue(1f)] public float TCH_BuildSpeed { get; set; } = 1f;
            [DefaultValue(true)] public bool TCH_EnableRange { get; set; } = true;
            [DefaultValue(0.2f)] public float TCH_Range { get; set; } = 0.2f;

            [Header("SPR_Settings")]
            [Range(0f, 10f)][DefaultValue(0.5f)] public float SPR_Damage { get; set; } = 0.5f;
            [Range(1, 100)][DefaultValue(25)] public int SPR_MinionsPerX { get; set; } = 25;
            [Range(1, 100)][DefaultValue(50)] public int SPR_SentriesPerX { get; set; } = 50;
        }

        public class ModIntegration
        {
            [Header("Mod_Integration")]
            [DefaultValue(true)] public bool EnableCalamityIntegration { get; set; } = true;
            [DefaultValue(true)] public bool EnableThoriumIntegration { get; set; } = true;
            [DefaultValue(true)] public bool EnableClickerClassIntegration { get; set; } = true;
            [DefaultValue(true)] public bool EnableGenericModIntegration { get; set; } = true;
            [DefaultValue(true)] public bool EnableSekirariaIntegration { get; set; } = true;

            [Header("RGE_Settings")]
            [Range(0f, 10f)][DefaultValue(0.5f)] public float RGE_Damage { get; set; } = 0.5f;
            [Range(0f, 10f)][DefaultValue(0.5f)] public float RGE_MaxStealthPerPoint { get; set; } = 0.5f;
            [Range(0f, 10f)][DefaultValue(1.5f)] public float RGE_Velocity { get; set; } = 1.5f;
            [Range(0f, 10f)][DefaultValue(0.5f)] public float RGE_AmmoConsumptionReduction { get; set; } = 0.5f;
            [DefaultValue(false)] public bool RGE_EnableStealthConsumptionReduction { get; set; } = false;
            [DefaultValue(25)] public int RGE_StealthConsumption90Threshold { get; set; } = 25;
            [DefaultValue(50)] public int RGE_StealthConsumption75Threshold { get; set; } = 50;
            [DefaultValue(75)] public int RGE_StealthConsumptionReductionThreshold { get; set; } = 75;
            [Range(0f, 10f)][DefaultValue(0.5f)] public float RGE_StealthRegenBonus { get; set; } = 0.5f;

            [Header("POW_CalamityEnhancements")]
            [Range(0f, 10f)][DefaultValue(0.5f)] public float POW_RageDamage { get; set; } = 0.5f;
            [DefaultValue(5)] public int POW_RageDuration { get; set; } = 5;
            [Range(0, 100000)][DefaultValue(2000)] public int POW_MaxRageDurationBonus { get; set; } = 2000;
            [DefaultValue(2)] public int POW_AdrenalineDuration { get; set; } = 2;

            [Header("BRD_Settings")]
            [Range(0f, 10f)][DefaultValue(0.5f)] public float BRD_Damage { get; set; } = 0.5f;
            [DefaultValue(5)] public int BRD_PointsPerMaxInspiration { get; set; } = 5;
            [DefaultValue(1f)] public float BRD_ArmorPen { get; set; } = 1f;
            [DefaultValue(true)] public bool BRD_EnableEmpowermentBoost { get; set; } = true;
            [Range(0f, 10f)][DefaultValue(0.1f)] public float BRD_EmpowermentDuration { get; set; } = 0.1f;

            [Header("HLR_Settings")]
            [Range(0f, 10f)][DefaultValue(0.5f)] public float HLR_Damage { get; set; } = 0.5f;
            [DefaultValue(1f)] public float HLR_HealingPower { get; set; } = 1f;
            [DefaultValue(5)][Range(1, 1000)] public int HLR_PointsPerEffectPoint { get; set; } = 5;
            [DefaultValue(1f)] public float HLR_ArmorPen { get; set; } = 1f;

            [Header("CLK_Settings")]
            [Range(0f, 10f)][DefaultValue(0.5f)] public float CLK_Damage { get; set; } = 0.5f;
            [Range(0f, 10f)][DefaultValue(1f)] public float CLK_Radius { get; set; } = 1f;
            [Range(0f, 10f)][DefaultValue(2f)] public float CLK_EffectThreshold { get; set; } = 2f;

            [Header("BLH_Settings")]
            [Range(0f, 10f)][DefaultValue(0.5f)] public float BLH_Damage { get; set; } = 0.5f;

            [Header("HNT_Settings")]
            [Range(0f, 10f)][DefaultValue(0.5f)] public float HNT_Damage { get; set; } = 0.5f;

            [Header("GMB_Settings")]
            [Range(0f, 10f)][DefaultValue(0.5f)] public float GMB_Damage { get; set; } = 0.5f;

            [Header("SHM_Settings")]
            [Range(0f, 10f)][DefaultValue(0.5f)] public float SHM_Damage { get; set; } = 0.5f;

            [Header("THR_Settings")]
            [Range(0f, 10f)][DefaultValue(0.5f)] public float THR_Damage { get; set; } = 0.5f;

            [Header("PST_Settings")]
            [Range(0f, 100f)][DefaultValue(5f)] public float PST_MaxPosture { get; set; } = 5f;
            [Range(0f, 500f)][DefaultValue(20f)] public float PST_PostureDamage { get; set; } = 20f;
            [Range(0f, 0.1f)][DefaultValue(0.01f)] public float PST_BlockDamageReduction { get; set; } = 0.01f;
        }

        public class ExtraLuckSettings
        {
            [Header("Extra_Luck_System")]
            [DefaultValue(false)][ReloadRequired] public bool EnableExtraLuckSystem { get; set; } = false;
            [Range(-15f, 15f)][DefaultValue(2f)] public float BaseExtraLuck { get; set; } = 2f;
        }

        public class Advanced
        {
            [Header("XP_Blacklist")]
            public List<string> XPBlacklistedNPCs { get; set; } = new List<string>
            {
                "Target Dummy", "Scarecrow Dummy", "Super Dummy"
            };

            [Header("Scaling_Blacklist")]
            public List<string> ScalingBlacklistedNPCs { get; set; } = new List<string>
            {
                "Target Dummy", "Scarecrow Dummy", "Super Dummy"
            };
        }
    }

    public class StatariaClientConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ClientSide;

        [Header("General")]
        [DefaultValue(true)] public bool EnableLevelUpSound { get; set; } = true;
        [DefaultValue(true)] public bool ShowXPBarAbovePlayer { get; set; } = true;
        [DefaultValue(true)] public bool ShowXPGainPopups { get; set; } = true;
        [DefaultValue(true)] public bool ShowDamageXPPopups { get; set; } = true;
        [DefaultValue(true)] public bool ShowKillXPPopups { get; set; } = true;
        [DefaultValue(true)] public bool ShowBossXPPopups { get; set; } = true;
        [DefaultValue(true)] public bool ShowLevelIndicator { get; set; } = true;
        [DefaultValue(true)] public bool ShowRebirthTitle { get; set; } = true;
        [Range(0f, 2f)][DefaultValue(1f)] public float IndicatorOpacity { get; set; } = 1f;
        [DefaultValue(true)] public bool ShowEnemyLevelIndicator { get; set; } = true;
        [DefaultValue(true)] public bool ShowEnemyLevelBehindWalls { get; set; } = true;
        [Range(0f, 2f)][DefaultValue(1f)] public float EnemyIndicatorOpacity { get; set; } = 1f;
        [DefaultValue(true)] public bool ShowCustomNormalMobHPBar { get; set; } = true;
        [DefaultValue(true)] public bool CustomNormalMobHPBarHoverOnly { get; set; } = true;

        [Header("SocketingSystem")]
        [DefaultValue(true)] public bool ShowSocketedCoresInTooltip { get; set; } = true;

        [Header("HUD")]
        [DefaultValue(true)] public bool EnableRoleCooldownHUD { get; set; } = true;
        [DefaultValue(true)] public bool EnableNecromancerHUD { get; set; } = true;

        [Header("ResourceBars")]
        [DefaultValue(0.79f)][Range(0f, 0.95f)][Slider][SliderColor(150, 0, 150)] public float PositionXPercent { get; set; } = 0.79f;
        [DefaultValue(0.01f)][Range(0f, 0.95f)][Slider][SliderColor(150, 0, 150)] public float PositionYPercent { get; set; } = 0.01f;
        [DefaultValue(300)][Range(100, 500)] public int BarWidth { get; set; } = 300;
        [DefaultValue(20)][Range(10, 50)] public int BarHeight { get; set; } = 20;
        [DefaultValue(3)][Range(0, 20)] public int BarPadding { get; set; } = 3;
        [DefaultValue(false)] public bool StretchXPBarToBottom { get; set; } = false;

        [Header("BossBars")]
        [DefaultValue(1f)][Range(0.5f, 3f)] public float BossBarScale { get; set; } = 1f;
        [DefaultValue(true)] public bool ShowBossHealthText { get; set; } = true;
        [DefaultValue(true)] public bool ShowBossName { get; set; } = true;
        [DefaultValue(456)][Range(200, 800)] public int BossBarWidth { get; set; } = 456;
        [DefaultValue(50f)][Range(0f, 100f)] public float BossBarXOffsetPercent { get; set; } = 50f;
        [DefaultValue(96f)][Range(0f, 100f)] public float BossBarYOffsetPercent { get; set; } = 96f;
        [DefaultValue(4)][Range(1, 20)] public int MaxVisibleBossBars { get; set; } = 4;
        public List<int> MiniBossNPCIDs { get; set; } = new List<int>();
        public List<int> ForcedBossNPCIDs { get; set; } = new List<int>();
        public List<int> ExcludedBossNPCIDs { get; set; } = new List<int>();
    }
}