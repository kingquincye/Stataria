using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using System.Collections.Generic;

namespace Stataria
{
    public class StatariaGlobalNPC : GlobalNPC
    {
        public override void OnKill(NPC npc)
        {
            if (npc.friendly || npc.lifeMax <= 5 || Main.netMode == NetmodeID.MultiplayerClient)
                return;

            var config = ModContent.GetInstance<StatariaConfig>();

            if (npc.boss)
                StatariaSystem.killedBossesGlobal.Add(npc.type);

            // For server or single player only
            foreach (Player p in Main.player)
            {
                if (p is null || !p.active || p.dead) continue;
                var rpg = p.GetModPlayer<RPGPlayer>();
                bool hasKilledBefore = rpg.rewardedBosses.Contains(npc.type);
                
                // --- Boss HP XP ---
                if (npc.boss)
                {
                    if (config.EnableBossHPXP)
                    {
                        // Always give HP XP when enabled
                        int hpXP = (int)(npc.lifeMax * config.KillXP);
                        
                        if (Main.netMode == NetmodeID.SinglePlayer)
                        {
                            // In single player, directly apply XP
                            rpg.GainXP(hpXP, "Boss HP");
                        }
                        else if (Main.netMode == NetmodeID.Server)
                        {
                            // In multiplayer server, send packet to clients
                            Stataria.SendBossXP(p.whoAmI, npc.type, hpXP, "Boss HP");
                        }
                    }
                    else
                    {
                        // Only give HP XP once per boss when disabled
                        if (!hasKilledBefore)
                        {
                            int hpXP = (int)(npc.lifeMax * config.KillXP);
                            
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

                // --- Bonus XP (flat or scaling) ---
                if (npc.boss)
                {
                    if (!config.BonusBossXPIsUnique || !hasKilledBefore)
                    {
                        int bonusXP = config.UseFlatBossXP
                            ? config.DefaultFlatBossXP
                            : (int)(rpg.XPToNext * config.BossXP / 100f);
                        
                        if (Main.netMode == NetmodeID.SinglePlayer)
                        {
                            rpg.GainXP(bonusXP, "Boss Bonus");
                        }
                        else if (Main.netMode == NetmodeID.Server)
                        {
                            Stataria.SendBossXP(p.whoAmI, npc.type, bonusXP, "Boss Bonus");
                        }
                    }

                    // Only track boss as rewarded if bonus XP is marked unique or HP XP is disabled
                    if (!hasKilledBefore && (config.BonusBossXPIsUnique || !config.EnableBossHPXP))
                    {
                        rpg.rewardedBosses.Add(npc.type);
                    }
                }

                // --- Regular (non-boss) kill XP ---
                if (!npc.boss)
                {
                    int killXP = (int)(npc.lifeMax * config.KillXP);
                    
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