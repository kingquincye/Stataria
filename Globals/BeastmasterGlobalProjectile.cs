using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;

namespace Stataria
{
    public class BeastmasterGlobalProjectile : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        public int summonWeaponType = -1;

        public override void OnSpawn(Projectile projectile, IEntitySource source)
        {
            if (!projectile.minion) return;

            if (source is EntitySource_ItemUse itemSrc)
            {
                summonWeaponType = itemSrc.Item.type;
                return;
            }

            if (source is EntitySource_Parent parent && parent.Entity is Projectile parentProj)
            {
                summonWeaponType =
                    parentProj.GetGlobalProjectile<BeastmasterGlobalProjectile>().summonWeaponType;
            }
        }

        public override GlobalProjectile NewInstance(Projectile projectile)
            => new BeastmasterGlobalProjectile();
    }
}