using Terraria;
using Terraria.ModLoader;
using System.Collections.Generic;
using Terraria.ID;
using System.Linq;
using System;
using Microsoft.Xna.Framework;
using Terraria.DataStructures;

namespace Stataria
{
    public class StatariaSystem : ModSystem
    {
        public static HashSet<int> killedBossesGlobal = new();

        private HashSet<int> syncedPlayers = new();

        public override void OnWorldLoad()
        {
            killedBossesGlobal.Clear();
            syncedPlayers.Clear();
        }

        public override void OnWorldUnload()
        {
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
            // Update Spirit Form ticks and expiration logic for all active players
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (player == null || !player.active)
                    continue;

                var clericPlayer = player.GetModPlayer<ClericPlayer>();
                if (clericPlayer != null && clericPlayer.IsInSpiritForm)
                {
                    player.ghost = true;
                    player.dead = true;
                    player.respawnTimer = 180; // Freeze respawn timer

                    // Ticking down happens on all clients/server to keep UI in sync
                    if (clericPlayer.SpiritFormTimer > 0)
                    {
                        clericPlayer.SpiritFormTimer--;
                    }

                    // Only the owner client handles expiration / validation checks
                    if (player.whoAmI == Main.myPlayer)
                    {
                        // Validate the Angel player
                        bool angelValid = false;
                        if (clericPlayer.SpiritAngelWhoAmI >= 0 && clericPlayer.SpiritAngelWhoAmI < Main.maxPlayers)
                        {
                            Player angel = Main.player[clericPlayer.SpiritAngelWhoAmI];
                            if (angel != null && angel.active && !angel.dead)
                            {
                                var angelRpg = angel.GetModPlayer<RPGPlayer>();
                                if (angelRpg?.ActiveRole?.ID == "Cleric" && angelRpg.ActiveRole.Status == RoleStatus.Active && angelRpg.AscendedRoles.Contains("Cleric"))
                                {
                                    angelValid = true;
                                }
                            }
                        }

                        if (!angelValid)
                        {
                            clericPlayer.IsInSpiritForm = false;
                            clericPlayer.SpiritFormTimer = 0;
                            player.dead = false;
                            player.ghost = false;
                            if (clericPlayer.SpiritDeathReason == null)
                            {
                                clericPlayer.SpiritDeathReason = PlayerDeathReason.ByCustomReason(Terraria.Localization.NetworkText.FromKey("Mods.Stataria.DeathMessage.SoulAnchorBroken", player.name));
                            }
                            clericPlayer.IsBypassingSoulAnchor = true;
                            player.KillMe(clericPlayer.SpiritDeathReason, 9999, 0);
                            clericPlayer.IsBypassingSoulAnchor = false;
                            if (player.difficulty == 2)
                            {
                                player.respawnTimer = 0;
                            }
                            if (Main.netMode == NetmodeID.MultiplayerClient)
                            {
                                clericPlayer.SyncAngelState();
                            }
                            continue;
                        }

                        if (clericPlayer.SpiritFormTimer <= 0)
                        {
                            clericPlayer.IsInSpiritForm = false;
                            player.dead = false;
                            player.ghost = false;
                            if (clericPlayer.SpiritDeathReason == null)
                            {
                                clericPlayer.SpiritDeathReason = PlayerDeathReason.ByCustomReason(Terraria.Localization.NetworkText.FromKey("Mods.Stataria.DeathMessage.SpiritFormExpired", player.name));
                            }
                            clericPlayer.IsBypassingSoulAnchor = true;
                            player.KillMe(clericPlayer.SpiritDeathReason, 9999, 0);
                            clericPlayer.IsBypassingSoulAnchor = false;
                            if (player.difficulty == 2)
                            {
                                player.respawnTimer = 0;
                            }
                            if (Main.netMode == NetmodeID.MultiplayerClient)
                            {
                                clericPlayer.SyncAngelState();
                            }
                            continue;
                        }
                    }
                }
            }

            if (Main.netMode != NetmodeID.Server)
                return;

            // Remove disconnected players so they get re-synced on reconnect
            syncedPlayers.RemoveWhere(i => i < 0 || i >= Main.maxPlayers || Main.player[i] == null || !Main.player[i].active);

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

                    for (int n = 0; n < Main.maxNPCs; n++)
                    {
                        NPC npc = Main.npc[n];
                        if (npc != null && npc.active)
                        {
                            var scalingData = npc.GetGlobalNPC<StatariaScalingGlobalNPC>();
                            if (scalingData != null && scalingData.hasBeenScaled)
                            {
                                Stataria.SyncNPCScaling(n, toWho: i);
                            }
                        }
                    }

                    syncedPlayers.Add(i);
                }
            }
        }

        public override void PostUpdateEverything()
        {
        }
    }
}