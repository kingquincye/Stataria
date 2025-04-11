using Terraria.ModLoader;
using Terraria;
using Terraria.ID;
using Microsoft.Xna.Framework;
using System;

namespace Stataria
{
    public class StatariaDebugCommand : ModCommand
    {
        public override CommandType Type => CommandType.Chat;

        public override string Command => "stataria";

        public override string Usage => "/stataria <reset | setlevel x | setxp x | setpoints x>";

        public override string Description => "Debug commands for Stataria mod";

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            var rpg = caller.Player.GetModPlayer<RPGPlayer>();

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

                default:
                    caller.Reply("Unknown subcommand. " + Usage, Color.Red);
                    break;
            }
        }
    }
}