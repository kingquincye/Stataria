using Terraria.ModLoader.Config;
using System.ComponentModel;
using System.Collections.Generic;

namespace Stataria
{
    public class StatariaConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ServerSide;

        [SeparatePage]
        public GeneralBalance generalBalance = new GeneralBalance();

        [SeparatePage]
        public XPVerification xpVerification = new XPVerification();

        [SeparatePage]
        public UISettings uiSettings = new UISettings();

        [SeparatePage]
        public ResourceBarsConfig resourceBars = new ResourceBarsConfig();

        [SeparatePage]
        public RebirthSystem rebirthSystem = new RebirthSystem();

        [SeparatePage]
        public RebirthAbilities rebirthAbilities = new RebirthAbilities();

        [SeparatePage]
        public RoleSettings roleSettings = new RoleSettings();

        [SeparatePage]
        public MultiplayerSettings multiplayerSettings = new MultiplayerSettings();

        [SeparatePage]
        public EnemyScaling enemyScaling = new EnemyScaling();

        [SeparatePage]
        public StatSettings statSettings = new StatSettings();

        [SeparatePage]
        public ModIntegration modIntegration = new ModIntegration();

        [SeparatePage]
        public Advanced advanced = new Advanced();

        public class GeneralBalance
        {
            [Header("General_Balance")]
            [DefaultValue(true)] public bool EnableBossHPXP { get; set; } = true;
            [DefaultValue(true)] public bool BonusBossXPIsUnique { get; set; } = true;
            [DefaultValue(false)] public bool UseFlatBossXP { get; set; } = false;
            [Range(0, 50000000)][DefaultValue(5000)] public int DefaultFlatBossXP { get; set; } = 5000;
            [DefaultValue(false)] public bool EnableLevelCap { get; set; } = false;
            [Range(1, 100000000)][DefaultValue(50)] public int LevelCapValue { get; set; } = 50;
            [DefaultValue(false)] public bool EnableStatPointRecalculation { get; set; } = false;
            [Range(1, 1000)][DefaultValue(5)] public int StatPointsPerLevel { get; set; } = 5;

            [Header("XP_Multipliers")]
            [Range(1f, 10f)][DefaultValue(1.5f)][SliderColor(150, 0, 150)] public float LevelScalingFactor { get; set; } = 1.5f;
            [Range(0f, 10f)][DefaultValue(0.25f)][SliderColor(150, 0, 150)] public float DamageXP { get; set; } = 0.25f;
            [Range(0f, 10f)][DefaultValue(0.5f)][SliderColor(150, 0, 150)] public float KillXP { get; set; } = 0.5f;
            [Range(0, 1000)][DefaultValue(25)] public int BossXP { get; set; } = 25;

            [Header("Damage_Calculation")]
            [DefaultValue(true)] public bool UseMultiplicativeDamage { get; set; } = true;
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

            public List<string> WhitelistedSources { get; set; } = new List<string>
            {
                "Boss Bonus",
                "Boss HP"
            };
        }

        public class UISettings
        {
            [Header("UI_Settings")]
            [DefaultValue(true)] public bool ShowXPBarAbovePlayer { get; set; } = true;
            [DefaultValue(true)] public bool ShowXPGainPopups { get; set; } = true;
            [DefaultValue(true)] public bool ShowDamageXPPopups { get; set; } = true;
            [DefaultValue(true)] public bool ShowKillXPPopups { get; set; } = true;
            [DefaultValue(true)] public bool ShowBossXPPopups { get; set; } = true;
            [DefaultValue(true)] public bool ShowLevelIndicator { get; set; } = true;
            [DefaultValue(true)] public bool ShowRebirthTitle { get; set; } = true;
            [Range(0f, 2f)][DefaultValue(1f)] public float IndicatorOpacity { get; set; } = 1f;
        }

        public class ResourceBarsConfig
        {
            [Header("Resource_Bars_Settings")]
            [DefaultValue(0.79f)][Range(0f, 0.95f)][Slider][SliderColor(150, 0, 150)] public float PositionXPercent { get; set; } = 0.79f;
            [DefaultValue(0.01f)][Range(0f, 0.95f)][Slider][SliderColor(150, 0, 150)] public float PositionYPercent { get; set; } = 0.01f;
            [DefaultValue(300)][Range(100, 500)] public int BarWidth { get; set; } = 300;
            [DefaultValue(20)][Range(10, 50)] public int BarHeight { get; set; } = 20;
            [DefaultValue(3)][Range(0, 20)] public int BarPadding { get; set; } = 3;

            [Header("Boss_Bars")]
            [Range(50, 200)][DefaultValue(100)] public int BossBarScale { get; set; } = 100;
            [Range(1, 10)][DefaultValue(4)] public int MaxVisibleBossBars { get; set; } = 4;
            [DefaultValue(true)] public bool ShowBossHealthText { get; set; } = true;
            [DefaultValue(true)] public bool ShowBossName { get; set; } = true;
            [Range(300, 1000)][DefaultValue(600)] public int BossBarWidth { get; set; } = 600;
            [Range(0f, 0.95f)][DefaultValue(0.95f)][Slider][SliderColor(150, 0, 150)] public float BossBarYOffsetPercent { get; set; } = 0.95f;
        }

        public class RebirthSystem
        {
            [Header("Rebirth_System")]
            [DefaultValue(true)] public bool EnableRebirthSystem { get; set; } = true;
            [Range(1, 10000)][DefaultValue(50)] public int RebirthLevelRequirement { get; set; } = 50;
            [Range(0f, 10f)][DefaultValue(0.5f)] public float RebirthXPMultiplier { get; set; } = 0.5f;
            [DefaultValue(true)] public bool ResetStatsOnRebirth { get; set; } = true;
            [DefaultValue(true)] public bool ResetBossRewardsOnRebirth { get; set; } = true;
            [DefaultValue(false)] public bool EnableDynamicRebirthLevelCap { get; set; } = false;
            [Range(1.1f, 10f)][DefaultValue(2f)][Increment(0.1f)] public float DynamicRebirthLevelCapMultiplier { get; set; } = 2f;
            [DefaultValue(false)] public bool EnableRebirthBonusStatPoints { get; set; } = false;
            [Range(0.1f, 10f)][DefaultValue(0.5f)] public float RebirthStatPointsMultiplier { get; set; } = 0.25f;
            [DefaultValue(true)] public bool EnableRebirthStatPointRecalculation { get; set; } = true;
            [DefaultValue(false)] public bool EnableProgressiveStatCaps { get; set; } = false;
            [Range(0.1f, 10f)][DefaultValue(0.5f)] public float ProgressiveStatCapMultiplier { get; set; } = 0.5f;

            [Header("Rebirth_Points")]
            [DefaultValue(false)] public bool EnableRebirthPointRecalculation { get; set; } = false;
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
            public List<string> GhostStatsAffectedStats { get; set; } = new List<string> { "VIT", "END" };
        }

        public class RebirthAbilities
        {
            [Header("Last_Stand")]
            [Range(0f, 100f)][DefaultValue(10f)] public float LastStandHealPercent { get; set; } = 10f;
            [Range(0, 10)][DefaultValue(3)] public int LastStandImmunityTime { get; set; } = 3;
            [Range(0, 300)][DefaultValue(180)] public int LastStandCooldown { get; set; } = 180;
            [DefaultValue(true)] public bool EnableLastStandCooldownBar { get; set; } = true;

            [Header("Teleport")]
            [Range(1, 60)][DefaultValue(3)] public int TeleportCooldown { get; set; } = 3;
            [DefaultValue(true)] public bool EnableTeleportCooldownBar { get; set; } = true;

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
        }

        public class RoleSettings
        {
            [Header("Role_System")]
            [DefaultValue(50)][Range(0, 1000)] public int BaseSwitchCost { get; set; } = 50;
            [DefaultValue(1f)][Range(0f, 5f)] public float SwitchCostMultiplier { get; set; } = 1f;
            [DefaultValue(true)] public bool EnableRoleProximity { get; set; } = true;
            [Range(500, 10000)][DefaultValue(1000)] public int RoleProximityRange { get; set; } = 1000;

            [Header("Crit_God")]
            [DefaultValue(50f)][Range(0f, 200f)] public float CritGodCritChance { get; set; } = 50f;
            [DefaultValue(1f)][Range(0f, 10f)] public float CritGodExcessCritToDamage { get; set; } = 1f;
            [DefaultValue(true)] public bool CritGodEnableSummonCrits { get; set; } = true;

            [Header("Vampire")]
            [DefaultValue(25f)][Range(0f, 200f)] public float VampireHealthBonus { get; set; } = 25f;
            [DefaultValue(15f)][Range(0f, 100f)] public float VampireMovementSpeed { get; set; } = 15f;
            [DefaultValue(15f)][Range(0f, 100f)] public float VampireBleedChance { get; set; } = 15f;
            [DefaultValue(1f)][Range(0.1f, 50f)] public float VampireBleedDamagePercent { get; set; } = 1f;
            [DefaultValue(3f)][Range(1f, 30f)] public float VampireBleedDuration { get; set; } = 3f;
            [DefaultValue(1.5f)][Range(0.1f, 5f)] public float VampireBleedTickInterval { get; set; } = 1.5f;
            [DefaultValue(10f)][Range(0f, 100f)] public float VampireBleedHealPercent { get; set; } = 10f;
            [DefaultValue(5f)][Range(0f, 50f)] public float VampireKillHealPercent { get; set; } = 5f;

            [Header("Beastmaster")]
            [DefaultValue(15f)][Range(0f, 100f)] public float BeastmasterDamagePerUniqueMinion { get; set; } = 15f;
            [DefaultValue(3)][Range(1, 10)] public int BeastmasterSlotsPerBonusSlot { get; set; } = 3;
            [DefaultValue(1)][Range(1, 5)] public int BeastmasterBonusSlotsGained { get; set; } = 1;
            [DefaultValue(true)] public bool BeastmasterReduceSPRSlotEfficiency { get; set; } = true;
            [DefaultValue(2f)][Range(1f, 10f)] public float BeastmasterSPRSlotPenaltyMultiplier { get; set; } = 2f;

            [Header("Apex_Summoner")]
            [DefaultValue(20f)][Range(0f, 100f)] public float ApexSummonerDamagePerUnusedSlot { get; set; } = 20f;

            [Header("Black_Knight")]
            [DefaultValue(1f)][Range(0f, 10f)] public float BlackKnightINTToMeleeDamage { get; set; } = 1f;
            [DefaultValue(1f)][Range(0f, 10f)] public float BlackKnightSTRToMagicDamage { get; set; } = 1f;
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
            [Range(0f, 100f)][DefaultValue(30f)] public float ClericHealthBonus { get; set; } = 30f;
            [Range(0f, 90f)][DefaultValue(50f)] public float ClericDefensePenalty { get; set; } = 50f;
            [DefaultValue(true)] public bool ClericDisableVitRegen { get; set; } = true;
            [Range(0f, 100f)][DefaultValue(15f)] public float ClericTeammateHealthBonus { get; set; } = 15f;
            [Range(0.1f, 10f)][DefaultValue(4f)] public float ClericSelfRegenPercent { get; set; } = 4f;
            [Range(0.1f, 10f)][DefaultValue(2f)] public float ClericTeammateRegenPercent { get; set; } = 2f;
            [Range(1f, 10f)][DefaultValue(3f)] public float ClericRegenInterval { get; set; } = 3f;
            [Range(1f, 30f)][DefaultValue(10f)] public float DivineInterventionDuration { get; set; } = 10f;
            [Range(30f, 600f)][DefaultValue(120f)] public float DivineInterventionCooldown { get; set; } = 120f;
            [DefaultValue(true)] public bool EnableDivineInterventionCooldownBar { get; set; } = true;
        }

        public class MultiplayerSettings
        {
            [Header("Multiplayer_Settings")]
            [DefaultValue(false)] public bool AllowSelfResetInMultiplayer { get; set; } = false;
            [DefaultValue(false)] public bool SplitKillXP { get; set; } = false;
            [DefaultValue(true)] public bool EnableXPProximity { get; set; } = true;
            [Range(500, 10000)][DefaultValue(1000)] public int XPProximityRange { get; set; } = 1000;
        }

        public class EnemyScaling
        {
            [Header("Enemy_Scaling")]
            [DefaultValue(true)] public bool EnableEnemyScaling { get; set; } = true;
            [DefaultValue(true)] public bool ScaleCrawlerEnemies { get; set; } = true;
            [Range(0f, 5f)][DefaultValue(0.10f)] public float EnemyHealthScaling { get; set; } = 0.10f;
            [Range(0f, 5f)][DefaultValue(0.05f)] public float EnemyDamageScaling { get; set; } = 0.05f;
            [DefaultValue(true)] public bool EnableDefenseCap { get; set; } = true;
            [Range(1, 100)][DefaultValue(3)] public int MaxDefenseMultiplier { get; set; } = 3;
            [Range(0f, 5f)][DefaultValue(0.02f)] public float EnemyDefenseScaling { get; set; } = 0.02f;
            [Range(0f, 10f)][DefaultValue(0.2f)] public float BossHealthScaling { get; set; } = 0.2f;
            [Range(0f, 10f)][DefaultValue(0.1f)] public float BossDamageScaling { get; set; } = 0.1f;
            [DefaultValue(true)] public bool ShowEnemyLevelIndicator { get; set; } = true;
            [DefaultValue(true)] public bool ShowEnemyLevelBehindWalls { get; set; } = true;
            [Range(0f, 2f)][DefaultValue(1f)] public float EnemyIndicatorOpacity { get; set; } = 1f;
            [DefaultValue(false)] public bool EnableLevelVariation { get; set; } = false;
            [Range(1, 100)][DefaultValue(5)] public int MaxLevelVariation { get; set; } = 5;
            [DefaultValue(false)] public bool EnableMinimumLevelDifference { get; set; } = false;
            [Range(1, 100)][DefaultValue(25)] public int MinimumLevelDifference { get; set; } = 25;

            [Header("Sync_Settings")]
            [Range(0, 300)][DefaultValue(2)] public int SyncDelayFrames { get; set; } = 2;
            [DefaultValue(true)] public bool ImmediateSyncInSingleplayer { get; set; } = true;
            [DefaultValue(true)] public bool PrioritizeBossSync { get; set; } = true;
            [Range(1, 30)][DefaultValue(5)] public int MaxSyncAttempts { get; set; } = 5;

            [Header("Multiplayer_Scaling")]
            [Range(0, 2)][DefaultValue(1)][Slider][SliderColor(150, 0, 150)][Increment(1)][DrawTicks] public int ScalingType { get; set; } = 1;
            [Range(1, 1000)][DefaultValue(5)] public int LevelsPerPlayer { get; set; } = 5;
            [DefaultValue(false)] public bool UseProximityForScaling { get; set; } = false;
            [Range(500, 10000)][DefaultValue(2000)] public int ScalingProximityRange { get; set; } = 2000;

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
            [Range(0, 10000)][DefaultValue(1000)] public int VIT_Cap { get; set; } = 1000;
            [Range(0, 10000)][DefaultValue(1000)] public int STR_Cap { get; set; } = 1000;
            [Range(0, 10000)][DefaultValue(1000)] public int AGI_Cap { get; set; } = 1000;
            [Range(0, 10000)][DefaultValue(1000)] public int INT_Cap { get; set; } = 1000;
            [Range(0, 10000)][DefaultValue(1000)] public int LUC_Cap { get; set; } = 1000;
            [Range(0, 10000)][DefaultValue(1000)] public int END_Cap { get; set; } = 1000;
            [Range(0, 10000)][DefaultValue(1000)] public int POW_Cap { get; set; } = 1000;
            [Range(0, 10000)][DefaultValue(1000)] public int DEX_Cap { get; set; } = 1000;
            [Range(0, 10000)][DefaultValue(1000)] public int SPR_Cap { get; set; } = 1000;
            [Range(0, 10000)][DefaultValue(1000)] public int TCH_Cap { get; set; } = 1000;
            [Range(0, 10000)][DefaultValue(1000)] public int RGE_Cap { get; set; } = 1000;
            [Range(0, 10000)][DefaultValue(1000)] public int BRD_Cap { get; set; } = 1000;
            [Range(0, 10000)][DefaultValue(1000)] public int HLR_Cap { get; set; } = 1000;
            [Range(0, 10000)][DefaultValue(1000)] public int CLK_Cap { get; set; } = 1000;

            [Header("VIT_Settings")]
            [Range(0, 1000)][DefaultValue(5)] public int VIT_HP { get; set; } = 5;
            [DefaultValue(false)] public bool UseCustomHpRegen { get; set; } = false;
            [Range(0f, 10f)][DefaultValue(0.5f)] public float CustomHpRegenPerVIT { get; set; } = 0.5f;
            [Range(0, 10)][DefaultValue(3)] public int CustomHpRegenDelay { get; set; } = 3;
            [DefaultValue(true)] public bool EnableHealingPotionBoost { get; set; } = true;
            [Range(0f, 10f)][DefaultValue(0.5f)] public float HealingPotionBoostPercent { get; set; } = 0.5f;

            [Header("STR_Settings")]
            [Range(0f, 10f)][DefaultValue(1f)] public float STR_Damage { get; set; } = 1f;
            [Range(0f, 10f)][DefaultValue(1f)] public float STR_Knockback { get; set; } = 1f;
            [DefaultValue(1)] public int STR_ArmorPen { get; set; } = 1;

            [Header("AGI_Settings")]
            [Range(0f, 10f)][DefaultValue(2f)] public float AGI_MoveSpeed { get; set; } = 2f;
            [Range(0f, 10f)][DefaultValue(1f)] public float AGI_AttackSpeed { get; set; } = 1f;
            [Range(0f, 2f)][DefaultValue(0.5f)] public float AGI_JumpHeight { get; set; } = 0.5f;
            [Range(0f, 1f)][DefaultValue(0.1f)] public float AGI_JumpSpeed { get; set; } = 0.1f;
            [DefaultValue(2)] public int AGI_WingTime { get; set; } = 2;

            [Header("INT_Settings")]
            [Range(0f, 10f)][DefaultValue(1f)] public float INT_Damage { get; set; } = 1f;
            [Range(0, 1000)][DefaultValue(5)] public int INT_MP { get; set; } = 5;
            [Range(0f, 10f)][DefaultValue(2f)] public float INT_ManaCostReduction { get; set; } = 2f;
            [DefaultValue(1)] public int INT_ArmorPen { get; set; } = 1;

            [Header("LUC_Settings")]
            [Range(0f, 10f)][DefaultValue(1f)] public float LUC_Crit { get; set; } = 1f;
            [DefaultValue(true)] public bool LUC_EnableFishing { get; set; } = true;
            [DefaultValue(1)] public int LUC_Fishing { get; set; } = 1;
            [DefaultValue(10)] public int LUC_AggroReduction { get; set; } = 10;
            [DefaultValue(true)] public bool LUC_EnableLuckBonus { get; set; } = true;
            [Range(-1f, 1f)][DefaultValue(0.02f)] public float LUC_LuckBonus { get; set; } = 0.02f;

            [Header("END_Settings")]
            [DefaultValue(2)] public int END_DefensePerX { get; set; } = 2;
            [DefaultValue(10)] public int END_Aggro { get; set; } = 10;
            [DefaultValue(true)] public bool EnableKnockbackResist { get; set; } = true;
            [DefaultValue(true)] public bool EnableDR { get; set; } = true;
            [DefaultValue(false)] public bool EnableEnemyKnockback { get; set; } = false;
            [DefaultValue(0.1f)] public float END_EnemyKnockbackMultiplier { get; set; } = 0.1f;

            [Header("POW_Settings")]
            [Range(0f, 10f)][DefaultValue(1f)] public float POW_Damage { get; set; } = 1f;

            [Header("DEX_Settings")]
            [Range(0f, 10f)][DefaultValue(1f)] public float DEX_Damage { get; set; } = 1f;
            [DefaultValue(1)] public int DEX_ArmorPen { get; set; } = 1;
            [Range(0f, 10f)][DefaultValue(1f)] public float DEX_AmmoConservation { get; set; } = 1f;

            [Header("TCH_Settings")]
            [DefaultValue(true)] public bool TCH_EnableMiningSpeed { get; set; } = true;
            [DefaultValue(1)] public int TCH_MiningSpeed { get; set; } = 1;
            [DefaultValue(true)] public bool TCH_EnableBuildSpeed { get; set; } = true;
            [DefaultValue(1)] public int TCH_BuildSpeed { get; set; } = 1;
            [DefaultValue(true)] public bool TCH_EnableRange { get; set; } = true;
            [DefaultValue(1)] public int TCH_Range { get; set; } = 1;

            [Header("SPR_Settings")]
            [Range(0f, 10f)][DefaultValue(1f)] public float SPR_Damage { get; set; } = 1f;
            [Range(1, 100)][DefaultValue(25)] public int SPR_MinionsPerX { get; set; } = 25;
            [Range(1, 100)][DefaultValue(50)] public int SPR_SentriesPerX { get; set; } = 50;
        }

        public class ModIntegration
        {
            [Header("Mod_Integration")]
            [DefaultValue(true)] public bool EnableCalamityIntegration { get; set; } = true;
            [DefaultValue(true)] public bool EnableThoriumIntegration { get; set; } = true;
            [DefaultValue(true)] public bool EnableClickerClassIntegration { get; set; } = true;

            [Header("RGE_Settings")]
            [Range(0f, 10f)][DefaultValue(1f)] public float RGE_Damage { get; set; } = 1f;
            [Range(0f, 10f)][DefaultValue(1f)] public float RGE_MaxStealthPerPoint { get; set; } = 1f;
            [Range(0f, 10f)][DefaultValue(2f)] public float RGE_Velocity { get; set; } = 2f;
            [Range(0f, 10f)][DefaultValue(1f)] public float RGE_AmmoConsumptionReduction { get; set; } = 1f;
            [DefaultValue(false)] public bool RGE_EnableStealthConsumptionReduction { get; set; } = false;
            [DefaultValue(25)] public int RGE_StealthConsumption85Threshold { get; set; } = 25;
            [DefaultValue(50)] public int RGE_StealthConsumption75Threshold { get; set; } = 50;
            [DefaultValue(75)] public int RGE_StealthConsumptionReductionThreshold { get; set; } = 75;

            [Header("POW_CalamityEnhancements")]
            [Range(0f, 10f)][DefaultValue(1f)] public float POW_RageDamage { get; set; } = 1f;
            [DefaultValue(10)] public int POW_RageDuration { get; set; } = 10;
            [Range(0, 100000)][DefaultValue(2000)] public int POW_MaxRageDurationBonus { get; set; } = 2000;
            [DefaultValue(5)] public int POW_AdrenalineDuration { get; set; } = 5;

            [Header("BRD_Settings")]
            [Range(0f, 10f)][DefaultValue(1f)] public float BRD_Damage { get; set; } = 1f;
            [DefaultValue(2)] public int BRD_PointsPerMaxInspiration { get; set; } = 2;
            [DefaultValue(1)] public int BRD_ArmorPen { get; set; } = 1;
            [DefaultValue(true)] public bool BRD_EnableEmpowermentBoost { get; set; } = true;
            [Range(0f, 10f)][DefaultValue(0.25f)] public float BRD_EmpowermentDuration { get; set; } = 0.25f;

            [Header("HLR_Settings")]
            [Range(0f, 10f)][DefaultValue(1f)] public float HLR_Damage { get; set; } = 1f;
            [DefaultValue(1)] public int HLR_HealingPower { get; set; } = 1;
            [DefaultValue(5)][Range(1, 1000)] public int HLR_PointsPerEffectPoint { get; set; } = 5;
            [DefaultValue(1)] public int HLR_ArmorPen { get; set; } = 1;

            [Header("CLK_Settings")]
            [Range(0f, 10f)][DefaultValue(1f)] public float CLK_Damage { get; set; } = 1f;
            [Range(0f, 10f)][DefaultValue(1f)] public float CLK_Radius { get; set; } = 1f;
            [Range(0f, 10f)][DefaultValue(2f)] public float CLK_EffectThreshold { get; set; } = 2f;
        }

        public class Advanced
        {
            [Header("XP_Blacklist")]
            public List<string> BlacklistedNPCs { get; set; } = new List<string>();
        }
    }
}