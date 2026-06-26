using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;
using Microsoft.Xna.Framework;
using System;

namespace Stataria.Globals
{
    public class DesperadoExtraSource : IEntitySource
    {
        public string Context => "DesperadoExtra";
    }

    public class DesperadoGlobalProjectile : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        public bool isRicochet = false;

        public override void OnSpawn(Projectile projectile, IEntitySource source)
        {
            if (!projectile.friendly || projectile.hostile || projectile.trap || projectile.minion || projectile.sentry)
                return;

            if (projectile.owner < 0 || projectile.owner >= Main.maxPlayers)
                return;

            Player player = Main.player[projectile.owner];
            if (player == null || !player.active || player.dead)
                return;

            var desperadoPlayer = player.GetModPlayer<DesperadoPlayer>();
            if (desperadoPlayer == null || !desperadoPlayer.IsDesperadoActive)
                return;

            // Prevent recursion: do not duplicate our own extra projectiles
            if (source is DesperadoExtraSource)
                return;

            // Only duplicate ranged projectiles that deal damage
            if (projectile.damage <= 0 || !projectile.CountsAsClass(DamageClass.Ranged))
                return;

            // Ensure the source is from weapon usage or parent projectile (holdouts/splits)
            bool isValidSource = false;
            if (source is EntitySource_ItemUse || source is EntitySource_ItemUse_WithAmmo)
            {
                isValidSource = true;
            }
            else if (source is EntitySource_Parent parentSource && parentSource.Entity is Projectile parentProj)
            {
                // Trace parent to see if it is a holdout or spawned by the player
                if (parentProj.owner == player.whoAmI && parentProj.friendly)
                {
                    isValidSource = true;
                }
            }

            if (!isValidSource)
                return;

            int stacks = desperadoPlayer.GetTempoStacks();
            var config = ModContent.GetInstance<StatariaConfig>();
            int extraProjCount = 0;
            if (config.roleSettings.DesperadoStacksPerExtraProjectile > 0)
            {
                extraProjCount = stacks / config.roleSettings.DesperadoStacksPerExtraProjectile;
                extraProjCount = Math.Min(extraProjCount, config.roleSettings.DesperadoMaxExtraProjectiles);
            }

            if (extraProjCount > 0)
            {
                int extraProjDamage = (int)(projectile.damage * config.roleSettings.DesperadoExtraProjectileDamageMultiplier);
                if (extraProjDamage < 1) extraProjDamage = 1;

                float ai0 = projectile.ai[0];
                float ai1 = projectile.ai[1];
                float ai2 = projectile.ai[2];

                if (source is EntitySource_OnHit onHit && onHit.Victim is NPC npc)
                {
                    if (projectile.aiStyle == (int)ProjAIStyleID.MagicMissile)
                    {
                        ai0 = -1f;
                        ai1 = npc.whoAmI;
                    }
                }

                for (int i = 0; i < extraProjCount; i++)
                {
                    Vector2 perturbedSpeed = projectile.velocity.RotatedByRandom(MathHelper.ToRadians(config.roleSettings.DesperadoExtraProjectileSpread));
                    Projectile.NewProjectile(
                        new DesperadoExtraSource(),
                        projectile.Center,
                        perturbedSpeed,
                        projectile.type,
                        extraProjDamage,
                        projectile.knockBack,
                        player.whoAmI,
                        ai0,
                        ai1,
                        ai2
                    );
                }
            }
        }

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
