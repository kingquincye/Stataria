using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Stataria.Projectiles
{
    public class ZombieThrallProjectile : ModProjectile
    {
        public override string Texture => "Stataria/icon";

        public int RemainingLifetimeTicks { get; set; } = 1800; // Default 30s in ticks
        public int TargetNoContactTimer { get; set; } = 0;
        public int LastTargetWhoAmI { get; set; } = -1;

        public override void SetStaticDefaults()
        {
            // Main.projFrames[Projectile.type] = 3; // We will use Zombie NPC frames (3 frames)
            ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;
            ProjectileID.Sets.MinionSacrificable[Projectile.type] = false;
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 40;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.minion = true;
            Projectile.minionSlots = 0f;
            Projectile.penetrate = -1;
            Projectile.tileCollide = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 30; // hit every 0.5 seconds
            Projectile.DamageType = DamageClass.Summon;
        }

        public override bool? CanCutTiles() => false;

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(RemainingLifetimeTicks);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            RemainingLifetimeTicks = reader.ReadInt32();
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            // Slide along ground, don't die
            return false;
        }

        public override void AI()
        {
            // 1. Tick down remaining lifetime
            RemainingLifetimeTicks--;
            if (RemainingLifetimeTicks <= 0)
            {
                Projectile.Kill();
                return;
            }

            Player player = Main.player[Projectile.owner];

            // 2. Gravitational forces
            Projectile.velocity.Y += 0.4f;
            if (Projectile.velocity.Y > 12f)
                Projectile.velocity.Y = 12f;

            // 3. Find target
            NPC target = null;
            float maxDistance = 1500f;
            float closestDist = maxDistance;

            if (player.HasMinionAttackTargetNPC)
            {
                NPC npc = Main.npc[player.MinionAttackTargetNPC];
                if (IsValidTarget(npc) && Vector2.Distance(Projectile.Center, npc.Center) < maxDistance)
                {
                    target = npc;
                }
            }

            if (target == null)
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC npc = Main.npc[i];
                    if (IsValidTarget(npc))
                    {
                        float dist = Vector2.Distance(Projectile.Center, npc.Center);
                        if (dist < closestDist)
                        {
                            closestDist = dist;
                            target = npc;
                        }
                    }
                }
            }

            // 4. Ground Check
            bool onGround = false;
            int startX = (int)(Projectile.Left.X / 16f);
            int endX = (int)(Projectile.Right.X / 16f);
            int feetY = (int)((Projectile.Bottom.Y + 2f) / 16f);

            for (int x = startX; x <= endX; x++)
            {
                Tile tile = Framing.GetTileSafely(x, feetY);
                if (tile.HasTile && (Main.tileSolid[tile.TileType] || Main.tileSolidTop[tile.TileType]))
                {
                    onGround = true;
                    break;
                }
            }

            // 5. Teleportation Logic
            bool shouldTeleport = false;
            Vector2 teleportTargetPos = Vector2.Zero;

            if (target != null)
            {
                if (target.whoAmI != LastTargetWhoAmI)
                {
                    LastTargetWhoAmI = target.whoAmI;
                    TargetNoContactTimer = 0;
                }
                else
                {
                    TargetNoContactTimer++;
                }

                float dx = Math.Abs(target.Center.X - Projectile.Center.X);
                float dy = Projectile.Center.Y - target.Center.Y; // positive if target is above projectile

                // Teleport if too far or if stuck without contact for 4 seconds (240 ticks)
                if (dx > 560f || dy > 240f || TargetNoContactTimer >= 240)
                {
                    shouldTeleport = true;
                    teleportTargetPos = target.Bottom - new Vector2(Projectile.width / 2, Projectile.height);
                    TargetNoContactTimer = 0;
                }
            }
            else
            {
                LastTargetWhoAmI = -1;
                TargetNoContactTimer = 0;

                // Follow player, teleport if too far
                float dx = Math.Abs(player.Center.X - Projectile.Center.X);
                float dy = Math.Abs(player.Center.Y - Projectile.Center.Y);
                if (dx > 560f || dy > 240f)
                {
                    shouldTeleport = true;
                    teleportTargetPos = player.Bottom - new Vector2(Projectile.width / 2, Projectile.height);
                }
            }

            if (shouldTeleport)
            {
                // Play shadowflame effect at current pos
                SpawnTeleportDust();

                // Teleport
                Projectile.position = teleportTargetPos + new Vector2(Main.rand.NextFloat(-10f, 10f), 0f);
                Projectile.velocity = Vector2.Zero;

                // Play shadowflame effect at new pos
                SpawnTeleportDust();

                if (Projectile.owner == Main.myPlayer && Main.netMode != NetmodeID.Server)
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item8, Projectile.Center);
                }
            }

            // 6. Movement and Jumping logic
            float runSpeed = 12f;
            float acceleration = 0.4f;
            float horizontalDiff = 0f;

            if (target != null)
            {
                horizontalDiff = target.Center.X - Projectile.Center.X;
            }
            else
            {
                horizontalDiff = player.Center.X - Projectile.Center.X;
            }

            int dir = horizontalDiff > 0 ? 1 : -1;
            Projectile.spriteDirection = dir;

            // Horizontal run speed adjustment
            if (Math.Abs(horizontalDiff) > 16f)
            {
                if (Math.Abs(Projectile.velocity.X) < runSpeed)
                {
                    Projectile.velocity.X += dir * acceleration;
                    if (Math.Abs(Projectile.velocity.X) > runSpeed)
                    {
                        Projectile.velocity.X = dir * runSpeed;
                    }
                }
                else
                {
                    Projectile.velocity.X = MathHelper.Lerp(Projectile.velocity.X, dir * runSpeed, 0.1f);
                }
            }
            else
            {
                Projectile.velocity.X = MathHelper.Lerp(Projectile.velocity.X, 0f, 0.25f);
            }

            // Jump to reach targets above
            if (target != null)
            {
                float verticalDiff = Projectile.Center.Y - target.Center.Y;
                if (verticalDiff > 32f && Math.Abs(horizontalDiff) < 240f && onGround)
                {
                    Projectile.velocity.Y = -12f; // High jump to chase target
                }
            }
            else
            {
                // follow player jump
                float verticalDiff = Projectile.Center.Y - player.Center.Y;
                if (verticalDiff > 32f && Math.Abs(horizontalDiff) < 240f && onGround)
                {
                    Projectile.velocity.Y = -11f;
                }
            }

            // Jump over walls/obstacles
            if (Math.Abs(Projectile.velocity.X) < 0.2f && Math.Abs(horizontalDiff) > 16f && onGround)
            {
                Projectile.velocity.Y = -12f; // jump over wall
            }

            // 7. Animations
            if (Projectile.velocity.Y != 0f && !onGround)
            {
                Projectile.frame = 2; // Jump/Air frame
            }
            else if (Math.Abs(Projectile.velocity.X) > 0.1f)
            {
                Projectile.frameCounter++;
                if (Projectile.frameCounter >= 6)
                {
                    Projectile.frameCounter = 0;
                    Projectile.frame = (Projectile.frame + 1) % 2; // alternates 0 and 1
                }
            }
            else
            {
                Projectile.frame = 0; // Idle
            }

            // 8. Green glowing eyes (dust particles)
            if (Main.rand.NextBool(3))
            {
                Vector2 eyePos = Projectile.Center + new Vector2(4 * Projectile.spriteDirection, -12);
                int d = Dust.NewDust(eyePos, 1, 1, DustID.CursedTorch, 0, 0, 150, default, 0.7f);
                Main.dust[d].noGravity = true;
                Main.dust[d].velocity *= 0.1f;
            }
        }

        private void SpawnTeleportDust()
        {
            for (int i = 0; i < 25; i++)
            {
                int d = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Shadowflame, 0, 0, 100, default, 1.3f);
                Main.dust[d].velocity *= 1.4f;
                Main.dust[d].noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Main.instance.LoadNPC(NPCID.Zombie);
            Texture2D texture = Terraria.GameContent.TextureAssets.Npc[NPCID.Zombie].Value;
            int numFrames = 3;
            int frameHeight = texture.Height / numFrames;
            Rectangle sourceRect = new Rectangle(0, Projectile.frame * frameHeight, texture.Width, frameHeight);
            Vector2 drawOrigin = new Vector2(texture.Width / 2, frameHeight / 2);

            // Dark purple translucent overlay tint
            Color purpleOverlay = new Color(130, 30, 200, 150);

            // Facing direction flip (zombie asset faces left, so flip if spriteDirection is 1)
            SpriteEffects effects = Projectile.spriteDirection > 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            Vector2 drawPos = Projectile.Center - Main.screenPosition;

            Main.EntitySpriteDraw(
                texture,
                drawPos,
                sourceRect,
                purpleOverlay,
                Projectile.rotation,
                drawOrigin,
                Projectile.scale,
                effects,
                0
            );

            return false; // Prevent default projectile drawing
        }

        public override void PostDraw(Color lightColor)
        {
            Player player = Main.player[Projectile.owner];
            var necPlayer = player.GetModPlayer<NecromancerPlayer>();
            float maxDuration = necPlayer.GetMaxSoulDuration();
            float currentDuration = RemainingLifetimeTicks / 60f;

            float pct = Math.Clamp(currentDuration / maxDuration, 0f, 1f);

            Texture2D pixel = Terraria.GameContent.TextureAssets.MagicPixel.Value;
            
            // Draw floating bar above head (aligned relative to screen coordinates)
            Vector2 barCenter = Projectile.Top - Main.screenPosition + new Vector2(0f, -8f);
            int barWidth = 24;
            int barHeight = 4;
            Vector2 barTopLeft = barCenter - new Vector2(barWidth / 2f, barHeight / 2f);

            Rectangle bgRect = new Rectangle((int)barTopLeft.X, (int)barTopLeft.Y, barWidth, barHeight);
            
            // Draw background
            Main.spriteBatch.Draw(pixel, bgRect, Color.Black * 0.5f);

            // Draw purple remaining duration bar
            int fillWidth = (int)(barWidth * pct);
            Rectangle fillRect = new Rectangle((int)barTopLeft.X, (int)barTopLeft.Y, fillWidth, barHeight);
            Color durationColor = new Color(170, 50, 220); // Vibrant purple
            
            Main.spriteBatch.Draw(pixel, fillRect, durationColor);

            // Draw border
            DrawBorder(Main.spriteBatch, bgRect, Color.Black * 0.8f);
        }

        private void DrawBorder(SpriteBatch spriteBatch, Rectangle rect, Color color)
        {
            Texture2D pixel = Terraria.GameContent.TextureAssets.MagicPixel.Value;
            int thickness = 1;
            // Top
            spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
            // Bottom
            spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y + rect.Height - thickness, rect.Width, thickness), color);
            // Left
            spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
            // Right
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            TargetNoContactTimer = 0;
        }

        public override void OnKill(int timeLeft)
        {
            if (RemainingLifetimeTicks <= 0)
            {
                // Natural expiration: crumble into bones and purple shadow residue
                for (int i = 0; i < 15; i++)
                {
                    int dustType = Main.rand.NextBool() ? DustID.Bone : DustID.Demonite;
                    int d = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, dustType, 0, 0, 100, default, 1f);
                    Main.dust[d].velocity *= 1.2f;
                    Main.dust[d].noGravity = (dustType == DustID.Demonite);
                }
            }
            else
            {
                // Recalled early or killed: shadowflame burst and play sound
                if (Main.netMode != NetmodeID.Server)
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item8, Projectile.Center);
                }
                for (int i = 0; i < 25; i++)
                {
                    int d = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Shadowflame, 0, 0, 100, default, 1.4f);
                    Main.dust[d].noGravity = true;
                    Main.dust[d].velocity *= 1.5f;
                }
            }
        }

        private bool IsValidTarget(NPC npc)
        {
            if (npc == null || !npc.active || npc.friendly || npc.townNPC || npc.dontTakeDamage)
                return false;

            if (npc.lifeMax <= 9 || NPCID.Sets.CountsAsCritter[npc.type])
                return false;

            var config = ModContent.GetInstance<StatariaConfig>();
            if (config?.roleSettings?.NecromancerThrallBlacklistedNPCs != null)
            {
                foreach (string entry in config.roleSettings.NecromancerThrallBlacklistedNPCs)
                {
                    if (entry.Equals(Lang.GetNPCNameValue(npc.type), StringComparison.OrdinalIgnoreCase) ||
                        (int.TryParse(entry, out int id) && id == npc.type))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
