using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader;
using System.IO;
using Terraria;

namespace Stataria
{
    public enum StatariaMessageType : byte
    {
        SyncPlayer
    }

    public class Stataria : Mod
    {
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
            }
        }
    }
}