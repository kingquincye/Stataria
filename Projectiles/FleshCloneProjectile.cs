using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.GameContent;

namespace Stataria.Projectiles
{
    public class FleshCloneProjectile : ModProjectile
    {
        public override string Texture => "Stataria/icon";

        // ai[0]: Current HP of the clone
        // ai[1]: Creation (Max) HP of the clone
        // ai[2]: Facing direction at spawn time (1 = right, -1 = left)

        public float CloneHP
        {
            get => Projectile.ai[0];
            set => Projectile.ai[0] = value;
        }

        public float MaxCloneHP
        {
            get => Projectile.ai[1];
            set => Projectile.ai[1] = value;
        }

        /// <summary>The direction the owner was facing when the clone was summoned.</summary>
        public int SpawnDirection => Projectile.ai[2] < 0 ? -1 : 1;

        private int hitCooldownTimer = 0;
        private float cloneDefense = 0f;
        private float cloneDR = 0f;
        private bool initialized = false;
        private float lastKnownHP = -1f;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = false;
            ProjectileID.Sets.MinionSacrificable[Projectile.type] = false;
        }

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 42;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 900; // Default 15s (900 ticks)
        }

        public override bool? CanCutTiles() => false;

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            // Just land on ground
            return false;
        }

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            var config = ModContent.GetInstance<StatariaConfig>();

            if (!initialized)
            {
                // Lock in stats from the owner player
                cloneDefense = owner.statDefense;
                
                // Get the player's DR
                cloneDR = owner.endurance;
                
                // Set projectile's timeLeft based on config
                Projectile.timeLeft = (int)(config.roleSettings.LivingFleshCloneDuration * 60f);
                initialized = true;
            }

            // Fall with gravity
            Projectile.velocity.Y += 0.4f;
            if (Projectile.velocity.Y > 12f)
                Projectile.velocity.Y = 12f;

            if (hitCooldownTimer > 0)
                hitCooldownTimer--;

            // Track synced HP changes for visual feedback
            if (lastKnownHP == -1f)
            {
                lastKnownHP = CloneHP;
            }

            if (CloneHP < lastKnownHP)
            {
                int dmgTaken = (int)(lastKnownHP - CloneHP);
                lastKnownHP = CloneHP;

                // Create blood dust when hit (runs on all clients)
                for (int d = 0; d < 8; d++)
                {
                    Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Blood);
                }

                // Combat text for damage taken
                if (Main.netMode != NetmodeID.Server)
                {
                    CombatText.NewText(Projectile.Hitbox, Color.Red, dmgTaken.ToString(), false, false);
                }
            }

            // Contact damage from hostile NPCs (Only runs on owner client)
            if (Projectile.owner == Main.myPlayer && hitCooldownTimer <= 0)
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC npc = Main.npc[i];
                    if (npc.active && !npc.friendly && npc.damage > 0 && npc.Hitbox.Intersects(Projectile.Hitbox))
                    {
                        // Calculate contact damage
                        float defenseFactor = Main.masterMode ? 1f : (Main.expertMode ? 0.75f : 0.5f);
                        int finalDmg = (int)(npc.damage - (cloneDefense * defenseFactor));
                        finalDmg = (int)(finalDmg * (1f - cloneDR));
                        finalDmg = Math.Max(1, finalDmg);

                        CloneHP -= finalDmg;
                        hitCooldownTimer = 30; // 0.5 seconds hit cooldown

                        Projectile.netUpdate = true; // Sync the HP update to other clients

                        if (CloneHP <= 0)
                        {
                            Projectile.Kill();
                            return;
                        }

                        break; // Only take hit from one NPC per frame
                    }
                }
            }
        }

        public override void OnKill(int timeLeft)
        {
            Player owner = Main.player[Projectile.owner];
            var config = ModContent.GetInstance<StatariaConfig>();

            // Calculate explosion damage
            int explosionDmg = (int)(MaxCloneHP * config.roleSettings.LivingFleshCloneDamageMultiplier);

            float radius = 250f;
            if (Main.netMode != NetmodeID.Server)
            {
                // Play sound and blood explosion effects on clients
                Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCDeath12, Projectile.Center);
                for (int i = 0; i < 50; i++)
                {
                    Vector2 vel = Main.rand.NextVector2Circular(10f, 10f);
                    Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Blood, vel, 0, default, 1.8f);
                    d.noGravity = true;
                }
            }

            // Deal damage to enemies in radius (authoritative on owner's client)
            if (Projectile.owner == Main.myPlayer)
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC npc = Main.npc[i];
                    if (npc.active && !npc.friendly && npc.lifeMax > 5 && Vector2.Distance(Projectile.Center, npc.Center) < radius)
                    {
                        int hitDirection = npc.Center.X > Projectile.Center.X ? 1 : -1;
                        
                        // Strike the NPC (this automatically syncs in multiplayer)
                        npc.SimpleStrikeNPC(explosionDmg, hitDirection, false, 2f, DamageClass.Generic);
                    }
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Player owner = Main.player[Projectile.owner];
            if (owner == null || !owner.active)
                return false;

            // ── Save all state that affects the rendered pose ──────────────────────
            Vector2 oldPos            = owner.position;
            int     oldDirection      = owner.direction;
            float   oldRotation       = owner.fullRotation;
            Rectangle oldBodyFrame    = owner.bodyFrame;
            Rectangle oldLegFrame     = owner.legFrame;
            int     oldImmuneTime     = owner.immuneTime;

            // Attack / item-swing state
            int   oldItemAnimation    = owner.itemAnimation;
            int   oldItemTime         = owner.itemTime;
            float oldItemRotation     = owner.itemRotation;
            int   oldHeldProj         = owner.heldProj;

            // Composite arm pose (used by mage staffs, bows, etc.)
            Player.CompositeArmData oldFrontArm = owner.compositeFrontArm;
            Player.CompositeArmData oldBackArm  = owner.compositeBackArm;

            // Velocity drives the walking leg animation
            Vector2 oldVelocity       = owner.velocity;

            // ── Force a fully idle pose ────────────────────────────────────────────
            owner.position      = Projectile.position;
            owner.direction     = SpawnDirection; // Use the direction captured at spawn, not Projectile.direction
            owner.fullRotation  = 0f;
            owner.immuneTime    = 0; // Prevent hit-flash from bleeding onto the clone sprite

            // Frame 0 = standing still
            owner.bodyFrame = new Rectangle(0, 0, owner.bodyFrame.Width, owner.bodyFrame.Height);
            owner.legFrame  = new Rectangle(0, 0, owner.legFrame.Width,  owner.legFrame.Height);

            // Zero attack / swing animation so arms hang idle
            owner.itemAnimation = 0;
            owner.itemTime      = 0;
            owner.itemRotation  = 0f;
            owner.heldProj      = -1;

            // Reset composite arms to the default idle stretch
            owner.compositeFrontArm = new Player.CompositeArmData(false, Player.CompositeArmStretchAmount.None, 0f);
            owner.compositeBackArm  = new Player.CompositeArmData(false, Player.CompositeArmStretchAmount.None, 0f);

            // Zero velocity so the leg-frame animator picks the standing frame
            owner.velocity = Vector2.Zero;

            // ── Draw ──────────────────────────────────────────────────────────────
            // Signal draw layers that this is a clone render — they should suppress themselves
            var lfPlayer = owner.GetModPlayer<LivingFleshPlayer>();
            lfPlayer.IsDrawingClone = true;
            try
            {
                Main.PlayerRenderer.DrawPlayer(Main.Camera, owner, owner.position, 0f, owner.fullRotationOrigin, 0f, Projectile.scale);
            }
            finally
            {
                lfPlayer.IsDrawingClone = false;
            }

            // ── Restore all state ──────────────────────────────────────────────────
            owner.position          = oldPos;
            owner.direction         = oldDirection;
            owner.fullRotation      = oldRotation;
            owner.bodyFrame         = oldBodyFrame;
            owner.legFrame          = oldLegFrame;
            owner.immuneTime        = oldImmuneTime;
            owner.itemAnimation     = oldItemAnimation;
            owner.itemTime          = oldItemTime;
            owner.itemRotation      = oldItemRotation;
            owner.heldProj          = oldHeldProj;
            owner.compositeFrontArm = oldFrontArm;
            owner.compositeBackArm  = oldBackArm;
            owner.velocity          = oldVelocity;

            return false; // Prevent default drawing
        }

        public override void PostDraw(Color lightColor)
        {
            // Draw HP bar above decoy's head
            if (MaxCloneHP <= 0) return;

            float hpRatio = Math.Clamp(CloneHP / MaxCloneHP, 0f, 1f);
            
            // Draw HP Bar
            int barWidth = 32;
            int barHeight = 4;
            Vector2 topPos = new Vector2(Projectile.Center.X, Projectile.position.Y);
            Vector2 barPos = topPos - new Vector2(barWidth / 2f, 8) - Main.screenPosition;

            Texture2D pixel = TextureAssets.MagicPixel.Value;

            // Background (black border)
            Main.spriteBatch.Draw(pixel, new Rectangle((int)barPos.X - 1, (int)barPos.Y - 1, barWidth + 2, barHeight + 2), Color.Black);
            
            // Fill (red HP)
            Main.spriteBatch.Draw(pixel, new Rectangle((int)barPos.X, (int)barPos.Y, (int)(barWidth * hpRatio), barHeight), Color.Red);
        }
    }
}
