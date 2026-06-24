using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Stataria.Projectiles;

namespace Stataria
{
    public class LivingFleshPlayer : ModPlayer
    {
        public int RallyableHealth = 0;
        public int RallyTimer = 0;
        public int CloneCooldownTimer = 0;

        /// <summary>
        /// Set to true only during FleshCloneProjectile.PreDraw to suppress draw layers
        /// (like the rally HUD) from running while the clone sprite is being rendered.
        /// </summary>
        public bool IsDrawingClone = false;
        
        private int passiveRegenTickTimer = 0;

        public bool IsLivingFleshActive => GetLivingFleshRole()?.Status == RoleStatus.Active && ModContent.GetInstance<StatariaConfig>().roleSettings.EnableRoleSystem;

        private Role GetLivingFleshRole()
        {
            var rpg = Player.GetModPlayer<RPGPlayer>();
            return rpg.AvailableRoles.TryGetValue("LivingFlesh", out Role role) ? role : null;
        }

        public override void ResetEffects()
        {
            if (!IsLivingFleshActive)
            {
                RallyableHealth = 0;
                RallyTimer = 0;
                return;
            }

            var config = ModContent.GetInstance<StatariaConfig>();

            // 1. Armor-to-HP and Armor-to-DR trade-off
            float baseHP = Player.statLifeMax2;
            float threshold = baseHP * config.roleSettings.LivingFleshDefenseToHPRatio;

            float scale = 0f;
            if (threshold > 0f)
            {
                scale = Math.Clamp(1f - (Player.statDefense / threshold), 0f, 1f);
            }

            // Apply Max HP Bonus
            int hpBonus = (int)(baseHP * config.roleSettings.LivingFleshMaxHPBonus * scale);
            Player.statLifeMax2 += hpBonus;

            // Apply DR (goes directly to player.endurance)
            Player.endurance += config.roleSettings.LivingFleshMaxDR * scale;

            // 2. HP-dependent damage scaling
            float hpRatio = (float)Player.statLife / Player.statLifeMax2;
            hpRatio = Math.Clamp(hpRatio, 0f, 1f);

            float minDmg = config.roleSettings.LivingFleshMinDamageMultiplier;
            float maxDmg = config.roleSettings.LivingFleshMaxDamageMultiplier;
            float currentDmgMult = minDmg + (maxDmg - minDmg) * hpRatio;

            // Scale all weapon damage
            Player.GetDamage(DamageClass.Generic) *= currentDmgMult;
        }

        public override void PreUpdate()
        {
            if (!IsLivingFleshActive)
                return;

            var config = ModContent.GetInstance<StatariaConfig>();

            // Decrement clone cooldown
            if (CloneCooldownTimer > 0)
                CloneCooldownTimer--;

            // Decrement rally timer
            if (RallyTimer > 0)
            {
                RallyTimer--;
                if (RallyTimer <= 0)
                {
                    RallyableHealth = 0;
                }
            }

            // 3. Constant always-active non-interrupted passive regeneration
            passiveRegenTickTimer++;
            int passiveInterval = (int)(config.roleSettings.LivingFleshPassiveRegenInterval * 60f);
            if (passiveRegenTickTimer >= passiveInterval)
            {
                passiveRegenTickTimer = 0;
                
                // Only run on the local player client (or in singleplayer) to prevent predicting/applying heals for others
                if (Player.whoAmI == Main.myPlayer)
                {
                    int heal = (int)(Player.statLifeMax2 * (config.roleSettings.LivingFleshPassiveRegenPercent / 100f));
                    
                    if (heal > 0 && Player.statLife < Player.statLifeMax2 && !Player.dead)
                    {
                        Player.statLife += heal;
                        if (Player.statLife > Player.statLifeMax2)
                            Player.statLife = Player.statLifeMax2;
                            
                        if (Main.netMode == NetmodeID.MultiplayerClient)
                        {
                            NetMessage.SendData(MessageID.PlayerLifeMana, -1, -1, null, Player.whoAmI);
                        }
                    }
                }
            }
        }

        public override void UpdateLifeRegen()
        {
            if (!IsLivingFleshActive)
                return;

            if (Player.HasBuff(ModContent.BuffType<LivingFleshRegenBuff>()))
            {
                var config = ModContent.GetInstance<StatariaConfig>();
                var rpgPlayer = Player.GetModPlayer<RPGPlayer>();
                int effectiveVIT = rpgPlayer.GetEffectiveStat("VIT");

                // Calculate regen amount based on base and VIT scaling
                int regenAmount = (int)(config.roleSettings.LivingFleshKillRegenBase + (effectiveVIT * config.roleSettings.LivingFleshKillRegenVitScale));
                Player.lifeRegen += regenAmount;
            }
        }

        public override void ModifyHurt(ref Player.HurtModifiers modifiers)
        {
            if (!IsLivingFleshActive)
                return;

            var config = ModContent.GetInstance<StatariaConfig>();

            // Store part of incoming damage as rallyable health
            modifiers.ModifyHurtInfo += (ref Player.HurtInfo info) =>
            {
                int dmgTaken = info.Damage;
                if (dmgTaken > 0)
                {
                    int storedRally = (int)(dmgTaken * (config.roleSettings.LivingFleshRallyStorePercent / 100f));
                    RallyableHealth = Math.Min(Player.statLifeMax2 - Player.statLife, RallyableHealth + storedRally);
                    RallyTimer = (int)(config.roleSettings.LivingFleshRallyDuration * 60f);
                }
            };
        }

        public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (IsLivingFleshActive)
            {
                TryRallyHeal(damageDone, target);
            }
        }

        public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (IsLivingFleshActive && proj.owner == Player.whoAmI)
            {
                TryRallyHeal(damageDone, target);
            }
        }

        private void TryRallyHeal(int damageDone, NPC target)
        {
            if (target.type == NPCID.TargetDummy)
                return;

            if (RallyTimer > 0 && RallyableHealth > 0)
            {
                var config = ModContent.GetInstance<StatariaConfig>();
                int heal = (int)(damageDone * (config.roleSettings.LivingFleshRallyHealPercent / 100f));
                heal = Math.Min(RallyableHealth, heal);
                heal = Math.Max(1, heal);

                Player.statLife += heal;
                if (Player.statLife > Player.statLifeMax2)
                    Player.statLife = Player.statLifeMax2;

                RallyableHealth -= heal;

                if (Main.netMode != NetmodeID.Server && heal > 0)
                {
                    Player.HealEffect(heal, true);
                    
                    // Blood dust particles flowing back from target to player
                    for (int i = 0; i < 5; i++)
                    {
                        Vector2 vel = Main.rand.NextVector2Circular(3f, 3f);
                        Dust d = Dust.NewDustPerfect(target.Center, DustID.Blood, vel, 0, default, 1.2f);
                        d.noGravity = true;
                    }
                }

                if (Main.netMode == NetmodeID.MultiplayerClient && Player.whoAmI == Main.myPlayer)
                {
                    NetMessage.SendData(MessageID.PlayerLifeMana, -1, -1, null, Player.whoAmI);
                }
            }
        }

        public void ActivateFleshClone()
        {
            var config = ModContent.GetInstance<StatariaConfig>();

            float sacrificePercent = config.roleSettings.LivingFleshCloneHPCost / 100f;
            int hpSacrificed = (int)(Player.statLife * sacrificePercent);
            
            // Ensure player stays alive with at least 1 HP
            hpSacrificed = Math.Min(hpSacrificed, Player.statLife - 1);

            if (hpSacrificed <= 0)
                return;

            Player.statLife -= hpSacrificed;

            if (Main.netMode == NetmodeID.MultiplayerClient && Player.whoAmI == Main.myPlayer)
            {
                NetMessage.SendData(MessageID.PlayerLifeMana, -1, -1, null, Player.whoAmI);
            }

            if (Main.netMode != NetmodeID.Server)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCDeath11, Player.position);
                for (int i = 0; i < 20; i++)
                {
                    Dust d = Dust.NewDustPerfect(Player.Center, DustID.Blood, Main.rand.NextVector2Circular(6f, 6f), 0, default, 1.6f);
                    d.noGravity = true;
                }
            }

            if (Player.whoAmI == Main.myPlayer)
            {
                int projType = ModContent.ProjectileType<FleshCloneProjectile>();
                Projectile.NewProjectile(
                    Player.GetSource_FromThis(),
                    Player.Center,
                    Vector2.Zero,
                    projType,
                    0,
                    0f,
                    Player.whoAmI,
                    hpSacrificed,        // ai[0] = current HP
                    hpSacrificed,        // ai[1] = max HP
                    Player.direction     // ai[2] = spawn-facing direction
                );

                CloneCooldownTimer = (int)(config.roleSettings.LivingFleshCloneCooldown * 60f);
            }
        }
    }
}
