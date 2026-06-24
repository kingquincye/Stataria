using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using System;

namespace Stataria.Globals
{
    public class DesperadoGlobalProjectile : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        public bool isRicochet = false;

        public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (projectile.owner < 0 || projectile.owner >= Main.maxPlayers)
                return;

            Player player = Main.player[projectile.owner];
            if (player == null || !player.active || player.dead)
                return;

            var desperadoPlayer = player.GetModPlayer<DesperadoPlayer>();
            if (desperadoPlayer == null || !desperadoPlayer.IsDesperadoActive)
                return;

            if (isRicochet)
                return;

            if (!projectile.CountsAsClass(DamageClass.Ranged) || !hit.Crit)
                return;

            var config = ModContent.GetInstance<StatariaConfig>();
            var rpg = player.GetModPlayer<RPGPlayer>();
            int dex = rpg.GetEffectiveStat("DEX");

            float baseChance = config.roleSettings.DesperadoRicochetBaseChance;
            float dexScale = config.roleSettings.DesperadoRicochetDexScale;
            float maxChance = config.roleSettings.DesperadoRicochetMaxChance;

            float finalChance = Math.Min(baseChance + (dex * dexScale), maxChance) / 100f;

            if (Main.rand.NextFloat() < finalChance)
            {
                NPC targetNPC = FindNearestNPCTarget(target, 400f);
                if (targetNPC != null)
                {
                    Vector2 direction = targetNPC.Center - target.Center;
                    direction.Normalize();
                    
                    float speed = projectile.velocity.Length();
                    if (speed < 1f) speed = 10f;
                    Vector2 newVelocity = direction * speed;

                    int damage = (int)(projectile.damage * config.roleSettings.DesperadoRicochetDamageMultiplier);
                    if (damage < 1) damage = 1;

                    if (player.whoAmI == Main.myPlayer)
                    {
                        int spawnedProj = Projectile.NewProjectile(
                            projectile.GetSource_FromThis(),
                            target.Center,
                            newVelocity,
                            projectile.type,
                            damage,
                            projectile.knockBack,
                            projectile.owner
                        );

                        if (spawnedProj >= 0 && spawnedProj < Main.maxProjectiles)
                        {
                            Main.projectile[spawnedProj].GetGlobalProjectile<DesperadoGlobalProjectile>().isRicochet = true;
                            Main.projectile[spawnedProj].penetrate = 1;
                        }
                    }
                }
            }
        }

        private NPC FindNearestNPCTarget(NPC currentTarget, float maxRange)
        {
            NPC nearestNPC = null;
            float nearestDistance = maxRange;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc != null && npc.active && !npc.friendly && npc.lifeMax > 5 && npc.type != NPCID.TargetDummy && npc.whoAmI != currentTarget.whoAmI)
                {
                    float distance = Vector2.Distance(currentTarget.Center, npc.Center);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestNPC = npc;
                    }
                }
            }

            return nearestNPC;
        }
    }
}
