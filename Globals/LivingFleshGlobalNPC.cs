using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Stataria.Projectiles;

namespace Stataria
{
    public class LivingFleshGlobalNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        private Vector2? originalPosition = null;
        private int spoofedPlayerIndex = -1;

        public override void OnKill(NPC npc)
        {
            if (npc.friendly || npc.lifeMax <= 5 || Main.netMode == NetmodeID.MultiplayerClient)
                return;

            var config = ModContent.GetInstance<StatariaConfig>();

            // Apply regen buff to nearby players with Living Flesh active
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player p = Main.player[i];
                if (p.active && !p.dead)
                {
                    var lfPlayer = p.GetModPlayer<LivingFleshPlayer>();
                    if (lfPlayer.IsLivingFleshActive)
                    {
                        float distance = Vector2.Distance(npc.Center, p.Center);
                        if (!config.roleSettings.EnableRoleProximity || distance <= config.roleSettings.RoleProximityRange)
                        {
                            int duration = (int)(config.roleSettings.LivingFleshKillRegenDuration * 60f);
                            p.AddBuff(ModContent.BuffType<LivingFleshRegenBuff>(), duration);
                        }
                    }
                }
            }
        }

        public override bool PreAI(NPC npc)
        {
            // Tricking target selection to aim for the Flesh Clone
            if (npc.target >= 0 && npc.target < 255)
            {
                Player targetPlayer = Main.player[npc.target];
                if (targetPlayer.active && !targetPlayer.dead)
                {
                    Projectile clone = FindPlayerClone(targetPlayer.whoAmI);
                    if (clone != null)
                    {
                        float distance = Vector2.Distance(npc.Center, clone.Center);
                        var config = ModContent.GetInstance<StatariaConfig>();
                        
                        if (distance <= config.roleSettings.LivingFleshCloneAggroRange)
                        {
                            // Save original player position and the spoof target index
                            originalPosition = targetPlayer.position;
                            spoofedPlayerIndex = targetPlayer.whoAmI;

                            // Swap player position to clone's position for target-finding AI
                            targetPlayer.position = clone.position;
                        }
                    }
                }
            }
            return true;
        }

        public override void PostAI(NPC npc)
        {
            // Restore player position immediately after AI runs
            if (spoofedPlayerIndex != -1 && originalPosition.HasValue)
            {
                Player targetPlayer = Main.player[spoofedPlayerIndex];
                targetPlayer.position = originalPosition.Value;

                // Reset trackers
                spoofedPlayerIndex = -1;
                originalPosition = null;
            }
        }

        private Projectile FindPlayerClone(int playerOwner)
        {
            int cloneType = ModContent.ProjectileType<FleshCloneProjectile>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile proj = Main.projectile[i];
                if (proj.active && proj.type == cloneType && proj.owner == playerOwner)
                {
                    return proj;
                }
            }
            return null;
        }
    }
}
