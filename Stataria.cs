using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader;
using System.IO;
using Terraria;
using Terraria.ID;

namespace Stataria
{
    public enum StatariaMessageType : byte
    {
        SyncPlayer,
        SyncGlobalBosses,
        BossXP,
        SyncRewardedBosses
    }

    public class Stataria : Mod
    {
        public static void SendBossXP(int playerIndex, int bossType, int xpAmount, string source)
        {
            if (Main.netMode != NetmodeID.Server)
                return;
                
            var packet = ModContent.GetInstance<Stataria>().GetPacket();
            packet.Write((byte)StatariaMessageType.BossXP);
            packet.Write(playerIndex);
            packet.Write(bossType);
            packet.Write(xpAmount);
            packet.Write(source);
            packet.Send();
        }
        
        // Add method to sync rewarded bosses specifically
        public static void SyncRewardedBosses(int playerIndex, int toWho = -1, int fromWho = -1)
        {
            if (Main.netMode != NetmodeID.Server || playerIndex < 0 || playerIndex >= Main.maxPlayers)
                return;
                
            Player player = Main.player[playerIndex];
            if (player == null || !player.active)
                return;
                
            var rpg = player.GetModPlayer<RPGPlayer>();
            
            var packet = ModContent.GetInstance<Stataria>().GetPacket();
            packet.Write((byte)StatariaMessageType.SyncRewardedBosses);
            packet.Write(playerIndex);
            packet.Write(rpg.rewardedBosses.Count);
            foreach (int bossId in rpg.rewardedBosses)
            {
                packet.Write(bossId);
            }
            packet.Send(toWho, fromWho);
        }
        
        // This method handles incoming packets.
        public override void HandlePacket(BinaryReader reader, int whoAmI)
        {
            // Read our packet type.
            StatariaMessageType msgType = (StatariaMessageType)reader.ReadByte();
            if (msgType == StatariaMessageType.SyncPlayer)
            {
                int playerIndex = reader.ReadInt32();
                if (playerIndex < 0 || playerIndex >= Main.maxPlayers)
                    return;
                    
                RPGPlayer rpg = Main.player[playerIndex].GetModPlayer<RPGPlayer>();
                rpg.Level = reader.ReadInt32();
                rpg.XP = reader.ReadInt32();
                rpg.XPToNext = reader.ReadInt32();
                rpg.StatPoints = reader.ReadInt32();
                rpg.VIT = reader.ReadInt32();
                rpg.STR = reader.ReadInt32();
                rpg.AGI = reader.ReadInt32();
                rpg.INT = reader.ReadInt32();
                rpg.LUC = reader.ReadInt32();
                rpg.END = reader.ReadInt32();
                rpg.POW = reader.ReadInt32();
                rpg.DEX = reader.ReadInt32();
                rpg.SPR = reader.ReadInt32();
                
                // Add this code to read rewardedBosses
                int bossCount = reader.ReadInt32();
                rpg.rewardedBosses.Clear();
                for (int i = 0; i < bossCount; i++)
                {
                    rpg.rewardedBosses.Add(reader.ReadInt32());
                }
            }
            else if (msgType == StatariaMessageType.SyncGlobalBosses)
            {
                // Add this new message type to sync global bosses
                int bossCount = reader.ReadInt32();
                StatariaSystem.killedBossesGlobal.Clear();
                for (int i = 0; i < bossCount; i++)
                {
                    StatariaSystem.killedBossesGlobal.Add(reader.ReadInt32());
                }
            }
            else if (msgType == StatariaMessageType.BossXP)
            {
                int playerIndex = reader.ReadInt32();
                int bossType = reader.ReadInt32();
                int xpAmount = reader.ReadInt32();
                string source = reader.ReadString();
                
                if (playerIndex >= 0 && playerIndex < Main.maxPlayers && Main.player[playerIndex].active)
                {
                    var rpg = Main.player[playerIndex].GetModPlayer<RPGPlayer>();
                    rpg.GainXP(xpAmount, source);
                    
                    // Track boss as rewarded only if this message is coming from a boss XP
                    if (source.Contains("Boss") && !rpg.rewardedBosses.Contains(bossType))
                    {
                        rpg.rewardedBosses.Add(bossType);
                        
                        // If in multiplayer server, make sure this gets synced to all clients
                        if (Main.netMode == NetmodeID.Server)
                        {
                            SyncRewardedBosses(playerIndex);
                        }
                    }
                }
            }
            else if (msgType == StatariaMessageType.SyncRewardedBosses)
            {
                int playerIndex = reader.ReadInt32();
                if (playerIndex < 0 || playerIndex >= Main.maxPlayers)
                    return;
                    
                var rpg = Main.player[playerIndex].GetModPlayer<RPGPlayer>();
                int bossCount = reader.ReadInt32();
                rpg.rewardedBosses.Clear();
                for (int i = 0; i < bossCount; i++)
                {
                    rpg.rewardedBosses.Add(reader.ReadInt32());
                }
            }
        }
    }
}