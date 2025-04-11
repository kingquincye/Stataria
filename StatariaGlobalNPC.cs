using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace Stataria
{
    public class StatariaGlobalNPC : GlobalNPC
    {
        public override void OnKill(NPC npc)
        {
            if (npc.friendly || npc.lifeMax <= 5 || Main.netMode == NetmodeID.MultiplayerClient)
                return;

            Player player = Main.player[npc.lastInteraction];
            if (player == null || !player.active)
                return;

            var rpg = player.GetModPlayer<RPGPlayer>();
            var config = ModContent.GetInstance<StatariaConfig>();

            int baseXP = (int)(npc.lifeMax * config.KillXP);

            if (npc.boss)
            {
                float bossMultiplier = config.BossXP / 100f;
                baseXP += (int)(rpg.XPToNext * bossMultiplier);
            }

            rpg.GainXP(baseXP);
        }
    }
}