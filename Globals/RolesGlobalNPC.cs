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

            player.statLife += healAmount;
            if (player.statLife > player.statLifeMax2)
                player.statLife = player.statLifeMax2;

            if (healAmount > 0 && Main.netMode != NetmodeID.Server)
            {
                player.HealEffect(healAmount, true);
            }
        }
    }
}