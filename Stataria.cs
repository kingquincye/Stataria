using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.Audio;
using Microsoft.Xna.Framework;

namespace Stataria
{
    public enum StatariaMessageType : byte
    {
        SyncPlayer,
        SyncGlobalBosses,
        BossXP,
        SyncRewardedBosses,
        SyncEliteStatus,
        SyncAbilities
    }

    public class Stataria : Mod
    {
        private static HashSet<int> syncedNPCs = new HashSet<int>();

        public override void Load()
        {
            StatariaLogger.GlobalDebugMode = false;
            StatariaLogger.Initialize(this);
            StatariaLogger.Info("Stataria mod loading started");

            base.Load();



            syncedNPCs.Clear();
            StatariaLogger.Info("Stataria mod loading completed");
        }

        public override void Unload()
        {
            base.Unload();

            StatariaUI.StatUI = null;
            StatariaUI.Panel = null;

            syncedNPCs.Clear();
        }


        public static void SendBossXP(int playerIndex, int bossType, long xpAmount, string source)
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

        public static void SyncNPCScaling(int npcIndex, int toWho = -1, int fromWho = -1)
        {
            if (Main.netMode != NetmodeID.Server || npcIndex < 0 || npcIndex >= Main.maxNPCs)
                return;

            NPC npc = Main.npc[npcIndex];
            if (npc == null || !npc.active)
                return;

            if (syncedNPCs.Contains(npcIndex))
                return;

            var scalingData = npc.GetGlobalNPC<StatariaScalingGlobalNPC>();

            var packet = ModContent.GetInstance<Stataria>().GetPacket();
            packet.Write((byte)StatariaMessageType.SyncEliteStatus);
            packet.Write(npcIndex);
            packet.Write(scalingData.IsElite);
            packet.Write(scalingData.Level);
            packet.Send(toWho, fromWho);

            syncedNPCs.Add(npcIndex);
        }

        public static void ClearSyncedNPCs()
        {
            syncedNPCs.Clear();
        }

        public override void HandlePacket(BinaryReader reader, int whoAmI)
        {
            StatariaMessageType msgType = (StatariaMessageType)reader.ReadByte();
            if (msgType == StatariaMessageType.SyncPlayer)
            {
                int playerIndex = reader.ReadInt32();
                if (playerIndex < 0 || playerIndex >= Main.maxPlayers)
                    return;

                RPGPlayer rpg = Main.player[playerIndex].GetModPlayer<RPGPlayer>();
                rpg.Level = reader.ReadInt32();
                rpg.XP = reader.ReadInt64();
                rpg.XPToNext = reader.ReadInt64();
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
                rpg.RGE = reader.ReadInt32();
                rpg.TCH = reader.ReadInt32();
                rpg.BRD = reader.ReadInt32();
                rpg.HLR = reader.ReadInt32();
                rpg.CLK = reader.ReadInt32();
                rpg.lastStandCooldownTimer = reader.ReadInt32();
                rpg.divineInterventionCooldownTimer = reader.ReadInt32();
                rpg.RebirthCount = reader.ReadInt32();
                rpg.RebirthPoints = reader.ReadInt32();
                rpg.AutoAllocateEnabled = reader.ReadBoolean();
                int statCount = reader.ReadInt32();
                rpg.AutoAllocateStats.Clear();
                for (int i = 0; i < statCount; i++)
                {
                    rpg.AutoAllocateStats.Add(reader.ReadString());
                }

                int bossCount = reader.ReadInt32();
                rpg.rewardedBosses.Clear();
                for (int i = 0; i < bossCount; i++)
                {
                    rpg.rewardedBosses.Add(reader.ReadInt32());
                }

                bool hasActiveRole = reader.ReadBoolean();
                if (hasActiveRole)
                {
                    string roleID = reader.ReadString();
                    RoleStatus roleStatus = (RoleStatus)reader.ReadByte();

                    if (rpg.AvailableRoles.ContainsKey(roleID))
                    {
                        foreach (var role in rpg.AvailableRoles.Values)
                        {
                            if (role.Status == RoleStatus.Active || role.Status == RoleStatus.Deactivated)
                                role.Status = RoleStatus.Available;
                        }

                        rpg.ActiveRole = rpg.AvailableRoles[roleID];
                        rpg.ActiveRole.Status = roleStatus;
                    }
                }
                else
                {
                    if (rpg.ActiveRole != null)
                    {
                        rpg.ActiveRole.Status = RoleStatus.Available;
                        rpg.ActiveRole = null;
                    }
                }
                rpg.RoleSwitchCount = reader.ReadInt32();

                if (Main.netMode == NetmodeID.Server)
                {
                    rpg.SyncPlayer(toWho: -1, fromWho: whoAmI, newPlayer: false);
                }
            }
            else if (msgType == StatariaMessageType.SyncGlobalBosses)
            {
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
                long xpAmount = reader.ReadInt64();
                string source = reader.ReadString();

                if (playerIndex >= 0 && playerIndex < Main.maxPlayers && Main.player[playerIndex].active)
                {
                    var rpg = Main.player[playerIndex].GetModPlayer<RPGPlayer>();
                    rpg.GainXP(xpAmount, source);

                    if (source.Contains("Boss") && !rpg.rewardedBosses.Contains(bossType))
                    {
                        rpg.rewardedBosses.Add(bossType);

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
            else if (msgType == StatariaMessageType.SyncEliteStatus)
            {
                int npcIndex = reader.ReadInt32();
                bool isElite = reader.ReadBoolean();
                int level = reader.ReadInt32();

                if (npcIndex >= 0 && npcIndex < Main.maxNPCs && Main.npc[npcIndex].active)
                {
                    var npcData = Main.npc[npcIndex].GetGlobalNPC<StatariaScalingGlobalNPC>();

                    var eliteProperty = typeof(StatariaScalingGlobalNPC).GetProperty("IsElite");
                    if (eliteProperty != null)
                        eliteProperty.SetValue(npcData, isElite, null);

                    var levelProperty = typeof(StatariaScalingGlobalNPC).GetProperty("Level");
                    if (levelProperty != null)
                        levelProperty.SetValue(npcData, level, null);
                }
            }
            else if (msgType == StatariaMessageType.SyncAbilities)
            {
                int playerIndex = reader.ReadInt32();
                if (playerIndex < 0 || playerIndex >= Main.maxPlayers)
                    return;

                RPGPlayer rpg = Main.player[playerIndex].GetModPlayer<RPGPlayer>();

                foreach (var ability in rpg.RebirthAbilities.Values)
                {
                    ability.IsUnlocked = false;
                    ability.Level = 0;
                    if (ability.AbilityType == RebirthAbilityType.Toggleable &&
                        ability.AbilityData.ContainsKey("Enabled"))
                    {
                        ability.AbilityData["Enabled"] = false;
                    }
                }

                int unlockedCount = reader.ReadInt32();
                for (int i = 0; i < unlockedCount; i++)
                {
                    string abilityId = reader.ReadString();
                    int level = reader.ReadInt32();
                    bool isEnabled = reader.ReadBoolean();

                    if (rpg.RebirthAbilities.ContainsKey(abilityId))
                    {
                        rpg.RebirthAbilities[abilityId].IsUnlocked = true;
                        rpg.RebirthAbilities[abilityId].Level = level;

                        if (rpg.RebirthAbilities[abilityId].AbilityType == RebirthAbilityType.Toggleable)
                        {
                            rpg.RebirthAbilities[abilityId].AbilityData["Enabled"] = isEnabled;
                        }
                    }
                }

                if (Main.netMode == NetmodeID.Server)
                {
                    rpg.SyncAbilities(toWho: -1, fromWho: whoAmI);
                }
            }
        }

        public override object Call(params object[] args)
        {
            if (args == null || args.Length == 0)
            {
                Logger.Warn("ModCall received with no arguments.");
                return null;
            }

            if (!(args[0] is string message))
            {
                Logger.Warn("First argument to ModCall must be a string message.");
                return null;
            }

            RPGPlayer rpgPlayer = null;

            RPGPlayer GetRPGPlayer(object playerArg)
            {
                if (playerArg is Player player)
                {
                    return player.GetModPlayer<RPGPlayer>();
                }
                else if (playerArg is int playerID && playerID >= 0 && playerID < Main.maxPlayers)
                {
                    return Main.player[playerID].GetModPlayer<RPGPlayer>();
                }
                Logger.Warn($"ModCall '{message}': Expected Player or playerID as second argument.");
                return null;
            }

            switch (message)
            {
                case "GetPlayerLevel":
                    if (args.Length < 2) { Logger.Warn("GetPlayerLevel: Not enough arguments."); return null; }
                    rpgPlayer = GetRPGPlayer(args[1]);
                    return rpgPlayer?.Level;

                case "GetPlayerXP":
                    if (args.Length < 2) { Logger.Warn("GetPlayerXP: Not enough arguments."); return null; }
                    rpgPlayer = GetRPGPlayer(args[1]);
                    return rpgPlayer?.XP;

                case "GetXPToNextLevel":
                    if (args.Length < 2) { Logger.Warn("GetXPToNextLevel: Not enough arguments."); return null; }
                    rpgPlayer = GetRPGPlayer(args[1]);
                    return rpgPlayer?.XPToNext;

                case "GetStatPoints":
                    if (args.Length < 2) { Logger.Warn("GetStatPoints: Not enough arguments."); return null; }
                    rpgPlayer = GetRPGPlayer(args[1]);
                    return rpgPlayer?.StatPoints;

                case "GetAllPlayerStats":
                    if (args.Length < 2) { Logger.Warn("GetAllPlayerStats: Not enough arguments."); return null; }
                    rpgPlayer = GetRPGPlayer(args[1]);
                    if (rpgPlayer == null) return null;
                    return new Dictionary<string, int>
                    {
                        { "VIT", rpgPlayer.VIT },
                        { "STR", rpgPlayer.STR },
                        { "AGI", rpgPlayer.AGI },
                        { "INT", rpgPlayer.INT },
                        { "LUC", rpgPlayer.LUC },
                        { "END", rpgPlayer.END },
                        { "POW", rpgPlayer.POW },
                        { "DEX", rpgPlayer.DEX },
                        { "SPR", rpgPlayer.SPR },
                        { "TCH", rpgPlayer.TCH },
                        { "RGE", rpgPlayer.RGE },
                        { "BRD", rpgPlayer.BRD },
                        { "HLR", rpgPlayer.HLR },
                        { "CLK", rpgPlayer.CLK }
                    };

                case "GetEffectiveStat":
                    if (args.Length < 3 || !(args[2] is string statNameEff)) { Logger.Warn("GetEffectiveStat: Incorrect arguments."); return null; }
                    rpgPlayer = GetRPGPlayer(args[1]);
                    return rpgPlayer?.GetEffectiveStat(statNameEff.ToUpper());

                case "GetGhostStat":
                    if (args.Length < 3 || !(args[2] is string statNameGhost)) { Logger.Warn("GetGhostStat: Incorrect arguments."); return null; }
                    rpgPlayer = GetRPGPlayer(args[1]);
                    if (rpgPlayer == null) return 0;
                    return rpgPlayer.GhostStats.TryGetValue(statNameGhost.ToUpper(), out int ghostValue) ? ghostValue : 0;

                case "GetRebirthCount":
                    if (args.Length < 2) { Logger.Warn("GetRebirthCount: Not enough arguments."); return null; }
                    rpgPlayer = GetRPGPlayer(args[1]);
                    return rpgPlayer?.RebirthCount;

                case "GetRebirthPoints":
                    if (args.Length < 2) { Logger.Warn("GetRebirthPoints: Not enough arguments."); return null; }
                    rpgPlayer = GetRPGPlayer(args[1]);
                    return rpgPlayer?.RebirthPoints;

                default:
                    Logger.Warn($"Unknown ModCall message: {message}");
                    return null;
            }
        }
    }
}