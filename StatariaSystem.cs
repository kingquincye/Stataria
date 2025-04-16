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

        public static void SyncGlobalBosses(int toWho = -1, int fromWho = -1)
        {
            if (Main.netMode == NetmodeID.SinglePlayer)
                return;
                
            var packet = ModContent.GetInstance<Stataria>().GetPacket();
            packet.Write((byte)StatariaMessageType.SyncGlobalBosses);
            packet.Write(killedBossesGlobal.Count);
            foreach (int bossId in killedBossesGlobal)
            {
                packet.Write(bossId);
            }
            packet.Send(toWho, fromWho);
        }

        public override void PostUpdatePlayers()
        {
            var config = ModContent.GetInstance<StatariaConfig>();

            // Sync global bosses list to new players
            if (Main.netMode == NetmodeID.Server)
            {
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    var player = Main.player[i];
                    if (player == null || !player.active)
                        continue;
                        
                    if (!syncedPlayers.Contains(i))
                    {
                        // This is a new player that hasn't been synced yet
                        SyncGlobalBosses(toWho: i);
                        syncedPlayers.Add(i);
                    }
                }
            }

            // Skip compensation if disabled
            if (!config.EnableAbsentPlayerCompensation)
                return;

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
                        xp = config.DefaultFlatBossXP;
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