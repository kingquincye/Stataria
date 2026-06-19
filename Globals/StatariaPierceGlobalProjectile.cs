using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Terraria.DataStructures;
using Microsoft.Xna.Framework;
using System;

namespace Stataria.Globals
{
    public class StatariaPierceGlobalProjectile : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        public bool hasAppliedPierce = false;
        public int bonusPierceCount = 0;

        public override void OnSpawn(Projectile projectile, IEntitySource source)
        {
            if (projectile.hostile || projectile.trap || projectile.minion || projectile.sentry)
                return;

            if (projectile.owner < 0 || projectile.owner >= Main.maxPlayers)
                return;

            Player player = Main.player[projectile.owner];
            if (player == null || !player.active)
                return;

            var rpg = player.GetModPlayer<RPGPlayer>();
            var config = ModContent.GetInstance<StatariaConfig>();

            if (!config.statSettings.EnablePOWPierce)
                return;

            int effectivePOW = rpg.GetEffectiveStat("POW");
            int scaling = config.statSettings.POW_PierceScaling;

            if (scaling <= 0) return;

            bonusPierceCount = effectivePOW / scaling;

            if (bonusPierceCount > 0)
            {
                if (projectile.penetrate != -1 && !IsExplosive(projectile))
                {
                    projectile.penetrate += bonusPierceCount;
                    projectile.usesLocalNPCImmunity = true;
                    projectile.localNPCHitCooldown = 15; 
                    hasAppliedPierce = true;
                }
            }
        }

        private bool IsExplosive(Projectile projectile)
        {
            if (projectile.aiStyle == ProjAIStyleID.Explosive) return true;

            switch (projectile.type)
            {
                case ProjectileID.Grenade:
                case ProjectileID.BouncyGrenade:
                case ProjectileID.StickyGrenade:
                case ProjectileID.RocketI:
                case ProjectileID.RocketII:
                case ProjectileID.RocketIII:
                case ProjectileID.RocketIV:
                case ProjectileID.ProximityMineI:
                case ProjectileID.ProximityMineII:
                case ProjectileID.ProximityMineIII:
                case ProjectileID.ProximityMineIV:
                case ProjectileID.Bomb:
                case ProjectileID.BouncyBomb:
                case ProjectileID.StickyBomb:
                case ProjectileID.Dynamite:
                case ProjectileID.BouncyDynamite:
                case ProjectileID.StickyDynamite:
                case ProjectileID.ExplosiveBunny:
                case ProjectileID.HellfireArrow:
                    return true;
            }

            return false;
        }
    }
}