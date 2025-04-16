using Terraria.ModLoader;
using Terraria;
using Terraria.ID;
using Microsoft.Xna.Framework;
using System;
using Terraria.Social.Steam;
using Steamworks;

namespace Stataria
{
    public class StatariaDebugCommand : ModCommand
    {
        private const string AdminSteamID = "76561198887778739"; // Replace with your real Steam64 ID

        public override CommandType Type => CommandType.Chat;

        public override string Command => "stataria";

        public override string Usage => "/stataria <reset | setlevel x | setxp x | setpoints x | setstat name x | clearbosses>";

        public override string Description => "Debug commands for Stataria mod (admin only)";

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            var rpg = caller.Player.GetModPlayer<RPGPlayer>();

            if (!Main.dedServ && Main.netMode != NetmodeID.Server)
            {
                if (SteamUser.BLoggedOn())
                {
                    var steamId = SteamUser.GetSteamID();
                    if (steamId.m_SteamID.ToString() != AdminSteamID)
                    {
                        caller.Reply("You do not have permission to use this command.", Color.Red);
                        return;
                    }
                }
                else
                {
                    caller.Reply("Steam not available. Cannot verify admin identity.", Color.Red);
                    return;
                }
            }

            if (args.Length == 0)
            {
                caller.Reply("Usage: " + Usage, Color.Red);
                return;
            }

            switch (args[0].ToLower())
            {
                case "reset":
                    rpg.Level = 1;
                    rpg.XP = 0;
                    rpg.XPToNext = 100;
                    rpg.StatPoints = 0;
                    rpg.VIT = rpg.STR = rpg.AGI = rpg.INT = rpg.LUC = rpg.END = rpg.POW = rpg.DEX = rpg.SPR = 0;
                    rpg.rewardedBosses.Clear();
                    caller.Reply("Stataria reset!", Color.Orange);
                    break;

                case "setlevel":
                    if (args.Length >= 2 && int.TryParse(args[1], out int level))
                    {
                        rpg.Level = level;
                        rpg.XP = 0;
                        rpg.XPToNext = (int)(100 * Math.Pow(level, 1.5));
                        caller.Reply($"Level set to {level}", Color.LightGreen);
                    }
                    else caller.Reply("Usage: /stataria setlevel <number>", Color.Red);
                    break;

                case "setxp":
                    if (args.Length >= 2 && int.TryParse(args[1], out int xp))
                    {
                        rpg.XP = xp;
                        caller.Reply($"XP set to {xp}", Color.Yellow);
                    }
                    else caller.Reply("Usage: /stataria setxp <number>", Color.Red);
                    break;

                case "setpoints":
                    if (args.Length >= 2 && int.TryParse(args[1], out int pts))
                    {
                        rpg.StatPoints = pts;
                        caller.Reply($"Stat points set to {pts}", Color.Purple);
                    }
                    else caller.Reply("Usage: /stataria setpoints <number>", Color.Red);
                    break;

                case "setstat":
                    if (args.Length >= 3 && int.TryParse(args[2], out int val))
                    {
                        bool success = SetStatByName(rpg, args[1], val);
                        if (success)
                            caller.Reply($"{args[1].ToUpper()} set to {val}", Color.Green);
                        else
                            caller.Reply("Unknown stat name. Valid: vit, str, agi, int, luc, end, pow, dex, spr", Color.Red);
                    }
                    else caller.Reply("Usage: /stataria setstat <name> <value>", Color.Red);
                    break;

                case "clearbosses":
                    rpg.rewardedBosses.Clear();
                    caller.Reply("Rewarded boss XP progress cleared.", Color.Cyan);
                    break;

                case "syncbosses":
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

                default:
                    caller.Reply("Unknown subcommand. " + Usage, Color.Red);
                    break;
            }
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
                default: return false;
            }
        }
    }
}