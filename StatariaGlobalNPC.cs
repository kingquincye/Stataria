using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using System;

namespace Stataria
{
    public class StatariaGlobalNPC : GlobalNPC
    {
        public override void OnKill(NPC npc)
        {
            if (npc.friendly || npc.lifeMax <= 5 || Main.netMode == NetmodeID.MultiplayerClient)
                return;

            var config = ModContent.GetInstance<StatariaConfig>();

            if (config.advanced.BlacklistedNPCs.Any(entry =>
                entry.Equals(Lang.GetNPCNameValue(npc.type), StringComparison.OrdinalIgnoreCase) ||
                entry.Equals(npc.TypeName, StringComparison.OrdinalIgnoreCase) ||
                (int.TryParse(entry, out int id) && id == npc.type)))
            {
                return;
            }

            if (npc.boss)
                StatariaSystem.killedBossesGlobal.Add(npc.type);

            var activePlayers = Main.player.Where(p => p != null && p.active && !p.dead).ToList();

            if (activePlayers.Count == 0)
                return;

            List<Player> eligiblePlayers = new List<Player>();
            if (config.multiplayerSettings.EnableXPProximity)
            {
                foreach (var p in activePlayers)
                {
                    if (p is null || !p.active || p.dead) continue;

                    float distance = Vector2.Distance(npc.Center, p.Center);
                    if (distance <= config.multiplayerSettings.XPProximityRange)
                    {
                        eligiblePlayers.Add(p);
                    }
                }
            }
            else
            {
                eligiblePlayers = activePlayers;
            }

            if (eligiblePlayers.Count == 0)
                return;

            float splitMultiplier = 1f;
            if (config.multiplayerSettings.SplitKillXP && eligiblePlayers.Count > 0)
            {
                splitMultiplier = 1f / eligiblePlayers.Count;
            }

            foreach (Player p in eligiblePlayers)
            {
                if (p is null || !p.active || p.dead) continue;
                var rpg = p.GetModPlayer<RPGPlayer>();
                bool hasKilledBefore = rpg.rewardedBosses.Contains(npc.type);

                if (npc.boss)
                {
                    if (config.generalBalance.EnableBossHPXP)
                    {
                        long hpXP = (long)(npc.lifeMax * config.generalBalance.KillXP);

                        if (config.multiplayerSettings.SplitKillXP)
                        {
                            hpXP = (long)(hpXP * splitMultiplier);
                        }

                        if (Main.netMode == NetmodeID.SinglePlayer)
                        {
                            rpg.GainXP(hpXP, "Boss HP");
                        }
                        else if (Main.netMode == NetmodeID.Server)
                        {
                            Stataria.SendBossXP(p.whoAmI, npc.type, hpXP, "Boss HP");
                        }
                    }
                    else
                    {
                        if (!hasKilledBefore)
                        {
                            long hpXP = (long)(npc.lifeMax * config.generalBalance.KillXP);

                            if (config.multiplayerSettings.SplitKillXP)
                            {
                                hpXP = (long)(hpXP * splitMultiplier);
                            }

                            if (Main.netMode == NetmodeID.SinglePlayer)
                            {
                                rpg.GainXP(hpXP, "First Boss HP");
                            }
                            else if (Main.netMode == NetmodeID.Server)
                            {
                                Stataria.SendBossXP(p.whoAmI, npc.type, hpXP, "First Boss HP");
                            }
                        }
                    }
                }

                if (npc.boss)
                {
                    if (!config.generalBalance.BonusBossXPIsUnique || !hasKilledBefore)
                    {
                        long bonusXP = config.generalBalance.UseFlatBossXP
                            ? config.generalBalance.DefaultFlatBossXP
                            : (long)(rpg.XPToNext * config.generalBalance.BossXP / 100f);

                        if (config.multiplayerSettings.SplitKillXP)
                        {
                            bonusXP = (long)(bonusXP * splitMultiplier);
                        }

                        if (Main.netMode == NetmodeID.SinglePlayer)
                        {
                            rpg.GainXP(bonusXP, "Boss Bonus");
                        }
                        else if (Main.netMode == NetmodeID.Server)
                        {
                            Stataria.SendBossXP(p.whoAmI, npc.type, bonusXP, "Boss Bonus");
                        }
                    }

                    if (!hasKilledBefore && (config.generalBalance.BonusBossXPIsUnique || !config.generalBalance.EnableBossHPXP))
                    {
                        rpg.rewardedBosses.Add(npc.type);
                    }
                }

                if (!npc.boss)
                {
                    long killXP = (long)(npc.lifeMax * config.generalBalance.KillXP);

                    if (config.multiplayerSettings.SplitKillXP)
                    {
                        killXP = (long)(killXP * splitMultiplier);
                    }

                    if (Main.netMode == NetmodeID.SinglePlayer)
                    {
                        rpg.GainXP(killXP, "Kill");
                    }
                    else if (Main.netMode == NetmodeID.Server)
                    {
                        Stataria.SendBossXP(p.whoAmI, npc.type, killXP, "Kill");
                    }
                }
            }
        }
    }
}