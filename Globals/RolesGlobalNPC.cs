using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Stataria
{
    public class RolesGlobalNPC : GlobalNPC
    {
        public override void OnKill(NPC npc)
        {
            if (npc.friendly || npc.lifeMax <= 5 || Main.netMode == NetmodeID.MultiplayerClient)
                return;

            var config = ModContent.GetInstance<StatariaConfig>();

            var activePlayers = Main.player.Where(p => p != null && p.active && !p.dead).ToList();
            if (activePlayers.Count == 0)
                return;

            List<Player> eligiblePlayers = new List<Player>();
            if (config.roleSettings.EnableRoleProximity)
            {
                foreach (var p in activePlayers)
                {
                    if (p is null || !p.active || p.dead) continue;

                    float distance = Vector2.Distance(npc.Center, p.Center);
                    if (distance <= config.roleSettings.RoleProximityRange)
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

            foreach (Player p in eligiblePlayers)
            {
                if (p?.active == true && !p.dead)
                {
                    HandleVampireKill(p, config);
                    HandleNecromancerKill(p, config);
                    HandleShinobiExecution(p, npc, config);
                }
            }
        }

        private void HandleShinobiExecution(Player player, NPC npc, StatariaConfig config)
        {
            var shinobiPlayer = player.GetModPlayer<ShinobiPlayer>();
            if (!shinobiPlayer.IsShinobiActive)
                return;

            if (SekirariaSupportHelper.SekirariaLoaded && SekirariaSupportHelper.IsPlayerExecutingNPC(player, npc))
            {
                int healAmount = (int)(player.statLifeMax2 * config.roleSettings.ShinobiExecutionHealPercent / 100f);
                healAmount = Math.Max(1, healAmount);

                if (Main.netMode == NetmodeID.SinglePlayer)
                {
                    player.statLife += healAmount;
                    if (player.statLife > player.statLifeMax2)
                        player.statLife = player.statLifeMax2;

                    if (healAmount > 0)
                    {
                        player.HealEffect(healAmount, true);
                    }
                }
                else if (Main.netMode == NetmodeID.Server)
                {
                    var packet = ModContent.GetInstance<Stataria>().GetPacket();
                    packet.Write((byte)StatariaMessageType.ShinobiExecutionHeal);
                    packet.Write(healAmount);
                    packet.Send(player.whoAmI);
                }
            }
        }

        private void HandleVampireKill(Player player, StatariaConfig config)
        {
            var vampirePlayer = player.GetModPlayer<VampirePlayer>();
            if (!vampirePlayer.IsVampireActive)
                return;

            int healAmount = (int)(player.statLifeMax2 * config.roleSettings.VampireKillHealPercent / 100f);
            healAmount = Math.Max(1, healAmount);

            if (Main.netMode == NetmodeID.SinglePlayer)
            {
                player.statLife += healAmount;
                if (player.statLife > player.statLifeMax2)
                    player.statLife = player.statLifeMax2;

                if (healAmount > 0)
                {
                    player.HealEffect(healAmount, true);
                }
            }
            else if (Main.netMode == NetmodeID.Server)
            {
                var packet = ModContent.GetInstance<Stataria>().GetPacket();
                packet.Write((byte)StatariaMessageType.VampireHealOnKill);
                packet.Write(healAmount);
                packet.Send(player.whoAmI);
            }
        }

        private void HandleNecromancerKill(Player player, StatariaConfig config)
        {
            var necromancerPlayer = player.GetModPlayer<NecromancerPlayer>();
            if (necromancerPlayer.IsNecromancerActive)
            {
                if (Main.netMode == NetmodeID.SinglePlayer)
                {
                    necromancerPlayer.HarvestSoul();
                }
                else if (Main.netMode == NetmodeID.Server)
                {
                    var packet = ModContent.GetInstance<Stataria>().GetPacket();
                    packet.Write((byte)StatariaMessageType.NecromancerHarvestSoulOnKill);
                    packet.Send(player.whoAmI);
                }
            }
        }

        private static readonly System.Reflection.FieldInfo _critOverrideField = 
            typeof(NPC.HitModifiers).GetField("_critOverride", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            if (projectile.owner >= 0 && projectile.owner < Main.maxPlayers)
            {
                Player player = Main.player[projectile.owner];
                if (player != null && player.active && !player.dead)
                {
                    var critGodPlayer = player.GetModPlayer<CritGodPlayer>();
                    if (critGodPlayer.EnableSummonCrits)
                    {
                        bool isSummonProjectile = projectile.minion || projectile.sentry || projectile.CountsAsClass(DamageClass.Summon);
                        if (isSummonProjectile)
                        {
                            var config = ModContent.GetInstance<StatariaConfig>();
                            var rpg = player.GetModPlayer<RPGPlayer>();

                            float critChance = config.roleSettings.CritGodCritChance;
                            critChance += rpg.GetEffectiveStat("LUC") * config.statSettings.LUC_Crit;

                            if (Main.rand.NextFloat(100f) < critChance)
                            {
                                if (_critOverrideField != null)
                                {
                                    object boxed = modifiers;
                                    _critOverrideField.SetValue(boxed, true);
                                    modifiers = (NPC.HitModifiers)boxed;
                                }
                                else
                                {
                                    modifiers.SetCrit();
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}