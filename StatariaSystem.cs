using Terraria;
using Terraria.ModLoader;
using System.Collections.Generic;
using Terraria.ID;

namespace Stataria
{
    public class StatariaSystem : ModSystem
    {
        public static HashSet<int> killedBossesGlobal = new();

        // Track who already received XP on join
        private HashSet<int> syncedPlayers = new();

        public override void OnWorldLoad()
        {
            killedBossesGlobal.Clear();
            syncedPlayers.Clear();
        }

        public override void PostUpdateInput()
        {
            if (Main.dedServ)
                return;

            if (StatariaKeybinds.ToggleStatUI.JustPressed)
            {
                if (StatariaUI.StatUI.CurrentState == null)
                    StatariaUI.StatUI.SetState(StatariaUI.Panel);
                else
                    StatariaUI.StatUI.SetState(null);
            }
        }

        public override void PostUpdatePlayers()
        {
            var config = ModContent.GetInstance<StatariaConfig>();

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                var player = Main.player[i];
                if (player == null || !player.active || syncedPlayers.Contains(i))
                    continue;

                var rpg = player.GetModPlayer<RPGPlayer>();

                foreach (int bossID in killedBossesGlobal)
                {
                    if (rpg.rewardedBosses.Contains(bossID))
                        continue;

                    int xp = 0;

                    if (config.UseFlatBossXP)
                        xp = config.FlatBossXPPerID.TryGetValue(bossID, out int val) ? val : 0;
                    else if (ContentSamples.NpcsByNetId.TryGetValue(bossID, out NPC dummy))
                    {
                        xp = (int)(dummy.lifeMax * config.KillXP);
                        xp += (int)(rpg.XPToNext * config.BossXP / 100f);
                    }

                    rpg.GainXP(xp);
                    rpg.rewardedBosses.Add(bossID);
                }

                syncedPlayers.Add(i);
            }
        }
    }
}