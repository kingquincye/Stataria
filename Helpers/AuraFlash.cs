using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Stataria.Core;

namespace Stataria.Helpers
{
    public static class AuraFlash
    {
        public static void TriggerLevelUpEffect(Player player, AdaptationCategory category)
        {
            if (Main.dedServ)
                return;

            SoundEngine.PlaySound(SoundID.Item29, player.Center);

            Color catColor = category.GetCategoryColor();

            // Ring burst of particles around player
            int count = 36;
            for (int i = 0; i < count; i++)
            {
                float angle = (float)(i * System.Math.Tau / count);
                Vector2 velocity = angle.ToRotationVector2() * 5.5f;

                Dust dust = Dust.NewDustPerfect(player.Center, DustID.FireworksRGB, velocity, 50, catColor, 1.8f);
                dust.noGravity = true;
            }

            // Vertical aura pillar burst
            for (int i = 0; i < 20; i++)
            {
                Vector2 pos = player.Center + new Vector2(Main.rand.NextFloat(-25f, 25f), Main.rand.NextFloat(-35f, 35f));
                Vector2 vel = new Vector2(0f, -Main.rand.NextFloat(3f, 7f));
                Dust dust = Dust.NewDustPerfect(pos, DustID.VenomStaff, vel, 80, catColor, 1.5f);
                dust.noGravity = true;
            }

            Lighting.AddLight(player.Center, catColor.ToVector3() * 2.5f);
        }

        public static void TriggerCheatDeathEffect(Player player)
        {
            if (Main.dedServ)
                return;

            SoundEngine.PlaySound(SoundID.Item4, player.Center);
            SoundEngine.PlaySound(SoundID.Item119, player.Center);

            Color deathColor = AdaptationCategory.Death.GetCategoryColor();

            // Large radial flash
            for (int i = 0; i < 50; i++)
            {
                float angle = (float)(i * System.Math.Tau / 50);
                Vector2 velocity = angle.ToRotationVector2() * Main.rand.NextFloat(4f, 10f);

                Dust dust = Dust.NewDustPerfect(player.Center, DustID.PortalBoltTrail, velocity, 30, deathColor, 2.2f);
                dust.noGravity = true;
            }

            Lighting.AddLight(player.Center, deathColor.ToVector3() * 4.0f);
        }
    }
}
