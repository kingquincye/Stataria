using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using Terraria.ID;
using Stataria.Buffs;
using System;

namespace Stataria
{
    public class ClericPlayer : ModPlayer
    {
        private int regenTimer = 0;
        private HashSet<int> playersInAura = new HashSet<int>();
        
        public bool IsClericActive => GetClericRole()?.Status == RoleStatus.Active;
        public bool IsDivineInterventionActive => Player.HasBuff(ModContent.BuffType<DivineInterventionBuff>());
        
        private Role GetClericRole()
        {
            var rpg = Player.GetModPlayer<RPGPlayer>();
            return rpg.AvailableRoles.TryGetValue("Cleric", out Role role) ? role : null;
        }

        public override void ResetEffects()
        {
            if (!IsClericActive)
            {
                playersInAura.Clear();
                return;
            }

            var config = ModContent.GetInstance<StatariaConfig>();
            
            float healthBonus = config.roleSettings.ClericHealthBonus / 100f;
            Player.statLifeMax2 = (int)(Player.statLifeMax2 * (1f + healthBonus));
            
            float defensePenalty = config.roleSettings.ClericDefensePenalty / 100f;
            Player.statDefense = Player.statDefense * (1f - defensePenalty);
            
            Player.AddBuff(ModContent.BuffType<ClericAuraBuff>(), 2);
        }

        public override void PostUpdate()
        {
            if (!IsClericActive) return;

            var config = ModContent.GetInstance<StatariaConfig>();
            
            regenTimer++;
            int regenIntervalTicks = (int)(config.roleSettings.ClericRegenInterval * 60f);
            
            if (regenTimer >= regenIntervalTicks)
            {
                ApplyRegeneration();
                regenTimer = 0;
            }
            
            UpdateAuraEffects();
        }

        private void UpdateAuraEffects()
        {
            var config = ModContent.GetInstance<StatariaConfig>();
            var rpg = Player.GetModPlayer<RPGPlayer>();
            
            HashSet<int> currentPlayersInAura = new HashSet<int>();
            bool divineInterventionActive = IsDivineInterventionActive;
            
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player otherPlayer = Main.player[i];
                if (otherPlayer == null || !otherPlayer.active || otherPlayer.dead || otherPlayer.whoAmI == Player.whoAmI)
                    continue;
                
                if (otherPlayer.team == 0)
                    continue;
                
                if (otherPlayer.team != Player.team)
                    continue;
                
                float distance = Vector2.Distance(Player.Center, otherPlayer.Center);
                
                if (distance <= config.roleSettings.ClericAuraRadius)
                {
                    currentPlayersInAura.Add(i);
                    
                    otherPlayer.AddBuff(ModContent.BuffType<ClericAuraBuff>(), 2);
                    
                    if (divineInterventionActive)
                    {
                        int clericBuffIndex = Player.FindBuffIndex(ModContent.BuffType<DivineInterventionBuff>());
                        if (clericBuffIndex >= 0)
                        {
                            int remainingTime = Player.buffTime[clericBuffIndex];
                            otherPlayer.AddBuff(ModContent.BuffType<DivineInterventionBuff>(), remainingTime);
                        }
                    }
                }
                else
                {
                    if (otherPlayer.HasBuff(ModContent.BuffType<DivineInterventionBuff>()))
                    {
                        otherPlayer.ClearBuff(ModContent.BuffType<DivineInterventionBuff>());
                    }
                }
            }
            
            playersInAura = currentPlayersInAura;
        }

        private void ApplyRegeneration()
        {
            var config = ModContent.GetInstance<StatariaConfig>();
            
            int selfHeal = (int)(Player.statLifeMax2 * config.roleSettings.ClericSelfRegenPercent / 100f);
            selfHeal = Math.Max(1, selfHeal);
            
            if (Player.statLife < Player.statLifeMax2)
            {
                Player.statLife += selfHeal;
                if (Player.statLife > Player.statLifeMax2)
                    Player.statLife = Player.statLifeMax2;
                
                if (Main.netMode != NetmodeID.Server)
                    Player.HealEffect(selfHeal, true);
            }
            
            foreach (int playerIndex in playersInAura)
            {
                Player teammate = Main.player[playerIndex];
                if (teammate == null || !teammate.active || teammate.dead)
                    continue;
                
                int teammateHeal = (int)(teammate.statLifeMax2 * config.roleSettings.ClericTeammateRegenPercent / 100f);
                teammateHeal = Math.Max(1, teammateHeal);
                
                if (teammate.statLife < teammate.statLifeMax2)
                {
                    teammate.statLife += teammateHeal;
                    if (teammate.statLife > teammate.statLifeMax2)
                        teammate.statLife = teammate.statLifeMax2;
                    
                    if (Main.netMode != NetmodeID.Server)
                        teammate.HealEffect(teammateHeal, false);
                }
            }
        }

        public void ActivateDivineIntervention()
        {
            var config = ModContent.GetInstance<StatariaConfig>();
            int duration = (int)(config.roleSettings.DivineInterventionDuration * 60f);
            
            Player.AddBuff(ModContent.BuffType<DivineInterventionBuff>(), duration);
            
            foreach (int playerIndex in playersInAura)
            {
                Player teammate = Main.player[playerIndex];
                if (teammate != null && teammate.active && !teammate.dead)
                {
                    teammate.AddBuff(ModContent.BuffType<DivineInterventionBuff>(), duration);
                }
            }
            
            if (Main.netMode != NetmodeID.Server)
            {
                for (int i = 0; i < 50; i++)
                {
                    Vector2 position = Player.Center + Main.rand.NextVector2Circular(config.roleSettings.ClericAuraRadius, config.roleSettings.ClericAuraRadius);
                    Dust dust = Dust.NewDustPerfect(position, DustID.YellowTorch, Vector2.Zero, 0, Color.Gold, 1.2f);
                    dust.noGravity = true;
                    dust.fadeIn = 0.5f;
                }
            }
        }
    }
}