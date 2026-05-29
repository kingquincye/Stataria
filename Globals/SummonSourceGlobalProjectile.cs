using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;
using System.Linq;

namespace Stataria
{
    public class SummonSourceGlobalProjectile : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        public int summonWeaponType = -1;

        public override void OnSpawn(Projectile projectile, IEntitySource source)
        {
            if ((projectile.minion || projectile.sentry || projectile.CountsAsClass(DamageClass.Summon)) && source is EntitySource_ItemUse itemSrc)
            {
                summonWeaponType = itemSrc.Item.type;
            }
            else if (source is EntitySource_Parent parent && parent.Entity is Projectile parentProj)
            {
                var parentGlobal = parentProj.GetGlobalProjectile<SummonSourceGlobalProjectile>();
                if (parentGlobal.summonWeaponType != -1)
                {
                    this.summonWeaponType = parentGlobal.summonWeaponType;
                }
            }
        }

        public override void ModifyHitNPC(Projectile projectile, NPC target, ref NPC.HitModifiers modifiers)
        {
            if ((projectile.minion || projectile.CountsAsClass(DamageClass.Summon)) && summonWeaponType != -1 && projectile.owner >= 0 && projectile.owner < Main.maxPlayers)
            {
                Player owner = Main.player[projectile.owner];
                if (!owner.active) return;

                Item sourceWeapon = owner.inventory.FirstOrDefault(item => !item.IsAir && item.type == summonWeaponType);

                if (sourceWeapon != null)
                {
                    var socketingData = sourceWeapon.GetGlobalItem<SocketingGlobalItem>();
                    float powerBonus = socketingData.GetTotalCoreEffect(CoreType.Power);

                    if (powerBonus > 0)
                    {
                        modifiers.FinalDamage *= 1 + (powerBonus / 100f);
                    }
                }
            }
        }

        public override GlobalProjectile NewInstance(Projectile projectile)
            => new SummonSourceGlobalProjectile();
    }
}