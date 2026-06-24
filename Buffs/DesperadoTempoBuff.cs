using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System;

namespace Stataria.Buffs
{
    public class DesperadoTempoBuff : ModBuff
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
                    int maxStacks = config.roleSettings.DesperadoMaxTempoStacks;
                    int stacks = Math.Min((player.buffTime[buffIndex] + 59) / 60, maxStacks);
                    
                    float attackSpeedBonus = stacks * config.roleSettings.DesperadoTempoAttackSpeedPerStack;
                    float velocityBonus = stacks * config.roleSettings.DesperadoTempoVelocityPerStack;
                    int extraProjectiles = 0;
                    if (config.roleSettings.DesperadoStacksPerExtraProjectile > 0)
                    {
                        extraProjectiles = Math.Min(stacks / config.roleSettings.DesperadoStacksPerExtraProjectile, config.roleSettings.DesperadoMaxExtraProjectiles);
                    }
                    
                    buffName = Terraria.Localization.Language.GetTextValue("Mods.Stataria.Buffs.DesperadoTempoBuff.DynamicName", stacks, maxStacks);
                    tip = Terraria.Localization.Language.GetTextValue("Mods.Stataria.Buffs.DesperadoTempoBuff.DynamicTooltip", attackSpeedBonus.ToString("0.#"), velocityBonus.ToString("0.#"), extraProjectiles);
                }
            }
        }
    }
}
