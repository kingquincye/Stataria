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
        BossXP
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
                    
                    // Also track boss as rewarded if needed
                    rpg.rewardedBosses.Add(bossType);
                }
            }
        }
    }
}