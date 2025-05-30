using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Stataria
{
    public class BleedDebuff : ModBuff
    {
        public override string Texture => "Terraria/Images/Buff_" + BuffID.Bleeding;

        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.pvpBuff[Type] = false;
            Main.buffNoSave[Type] = true;
            BuffID.Sets.LongerExpertDebuff[Type] = false;
        }

        public override void Update(NPC npc, ref int buffIndex)
        {
            npc.GetGlobalNPC<BleedGlobalNPC>().isBleedActive = true;
        }

        public override bool ReApply(NPC npc, int time, int buffIndex)
        {
            npc.buffTime[buffIndex] = time;
            return false;
        }
    }
}