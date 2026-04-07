using Terraria.ModLoader;
using Terraria;
using Terraria.Localization;
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

        public override string Usage => Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.Usage");

        public override string Description => Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.Description");

        private bool IsAdmin(CommandCaller caller)
        {
            if (!Main.dedServ && Main.netMode != NetmodeID.Server)
            {
                if (SteamUser.BLoggedOn())
                {
                    var steamId = SteamUser.GetSteamID();
                    if (steamId.m_SteamID.ToString() != AdminSteamID)
                    {
                        caller.Reply(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.NoPermission"), Color.Red);
                        return false;
                    }
                }
                else
                {
                    caller.Reply(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.SteamNotAvailable"), Color.Red);
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
                caller.Reply(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.UsagePrefix", Usage), Color.Red);
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
                    rpg.VIT = rpg.STR = rpg.AGI = rpg.INT = rpg.LUC = rpg.END = rpg.POW = rpg.DEX = rpg.SPR = rpg.RGE = rpg.BRD = rpg.HLR = rpg.TCH = rpg.CLK = rpg.BLH = rpg.HNT = rpg.GMB = rpg.SHM = rpg.THR = 0;
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

                    caller.Reply(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.ResetSuccess"), Color.Orange);
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

                        caller.Reply(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.SetLevelSuccess", level), Color.LightGreen);
                    }
                    else caller.Reply(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.SetLevelUsage"), Color.Red);
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

                        caller.Reply(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.SetXPSuccess", xp.ToString("N0")), Color.Yellow);
                    }
                    else caller.Reply(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.SetXPUsage"), Color.Red);
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

                                caller.Reply(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.SetPointsRPSuccess", rebirthPts), Color.Gold);
                            }
                            else caller.Reply(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.SetPointsRPUsage"), Color.Red);
                        }
                        else if (int.TryParse(args[1], out int statPts))
                        {
                            rpg.StatPoints = statPts;

                            if (Main.netMode != NetmodeID.SinglePlayer)
                            {
                                rpg.SyncPlayer(-1, caller.Player.whoAmI, false);
                            }

                            caller.Reply(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.SetPointsStatSuccess", statPts), Color.Purple);
                        }
                        else
                        {
                            caller.Reply(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.SetPointsUsage"), Color.Red);
                        }
                    }
                    else caller.Reply(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.SetPointsUsage"), Color.Red);
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

                            caller.Reply(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.SetStatSuccess", args[1].ToUpper(), val), Color.Green);
                        }
                        else
                            caller.Reply(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.SetStatUnknown"), Color.Red);
                    }
                    else caller.Reply(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.SetStatUsage"), Color.Red);
                    break;

                case "clearbosses":
                    if (!IsAdmin(caller)) return;

                    rpg.rewardedBosses.Clear();

                    if (Main.netMode != NetmodeID.SinglePlayer)
                    {
                        rpg.SyncPlayer(-1, caller.Player.whoAmI, false);
                    }

                    caller.Reply(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.ClearBossesSuccess"), Color.Cyan);
                    break;

                case "syncbosses":
                    if (!IsAdmin(caller)) return;

                    if (Main.netMode == NetmodeID.Server || Main.netMode == NetmodeID.MultiplayerClient)
                    {
                        StatariaSystem.SyncGlobalBosses();
                        caller.Reply(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.SyncBossesSuccess"), Color.Green);
                    }
                    else
                    {
                        caller.Reply(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.SyncBossesSinglePlayer"), Color.Red);
                    }
                    break;

                case "debug":
                    if (!IsAdmin(caller)) return;

                    bool newDebugMode = !StatariaLogger.GlobalDebugMode;
                    StatariaLogger.UpdateDebugMode(ModContent.GetInstance<Stataria>(), newDebugMode);

                    caller.Reply(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.DebugModeToggle", StatariaLogger.GlobalDebugMode ? Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.StatusOn") : Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.StatusOff")), Color.Yellow);
                    StatariaLogger.Info(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.DebugModeToggleLog", StatariaLogger.GlobalDebugMode ? Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.StatusOn") : Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.StatusOff")));
                    break;

                case "diagnose":
                    if (!IsAdmin(caller)) return;

                    CalamitySupportHelper.RunFieldValidation();
                    ThoriumSupportHelper.RunFieldValidation();

                    bool calIntegrationOk = CalamitySupportHelper.IsCalamityIntegrationWorking();
                    bool thorIntegrationOk = ThoriumSupportHelper.IsThoriumIntegrationWorking();

                    caller.Reply(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.DiagnoseHeader"), Color.Yellow);

                    if (CalamitySupportHelper.CalamityLoaded) {
                        caller.Reply(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.DiagnoseCalStatus", calIntegrationOk ? Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.StatusWorking") : Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.StatusErrorsDetected")),
                                    calIntegrationOk ? Color.Green : Color.Red);
                        StatariaLogger.Info(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.DiagnoseCalStatus", calIntegrationOk ? Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.StatusWorking") : Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.StatusErrorsDetected")));

                        if (!calIntegrationOk)
                        {
                            caller.Reply(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.DiagnoseFoundFields"), Color.Yellow);
                            caller.Reply(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.DiagnoseField_RogueClass", CalamitySupportHelper.FoundRogueClass),
                                        CalamitySupportHelper.FoundRogueClass ? Color.Green : Color.Red);
                            StatariaLogger.Info(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.DiagnoseField_RogueClass", CalamitySupportHelper.FoundRogueClass));
                            StatariaLogger.Info(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.DiagnoseField_RogueStealth", CalamitySupportHelper.FoundRogueStealth));
                            StatariaLogger.Info(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.DiagnoseField_MaxStealth", CalamitySupportHelper.FoundRogueStealthMax));
                            StatariaLogger.Info(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.DiagnoseField_StandstillGen", CalamitySupportHelper.FoundStealthGenStandstill));
                            StatariaLogger.Info(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.DiagnoseField_MovingGen", CalamitySupportHelper.FoundStealthGenMoving));
                            StatariaLogger.Info(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.DiagnoseField_StealthDamage", CalamitySupportHelper.FoundStealthDamage));
                            StatariaLogger.Info(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.DiagnoseField_RogueVelocity", CalamitySupportHelper.FoundRogueVelocity));
                            StatariaLogger.Info(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.DiagnoseField_Rage", CalamitySupportHelper.FoundRage));
                            StatariaLogger.Info(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.DiagnoseField_RageMax", CalamitySupportHelper.FoundRageMax));
                            StatariaLogger.Info(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.DiagnoseField_RageDuration", CalamitySupportHelper.FoundRageDuration));
                            StatariaLogger.Info(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.DiagnoseField_RageDamage", CalamitySupportHelper.FoundRageDamage));
                            StatariaLogger.Info(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.DiagnoseField_Adrenaline", CalamitySupportHelper.FoundAdrenaline));
                            StatariaLogger.Info(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.DiagnoseField_AdrenalineMax", CalamitySupportHelper.FoundAdrenalineMax));
                            StatariaLogger.Info(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.DiagnoseField_AdrenalineDuration", CalamitySupportHelper.FoundAdrenalineDuration));
                        }
                    }

                    if (ThoriumSupportHelper.ThoriumLoaded) {
                        caller.Reply(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.DiagnoseThorStatus", thorIntegrationOk ? Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.StatusWorking") : Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.StatusErrorsDetected")),
                                    thorIntegrationOk ? Color.Green : Color.Red);
                        StatariaLogger.Info(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.DiagnoseThorStatus", thorIntegrationOk ? Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.StatusWorking") : Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.StatusErrorsDetected")));
                    }
                    break;

                case "weapondebug":
                    if (!IsAdmin(caller)) return;

                    Player player = caller.Player;
                    Item heldItem = player.HeldItem;

                    if (heldItem == null || heldItem.IsAir)
                    {
                        caller.Reply(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.WeaponDebugNoWeapon"), Color.Red);
                        return;
                    }

                    caller.Reply(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.WeaponDebugHeader"), Color.Yellow);
                    caller.Reply(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.WeaponDebugName", heldItem.Name), Color.White);
                    caller.Reply(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.WeaponDebugItemType", heldItem.type), Color.White);
                    caller.Reply(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.WeaponDebugBaseDamage", heldItem.damage), Color.White);

                    string damageTypeName = heldItem.DamageType?.GetType().Name ?? "None";
                    string damageTypeString = heldItem.DamageType?.ToString() ?? "None";

                    caller.Reply(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.WeaponDebugDamageTypeClass", damageTypeName), Color.White);
                    caller.Reply(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.WeaponDebugDamageTypeString", damageTypeString), Color.White);

                    caller.Reply(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.WeaponDebugDamageClasses"), Color.White);
                    caller.Reply(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.WeaponDebugMelee", heldItem.CountsAsClass(DamageClass.Melee)), Color.White);
                    caller.Reply(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.WeaponDebugRanged", heldItem.CountsAsClass(DamageClass.Ranged)), Color.White);
                    caller.Reply(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.WeaponDebugMagic", heldItem.CountsAsClass(DamageClass.Magic)), Color.White);
                    caller.Reply(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.WeaponDebugSummon", heldItem.CountsAsClass(DamageClass.Summon)), Color.White);

                    if (heldItem.ModItem != null)
                    {
                        caller.Reply(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.WeaponDebugModItemInfo"), Color.White);
                        caller.Reply(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.WeaponDebugMod", heldItem.ModItem.Mod.Name), Color.White);
                        caller.Reply(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.WeaponDebugItemClass", heldItem.ModItem.GetType().Name), Color.White);
                        caller.Reply(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.WeaponDebugNamespace", heldItem.ModItem.GetType().Namespace), Color.White);
                        caller.Reply(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.WeaponDebugFullName", heldItem.ModItem.GetType().FullName), Color.White);
                    }

                    if (CalamitySupportHelper.CalamityLoaded)
                    {
                        bool isRogueWeapon = CalamitySupportHelper.IsRogueWeapon(heldItem);
                        caller.Reply(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.WeaponDebugIsRogueWeapon", isRogueWeapon),
                            isRogueWeapon ? Color.Green : Color.Red);

                        caller.Reply(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.WeaponDebugRogueDetectionDetails"), Color.White);

                        if (heldItem.ModItem?.Mod?.Name == "CalamityMod")
                        {
                            caller.Reply(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.WeaponDebugIsCalamityModItem"), Color.Green);
                        }

                        StatariaLogger.Debug(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.WeaponDebugRogueWeaponLog", heldItem.Name, isRogueWeapon));
                    }
                    else
                    {
                        caller.Reply(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.WeaponDebugCalamityNotLoaded"), Color.Yellow);
                    }

                    var config = ModContent.GetInstance<StatariaConfig>();

                    caller.Reply(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.WeaponDebugStatEffects"), Color.Yellow);
                    caller.Reply(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.WeaponDebugSTR", rpg.STR, (rpg.STR * (config.statSettings.STR_Damage / 100f)).ToString("F2")), Color.White);
                    caller.Reply(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.WeaponDebugINT", rpg.INT, (rpg.INT * (config.statSettings.INT_Damage / 100f)).ToString("F2")), Color.White);
                    caller.Reply(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.WeaponDebugDEX", rpg.DEX, (rpg.DEX * (config.statSettings.DEX_Damage / 100f)).ToString("F2")), Color.White);
                    caller.Reply(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.WeaponDebugSPR", rpg.SPR, (rpg.SPR * (config.statSettings.SPR_Damage / 100f)).ToString("F2")), Color.White);
                    caller.Reply(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.WeaponDebugRGE", rpg.RGE, (rpg.RGE * (config.modIntegration.RGE_Damage / 100f)).ToString("F2")), Color.White);
                    caller.Reply(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.WeaponDebugPOW", rpg.POW, (rpg.POW * (config.statSettings.POW_Damage / 100f)).ToString("F2")), Color.White);

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

                    caller.Reply(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.WeaponDebugAppliedStatBoost", appliedStat, statBonus.ToString("F2")), Color.Yellow);
                    StatariaLogger.Debug(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.WeaponDebugAppliedStatBoostLog", appliedStat, statBonus.ToString("F2")));
                    break;

                case "cal":
                    if (!IsAdmin(caller)) return;

                    if (!CalamitySupportHelper.CalamityLoaded)
                    {
                        caller.Reply(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.CalCalamityNotDetected"), Color.Red);
                        return;
                    }

                    var configCal = ModContent.GetInstance<StatariaConfig>();
                    if (!configCal.modIntegration.EnableCalamityIntegration)
                    {
                        caller.Reply(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.CalIntegrationDisabled"), Color.Red);
                        return;
                    }

                    if (args.Length < 2)
                    {
                        caller.Reply(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.CalUsage"), Color.Red);
                        return;
                    }

                    switch (args[1].ToLower())
                    {
                        case "fillrage":
                            if (CalamitySupportHelper.FoundRage && CalamitySupportHelper.FoundRageMax)
                            {
                                float rageMax = CalamitySupportHelper.GetRageMax(caller.Player);
                                CalamitySupportHelper.SetRage(caller.Player, rageMax);
                                caller.Reply(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.CalFillRageSuccess", rageMax), Color.Orange);
                                StatariaLogger.Debug(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.CalFillRageSuccess", rageMax));
                            }
                            else
                            {
                                caller.Reply(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.CalFillRageError"), Color.Red);
                            }
                            break;

                        case "filladrenaline":
                            if (CalamitySupportHelper.FoundAdrenaline && CalamitySupportHelper.FoundAdrenalineMax)
                            {
                                float adrenalineMax = CalamitySupportHelper.GetAdrenalineMax(caller.Player);
                                CalamitySupportHelper.SetAdrenaline(caller.Player, adrenalineMax);
                                caller.Reply(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.CalFillAdrenalineSuccess", adrenalineMax), Color.Cyan);
                                StatariaLogger.Debug(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.CalFillAdrenalineSuccess", adrenalineMax));
                            }
                            else
                            {
                                caller.Reply(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.CalFillAdrenalineError"), Color.Red);
                            }
                            break;

                        case "infrage":
                            CalamitySupportHelper.ToggleInfiniteRage();
                            caller.Reply(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.CalInfRageToggle", CalamitySupportHelper.InfiniteRageEnabled ? Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.StatusOn") : Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.StatusOff")),
                                        CalamitySupportHelper.InfiniteRageEnabled ? Color.Green : Color.Red);
                            StatariaLogger.Debug(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.CalInfRageToggleLog", CalamitySupportHelper.InfiniteRageEnabled ? Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.StatusOn") : Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.StatusOff")));
                            if (CalamitySupportHelper.InfiniteRageEnabled)
                            {
                                float rageMax = CalamitySupportHelper.GetRageMax(caller.Player);
                                CalamitySupportHelper.SetRage(caller.Player, rageMax);
                            }
                            break;

                        case "infadren":
                            CalamitySupportHelper.ToggleInfiniteAdrenaline();
                            caller.Reply(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.CalInfAdrenalineToggle", CalamitySupportHelper.InfiniteAdrenalineEnabled ? Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.StatusOn") : Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.StatusOff")),
                                        CalamitySupportHelper.InfiniteAdrenalineEnabled ? Color.Green : Color.Red);
                            StatariaLogger.Debug(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.CalInfAdrenalineToggleLog", CalamitySupportHelper.InfiniteAdrenalineEnabled ? Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.StatusOn") : Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.StatusOff")));
                            if (CalamitySupportHelper.InfiniteAdrenalineEnabled)
                            {
                                float adrenalineMax = CalamitySupportHelper.GetAdrenalineMax(caller.Player);
                                CalamitySupportHelper.SetAdrenaline(caller.Player, adrenalineMax);
                            }
                            break;

                        default:
                            caller.Reply(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.UnknownCalSubcommand"), Color.Red);
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
                            caller.Reply(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.TestXPUIAccepted"), Color.Green);
                            XPVerificationUI.HideVerification();
                        },
                        () => {
                            caller.Reply(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.TestXPUIRejected"), Color.Red);
                            XPVerificationUI.HideVerification();
                        },
                        () => {
                            caller.Reply(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.TestXPUIAllAccepted"), Color.Green);
                            XPVerificationUI.HideVerification();
                        },
                        () => {
                            caller.Reply(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.TestXPUIAllRejected"), Color.Red);
                            XPVerificationUI.HideVerification();
                        }
                    );

                    caller.Reply(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.TestXPUIShowing", testAmount, testSource, testIndex, testCount), Color.Yellow);
                    break;

                default:
                    caller.Reply(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.UnknownSubcommand", Usage), Color.Red);
                    break;
            }
        }

        private void HandleSelfReset(CommandCaller caller)
        {
            var config = ModContent.GetInstance<StatariaConfig>();

            if (Main.netMode == NetmodeID.MultiplayerClient && !config.multiplayerSettings.AllowSelfResetInMultiplayer)
            {
                caller.Reply(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.SelfResetMultiplayerDisabled"), Color.Red);
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
                caller.Reply(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.SelfResetWarning1"), Color.Red);
                caller.Reply(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.SelfResetWarning2"), Color.Red);
                caller.Reply(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.SelfResetWarning3"), Color.Yellow);
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
            rpg.VIT = rpg.STR = rpg.AGI = rpg.INT = rpg.LUC = rpg.END = rpg.POW = rpg.DEX = rpg.SPR = rpg.RGE = rpg.BRD = rpg.HLR = rpg.TCH = rpg.CLK = rpg.BLH = rpg.HNT = rpg.GMB = rpg.SHM = rpg.THR = 0;
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

            caller.Reply(Language.GetTextValue("Mods.Stataria.Commands.StatariaCommands.SelfResetSuccess"), Color.Orange);
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
                case "blh": rpg.BLH = value; return true;
                case "hnt": rpg.HNT = value; return true;
                case "gmb": rpg.GMB = value; return true;
                case "shm": rpg.SHM = value; return true;
                case "thr": rpg.THR = value; return true;
                default: return false;
            }
        }
    }
}