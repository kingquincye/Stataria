using Terraria;
using Terraria.ModLoader;
using System.Collections.Generic;
using Terraria.ID;
using System.Linq;
using System;

namespace Stataria
{
    public class StatariaSystem : ModSystem
    {
        public static HashSet<int> killedBossesGlobal = new();

        private HashSet<int> syncedPlayers = new();

        private int clearSyncTimer = 0;
        private const int CLEAR_SYNC_INTERVAL = 3600;

        public override void OnWorldLoad()
        {
            killedBossesGlobal.Clear();
            syncedPlayers.Clear();

            Stataria.ClearSyncedNPCs();
        }

        public override void OnWorldUnload()
        {
            Stataria.ClearSyncedNPCs();
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

        public static void SyncPlayerBosses(int playerIndex, int toWho = -1, int fromWho = -1)
        {
            if (Main.netMode == NetmodeID.SinglePlayer || playerIndex < 0 || playerIndex >= Main.maxPlayers)
                return;

            Player player = Main.player[playerIndex];
            if (player == null || !player.active)
                return;

            var rpg = player.GetModPlayer<RPGPlayer>();

            rpg.SyncPlayer(toWho, fromWho, false);
        }

        public override void PostUpdatePlayers()
        {
            if (Main.netMode != NetmodeID.Server)
                return;

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                var player = Main.player[i];
                if (player == null || !player.active)
                    continue;

                if (!syncedPlayers.Contains(i))
                {
                    SyncGlobalBosses(toWho: i);

                    for (int j = 0; j < Main.maxPlayers; j++)
                    {
                        if (j != i && Main.player[j] != null && Main.player[j].active)
                        {
                            SyncPlayerBosses(j, toWho: i);
                        }
                    }

                    SyncPlayerBosses(i);

                    Stataria.SyncRewardedBosses(i);

                    syncedPlayers.Add(i);
                }
            }
        }

        public override void PostUpdateEverything()
        {
            if (Main.netMode != NetmodeID.Server)
                return;

            var config = ModContent.GetInstance<StatariaConfig>();

            int clearInterval = config.enemyScaling.SyncDelayFrames <= 0
                ? 600
                : 3600;

            clearSyncTimer++;
            if (clearSyncTimer >= clearInterval)
            {
                int activeNPCs = Main.npc.Count(n => n.active);

                if (activeNPCs < 10 || config.enemyScaling.SyncDelayFrames <= 0)
                {
                    Stataria.ClearSyncedNPCs();
                }

                clearSyncTimer = 0;
            }

            if (Main.player.Count(p => p.active) == 0)
            {
                Stataria.ClearSyncedNPCs();
            }
        }
    }
}