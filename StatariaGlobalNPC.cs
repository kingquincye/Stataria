using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using System.Collections.Generic;

namespace Stataria
{
    public class StatariaGlobalNPC : GlobalNPC
    {
        public static HashSet<int> killedBossesGlobal = new();

        public override void OnKill(NPC npc)
        {
            if (npc.friendly || npc.lifeMax <= 5 || Main.netMode == NetmodeID.MultiplayerClient)
                return;

            var config = ModContent.GetInstance<StatariaConfig>();

            if (npc.boss)
                killedBossesGlobal.Add(npc.type);

            foreach (Player p in Main.player)
            {
                if (p is null || !p.active) continue;
                var rpg = p.GetModPlayer<RPGPlayer>();

                int xpToGive = 0;

                if (npc.boss)
                {
                    if (rpg.rewardedBosses.Contains(npc.type))
                        continue;

                    if (config.UseFlatBossXP)
                        xpToGive = config.FlatBossXPPerID.TryGetValue(npc.type, out int flat) ? flat : 0;
                    else
                        xpToGive = (int)(npc.lifeMax * config.KillXP) + (int)(rpg.XPToNext * config.BossXP / 100f);

                    rpg.rewardedBosses.Add(npc.type);
                }
                else
                {
                    xpToGive = (int)(npc.lifeMax * config.KillXP);
                }

                rpg.GainXP(xpToGive);
            }
        }
    }
}