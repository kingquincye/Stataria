using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Stataria.Buffs
{
    public class ShowdownBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;
            Main.pvpBuff[Type] = false;
            Main.buffNoSave[Type] = false;
            BuffID.Sets.LongerExpertDebuff[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            // The showdown effects are handled in DesperadoPlayer.cs
        }
    }
}
