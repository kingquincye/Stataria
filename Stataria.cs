using System;
using Terraria.Localization;
using System.Collections.Generic;
using System.Linq;
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
        SyncAbilities,
        SyncSocketedItem,
        SyncNecromancerSouls,
        SyncBerserkerSavageRoar,
        SyncSpellweaverState,
        AngelResurrect,
        SyncAngelState,
        NecromancerHarvestSoulOnKill
    }

    public class Stataria : Mod
    {
        public static Dictionary<int, (bool IsElite, int Level, double CustomLifeMax)> pendingNpcScaling = new Dictionary<int, (bool, int, double)>();

        public override void Load()
        {
            pendingNpcScaling = new Dictionary<int, (bool, int, double)>();
            StatariaLogger.GlobalDebugMode = false;
            StatariaLogger.Initialize(this);
            StatariaLogger.Info(Language.GetTextValue("Mods.Stataria.Logging.Stataria.ModLoadingStarted"));

            base.Load();

            StatariaLogger.Info(Language.GetTextValue("Mods.Stataria.Logging.Stataria.ModLoadingCompleted"));
        }

        public override void Unload()
        {
            base.Unload();

            StatariaUI.StatUI = null;
            StatariaUI.Panel = null;
            StatariaUI.SkillTreeUI = null;
            StatariaUI.SkillTreePanel = null;
            StatariaUI.XPVerificationUI = null;
            StatariaUI.RoleSelectionUI = null;
            StatariaUI.RoleSelectionPanel = null;
            StatariaUI.TabBarInterface = null;
            StatariaUI.TabBarPanel = null;
            StatariaUI.SocketingUI = null;
            StatariaUI.SocketingPanel = null;
            pendingNpcScaling = null;
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

            var scalingData = npc.GetGlobalNPC<StatariaScalingGlobalNPC>();

            var packet = ModContent.GetInstance<Stataria>().GetPacket();
            packet.Write((byte)StatariaMessageType.SyncEliteStatus);
            packet.Write(npcIndex);
            packet.Write(scalingData.IsElite);
            packet.Write(scalingData.Level);
            packet.Write(scalingData.CustomLifeMax);
            packet.Send(toWho, fromWho);
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
                rpg.BLH = reader.ReadInt32();
                rpg.HNT = reader.ReadInt32();
                rpg.GMB = reader.ReadInt32();
                rpg.SHM = reader.ReadInt32();
                rpg.THR = reader.ReadInt32();
                rpg.PST = reader.ReadInt32();
                rpg.lastStandCooldownTimer = reader.ReadInt32();
                rpg.divineInterventionCooldownTimer = reader.ReadInt32();
                rpg.RebirthCount = reader.ReadInt32();
                rpg.RebirthPoints = reader.ReadInt32();
                int ascendedCount = reader.ReadInt32();
                rpg.AscendedRoles.Clear();
                for (int i = 0; i < ascendedCount; i++)
                {
                    rpg.AscendedRoles.Add(reader.ReadString());
                }
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

                        rpg.RawActiveRole = rpg.AvailableRoles[roleID];
                        rpg.RawActiveRole.Status = roleStatus;
                    }
                }
                else
                {
                    if (rpg.RawActiveRole != null)
                    {
                        rpg.RawActiveRole.Status = RoleStatus.Available;
                        rpg.RawActiveRole = null;
                    }
                }
                rpg.RoleSwitchCount = reader.ReadInt32();
                rpg.BossKillsCount = reader.ReadInt32();
                rpg.UpdateAscendedRoleProperties();

                if (Main.netMode == NetmodeID.MultiplayerClient && playerIndex == Main.myPlayer)
                {
                    StatariaUI.RoleSelectionPanel?.RefreshRolesList();
                }


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
                double customLifeMax = reader.ReadDouble();

                if (npcIndex >= 0 && npcIndex < Main.maxNPCs && Main.npc[npcIndex] != null)
                {
                    if (Main.npc[npcIndex].active)
                    {
                        var npcData = Main.npc[npcIndex].GetGlobalNPC<StatariaScalingGlobalNPC>();

                        if (!npcData.hasBeenScaled)
                        {
                            npcData.IsElite = isElite;
                            npcData.Level = level;
                            npcData.CustomLifeMax = customLifeMax;
                            npcData.ApplyScaling(Main.npc[npcIndex]);
                            npcData.hasBeenScaled = true;
                        }
                    }
                    else
                    {
                        pendingNpcScaling[npcIndex] = (isElite, level, customLifeMax);
                    }
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
            else if (msgType == StatariaMessageType.SyncSocketedItem)
            {
                int playerIndex = reader.ReadInt32();
                int itemSlot = reader.ReadInt32();
                
                if (playerIndex < 0 || playerIndex >= Main.maxPlayers)
                    return;
                    
                Player player = Main.player[playerIndex];
                if (player == null || !player.active)
                    return;
                    
                Item item = null;
                if (itemSlot >= 0 && itemSlot < player.inventory.Length)
                {
                    item = player.inventory[itemSlot];
                }
                else if (itemSlot == -1)
                {
                    if (StatariaUI.SocketingPanel != null)
                        item = StatariaUI.SocketingPanel.SocketingItemSlot;
                }
                
                if (item == null || item.IsAir)
                    return;
                    
                var socketingData = item.GetGlobalItem<SocketingGlobalItem>();
                
                int coreCount = reader.ReadInt32();
                socketingData.SocketedCores.Clear();
                
                for (int i = 0; i < coreCount; i++)
                {
                    CoreType type = (CoreType)reader.ReadInt32();
                    int tier = reader.ReadInt32();
                    int count = reader.ReadInt32();
                    socketingData.SocketedCores.Add(new SocketedCore(type, tier, count));
                }
                
                socketingData.ExpandedSlots = reader.ReadInt32();
                socketingData.MaxSlots = SocketingGlobalItem.GetBaseSlots(item) + socketingData.ExpandedSlots;
                
                if (Main.netMode == NetmodeID.Server)
                {
                    var packet = ModContent.GetInstance<Stataria>().GetPacket();
                    packet.Write((byte)StatariaMessageType.SyncSocketedItem);
                    packet.Write(playerIndex);
                    packet.Write(itemSlot);
                    packet.Write(coreCount);
                    
                    foreach (var core in socketingData.SocketedCores)
                    {
                        packet.Write((int)core.Type);
                        packet.Write(core.Tier);
                        packet.Write(core.Count);
                    }
                    
                    packet.Write(socketingData.ExpandedSlots);
                    packet.Send(-1, whoAmI);
                }
                
                if (Main.LocalPlayer.whoAmI == playerIndex && StatariaUI.SocketingUI?.CurrentState != null)
                {
                    StatariaUI.SocketingPanel?.RefreshUI();
                }
            }
            else if (msgType == StatariaMessageType.SyncNecromancerSouls)
            {
                int playerIndex = reader.ReadInt32();
                if (playerIndex >= 0 && playerIndex < Main.maxPlayers)
                {
                    var necPlayer = Main.player[playerIndex].GetModPlayer<NecromancerPlayer>();
                    int count = reader.ReadInt32();
                    necPlayer.SoulReserveLifetimes.Clear();
                    for (int i = 0; i < count; i++)
                    {
                        necPlayer.SoulReserveLifetimes.Add(reader.ReadSingle());
                    }
                    necPlayer.IsRecalled = reader.ReadBoolean();

                    if (Main.netMode == NetmodeID.Server)
                    {
                        necPlayer.SyncSouls(toWho: -1, fromWho: whoAmI);
                    }
                }
            }
            else if (msgType == StatariaMessageType.SyncBerserkerSavageRoar)
            {
                int playerIndex = reader.ReadInt32();
                int roarTimer = reader.ReadInt32();
                int roarCooldown = reader.ReadInt32();

                if (playerIndex >= 0 && playerIndex < Main.maxPlayers)
                {
                    var berserkerPlayer = Main.player[playerIndex].GetModPlayer<BerserkerPlayer>();
                    bool wasActive = berserkerPlayer.SavageRoarTimer > 0;
                    berserkerPlayer.SavageRoarTimer = roarTimer;
                    berserkerPlayer.SavageRoarCooldownTimer = roarCooldown;

                    if (Main.netMode == NetmodeID.Server)
                    {
                        berserkerPlayer.SyncSavageRoar(toWho: -1, fromWho: whoAmI);
                    }
                    else
                    {
                        if (!wasActive && roarTimer > 0 && playerIndex != Main.myPlayer)
                        {
                            SoundEngine.PlaySound(SoundID.Roar, berserkerPlayer.Player.position);
                            CombatText.NewText(berserkerPlayer.Player.Hitbox, Color.Red, "Savage Roar!", true);

                            for (int i = 0; i < 30; i++)
                            {
                                Vector2 vel = Main.rand.NextVector2Circular(5f, 5f);
                                Dust d = Dust.NewDustPerfect(berserkerPlayer.Player.Center, DustID.Blood, vel, 0, default, 1.8f);
                                d.noGravity = true;
                            }
                        }
                    }
                }
            }
            else if (msgType == StatariaMessageType.SyncSpellweaverState)
            {
                int playerIndex = reader.ReadInt32();
                float charge = reader.ReadSingle();

                if (playerIndex >= 0 && playerIndex < Main.maxPlayers)
                {
                    var spellweaverPlayer = Main.player[playerIndex].GetModPlayer<SpellweaverPlayer>();
                    spellweaverPlayer.ElementalCharge = charge;

                    if (Main.netMode == NetmodeID.Server)
                    {
                        spellweaverPlayer.SyncSpellweaverState(toWho: -1, fromWho: whoAmI);
                    }
                }
            }
            else if (msgType == StatariaMessageType.AngelResurrect)
            {
                int angelWhoAmI = reader.ReadInt32();
                float healPercent = reader.ReadSingle();
                float invulTime = reader.ReadSingle();

                if (Main.netMode == NetmodeID.Server)
                {
                    var packet = GetPacket();
                    packet.Write((byte)StatariaMessageType.AngelResurrect);
                    packet.Write(angelWhoAmI);
                    packet.Write(healPercent);
                    packet.Write(invulTime);
                    packet.Send();
                }
                else
                {
                    if (angelWhoAmI >= 0 && angelWhoAmI < Main.maxPlayers)
                    {
                        Player angel = Main.player[angelWhoAmI];
                        if (angel != null && angel.active)
                        {
                            var config = ModContent.GetInstance<StatariaConfig>();
                            for (int i = 0; i < Main.maxPlayers; i++)
                            {
                                Player other = Main.player[i];
                                if (other == null || !other.active)
                                    continue;

                                bool isTeammate = false;
                                if (angel.team != 0 && angel.team == other.team)
                                {
                                    isTeammate = true;
                                }
                                else if (config.roleSettings.ClericAllowAuraOnNoTeam && angel.team == 0 && other.team == 0)
                                {
                                    isTeammate = true;
                                }

                                if (!isTeammate)
                                    continue;

                                var otherCleric = other.GetModPlayer<ClericPlayer>();
                                if (otherCleric.IsInSpiritForm)
                                {
                                    float distance = Vector2.Distance(angel.Center, other.Center);
                                    if (distance <= config.roleSettings.AngelAuraRadius)
                                    {
                                        SoundEngine.PlaySound(SoundID.Item4, other.Center);
                                        for (int d = 0; d < 30; d++)
                                        {
                                            Dust dust = Dust.NewDustPerfect(other.Center + Main.rand.NextVector2Circular(20f, 20f), DustID.GoldFlame, Main.rand.NextVector2Circular(3f, 3f), 100, Color.Gold, 1.5f);
                                            dust.noGravity = true;
                                        }
                                        if (Main.myPlayer == other.whoAmI)
                                        {
                                            otherCleric.ResurrectLocal(healPercent, invulTime);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else if (msgType == StatariaMessageType.SyncAngelState)
            {
                int playerIndex = reader.ReadInt32();
                if (playerIndex >= 0 && playerIndex < Main.maxPlayers)
                {
                    var clericPlayer = Main.player[playerIndex].GetModPlayer<ClericPlayer>();
                    bool wasInSpirit = clericPlayer.IsInSpiritForm;
                    clericPlayer.IsInSpiritForm = reader.ReadBoolean();
                    clericPlayer.SpiritFormTimer = reader.ReadInt32();
                    clericPlayer.SpiritAngelWhoAmI = reader.ReadInt32();
                    clericPlayer.DivineResurrectionCooldownTimer = reader.ReadInt32();
                    
                    bool wasChanneling = clericPlayer.IsResurrectionChanneling;
                    clericPlayer.IsResurrectionChanneling = reader.ReadBoolean();
                    clericPlayer.ChannelingTimer = reader.ReadInt32();
                    clericPlayer.ChannelingMaxTime = reader.ReadInt32();

                    if (Main.netMode == NetmodeID.Server)
                    {
                        clericPlayer.SyncAngelState(toWho: -1, fromWho: whoAmI);
                    }
                    else
                    {
                        if (playerIndex != Main.myPlayer)
                        {
                            // Play spirit form entry effects
                            if (!wasInSpirit && clericPlayer.IsInSpiritForm)
                            {
                                SoundEngine.PlaySound(SoundID.Item8, clericPlayer.Player.Center);
                                for (int i = 0; i < 20; i++)
                                {
                                    Dust dust = Dust.NewDustPerfect(clericPlayer.Player.Center, DustID.MagicMirror, Main.rand.NextVector2Circular(3f, 3f), 150, Color.Cyan, 1.2f);
                                    dust.noGravity = true;
                                }
                            }
                            
                            // Play channeling start sound
                            if (!wasChanneling && clericPlayer.IsResurrectionChanneling)
                            {
                                SoundEngine.PlaySound(SoundID.Item29, clericPlayer.Player.Center);
                            }
                        }
                    }
                }
            }
            else if (msgType == StatariaMessageType.NecromancerHarvestSoulOnKill)
            {
                if (Main.netMode == NetmodeID.MultiplayerClient)
                {
                    var necPlayer = Main.LocalPlayer.GetModPlayer<NecromancerPlayer>();
                    if (necPlayer.IsNecromancerActive)
                    {
                        necPlayer.HarvestSoul();
                    }
                }
            }
        }

        public override object Call(params object[] args)
        {
            if (args == null || args.Length == 0)
            {
                Logger.Warn(Language.GetTextValue("Mods.Stataria.Logging.Stataria.ModCallNoArgs"));
                return null;
            }

            if (!(args[0] is string message))
            {
                Logger.Warn(Language.GetTextValue("Mods.Stataria.Logging.Stataria.ModCallFirstArgString"));
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
                Logger.Warn(Language.GetTextValue("Mods.Stataria.Logging.Stataria.ModCallExpectedPlayer", message));
                return null;
            }

            NPC GetNPC(object npcArg)
            {
                if (npcArg is NPC npc)
                {
                    return npc;
                }
                else if (npcArg is int npcIndex && npcIndex >= 0 && npcIndex < Main.maxNPCs)
                {
                    return Main.npc[npcIndex];
                }
                Logger.Warn($"Stataria Mod.Call ({message}): Expected NPC instance or NPC index as second argument.");
                return null;
            }

            switch (message)
            {
                case "GetPlayerLevel":
                    if (args.Length < 2) { Logger.Warn(Language.GetTextValue("Mods.Stataria.Logging.Stataria.ModCallNotEnoughArgs", "GetPlayerLevel")); return null; }
                    rpgPlayer = GetRPGPlayer(args[1]);
                    return rpgPlayer?.Level;

                case "GetPlayerXP":
                    if (args.Length < 2) { Logger.Warn(Language.GetTextValue("Mods.Stataria.Logging.Stataria.ModCallNotEnoughArgs", "GetPlayerXP")); return null; }
                    rpgPlayer = GetRPGPlayer(args[1]);
                    return rpgPlayer?.XP;

                case "GetXPToNextLevel":
                    if (args.Length < 2) { Logger.Warn(Language.GetTextValue("Mods.Stataria.Logging.Stataria.ModCallNotEnoughArgs", "GetXPToNextLevel")); return null; }
                    rpgPlayer = GetRPGPlayer(args[1]);
                    return rpgPlayer?.XPToNext;

                case "GetStatPoints":
                    if (args.Length < 2) { Logger.Warn(Language.GetTextValue("Mods.Stataria.Logging.Stataria.ModCallNotEnoughArgs", "GetStatPoints")); return null; }
                    rpgPlayer = GetRPGPlayer(args[1]);
                    return rpgPlayer?.StatPoints;

                case "GetAllPlayerStats":
                    if (args.Length < 2) { Logger.Warn(Language.GetTextValue("Mods.Stataria.Logging.Stataria.ModCallNotEnoughArgs", "GetAllPlayerStats")); return null; }
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
                        { "CLK", rpgPlayer.CLK },
                        { "BLH", rpgPlayer.BLH },
                        { "HNT", rpgPlayer.HNT },
                        { "GMB", rpgPlayer.GMB },
                        { "SHM", rpgPlayer.SHM },
                        { "THR", rpgPlayer.THR },
                        { "PST", rpgPlayer.PST }
                    };

                case "GetEffectiveStat":
                    if (args.Length < 3 || !(args[2] is string statNameEff)) { Logger.Warn(Language.GetTextValue("Mods.Stataria.Logging.Stataria.ModCallIncorrectArgs", "GetEffectiveStat")); return null; }
                    rpgPlayer = GetRPGPlayer(args[1]);
                    return rpgPlayer?.GetEffectiveStat(statNameEff.ToUpper());

                case "GetGhostStat":
                    if (args.Length < 3 || !(args[2] is string statNameGhost)) { Logger.Warn(Language.GetTextValue("Mods.Stataria.Logging.Stataria.ModCallIncorrectArgs", "GetGhostStat")); return null; }
                    rpgPlayer = GetRPGPlayer(args[1]);
                    if (rpgPlayer == null) return 0;
                    return rpgPlayer.GhostStats.TryGetValue(statNameGhost.ToUpper(), out int ghostValue) ? ghostValue : 0;

                case "GetRebirthCount":
                    if (args.Length < 2) { Logger.Warn(Language.GetTextValue("Mods.Stataria.Logging.Stataria.ModCallNotEnoughArgs", "GetRebirthCount")); return null; }
                    rpgPlayer = GetRPGPlayer(args[1]);
                    return rpgPlayer?.RebirthCount;

                case "GetRebirthPoints":
                    if (args.Length < 2) { Logger.Warn(Language.GetTextValue("Mods.Stataria.Logging.Stataria.ModCallNotEnoughArgs", "GetRebirthPoints")); return null; }
                    rpgPlayer = GetRPGPlayer(args[1]);
                    return rpgPlayer?.RebirthPoints;

                case "GetNPCCustomLifeMax":
                    if (args.Length < 2) { Logger.Warn($"Stataria Mod.Call: Not enough arguments for GetNPCCustomLifeMax"); return null; }
                    var npcMax = GetNPC(args[1]);
                    if (npcMax == null) return null;
                    if (npcMax.TryGetGlobalNPC<StatariaScalingGlobalNPC>(out var scalingMax))
                    {
                        return scalingMax.CustomLifeMax;
                    }
                    return -1.0;

                case "GetNPCCustomLife":
                    if (args.Length < 2) { Logger.Warn($"Stataria Mod.Call: Not enough arguments for GetNPCCustomLife"); return null; }
                    var npcLife = GetNPC(args[1]);
                    if (npcLife == null) return null;
                    if (npcLife.TryGetGlobalNPC<StatariaScalingGlobalNPC>(out var scalingLife))
                    {
                        return scalingLife.CustomLife;
                    }
                    return -1.0;

                case "UsesNPCCustomHP":
                    if (args.Length < 2) { Logger.Warn($"Stataria Mod.Call: Not enough arguments for UsesNPCCustomHP"); return null; }
                    var npcUse = GetNPC(args[1]);
                    if (npcUse == null) return null;
                    if (npcUse.TryGetGlobalNPC<StatariaScalingGlobalNPC>(out var scalingUse))
                    {
                        return scalingUse.UsesCustomHP;
                    }
                    return false;

                default:
                    Logger.Warn(Language.GetTextValue("Mods.Stataria.Logging.Stataria.ModCallUnknownMessage", message));
                    return null;
            }
        }
    }
}