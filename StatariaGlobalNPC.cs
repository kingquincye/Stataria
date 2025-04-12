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
                bool hasKilledBefore = rpg.rewardedBosses.Contains(npc.type);

                // --- Boss HP XP ---
                if (npc.boss)
                {
                    if (config.EnableBossHPXP)
                    {
                        // Always give HP XP when enabled
                        xpToGive += (int)(npc.lifeMax * config.KillXP);
                    }
                    else
                    {
                        // Only give HP XP once per boss when disabled
                        if (!hasKilledBefore)
                        {
                            xpToGive += (int)(npc.lifeMax * config.KillXP);
                        }
                    }
                }

                // --- Bonus XP (flat or scaling) ---
                if (npc.boss)
                {
                    if (!config.BonusBossXPIsUnique || !hasKilledBefore)
                    {
                        int bonusXP = config.UseFlatBossXP
                            ? config.DefaultFlatBossXP
                            : (int)(rpg.XPToNext * config.BossXP / 100f);

                        xpToGive += bonusXP;
                    }

                    // Only track boss as rewarded if bonus XP is marked unique or HP XP is disabled (making it unique)
                    if (!hasKilledBefore && (config.BonusBossXPIsUnique || !config.EnableBossHPXP))
                    {
                        rpg.rewardedBosses.Add(npc.type);
                    }
                }

                // --- Regular (non-boss) kill XP ---
                if (!npc.boss)
                {
                    xpToGive = (int)(npc.lifeMax * config.KillXP);
                }

                rpg.GainXP(xpToGive);
            }
        }
    }
}