using Terraria.ModLoader;
using Terraria;
using Terraria.ID;
using Microsoft.Xna.Framework;
using System;
using Steamworks;
using System.Linq;
using System.Collections.Generic;

namespace Stataria
{
    public class StatariaCommands : ModCommand
    {
        private const string AdminSteamID = ""; // your steamID
        private static Dictionary<int, DateTime> selfResetConfirmations = new Dictionary<int, DateTime>();
        private static readonly TimeSpan ConfirmationTimeout = TimeSpan.FromSeconds(30);

        public override CommandType Type => CommandType.Chat;

        public override string Command => "stataria";

        public override string Usage => "/stataria <reset | selfreset | setlevel x | setxp x | setpoints [rp] x | setstat name x | clearbosses | syncbosses | debug | diagnose | weapondebug | testxpui [amount] [total_count] [current_index] [source] | cal <fillrage | filladrenaline | infrage | infadren>>";

        public override string Description => "Commands for Stataria mod";

        private bool IsAdmin(CommandCaller caller)
        {
            if (!Main.dedServ && Main.netMode != NetmodeID.Server)
            {
                if (SteamUser.BLoggedOn())
                {
                    var steamId = SteamUser.GetSteamID();
                    if (steamId.m_SteamID.ToString() != AdminSteamID)
                    {
                        caller.Reply("You do not have permission to use this command.", Color.Red);
                        return false;
                    }
                }
                else
                {
                    caller.Reply("Steam not available. Cannot verify admin identity.", Color.Red);
                    return false;
                }
            }
            return true;
        }

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            var rpg = caller.Player.GetModPlayer<RPGPlayer>();
            var cfg = ModContent.GetInstance<StatariaConfig>();

            if (args.Length == 0)
            {
                caller.Reply("Usage: " + Usage, Color.Red);
                return;
            }

            switch (args[0].ToLower())
            {
                case "reset":
                    if (!IsAdmin(caller)) return;

                    rpg.Level = 1;
                    rpg.XP = 0L;
                    rpg.XPToNext = (long)(100L * Math.Pow(rpg.Level, cfg.generalBalance.LevelScalingFactor));
                    rpg.StatPoints = 0;
                    rpg.VIT = rpg.STR = rpg.AGI = rpg.INT = rpg.LUC = rpg.END = rpg.POW = rpg.DEX = rpg.SPR = rpg.RGE = rpg.BRD = rpg.HLR = rpg.TCH = rpg.CLK = 0;
                    rpg.rewardedBosses.Clear();
                    rpg.RebirthCount = 0;
                    rpg.RebirthPoints = 0;
                    rpg.WasRetroRPGranted = false;
                    rpg.AutoAllocateEnabled = false;
                    rpg.AutoAllocateStats.Clear();
                    foreach (var ability in rpg.RebirthAbilities.Values)
                    {
                        ability.IsUnlocked = false;
                        ability.Level = 0;
                        if (ability.AbilityType == RebirthAbilityType.Toggleable && ability.AbilityData.ContainsKey("Enabled"))
                        {
                            ability.AbilityData["Enabled"] = false;
                        }
                    }

                    rpg.ResetRoles();

                    if (Main.netMode != NetmodeID.SinglePlayer)
                    {
                        rpg.SyncPlayer(-1, caller.Player.whoAmI, false);
                        rpg.SyncAbilities();
                    }

                    caller.Reply("Stataria reset!", Color.Orange);
                    break;

                case "selfreset":
                    HandleSelfReset(caller);
                    break;

                case "setlevel":
                    if (!IsAdmin(caller)) return;

                    if (args.Length >= 2 && int.TryParse(args[1], out int level))
                    {
                        rpg.Level = Math.Max(1, level);
                        rpg.XP = 0L;
                        rpg.XPToNext = (long)(100L * Math.Pow(rpg.Level, cfg.generalBalance.LevelScalingFactor));

                        if (Main.netMode != NetmodeID.SinglePlayer)
                        {
                            rpg.SyncPlayer(-1, caller.Player.whoAmI, false);
                        }

                        caller.Reply($"Level set to {level}", Color.LightGreen);
                    }
                    else caller.Reply("Usage: /stataria setlevel <number>", Color.Red);
                    break;

                case "setxp":
                    if (!IsAdmin(caller)) return;

                    if (args.Length >= 2 && long.TryParse(args[1], out long xp))
                    {
                        rpg.XP = xp;

                        if (Main.netMode != NetmodeID.SinglePlayer)
                        {
                            rpg.SyncPlayer(-1, caller.Player.whoAmI, false);
                        }

                        caller.Reply($"XP set to {xp:N0}", Color.Yellow);
                    }
                    else caller.Reply("Usage: /stataria setxp <number>", Color.Red);
                    break;

                case "setpoints":
                    if (!IsAdmin(caller)) return;

                    if (args.Length >= 2)
                    {
                        if (args[1].ToLower() == "rp")
                        {
                            if (args.Length >= 3 && int.TryParse(args[2], out int rebirthPts))
                            {
                                rpg.RebirthPoints = rebirthPts;

                                if (Main.netMode != NetmodeID.SinglePlayer)
                                {
                                    rpg.SyncPlayer(-1, caller.Player.whoAmI, false);
                                }

                                caller.Reply($"Rebirth Points set to {rebirthPts}", Color.Gold);
                            }
                            else caller.Reply("Usage: /stataria setpoints rp <number>", Color.Red);
                        }
                        else if (int.TryParse(args[1], out int statPts))
                        {
                            rpg.StatPoints = statPts;

                            if (Main.netMode != NetmodeID.SinglePlayer)
                            {
                                rpg.SyncPlayer(-1, caller.Player.whoAmI, false);
                            }

                            caller.Reply($"Stat points set to {statPts}", Color.Purple);
                        }
                        else
                        {
                            caller.Reply("Usage: /stataria setpoints <number> or /stataria setpoints rp <number>", Color.Red);
                        }
                    }
                    else caller.Reply("Usage: /stataria setpoints <number> or /stataria setpoints rp <number>", Color.Red);
                    break;

                case "setstat":
                    if (!IsAdmin(caller)) return;

                    if (args.Length >= 3 && int.TryParse(args[2], out int val))
                    {
                        bool success = SetStatByName(rpg, args[1], val);
                        if (success)
                        {
                            if (Main.netMode != NetmodeID.SinglePlayer)
                            {
                                rpg.SyncPlayer(-1, caller.Player.whoAmI, false);
                            }

                            caller.Reply($"{args[1].ToUpper()} set to {val}", Color.Green);
                        }
                        else
                            caller.Reply("Unknown stat name. Valid: vit, str, agi, int, luc, end, pow, dex, spr, tch, rge, brd, hlr, clk", Color.Red);
                    }
                    else caller.Reply("Usage: /stataria setstat <name> <value>", Color.Red);
                    break;

                case "clearbosses":
                    if (!IsAdmin(caller)) return;

                    rpg.rewardedBosses.Clear();

                    if (Main.netMode != NetmodeID.SinglePlayer)
                    {
                        rpg.SyncPlayer(-1, caller.Player.whoAmI, false);
                    }

                    caller.Reply("Rewarded boss XP progress cleared.", Color.Cyan);
                    break;

                case "syncbosses":
                    if (!IsAdmin(caller)) return;

                    if (Main.netMode == NetmodeID.Server || Main.netMode == NetmodeID.MultiplayerClient)
                    {
                        StatariaSystem.SyncGlobalBosses();
                        caller.Reply("Forced boss list sync.", Color.Green);
                    }
                    else
                    {
                        caller.Reply("Cannot sync in single player.", Color.Red);
                    }
                    break;

                case "debug":
                    if (!IsAdmin(caller)) return;

                    bool newDebugMode = !StatariaLogger.GlobalDebugMode;
                    StatariaLogger.UpdateDebugMode(ModContent.GetInstance<Stataria>(), newDebugMode);

                    caller.Reply($"Debug mode: {(StatariaLogger.GlobalDebugMode ? "ON" : "OFF")}", Color.Yellow);
                    StatariaLogger.Info($"Debug mode toggled to {(StatariaLogger.GlobalDebugMode ? "ON" : "OFF")}");
                    break;

                case "diagnose":
                    if (!IsAdmin(caller)) return;

                    CalamitySupportHelper.RunFieldValidation();
                    ThoriumSupportHelper.RunFieldValidation();

                    bool calIntegrationOk = CalamitySupportHelper.IsCalamityIntegrationWorking();
                    bool thorIntegrationOk = ThoriumSupportHelper.IsThoriumIntegrationWorking();

                    caller.Reply("System Diagnostics:", Color.Yellow);

                    if (CalamitySupportHelper.CalamityLoaded) {
                        caller.Reply($"Calamity integration status: {(calIntegrationOk ? "WORKING" : "ERRORS DETECTED")}",
                                    calIntegrationOk ? Color.Green : Color.Red);
                        StatariaLogger.Info($"Calamity integration status: {(calIntegrationOk ? "WORKING" : "ERRORS DETECTED")}");

                        if (!calIntegrationOk)
                        {
                            caller.Reply("Found fields:", Color.Yellow);
                            caller.Reply($"Rogue Class: {CalamitySupportHelper.FoundRogueClass}",
                                        CalamitySupportHelper.FoundRogueClass ? Color.Green : Color.Red);
                            StatariaLogger.Info($"Rogue Class: {CalamitySupportHelper.FoundRogueClass}");
                            StatariaLogger.Info($"Rogue Stealth: {CalamitySupportHelper.FoundRogueStealth}");
                            StatariaLogger.Info($"Max Stealth: {CalamitySupportHelper.FoundRogueStealthMax}");
                            StatariaLogger.Info($"Standstill Gen: {CalamitySupportHelper.FoundStealthGenStandstill}");
                            StatariaLogger.Info($"Moving Gen: {CalamitySupportHelper.FoundStealthGenMoving}");
                            StatariaLogger.Info($"Stealth Damage: {CalamitySupportHelper.FoundStealthDamage}");
                            StatariaLogger.Info($"Rogue Velocity: {CalamitySupportHelper.FoundRogueVelocity}");
                            StatariaLogger.Info($"Rogue Ammo Cost: {CalamitySupportHelper.FoundRogueAmmoCost}");
                            StatariaLogger.Info($"Rage: {CalamitySupportHelper.FoundRage}");
                            StatariaLogger.Info($"Rage Max: {CalamitySupportHelper.FoundRageMax}");
                            StatariaLogger.Info($"Rage Duration: {CalamitySupportHelper.FoundRageDuration}");
                            StatariaLogger.Info($"Rage Damage: {CalamitySupportHelper.FoundRageDamage}");
                            StatariaLogger.Info($"Adrenaline: {CalamitySupportHelper.FoundAdrenaline}");
                            StatariaLogger.Info($"Adrenaline Max: {CalamitySupportHelper.FoundAdrenalineMax}");
                            StatariaLogger.Info($"Adrenaline Duration: {CalamitySupportHelper.FoundAdrenalineDuration}");
                        }
                    }

                    if (ThoriumSupportHelper.ThoriumLoaded) {
                        caller.Reply($"Thorium integration status: {(thorIntegrationOk ? "WORKING" : "ERRORS DETECTED")}",
                                    thorIntegrationOk ? Color.Green : Color.Red);
                        StatariaLogger.Info($"Thorium integration status: {(thorIntegrationOk ? "WORKING" : "ERRORS DETECTED")}");
                    }
                    break;

                case "weapondebug":
                    if (!IsAdmin(caller)) return;

                    Player player = caller.Player;
                    Item heldItem = player.HeldItem;

                    if (heldItem == null || heldItem.IsAir)
                    {
                        caller.Reply("No weapon equipped.", Color.Red);
                        return;
                    }

                    caller.Reply($"===== Weapon Debug Info =====", Color.Yellow);
                    caller.Reply($"Name: {heldItem.Name}", Color.White);
                    caller.Reply($"Type: {heldItem.type}", Color.White);
                    caller.Reply($"Base Damage: {heldItem.damage}", Color.White);

                    string damageTypeName = heldItem.DamageType?.GetType().Name ?? "None";
                    string damageTypeString = heldItem.DamageType?.ToString() ?? "None";

                    caller.Reply($"DamageType Class: {damageTypeName}", Color.White);
                    caller.Reply($"DamageType String: {damageTypeString}", Color.White);

                    caller.Reply("Damage Classes:", Color.White);
                    caller.Reply($"  Melee: {heldItem.CountsAsClass(DamageClass.Melee)}", Color.White);
                    caller.Reply($"  Ranged: {heldItem.CountsAsClass(DamageClass.Ranged)}", Color.White);
                    caller.Reply($"  Magic: {heldItem.CountsAsClass(DamageClass.Magic)}", Color.White);
                    caller.Reply($"  Summon: {heldItem.CountsAsClass(DamageClass.Summon)}", Color.White);

                    if (heldItem.ModItem != null)
                    {
                        caller.Reply("Mod Item Info:", Color.White);
                        caller.Reply($"  Mod: {heldItem.ModItem.Mod.Name}", Color.White);
                        caller.Reply($"  Class: {heldItem.ModItem.GetType().Name}", Color.White);
                        caller.Reply($"  Namespace: {heldItem.ModItem.GetType().Namespace}", Color.White);
                        caller.Reply($"  FullName: {heldItem.ModItem.GetType().FullName}", Color.White);
                    }

                    if (CalamitySupportHelper.CalamityLoaded)
                    {
                        bool isRogueWeapon = CalamitySupportHelper.IsRogueWeapon(heldItem);
                        caller.Reply($"Is Rogue Weapon: {isRogueWeapon}",
                            isRogueWeapon ? Color.Green : Color.Red);

                        caller.Reply("Rogue Detection Details:", Color.White);

                        if (heldItem.ModItem?.Mod?.Name == "CalamityMod")
                        {
                            caller.Reply("  Is Calamity ModItem: True", Color.Green);
                        }

                        StatariaLogger.Debug($"Weapon debug: {heldItem.Name} is Rogue weapon: {isRogueWeapon}");
                    }
                    else
                    {
                        caller.Reply("Calamity mod not loaded.", Color.Yellow);
                    }

                    var config = ModContent.GetInstance<StatariaConfig>();

                    caller.Reply("Stat Effects:", Color.Yellow);
                    caller.Reply($"  STR: {rpg.STR} (Melee DMG: +{rpg.STR * (config.statSettings.STR_Damage / 100f):F2})", Color.White);
                    caller.Reply($"  INT: {rpg.INT} (Magic DMG: +{rpg.INT * (config.statSettings.INT_Damage / 100f):F2})", Color.White);
                    caller.Reply($"  DEX: {rpg.DEX} (Ranged DMG: +{rpg.DEX * (config.statSettings.DEX_Damage / 100f):F2})", Color.White);
                    caller.Reply($"  SPR: {rpg.SPR} (Summon DMG: +{rpg.SPR * (config.statSettings.SPR_Damage / 100f):F2})", Color.White);
                    caller.Reply($"  RGE: {rpg.RGE} (Rogue DMG: +{rpg.RGE * (config.modIntegration.RGE_Damage / 100f):F2})", Color.White);
                    caller.Reply($"  POW: {rpg.POW} (Other DMG: +{rpg.POW * (config.statSettings.POW_Damage / 100f):F2})", Color.White);

                    string appliedStat = "None";
                    float statBonus = 0f;

                    bool isRogue = CalamitySupportHelper.CalamityLoaded && CalamitySupportHelper.IsRogueWeapon(heldItem);

                    if (heldItem.CountsAsClass(DamageClass.Melee)) {
                        appliedStat = "STR";
                        statBonus = rpg.STR * (config.statSettings.STR_Damage / 100f);
                    }
                    else if (heldItem.CountsAsClass(DamageClass.Magic)) {
                        appliedStat = "INT";
                        statBonus = rpg.INT * (config.statSettings.INT_Damage / 100f);
                    }
                    else if (heldItem.CountsAsClass(DamageClass.Ranged)) {
                        appliedStat = "DEX";
                        statBonus = rpg.DEX * (config.statSettings.DEX_Damage / 100f);
                    }
                    else if (heldItem.CountsAsClass(DamageClass.Summon)) {
                        appliedStat = "SPR";
                        statBonus = rpg.SPR * (config.statSettings.SPR_Damage / 100f);
                    }
                    else if (isRogue) {
                        appliedStat = "RGE";
                        statBonus = rpg.RGE * (config.modIntegration.RGE_Damage / 100f);
                    }
                    else {
                        appliedStat = "POW";
                        statBonus = rpg.POW * (config.statSettings.POW_Damage / 100f);
                    }

                    caller.Reply($"Applied Stat Boost: {appliedStat} (+{statBonus:F2})", Color.Yellow);
                    StatariaLogger.Debug($"Applied stat boost: {appliedStat} (+{statBonus:F2})");
                    break;

                case "cal":
                    if (!IsAdmin(caller)) return;

                    if (!CalamitySupportHelper.CalamityLoaded)
                    {
                        caller.Reply("Error: Calamity Mod not detected. These commands require Calamity Mod.", Color.Red);
                        return;
                    }

                    var configCal = ModContent.GetInstance<StatariaConfig>();
                    if (!configCal.modIntegration.EnableCalamityIntegration)
                    {
                        caller.Reply("Error: Calamity integration is disabled in the config.", Color.Red);
                        return;
                    }

                    if (args.Length < 2)
                    {
                        caller.Reply("Usage: /stataria cal <fillrage | filladrenaline | infrage | infadren>", Color.Red);
                        return;
                    }

                    switch (args[1].ToLower())
                    {
                        case "fillrage":
                            if (CalamitySupportHelper.FoundRage && CalamitySupportHelper.FoundRageMax)
                            {
                                float rageMax = CalamitySupportHelper.GetRageMax(caller.Player);
                                CalamitySupportHelper.SetRage(caller.Player, rageMax);
                                caller.Reply($"Rage filled to maximum ({rageMax}).", Color.Orange);
                                StatariaLogger.Debug($"Rage filled to maximum ({rageMax}).");
                            }
                            else
                            {
                                caller.Reply("Error: Could not access Rage values.", Color.Red);
                            }
                            break;

                        case "filladrenaline":
                            if (CalamitySupportHelper.FoundAdrenaline && CalamitySupportHelper.FoundAdrenalineMax)
                            {
                                float adrenalineMax = CalamitySupportHelper.GetAdrenalineMax(caller.Player);
                                CalamitySupportHelper.SetAdrenaline(caller.Player, adrenalineMax);
                                caller.Reply($"Adrenaline filled to maximum ({adrenalineMax}).", Color.Cyan);
                                StatariaLogger.Debug($"Adrenaline filled to maximum ({adrenalineMax}).");
                            }
                            else
                            {
                                caller.Reply("Error: Could not access Adrenaline values.", Color.Red);
                            }
                            break;

                        case "infrage":
                            CalamitySupportHelper.ToggleInfiniteRage();
                            caller.Reply($"Infinite Rage: {(CalamitySupportHelper.InfiniteRageEnabled ? "ON" : "OFF")}",
                                        CalamitySupportHelper.InfiniteRageEnabled ? Color.Green : Color.Red);
                            StatariaLogger.Debug($"Infinite Rage toggled to: {(CalamitySupportHelper.InfiniteRageEnabled ? "ON" : "OFF")}");
                            if (CalamitySupportHelper.InfiniteRageEnabled)
                            {
                                float rageMax = CalamitySupportHelper.GetRageMax(caller.Player);
                                CalamitySupportHelper.SetRage(caller.Player, rageMax);
                            }
                            break;

                        case "infadren":
                            CalamitySupportHelper.ToggleInfiniteAdrenaline();
                            caller.Reply($"Infinite Adrenaline: {(CalamitySupportHelper.InfiniteAdrenalineEnabled ? "ON" : "OFF")}",
                                        CalamitySupportHelper.InfiniteAdrenalineEnabled ? Color.Green : Color.Red);
                            StatariaLogger.Debug($"Infinite Adrenaline toggled to: {(CalamitySupportHelper.InfiniteAdrenalineEnabled ? "ON" : "OFF")}");
                            if (CalamitySupportHelper.InfiniteAdrenalineEnabled)
                            {
                                float adrenalineMax = CalamitySupportHelper.GetAdrenalineMax(caller.Player);
                                CalamitySupportHelper.SetAdrenaline(caller.Player, adrenalineMax);
                            }
                            break;

                        default:
                            caller.Reply("Unknown Calamity subcommand. Usage: /stataria cal <fillrage | filladrenaline | infrage | infadren>", Color.Red);
                            break;
                    }
                    break;

                case "testxpui":
                    if (!IsAdmin(caller))
                        return;

                    long testAmount = 100000;
                    string testSource = "Test Source";
                    int testCount = 1;
                    int testIndex = 1;

                    if (args.Length >= 2 && long.TryParse(args[1], out long amount))
                    {
                        testAmount = amount;
                    }

                    if (args.Length >= 3 && int.TryParse(args[2], out int count))
                    {
                        testCount = count;
                    }

                    if (args.Length >= 4 && int.TryParse(args[3], out int index))
                    {
                        testIndex = index;
                        testIndex = Math.Min(testIndex, testCount);
                    }

                    if (args.Length >= 5)
                    {
                        testSource = string.Join(" ", args.Skip(4));
                    }

                    var testVerification = new PendingXPGain(testAmount, testSource);

                    XPVerificationUI.ShowVerification(
                        testVerification,
                        testIndex,
                        testCount,
                        () => {
                            caller.Reply("XP accepted!", Color.Green);
                            XPVerificationUI.HideVerification();
                        },
                        () => {
                            caller.Reply("XP rejected!", Color.Red);
                            XPVerificationUI.HideVerification();
                        },
                        () => {
                            caller.Reply("All XP accepted!", Color.Green);
                            XPVerificationUI.HideVerification();
                        },
                        () => {
                            caller.Reply("All XP rejected!", Color.Red);
                            XPVerificationUI.HideVerification();
                        }
                    );

                    caller.Reply($"Showing XP verification UI with {testAmount} XP from '{testSource}', notification {testIndex}/{testCount}", Color.Yellow);
                    break;

                default:
                    caller.Reply("Unknown subcommand. " + Usage, Color.Red);
                    break;
            }
        }

        private void HandleSelfReset(CommandCaller caller)
        {
            var config = ModContent.GetInstance<StatariaConfig>();

            if (Main.netMode == NetmodeID.MultiplayerClient && !config.multiplayerSettings.AllowSelfResetInMultiplayer)
            {
                caller.Reply("Self-reset is disabled in multiplayer on this server.", Color.Red);
                return;
            }

            int playerId = caller.Player.whoAmI;
            DateTime now = DateTime.Now;

            var expiredKeys = selfResetConfirmations.Where(kvp => now - kvp.Value > ConfirmationTimeout).Select(kvp => kvp.Key).ToList();
            foreach (var key in expiredKeys)
            {
                selfResetConfirmations.Remove(key);
            }

            if (selfResetConfirmations.ContainsKey(playerId))
            {
                ExecuteSelfReset(caller);
                selfResetConfirmations.Remove(playerId);
            }
            else
            {
                selfResetConfirmations[playerId] = now;
                caller.Reply("WARNING: This will completely reset your Stataria progress!", Color.Red);
                caller.Reply("All levels, stats, XP, rebirth progress, and abilities will be lost!", Color.Red);
                caller.Reply("Type '/stataria selfreset' again within 30 seconds to confirm.", Color.Yellow);
            }
        }

        private void ExecuteSelfReset(CommandCaller caller)
        {
            var rpg = caller.Player.GetModPlayer<RPGPlayer>();
            var cfg = ModContent.GetInstance<StatariaConfig>();

            rpg.Level = 1;
            rpg.XP = 0L;
            rpg.XPToNext = (long)(100L * Math.Pow(rpg.Level, cfg.generalBalance.LevelScalingFactor));
            rpg.StatPoints = 0;
            rpg.VIT = rpg.STR = rpg.AGI = rpg.INT = rpg.LUC = rpg.END = rpg.POW = rpg.DEX = rpg.SPR = rpg.RGE = rpg.BRD = rpg.HLR = rpg.TCH = rpg.CLK = 0;
            rpg.rewardedBosses.Clear();
            rpg.RebirthCount = 0;
            rpg.RebirthPoints = 0;
            rpg.WasRetroRPGranted = false;
            rpg.AutoAllocateEnabled = false;
            rpg.AutoAllocateStats.Clear();

            foreach (var ability in rpg.RebirthAbilities.Values)
            {
                ability.IsUnlocked = false;
                ability.Level = 0;
                if (ability.AbilityType == RebirthAbilityType.Toggleable && ability.AbilityData.ContainsKey("Enabled"))
                {
                    ability.AbilityData["Enabled"] = false;
                }
            }

            rpg.ResetRoles();

            if (Main.netMode != NetmodeID.SinglePlayer)
            {
                rpg.SyncPlayer(-1, caller.Player.whoAmI, false);
                rpg.SyncAbilities();
            }

            caller.Reply("Your Stataria progress has been completely reset!", Color.Orange);
        }

        private bool SetStatByName(RPGPlayer rpg, string name, int value)
        {
            switch (name.ToLower())
            {
                case "vit": rpg.VIT = value; return true;
                case "str": rpg.STR = value; return true;
                case "agi": rpg.AGI = value; return true;
                case "int": rpg.INT = value; return true;
                case "luc": rpg.LUC = value; return true;
                case "end": rpg.END = value; return true;
                case "pow": rpg.POW = value; return true;
                case "dex": rpg.DEX = value; return true;
                case "spr": rpg.SPR = value; return true;
                case "tch": rpg.TCH = value; return true;
                case "rge": rpg.RGE = value; return true;
                case "brd": rpg.BRD = value; return true;
                case "hlr": rpg.HLR = value; return true;
                case "clk": rpg.CLK = value; return true;
                default: return false;
            }
        }
    }
}