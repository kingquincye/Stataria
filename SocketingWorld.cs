using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace Stataria
{
    public class SocketingWorld : ModSystem
    {
        public static bool HasDefeatedEvilBoss = false;
        public static bool IsHardmode = false;

        public override void SaveWorldData(TagCompound tag)
        {
            tag["HasDefeatedEvilBoss"] = HasDefeatedEvilBoss;
        }

        public override void LoadWorldData(TagCompound tag)
        {
            HasDefeatedEvilBoss = tag.GetBool("HasDefeatedEvilBoss");
        }

        public override void PostUpdateWorld()
        {
            IsHardmode = Main.hardMode;

            if (!HasDefeatedEvilBoss && NPC.downedBoss2)
            {
                HasDefeatedEvilBoss = true;
            }
        }
    }
}