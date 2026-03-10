using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace Stataria
{
    public class GiantGripGlobalProjectile : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        public bool fromGiantsGrip = false;

        public override void OnSpawn(Projectile projectile, IEntitySource source)
        {
            if (projectile.owner < 0 || projectile.owner >= Main.maxPlayers)
            {
                return;
            }

            Player player = Main.player[projectile.owner];
            if (!player.active)
            {
                return;
            }

            var rpg = player.GetModPlayer<RPGPlayer>();
            if (rpg == null)
            {
                return;
            }

            if (rpg.RebirthAbilities.TryGetValue("GiantsGrip", out RebirthAbility ability) && ability.IsUnlocked)
            {
                bool shouldScale = false;

                if (source is EntitySource_ItemUse itemSource && itemSource.Item.CountsAsClass(DamageClass.Melee))
                {
                    shouldScale = true;
                }
                else if (source is EntitySource_Parent parentSource && parentSource.Entity is Projectile parentProj)
                {
                    if (parentProj.GetGlobalProjectile<GiantGripGlobalProjectile>().fromGiantsGrip)
                    {
                        shouldScale = true;
                    }
                }

                if (shouldScale)
                {
                    projectile.scale *= 1.33f;
                    this.fromGiantsGrip = true;
                }
            }
        }
    }
}