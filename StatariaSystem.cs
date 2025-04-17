using Terraria;
using Terraria.ModLoader;
using System.Collections.Generic;
using Terraria.ID;
using System.Linq;

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

        // New method to sync player's rewarded bosses
        public static void SyncPlayerBosses(int playerIndex, int toWho = -1, int fromWho = -1)
        {
            if (Main.netMode == NetmodeID.SinglePlayer || playerIndex < 0 || playerIndex >= Main.maxPlayers)
                return;
            
            Player player = Main.player[playerIndex];
            if (player == null || !player.active)
                return;
                
            var rpg = player.GetModPlayer<RPGPlayer>();
            
            // Use SyncPlayer method which already includes rewarded bosses
            rpg.SyncPlayer(toWho, fromWho, false);
        }

        public override void PostUpdatePlayers()
        {
            // Only sync in multiplayer
            if (Main.netMode != NetmodeID.Server)
                return;
                
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                var player = Main.player[i];
                if (player == null || !player.active)
                    continue;
                    
                if (!syncedPlayers.Contains(i))
                {
                    // This is a new player that hasn't been synced yet
                    SyncGlobalBosses(toWho: i);
                    
                    // For each already connected player, sync their rewardedBosses to this new player
                    for (int j = 0; j < Main.maxPlayers; j++)
                    {
                        if (j != i && Main.player[j] != null && Main.player[j].active)
                        {
                            SyncPlayerBosses(j, toWho: i);
                        }
                    }
                    
                    // Also sync this new player's data to everyone else
                    SyncPlayerBosses(i);
                    
                    // Send specific boss reward data for this player
                    Stataria.SyncRewardedBosses(i);
                    
                    syncedPlayers.Add(i);
                }
            }
        }
    }
}