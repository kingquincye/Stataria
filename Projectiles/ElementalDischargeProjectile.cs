using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Stataria.Projectiles
{
    public class ElementalDischargeProjectile : ModProjectile
    {
        public override string Texture => "Stataria/icon";

        public ref float ConsumedCharge => ref Projectile.ai[0];
        public ref float Timer => ref Projectile.ai[1];

        private const int ChargeUpTime = 45; // 0.75 seconds charge up

        public override void SetStaticDefaults()
        {
            // No static defaults needed
        }

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Magic;
            
            // Set longer invulnerability frames for hit detection
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1; 
        }

        public float GetBlastRadius()
        {
            int intStat = Main.player[Projectile.owner].GetModPlayer<RPGPlayer>().GetEffectiveStat("INT");
            float baseRadius = 1000f; // Screen-wide radius
            return baseRadius * (1f + intStat * 0.005f); // +0.5% radius per INT
        }

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            if (player == null || !player.active || player.dead)
            {
                Projectile.Kill();
                return;
            }

            // Keep centered on the player
            Projectile.Center = player.Center;

            Timer++;

            if (Timer < ChargeUpTime)
            {
                // Pre-discharge: Swirling vortex of elemental particles and glowing hands
                if (Main.netMode != NetmodeID.Server)
                {
                    // Swirl particles (orbiting player)
                    int numSparks = 3;
                    for (int i = 0; i < numSparks; i++)
                    {
                        float angle = (Timer * 0.25f) + (i * MathHelper.TwoPi / numSparks);
                        float dist = 40f * (1f - Timer / (float)ChargeUpTime) + 15f; // spirals inward
                        Vector2 offset = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * dist;
                        Vector2 pos = player.Center + offset;

                        // Choose dust type randomly representing fire, ice, or electric
                        int dustType = Main.rand.Next(3) switch
                        {
                            0 => DustID.Torch,       // Fire (orange)
                            1 => DustID.IceTorch,    // Ice (light blue)
                            _ => DustID.Electric     // Lightning (cyan)
                        };

                        Dust d = Dust.NewDustPerfect(pos, dustType, Vector2.Zero, 100, default, 1.1f);
                        d.noGravity = true;
                        d.velocity = player.velocity; // move with player
                    }

                    // Hands glowing
                    Vector2 leftHand = player.Center + new Vector2(-12f * player.direction, -4f);
                    Vector2 rightHand = player.Center + new Vector2(12f * player.direction, -4f);

                    if (Main.rand.NextBool(2))
                    {
                        Dust d1 = Dust.NewDustPerfect(leftHand, DustID.MagicMirror, Vector2.Zero, 150, Color.DeepSkyBlue, 1.0f);
                        d1.noGravity = true;
                        d1.velocity = player.velocity;
                    }
                    if (Main.rand.NextBool(2))
                    {
                        Dust d2 = Dust.NewDustPerfect(rightHand, DustID.MagicMirror, Vector2.Zero, 150, Color.DeepSkyBlue, 1.0f);
                        d2.noGravity = true;
                        d2.velocity = player.velocity;
                    }
                }
            }
            else if (Timer == ChargeUpTime)
            {
                // Discharge phase!
                float radius = GetBlastRadius();
                
                // Play explosion sound
                if (Main.netMode != NetmodeID.Server)
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item62, Projectile.Center);
                }

                // Expand bounds to hit enemies
                Projectile.position = Projectile.Center;
                Projectile.width = (int)(radius * 2f);
                Projectile.height = (int)(radius * 2f);
                Projectile.Center = player.Center; // Keep centered
                Projectile.friendly = true;
                
                if (Projectile.owner == Main.myPlayer)
                {
                    Projectile.Damage();
                }

                // Spawn explosion visual ring of elemental particles
                if (Main.netMode != NetmodeID.Server)
                {
                    int particlesCount = Math.Min(250, (int)(radius / 4f));
                    for (int i = 0; i < particlesCount; i++)
                    {
                        float angle = i * MathHelper.TwoPi / particlesCount;
                        Vector2 dir = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
                        float speed = Main.rand.NextFloat(6f, 12f);
                        Vector2 vel = dir * speed;

                        int dustType = Main.rand.Next(3) switch
                        {
                            0 => DustID.Torch,
                            1 => DustID.IceTorch,
                            _ => DustID.Electric
                        };

                        Dust d = Dust.NewDustPerfect(player.Center, dustType, vel, 100, default, 1.8f);
                        d.noGravity = true;
                        d.fadeIn = 0.5f;
                    }

                    // Secondary shockwave expanding ring
                    for (int i = 0; i < 40; i++)
                    {
                        float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                        Vector2 vel = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * Main.rand.NextFloat(2f, 5f);
                        Dust d = Dust.NewDustPerfect(player.Center, DustID.MagicMirror, vel, 100, Color.Cyan, 1.5f);
                        d.noGravity = true;
                    }
                }

                Projectile.Kill();
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // Do not draw the default projectile icon
            return false;
        }
    }
}
