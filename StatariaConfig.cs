using Terraria.ModLoader.Config;
using System.ComponentModel;

namespace Stataria
{
    public class StatariaConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ServerSide;

        // ───────────────────────────────────────────────
        [Header("UI_Settings")]
        [DefaultValue(true)] public bool ShowXPBarAbovePlayer;

        // ───────────────────────────────────────────────
        [Header("General_Balance")]
        [DefaultValue(true)] public bool EnableBossHPXP;
        [DefaultValue(false)] public bool BonusBossXPIsUnique;
        [DefaultValue(false)] public bool UseFlatBossXP;
        [Range(0, 50000000)][DefaultValue(5000)] public int DefaultFlatBossXP;
        [DefaultValue(false)] public bool EnableLevelCap;
        [Range(1, 100000000)][DefaultValue(50)] public int LevelCapValue;

        // ───────────────────────────────────────────────
        [Header("XP_Multipliers")]
        [Range(0f, 10f)][DefaultValue(0.25f)] public float DamageXP;
        [Range(0f, 10f)][DefaultValue(0.5f)] public float KillXP;
        [Range(0, 1000)][DefaultValue(25)] public int BossXP;

        // ───────────────────────────────────────────────
        [Header("VIT_Settings")]
        [DefaultValue(5)] public int VIT_HP;
        [DefaultValue(false)] public bool UseCustomHpRegen;
        [Range(0f, 10f)][DefaultValue(0.5f)] public float CustomHpRegenPerVIT;
        [Range(0, 300)][DefaultValue(180)] public int CustomHpRegenDelay; // frames (2 seconds default)
        //[DefaultValue(1)] public int VIT_Breath; // Seconds per point

        // ───────────────────────────────────────────────
        [Header("STR_Settings")]
        [DefaultValue(1)] public int STR_Damage;
        [DefaultValue(1)] public int STR_Knockback;
        [DefaultValue(1)] public int STR_ArmorPen;

        // ───────────────────────────────────────────────
        [Header("AGI_Settings")]
        [DefaultValue(5)] public int AGI_MoveSpeed;
        [DefaultValue(1)] public int AGI_AttackSpeed;
        [Range(0f, 2f)][DefaultValue(1f)] public float AGI_JumpHeight;
        [Range(0f, 1f)][DefaultValue(0.2f)] public float AGI_JumpSpeed;
        [DefaultValue(5)] public int AGI_WingTime;
        [DefaultValue(25)] public int AGI_DashUnlockAt;
        [DefaultValue(25)] public int AGI_SwimUnlockAt;
        [DefaultValue(25)] public int AGI_JumpUnlockAt;
        [DefaultValue(50)] public int AGI_NoFallDamageUnlockAt;
        [DefaultValue(100)] public int AGI_TeleportUnlockAt;
        [DefaultValue(3)] public int AGI_TeleportCooldown;
        [DefaultValue(true)] public bool EnableTeleportCooldownBar;

        // ───────────────────────────────────────────────
        [Header("INT_Settings")]
        [DefaultValue(1)] public int INT_Damage;
        [DefaultValue(5)] public int INT_MP;
        //[DefaultValue(1)] public int INT_ManaRegen;
        [DefaultValue(2)] public int INT_ManaCostReduction;
        [DefaultValue(1)] public int INT_ArmorPen;

        // ───────────────────────────────────────────────
        [Header("LUC_Settings")]
        [DefaultValue(1)] public int LUC_Crit; // Now int instead of float
        [DefaultValue(1)] public int LUC_Fishing;
        [DefaultValue(10)] public int LUC_AggroReduction;
        [Range(-1f, 1f)][DefaultValue(0.02f)] public float LUC_LuckBonus;

        // ───────────────────────────────────────────────
        [Header("END_Settings")]
        
        [DefaultValue(2)] public int END_DefensePerX;
        [DefaultValue(10)] public int END_Aggro;
        [DefaultValue(true)] public bool EnableKnockbackResist;
        [DefaultValue(true)] public bool EnableDR;
        [DefaultValue(true)] public bool EnableEnemyKnockback;
        [DefaultValue(0.1f)] public float END_EnemyKnockbackMultiplier;
        // Debuff resistance has been removed

        // ───────────────────────────────────────────────
        [Header("POW_Settings")]
        [DefaultValue(1)] public int POW_Damage; // Full value applied to modded damage types

        // ───────────────────────────────────────────────
        [Header("DEX_Settings")]
        [DefaultValue(1)] public int DEX_Damage;
        [DefaultValue(1)] public int DEX_MiningSpeed;
        [DefaultValue(1)] public int DEX_BuildSpeed;
        [DefaultValue(1)] public int DEX_Range;

        // ───────────────────────────────────────────────
        [Header("SPR_Settings")]
        [DefaultValue(1)] public int SPR_Damage;
        //[DefaultValue(1)] public int SPR_AttackSpeed;
        [Range(1, 100)][DefaultValue(10)] public int SPR_MinionsPerX;
        [Range(1, 100)][DefaultValue(20)] public int SPR_SentriesPerX;
    }
}