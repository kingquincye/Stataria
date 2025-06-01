using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Stataria.Buffs
{
    public class DivineInterventionBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;
            Main.pvpBuff[Type] = false;
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = true;
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
                    buffName = $"Divine Intervention ({timeLeft:0.0}s)";
                    tip = "Protected from harmful debuffs by divine grace";
                }
            }
        }
        
        public override void Update(Player player, ref int buffIndex)
        {
            // ToDo: Effects to go here.
        }
    }
}