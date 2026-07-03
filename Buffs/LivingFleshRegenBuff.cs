using Terraria;
using Terraria.ModLoader;

namespace Stataria.Buffs
{
    public class LivingFleshRegenBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            // Regen handling runs in LivingFleshPlayer.cs to prevent damage interrupts
        }
    }
}
