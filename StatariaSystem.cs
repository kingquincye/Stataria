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
            // Only sync global bosses list to new players in multiplayer
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
            
            // The compensation system code has been removed
        }
    }
}