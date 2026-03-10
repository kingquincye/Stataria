using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace Stataria.Buffs
{
    public class ArcaneSurgeBuff : ModBuff
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
            Player player = Main.LocalPlayer;
            
            if (player != null)
            {
                int buffIndex = player.FindBuffIndex(Type);
                if (buffIndex >= 0)
                {
                    float timeLeft = player.buffTime[buffIndex] / 60f;
                    
                    var rpgPlayer = player.GetModPlayer<RPGPlayer>();
                    float damageBonus = rpgPlayer.GetArcaneSurgeDamageBonus();
                    
                    buffName = $"Arcane Surge ({timeLeft:0.0}s)";
                    tip = $"+{damageBonus:0.#}% magic damage";
                }
            }
        }
    }
}