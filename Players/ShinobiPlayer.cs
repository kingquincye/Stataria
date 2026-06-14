using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.ID;
using Microsoft.Xna.Framework;
using System;
using Terraria.DataStructures;

namespace Stataria
{
    public class ShinobiPlayer : ModPlayer
    {
        public int MortalDrawCooldownTimer = 0;
        public int MortalDrawAnimationTimer = 0;

        public bool IsShinobiActive => GetShinobiRole()?.Status == RoleStatus.Active && ModContent.GetInstance<StatariaConfig>().roleSettings.EnableRoleSystem;

        private Role GetShinobiRole()
        {
            var rpg = Player.GetModPlayer<RPGPlayer>();
            return rpg.AvailableRoles.TryGetValue("Shinobi", out Role role) ? role : null;
        }

        public override void SaveData(TagCompound tag)
        {
            tag["MortalDrawCooldownTimer"] = MortalDrawCooldownTimer;
        }

        public override void LoadData(TagCompound tag)
        {
            MortalDrawCooldownTimer = tag.ContainsKey("MortalDrawCooldownTimer") ? tag.GetInt("MortalDrawCooldownTimer") : 0;
        }

        public override void Initialize()
        {
            MortalDrawCooldownTimer = 0;
            MortalDrawAnimationTimer = 0;
        }

        public override void ResetEffects()
        {
            if (!IsShinobiActive)
            {
                MortalDrawAnimationTimer = 0;
                return;
            }
        }

        public override void PreUpdate()
        {
            if (MortalDrawCooldownTimer > 0)
            {
                MortalDrawCooldownTimer--;
            }

            if (MortalDrawAnimationTimer > 0)
            {
                MortalDrawAnimationTimer--;

                // Disable player controls during animation
                Player.controlLeft = false;
                Player.controlRight = false;
                Player.controlUp = false;
                Player.controlDown = false;
                Player.controlJump = false;
                Player.controlUseItem = false;

                // Grant invincibility frames during swing animation
                Player.immune = true;
                Player.immuneTime = Math.Max(Player.immuneTime, 2);

                // Stop horizontal movement
                Player.velocity.X = 0f;

                if (MortalDrawAnimationTimer == 24) // 2/3 of 36 frames
                {
                    PerformMortalDrawSlash();
                }

                if (MortalDrawAnimationTimer == 0)
                {
                    // Grant 1 second (60 frames) post-skill immunity when swing is done
                    Player.immune = true;
                    Player.immuneTime = Math.Max(Player.immuneTime, 60);
                }
            }
        }

        public override bool CanUseItem(Item item)
        {
            if (MortalDrawAnimationTimer > 0)
            {
                return false;
            }
            return base.CanUseItem(item);
        }

        public override void PostUpdate()
        {
            if (MortalDrawAnimationTimer > 0)
            {
                // Composite arm swing animation math
                int total = 36;
                int strikeStart = 24;
                int windupDuration = total - strikeStart;

                float angle = 0f;
                float swordAngle = 0f;

                if (MortalDrawAnimationTimer > strikeStart)
                {
                    // Wind-up phase (12 frames)
                    float progress = (total - MortalDrawAnimationTimer) / (float)Math.Max(1, windupDuration);
                    angle = -0.5f - progress * 0.5f; // sweeps arm backwards/upwards
                    swordAngle = 0.4f - progress * 0.8f; // angles sword backwards
                }
                else
                {
                    // Strike & follow-through phase (24 frames)
                    float progress = (strikeStart - MortalDrawAnimationTimer) / (float)Math.Max(1, strikeStart);
                    angle = -1.0f + progress * 1.5f; // sweeps arm down and forward
                    swordAngle = -0.4f + progress * 1.8f; // sweeps blade in wide cutting arc
                }

                float armRotation = (-MathHelper.ToRadians(90f) - angle) * Player.direction;
                Player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.ThreeQuarters, armRotation);
                Player.itemLocation = Player.GetFrontHandPosition(Player.CompositeArmStretchAmount.ThreeQuarters, armRotation);
                Player.itemRotation = -swordAngle * Player.direction;

                // Shift the handle offset slightly to align with the palm
                Player.itemLocation += new Vector2(-2f * Player.direction, 2f);
            }
        }

        public override void ModifyDrawInfo(ref PlayerDrawSet drawInfo)
        {
            if (IsShinobiActive && MortalDrawAnimationTimer > 0)
            {
                if (SekirariaSupportHelper.HasParrySword(Player, out Item parrySwordItem))
                {
                    drawInfo.heldItem = parrySwordItem;
                }
            }
        }

        public void ActivateMortalDraw()
        {
            var config = ModContent.GetInstance<StatariaConfig>();
            MortalDrawCooldownTimer = (int)(config.roleSettings.ShinobiMortalDrawCooldown * 60f);
            MortalDrawAnimationTimer = 36;

            if (Main.netMode != NetmodeID.Server)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item19, Player.position); // Sword swing whoosh
                CombatText.NewText(Player.Hitbox, Color.Purple, "Mortal Draw!", true);
            }

            if (Main.netMode != NetmodeID.SinglePlayer)
            {
                SyncShinobiState();
            }
        }

        private void PerformMortalDrawSlash()
        {
            var config = ModContent.GetInstance<StatariaConfig>();

            if (SekirariaSupportHelper.SekirariaLoaded)
            {
                // Scan all posture-broken enemies in range
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC npc = Main.npc[i];
                    if (npc.active && !npc.friendly && (npc.damage > 0 || SekirariaSupportHelper.IsPostureBroken(npc)))
                    {
                        float dist = Vector2.Distance(Player.Center, npc.Center);
                        if (dist <= config.roleSettings.ShinobiMortalDrawRange && SekirariaSupportHelper.IsPostureBroken(npc))
                        {
                            // Perform execution
                            SekirariaSupportHelper.PerformExecutionOnNPC(Player, npc);

                            // Healing per execution
                            int healAmount = (int)(Player.statLifeMax2 * config.roleSettings.ShinobiExecutionHealPercent / 100f);
                            healAmount = Math.Max(1, healAmount);
                            Player.statLife += healAmount;
                            if (Player.statLife > Player.statLifeMax2)
                                Player.statLife = Player.statLifeMax2;

                            if (healAmount > 0 && Main.netMode != NetmodeID.Server)
                            {
                                Player.HealEffect(healAmount, true);
                            }
                        }
                    }
                }
            }

            if (Main.netMode != NetmodeID.Server)
            {
                // Play slice sound
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item71, Player.position);
                
                // Visual screen flash/slash (fixed visual size centered on player)
                SpawnMortalDrawScreenVisuals();
            }
        }

        private void SpawnMortalDrawScreenVisuals()
        {
            // Center crossing slashes around player (set size covering the immediate screen area)
            for (int i = -80; i <= 80; i++)
            {
                Vector2 offset = new Vector2(i * 12f, i * 6f);
                int d = Dust.NewDust(Player.Center + offset - new Vector2(4, 4), 8, 8, DustID.PurpleTorch, 0f, 0f, 100, default, 2f);
                if (d >= 0 && d < Main.maxDust)
                {
                    Main.dust[d].noGravity = true;
                    Main.dust[d].velocity = Main.rand.NextVector2Circular(1f, 1f);
                }
            }
            for (int i = -80; i <= 80; i++)
            {
                Vector2 offset = new Vector2(i * 12f, -i * 6f);
                int d = Dust.NewDust(Player.Center + offset - new Vector2(4, 4), 8, 8, DustID.PurpleTorch, 0f, 0f, 100, default, 2f);
                if (d >= 0 && d < Main.maxDust)
                {
                    Main.dust[d].noGravity = true;
                    Main.dust[d].velocity = Main.rand.NextVector2Circular(1f, 1f);
                }
            }

            // A burst of sparks at the center
            for (int i = 0; i < 35; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(8f, 8f);
                int d = Dust.NewDust(Player.Center, 8, 8, DustID.PurpleTorch, vel.X, vel.Y, 100, default, 1.8f);
                if (d >= 0 && d < Main.maxDust)
                {
                    Main.dust[d].noGravity = true;
                }
            }
        }

        public void SyncShinobiState(int toWho = -1, int fromWho = -1)
        {
            if (Main.netMode == NetmodeID.SinglePlayer)
                return;
            var packet = ModContent.GetInstance<Stataria>().GetPacket();
            packet.Write((byte)StatariaMessageType.SyncShinobiState);
            packet.Write(Player.whoAmI);
            packet.Write(MortalDrawCooldownTimer);
            packet.Write(MortalDrawAnimationTimer);
            packet.Send(toWho, fromWho);
        }

        public override void SyncPlayer(int toWho, int fromWho, bool newPlayer)
        {
            SyncShinobiState(toWho, fromWho);
        }
    }
}
