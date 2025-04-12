using Terraria.ModLoader.Config;
using System.ComponentModel;
using System.Collections.Generic;

namespace Stataria
{
    public class StatariaConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ServerSide;

        [Header("Balances")]
        [DefaultValue(true)] public bool EnableBossHPXP;
        [DefaultValue(false)] public bool BonusBossXPIsUnique;
        [DefaultValue(false)] public bool UseFlatBossXP;
        [Range(0, 50000000)][DefaultValue(5000)] public int DefaultFlatBossXP;
        [DefaultValue(false)] public bool EnableAbsentPlayerCompensation;
        [DefaultValue(false)] public bool EnableLevelCap;
        [Range(0, 100000000)][DefaultValue(50)] public int LevelCapValue;

        [Header("XP_Multipliers")]
        [Range(0f, 5f), DefaultValue(0.25f)] public float DamageXP;
        [Range(0f, 5f), DefaultValue(0.5f)] public float KillXP;
        [Range(0, 1000), DefaultValue(25)] public int BossXP;

        [Header("Stat_Scaling")]
        [DefaultValue(1)] public int STR_Damage;
        [DefaultValue(1)] public int INT_Damage;
        [DefaultValue(1)] public int DEX_Damage;
        [DefaultValue(1)] public int SPR_Damage;
        [DefaultValue(1)] public int POW_Damage;
        [DefaultValue(50)] public int LUC_Crit;
        [DefaultValue(5)] public int VIT_HP;
        [DefaultValue(5)] public int INT_MP;
    }
}