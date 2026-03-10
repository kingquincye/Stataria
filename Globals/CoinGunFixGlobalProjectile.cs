using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Stataria
{
    public class CoinGunFixGlobalProjectile : GlobalProjectile
    {
        public override void ModifyHitNPC(Projectile projectile, NPC target, ref NPC.HitModifiers modifiers)
        {
            bool isCoinProjectile = projectile.type >= ProjectileID.CopperCoin && projectile.type <= ProjectileID.PlatinumCoin;

            if (!isCoinProjectile)
            {
                return;
            }

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
            var config = ModContent.GetInstance<StatariaConfig>();


            int effectiveDEX = rpg.GetEffectiveStat("DEX");
            float dexBonus = effectiveDEX * (config.statSettings.DEX_Damage / 100f);

            int effectivePOW = rpg.GetEffectiveStat("POW");
            float powBonus = effectivePOW * 0.001f;

            float totalBonus = dexBonus + powBonus;

            if (config.generalBalance.UseMultiplicativeDamage)
            {
                modifiers.FinalDamage *= (1 + totalBonus);
            }
            else
            {
                modifiers.FinalDamage += totalBonus;
            }
        }
    }
}