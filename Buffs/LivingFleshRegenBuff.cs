using Terraria;
using Terraria.ModLoader;

namespace Stataria
{
    public class LivingFleshRegenBuff : ModBuff
    {
        public override string Texture => "Stataria/icon";

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
