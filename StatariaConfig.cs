using Terraria.ModLoader.Config;
using System.ComponentModel;

namespace Stataria
{
    public class StatariaConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ServerSide;

        [Header("XP_Multipliers")]

        [Range(0f, 5f)]
        [DefaultValue(0.25f)]
        public float DamageXP;

        [Range(0f, 5f)]
        [DefaultValue(0.5f)]
        public float KillXP;

        [Range(0, 1000)]
        [DefaultValue(25)]
        public int BossXP;

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