using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using System;

namespace Stataria.Buffs
{
    public class DarkFocusBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;
            Main.pvpBuff[Type] = false;
            Main.buffNoSave[Type] = false;
            BuffID.Sets.LongerExpertDebuff[Type] = false;
        }

        public override void ModifyBuffText(ref string buffName, ref string tip, ref int rare)
        {
            var config = ModContent.GetInstance<StatariaConfig>();
            Player player = Main.LocalPlayer;
            
            if (player != null)
            {
                int buffIndex = player.FindBuffIndex(Type);
                if (buffIndex >= 0)
                {
                    int stacks = Math.Min((player.buffTime[buffIndex] + 59) / 60, config.roleSettings.BlackKnightMaxDarkFocusStacks);
                    int maxStacks = config.roleSettings.BlackKnightMaxDarkFocusStacks;
                    
                    float critChance = stacks * config.roleSettings.BlackKnightDarkFocusCritChancePerStack;
                    float critDamage = stacks * config.roleSettings.BlackKnightDarkFocusCritDamagePerStack;
                    
                    buffName = $"Dark Focus ({stacks}/{maxStacks})";
                    tip = $"Next melee attack: +{critChance:0.#}% crit chance, +{critDamage:0.#}% crit damage";
                }
            }
        }
    }
}